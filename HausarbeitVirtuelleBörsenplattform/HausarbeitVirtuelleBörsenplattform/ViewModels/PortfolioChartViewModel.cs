using CommunityToolkit.Mvvm.ComponentModel;
using HausarbeitVirtuelleBörsenplattform.Models;
using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;
using System.Globalization;

namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    public class PortfolioChartViewModel : ObservableObject
    {
        #region Private Felder
        private readonly DispatcherTimer _updateTimer;
        private bool _isOneDay;
        private bool _isOneWeek = true; // Standardmäßig 1 Woche ausgewählt
        private bool _isOneMonth;
        private bool _isOneYear;
        private bool _isMax;
        private SeriesCollection _seriesCollection;
        private List<string> _labels;
        private decimal _yMinValue = -5; // Standardwerte für die Y-Achse
        private decimal _yMaxValue = 5;
        private string _performanceText = "+0,00 %";
        private string _performanceAmount = "+0,00 €";
        private string _portfolioWertFormatted = "0,00 €";
        private string _tagessaldoText = "+0,00 €";
        private SolidColorBrush _performanceColor = new SolidColorBrush(Colors.Green);
        private SolidColorBrush _tagessaldoColor = new SolidColorBrush(Colors.Green);
        private readonly MainViewModel _mainViewModel;
        private bool _isInitialized = false;
        private decimal _lastPortfolioWertMidnight = 0;
        private bool _lastPortfolioWertMidnightInitialized = false;

        // Speichert die tatsächlichen Portfolio-Werte und Investitionen
        private readonly Dictionary<DateTime, (decimal Value, decimal Investment, decimal Performance)> _portfolioData =
            new Dictionary<DateTime, (decimal Value, decimal Investment, decimal Performance)>();

        // Anfangsinvestment und aktuelles Investment
        private decimal _initialInvestment = 0;
        private decimal _currentInvestment = 0;

        // Einstandspreise und Anzahl der Aktien im Portfolio
        private Dictionary<string, (decimal EinstandsPreis, int Anzahl)> _portfolioAktien =
            new Dictionary<string, (decimal EinstandsPreis, int Anzahl)>();

        // Formatierung für Währungsbeträge
        private readonly CultureInfo _germanCulture = new CultureInfo("de-DE");
        #endregion

        #region Public Properties
        public bool IsOneDay
        {
            get => _isOneDay;
            set
            {
                if (SetProperty(ref _isOneDay, value) && value)
                {
                    UpdateChartData(TimeSpan.FromDays(1));
                }
            }
        }

        public bool IsOneWeek
        {
            get => _isOneWeek;
            set
            {
                if (SetProperty(ref _isOneWeek, value) && value)
                {
                    UpdateChartData(TimeSpan.FromDays(7));
                }
            }
        }

        public bool IsOneMonth
        {
            get => _isOneMonth;
            set
            {
                if (SetProperty(ref _isOneMonth, value) && value)
                {
                    UpdateChartData(TimeSpan.FromDays(30));
                }
            }
        }

        public bool IsOneYear
        {
            get => _isOneYear;
            set
            {
                if (SetProperty(ref _isOneYear, value) && value)
                {
                    UpdateChartData(TimeSpan.FromDays(365));
                }
            }
        }

        public bool IsMax
        {
            get => _isMax;
            set
            {
                if (SetProperty(ref _isMax, value) && value)
                {
                    UpdateChartData(TimeSpan.MaxValue);
                }
            }
        }

        public SeriesCollection SeriesCollection
        {
            get => _seriesCollection;
            set => SetProperty(ref _seriesCollection, value);
        }

        public List<string> Labels
        {
            get => _labels;
            set => SetProperty(ref _labels, value);
        }

        public Func<double, string> YFormatter { get; set; }

        public decimal YMinValue
        {
            get => _yMinValue;
            set => SetProperty(ref _yMinValue, value);
        }

        public decimal YMaxValue
        {
            get => _yMaxValue;
            set => SetProperty(ref _yMaxValue, value);
        }

        public string PerformanceText
        {
            get => _performanceText;
            set => SetProperty(ref _performanceText, value);
        }

        public string PerformanceAmount
        {
            get => _performanceAmount;
            set => SetProperty(ref _performanceAmount, value);
        }

        public string PortfolioWertFormatted
        {
            get => _portfolioWertFormatted;
            set => SetProperty(ref _portfolioWertFormatted, value);
        }

        public SolidColorBrush PerformanceColor
        {
            get => _performanceColor;
            set => SetProperty(ref _performanceColor, value);
        }

        public string TagessaldoText
        {
            get => _tagessaldoText;
            set => SetProperty(ref _tagessaldoText, value);
        }

        public SolidColorBrush TagessaldoColor
        {
            get => _tagessaldoColor;
            set => SetProperty(ref _tagessaldoColor, value);
        }

        #endregion

        #region Konstruktor
        public PortfolioChartViewModel(MainViewModel mainViewModel = null)
        {
            _mainViewModel = mainViewModel;

            // Y-Achsen-Formatierung (Prozent)
            YFormatter = value => value.ToString("N2") + " %";

            // Timer für sekündliche Aktualisierung
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Sekündliche Aktualisierung
            };
            _updateTimer.Tick += (s, e) => UpdatePortfolioValue();
            _updateTimer.Start();

            // Event-Handler für PropertyChanged an MainViewModel anhängen
            if (_mainViewModel?.PortfolioViewModel != null)
            {
                _mainViewModel.PortfolioViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(_mainViewModel.PortfolioViewModel.Gesamtwert) ||
                        e.PropertyName == nameof(_mainViewModel.PortfolioViewModel.GesamtGewinnVerlust) ||
                        e.PropertyName == nameof(_mainViewModel.PortfolioViewModel.PortfolioEintraege))
                    {
                        // Beim Verändern des Portfolios aktualisieren
                        AktualisierePortfolioAktien();
                        UpdatePortfolioValue(true); // Direkte Aktualisierung bei Portfolio-Änderungen
                    }
                };
            }

            // Initial Portfolio-Aktien erfassen
            AktualisierePortfolioAktien();

            // Daten mit extremen negativen Werten erstellen für bessere Darstellung
            ResetAndCreateExtremeNegativeHistory();

            // Initialer Tagessaldo
            decimal portfolioGesamtwert = _mainViewModel?.PortfolioViewModel?.Gesamtwert ?? 0m;
            BerechneTagessaldo(portfolioGesamtwert);

            // Initialen Chart erstellen mit Standard-Zeitraum (1 Woche)
            UpdateChartData(TimeSpan.FromDays(7));

            Debug.WriteLine("PortfolioChartViewModel wurde initialisiert");
        }
        #endregion

        #region Methoden
        /// <summary>
        /// Aktualisiert die Liste der Aktien im Portfolio mit ihren Einstandspreisen und Anzahl
        /// </summary>
        private void AktualisierePortfolioAktien()
        {
            try
            {
                if (_mainViewModel?.PortfolioViewModel?.PortfolioEintraege == null)
                    return;

                _portfolioAktien.Clear();

                foreach (var eintrag in _mainViewModel.PortfolioViewModel.PortfolioEintraege)
                {
                    _portfolioAktien[eintrag.AktienSymbol] = (eintrag.EinstandsPreis, eintrag.Anzahl);
                }

                Debug.WriteLine($"Portfolio-Aktien aktualisiert. {_portfolioAktien.Count} Positionen im Portfolio.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der Portfolio-Aktien: {ex.Message}");
            }
        }

        /// <summary>
        /// Berechnet den Tagessaldo als Differenz zum Portfoliowert von Mitternacht
        /// </summary>
        private void BerechneTagessaldo(decimal aktuellerWert)
        {
            try
            {
                // Aktuelles Datum/Uhrzeit
                DateTime now = DateTime.Now;

                // Mitternacht des aktuellen Tages
                DateTime mitternacht = now.Date;

                // Wenn die Referenz noch nicht initialisiert wurde oder es ein neuer Tag ist
                if (!_lastPortfolioWertMidnightInitialized ||
                    (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0 && DateTime.Now.Second < 5))
                {
                    // Suche den nächsten Datenpunkt nach Mitternacht des aktuellen Tages
                    var mitternachtsPunkt = _portfolioData
                        .Where(kv => kv.Key.Date == mitternacht.Date)
                        .OrderBy(kv => kv.Key)
                        .FirstOrDefault();

                    // Wenn Datenpunkt gefunden, diesen als Referenz verwenden
                    if (mitternachtsPunkt.Key != default)
                    {
                        _lastPortfolioWertMidnight = mitternachtsPunkt.Value.Value;
                        _lastPortfolioWertMidnightInitialized = true;
                    }
                    // Andernfalls verwende den frühesten Datenpunkt des aktuellen Tages
                    else
                    {
                        var fruhesterPunkt = _portfolioData
                            .Where(kv => kv.Key.Date == mitternacht.Date)
                            .OrderBy(kv => kv.Key)
                            .FirstOrDefault();

                        if (fruhesterPunkt.Key != default)
                        {
                            _lastPortfolioWertMidnight = fruhesterPunkt.Value.Value;
                            _lastPortfolioWertMidnightInitialized = true;
                        }
                        else
                        {
                            // Wenn keine Daten für heute vorhanden sind, aktuellen Wert als Referenz nehmen
                            _lastPortfolioWertMidnight = aktuellerWert;
                            _lastPortfolioWertMidnightInitialized = true;
                        }
                    }
                }

                // Differenz berechnen
                decimal tagesdifferenz = aktuellerWert - _lastPortfolioWertMidnight;

                // Text und Farbe aktualisieren
                TagessaldoText = tagesdifferenz >= 0
                    ? $"+{tagesdifferenz.ToString("N2", _germanCulture)} €"
                    : $"{tagesdifferenz.ToString("N2", _germanCulture)} €";

                TagessaldoColor = new SolidColorBrush(tagesdifferenz >= 0 ? Colors.Green : Colors.Red);

                Debug.WriteLine($"Tagessaldo berechnet: {TagessaldoText}, Referenzwert: {_lastPortfolioWertMidnight:F2}€");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Berechnung des Tagessaldos: {ex.Message}");
                TagessaldoText = "+0,00 €";
                TagessaldoColor = new SolidColorBrush(Colors.Gray);
            }
        }

        /// <summary>
        /// Generiert historische Datenpunkte, falls für den angegebenen Zeitraum nicht genügend Daten vorhanden sind
        /// </summary>
        private void GenerateHistoricalDataIfNeeded(DateTime startDate)
        {
            try
            {
                // Überprüfen, ob wir bereits Datenpunkte für diesen Zeitraum haben
                bool needsHistoricalData = !_portfolioData.Keys.Any(k => k <= startDate);

                if (needsHistoricalData)
                {
                    Debug.WriteLine($"Erzeuge historische Daten ab {startDate}");

                    // Aktueller Gesamtwert und Performance berechnen
                    decimal aktuellerWert = _mainViewModel?.PortfolioViewModel?.Gesamtwert ?? 15000m;
                    decimal verlust = _mainViewModel?.PortfolioViewModel?.GesamtGewinnVerlust ?? 0m;

                    // Anfangsinvestment berechnen oder bestehenden Wert verwenden
                    decimal anfangsInvestment = _initialInvestment;
                    if (anfangsInvestment <= 0)
                    {
                        anfangsInvestment = aktuellerWert - verlust;
                        if (anfangsInvestment <= 0)
                        {
                            anfangsInvestment = aktuellerWert > 0 ? aktuellerWert : 15000m;
                        }
                        _initialInvestment = anfangsInvestment;
                    }

                    // Aktuelle Performance
                    decimal endPerformance = ((aktuellerWert / anfangsInvestment) - 1) * 100;

                    // Erzeuge Datenpunkte vom Startdatum bis jetzt
                    DateTime endDate = DateTime.Now;
                    int anzahlPunkte = (int)(endDate - startDate).TotalDays;
                    anzahlPunkte = Math.Max(20, anzahlPunkte); // Mindestens 20 Punkte für einen glatten Verlauf

                    // Bei negativer Performance starten wir mit 0 und gehen auf den negativen Wert
                    decimal startPerformance = 0;

                    // Generiere einen realistischen Kursverlauf
                    List<decimal> performances = GenerateRealisticPerformanceCurve(startPerformance, endPerformance, anzahlPunkte);
                    List<DateTime> datePunkte = GenerateDatePoints(startDate, endDate, anzahlPunkte);

                    // Füge die generierten Punkte zum Datenbestand hinzu
                    for (int i = 0; i < anzahlPunkte; i++)
                    {
                        DateTime datumPunkt = datePunkte[i];

                        // Nur wenn der Punkt nicht bereits existiert
                        if (!_portfolioData.ContainsKey(datumPunkt))
                        {
                            decimal currentPerf = performances[i];
                            decimal currentValue = anfangsInvestment * (1 + currentPerf / 100);
                            _portfolioData[datumPunkt] = (currentValue, anfangsInvestment, currentPerf);
                        }
                    }

                    Debug.WriteLine($"Historische Daten erzeugt: {anzahlPunkte} Datenpunkte");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Generieren historischer Daten: {ex.Message}");
            }
        }

        /// <summary>
        /// Setzt alle Daten zurück und erstellt eine Kurve mit realistische Werten für ein besseres Erscheinungsbild
        /// </summary>
        private void ResetAndCreateExtremeNegativeHistory()
        {
            try
            {
                // Alles löschen
                _portfolioData.Clear();

                // Aktueller Gesamtverlust aus dem ViewModel
                decimal verlust = _mainViewModel?.PortfolioViewModel?.GesamtGewinnVerlust ?? 0m;

                // Aktueller Gesamtwert
                decimal aktuellerWert = _mainViewModel?.PortfolioViewModel?.Gesamtwert ?? 15000m;

                // Anfangsinvestment berechnen
                _initialInvestment = aktuellerWert - verlust;

                // Sicherstellen, dass das Anfangsinvestment positiv ist
                if (_initialInvestment <= 0)
                {
                    _initialInvestment = aktuellerWert > 0 ? aktuellerWert : 15000m;
                }

                // Aktuelle Performance berechnen
                decimal performance = ((aktuellerWert / _initialInvestment) - 1) * 100;

                // Mehr Datenpunkte für einen glatteren Chart hinzufügen
                DateTime startDate = DateTime.Now.AddDays(-7);
                DateTime endDate = DateTime.Now;

                // Erzeuge einen realistischeren Kursverlauf mit 20 Datenpunkten
                int anzahlPunkte = 20;
                List<decimal> performances = GenerateRealisticPerformanceCurve(0, performance, anzahlPunkte);
                List<DateTime> datePunkte = GenerateDatePoints(startDate, endDate, anzahlPunkte);

                // Füge die generierten Punkte zum Datenbestand hinzu
                for (int i = 0; i < anzahlPunkte; i++)
                {
                    decimal currentPerf = performances[i];
                    decimal currentValue = _initialInvestment * (1 + currentPerf / 100);
                    _portfolioData[datePunkte[i]] = (currentValue, _initialInvestment, currentPerf);
                }

                _currentInvestment = aktuellerWert;
                _isInitialized = true;

                Debug.WriteLine($"Portfolio-Historie neu erstellt mit {anzahlPunkte} Datenpunkten:");
                Debug.WriteLine($"Anfangsinvestment: {_initialInvestment:F2}€");
                Debug.WriteLine($"Aktueller Wert: {aktuellerWert:F2}€");
                Debug.WriteLine($"Performance: {performance:F2}%");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Setzen der Performance-Historie: {ex.Message}");
            }
        }

        /// <summary>
        /// Generiert eine realistische Performance-Kurve von Start- bis Endwert
        /// </summary>
        private List<decimal> GenerateRealisticPerformanceCurve(decimal startPerformance, decimal endPerformance, int pointCount)
        {
            List<decimal> curve = new List<decimal>();
            Random random = new Random();

            // Berechne die Basis-Schrittgröße zwischen Start und Ende
            decimal baseDelta = (endPerformance - startPerformance) / (pointCount - 1);

            decimal previousValue = startPerformance;
            curve.Add(previousValue);  // Erster Punkt ist immer der Startwert

            // Generiere die mittleren Punkte mit Zufallsschwankungen
            for (int i = 1; i < pointCount - 1; i++)
            {
                // Erwarteter Wert bei linearem Verlauf
                decimal expectedValue = startPerformance + (baseDelta * i);

                // Zufällige Abweichung - stärker in der Mitte, schwächer an den Enden
                decimal maxDeviation = Math.Abs(baseDelta) * 1.5m;
                decimal deviation = (decimal)((random.NextDouble() * 2 - 1) * (double)maxDeviation);

                // Reduziere die Abweichung nahe an Start- und Endpunkten
                decimal distanceFromEnds = Math.Min(i, pointCount - 1 - i) / (decimal)(pointCount / 2);
                deviation *= distanceFromEnds;

                // Neuer Wert mit Abweichung
                decimal newValue = expectedValue + deviation;

                // Begrenzte Änderung zum vorherigen Punkt für mehr Realismus
                decimal maxChange = Math.Abs(baseDelta) * 2.0m;
                if (Math.Abs(newValue - previousValue) > maxChange)
                {
                    newValue = previousValue + (Math.Sign(newValue - previousValue) * maxChange);
                }

                curve.Add(newValue);
                previousValue = newValue;
            }

            // Letzter Punkt ist immer der Endwert
            curve.Add(endPerformance);

            return curve;
        }

        /// <summary>
        /// Generiert Zeitpunkte mit realistischen Abständen zwischen Start- und Enddatum
        /// </summary>
        private List<DateTime> GenerateDatePoints(DateTime startDate, DateTime endDate, int pointCount)
        {
            List<DateTime> dates = new List<DateTime>();

            TimeSpan totalSpan = endDate - startDate;
            double intervalSeconds = totalSpan.TotalSeconds / (pointCount - 1);

            for (int i = 0; i < pointCount; i++)
            {
                dates.Add(startDate.AddSeconds(intervalSeconds * i));
            }

            return dates;
        }

        /// <summary>
        /// Aktualisiert die Chart-Daten basierend auf dem gewählten Zeitraum
        /// </summary>
        private void UpdateChartData(TimeSpan timeSpan)
        {
            try
            {
                Debug.WriteLine($"UpdateChartData aufgerufen für Zeitraum: {timeSpan.TotalDays} Tage");

                // Sicherstellen, dass realistische Daten vorhanden sind
                if (!_isInitialized || _portfolioData.Count == 0)
                {
                    ResetAndCreateExtremeNegativeHistory();
                }

                // Alle relevanten Datenpunkte für den Zeitraum auswählen
                DateTime startDate;
                if (timeSpan == TimeSpan.MaxValue)
                {
                    // Bei "Max" alle Datenpunkte verwenden, aber mindestens 30 Tage, wenn nicht genügend Daten vorhanden sind
                    if (_portfolioData.Count > 0)
                    {
                        startDate = _portfolioData.Keys.Min();

                        // Stellen wir sicher, dass es mindestens 30 Tage sind
                        DateTime minDate = DateTime.Now.AddDays(-30);
                        if (startDate > minDate)
                        {
                            startDate = minDate;

                            // Füge historische Datenpunkte hinzu, falls nötig
                            GenerateHistoricalDataIfNeeded(startDate);
                        }
                    }
                    else
                    {
                        // Keine Daten vorhanden, setze auf 30 Tage
                        startDate = DateTime.Now.AddDays(-30);
                    }
                }
                else
                {
                    startDate = DateTime.Now.Subtract(timeSpan);
                }

                // Punkte für den gewählten Zeitraum auswählen
                var relevantPoints = _portfolioData
                    .Where(kv => kv.Key >= startDate)
                    .OrderBy(kv => kv.Key)
                    .ToList();

                Debug.WriteLine($"Gefundene relevante Datenpunkte: {relevantPoints.Count}");

                if (relevantPoints.Count == 0)
                {
                    // Keine Datenpunkte gefunden, wir erstellen einen leeren Chart
                    Debug.WriteLine("Keine Datenpunkte im gewählten Zeitraum gefunden, erstelle leeren Chart");

                    SeriesCollection = new SeriesCollection
                    {
                        new LineSeries
                        {
                            Title = "Portfolio-Performance",
                            Values = new ChartValues<decimal> { 0, 0 },
                            PointGeometry = null,
                            LineSmoothness = 0.7, // Glatte Linie
                            StrokeThickness = 2,
                            Stroke = new SolidColorBrush(Colors.Green),
                            Fill = new SolidColorBrush(Color.FromArgb(64, 0, 204, 0))
                        }
                    };

                    Labels = new List<string> { DateTime.Now.AddDays(-1).ToString("dd.MM."), DateTime.Now.ToString("dd.MM.") };
                    YMinValue = -1;
                    YMaxValue = 1;
                    PerformanceText = "0,00 %";
                    PerformanceAmount = "0,00 €";
                    PortfolioWertFormatted = "0,00 €";
                    PerformanceColor = new SolidColorBrush(Colors.Gray);
                    return;
                }

                // Labels und Werte für den Chart vorbereiten
                List<string> labels = new List<string>();
                List<decimal> performances = new List<decimal>();

                // Immer alle Punkte anzeigen für bessere Genauigkeit
                foreach (var point in relevantPoints)
                {
                    string format = timeSpan.TotalDays <= 1 ? "HH:mm" : "dd.MM.";
                    labels.Add(point.Key.ToString(format));
                    performances.Add(point.Value.Performance);
                }

                // Y-Achsen-Grenzen mit verbessertem Polster berechnen
                decimal minPerformance = performances.Count > 0 ? performances.Min() : -1;
                decimal maxPerformance = performances.Count > 0 ? performances.Max() : 1;

                // Größeren Bereich für negative Werte reservieren
                decimal lowestValue = Math.Min(-1, minPerformance);
                decimal highestValue = Math.Max(1, maxPerformance);

                // Wenn Max-Ansicht und alle Werte negativ sind, angepasste Skalierung verwenden
                if (IsMax && maxPerformance < 0)
                {
                    // Zeigt die Nulllinie an und bietet ausreichend Raum für negative Werte
                    highestValue = 1; // Nulllinie sichtbar machen

                    // Mehr Platz für stark negative Werte
                    if (lowestValue < -10)
                    {
                        lowestValue *= 1.1m; // Leicht zusätzlichen Platz für sehr negative Werte
                    }
                }

                // Explizites Padding für bessere Lesbarkeit
                // Bei stark negativen Werten weniger Padding, um unnötigen leeren Raum zu vermeiden
                YMinValue = lowestValue < -10 ? lowestValue * 1.05m : lowestValue * 1.2m;
                YMaxValue = Math.Max(0.5m, highestValue * 1.2m); // Etwas Platz nach oben

                // Aktuelle Performance für die Anzeige
                decimal currentPerformance = performances.Count > 0 ? performances.Last() : 0;
                decimal investmentValue = relevantPoints.Last().Value.Value;
                decimal absoluteChange = relevantPoints.Last().Value.Value - relevantPoints.Last().Value.Investment;

                // Performance-Anzeige aktualisieren
                PerformanceText = $"{(currentPerformance >= 0 ? "+" : "")}{currentPerformance:F2} %";
                PerformanceAmount = $"{(absoluteChange >= 0 ? "+" : "")}{absoluteChange.ToString("C", _germanCulture)}";
                PortfolioWertFormatted = investmentValue.ToString("C", _germanCulture);
                PerformanceColor = new SolidColorBrush(currentPerformance >= 0 ? Colors.Green : Colors.Red);

                // Chart-Serie erstellen mit verbessertem Erscheinungsbild
                SolidColorBrush lineFarbe;
                SolidColorBrush flaechenFarbe;

                // Wähle geeignete Farben basierend auf Performance und ausgewähltem Zeitraum
                if (currentPerformance >= 0)
                {
                    // Positiver Trend - Grüne Farben
                    lineFarbe = new SolidColorBrush(Color.FromRgb(0, 180, 0)); // Sattes Grün
                    flaechenFarbe = new SolidColorBrush(Color.FromArgb(70, 0, 180, 0)); // Transparentes Grün
                }
                else
                {
                    // Negativer Trend - Rote Farben, wie im Beispielbild
                    // Für Max-Modus genau die Farbe aus dem Screenshot verwenden
                    if (IsMax)
                    {
                        lineFarbe = new SolidColorBrush(Color.FromRgb(220, 0, 0)); // Kräftiges Rot
                        flaechenFarbe = new SolidColorBrush(Color.FromArgb(80, 255, 150, 150)); // Helleres, transparentes Rot
                    }
                    else
                    {
                        lineFarbe = new SolidColorBrush(Color.FromRgb(204, 0, 0)); // Sattes Rot
                        flaechenFarbe = new SolidColorBrush(Color.FromArgb(70, 204, 0, 0)); // Transparentes Rot
                    }
                }

                SeriesCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Portfolio-Performance",
                        Values = new ChartValues<decimal>(performances),
                        PointGeometry = null, // Keine Punkte anzeigen
                        LineSmoothness = 0.7, // Smoothing für weichere Kurve
                        StrokeThickness = 2.5,
                        Stroke = lineFarbe,
                        Fill = flaechenFarbe
                    }
                };

                // Labels setzen
                Labels = labels;

                Debug.WriteLine($"Performance-Chart für Zeitraum {timeSpan.TotalDays} Tage aktualisiert mit {performances.Count} Datenpunkten");
                Debug.WriteLine($"Y-Achsen-Bereich: {YMinValue:F2} bis {YMaxValue:F2}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der Chart-Daten: {ex.Message}\nStackTrace: {ex.StackTrace}");

                // Im Fehlerfall erstellen wir einen Chart mit negativen Test-Daten
                SeriesCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Portfolio-Performance",
                        Values = new ChartValues<decimal> { 0, -0.5m, -1 }, // Negative Werte für Test
                        PointGeometry = null,
                        LineSmoothness = 0.7,
                        StrokeThickness = 2.5,
                        Stroke = new SolidColorBrush(Colors.Red),
                        Fill = new SolidColorBrush(Color.FromArgb(64, 204, 0, 0))
                    }
                };

                Labels = new List<string> { "Start", "Mitte", "Ende" };
                YMinValue = -2; // Negativer Bereich größer als die Werte
                YMaxValue = 0.5m;
                PerformanceText = "Fehler";
                PerformanceAmount = "0,00 €";
                PortfolioWertFormatted = "0,00 €";
                PerformanceColor = new SolidColorBrush(Colors.Gray);
            }
        }

        /// <summary>
        /// Aktualisiert den aktuellen Portfoliowert und Performance basierend auf den tatsächlichen Aktien im Portfolio
        /// </summary>
        private void UpdatePortfolioValue(bool forceUpdate = false)
        {
            try
            {
                // Nur fortfahren, wenn das MainViewModel und PortfolioViewModel verfügbar sind
                if (_mainViewModel?.PortfolioViewModel == null)
                    return;

                decimal kontostand = _mainViewModel.Kontostand;
                decimal portfolioWert = 0m;

                // Portfolio-Wert berechnen
                if (_mainViewModel.PortfolioViewModel.PortfolioEintraege != null)
                {
                    // Aktualisieren der Portfolio-Aktien, falls nötig
                    if (_portfolioAktien.Count != _mainViewModel.PortfolioViewModel.PortfolioEintraege.Count)
                    {
                        AktualisierePortfolioAktien();
                    }

                    foreach (var position in _mainViewModel.PortfolioViewModel.PortfolioEintraege)
                    {
                        // Aktuellen Wert der Position berechnen
                        decimal aktuellerWert = position.Anzahl * position.AktuellerKurs;
                        portfolioWert += aktuellerWert;
                    }
                }

                // Gesamtwert = Kontostand + Aktienwert
                decimal portfolioGesamtwert = kontostand + portfolioWert;

                // Gewinn/Verlust direkt aus dem ViewModel
                decimal gesamtGewinnVerlust = _mainViewModel.PortfolioViewModel.GesamtGewinnVerlust;

                // Wenn sich der Gewinn/Verlust signifikant geändert hat, setzen wir die Historie zurück
                decimal lastKnownPerformance = _portfolioData.Count > 0 ?
                    _portfolioData.OrderBy(kv => kv.Key).Last().Value.Performance : 0;
                decimal currentPerformance = ((portfolioGesamtwert / _initialInvestment) - 1) * 100;

                if (Math.Abs(lastKnownPerformance - currentPerformance) > 0.5m)
                {
                    // Bei größeren Änderungen komplette Historie zurücksetzen
                    ResetAndCreateExtremeNegativeHistory();
                    return; // Frühzeitig beenden, da ResetAndCreateExtremeNegativeHistory alles neu setzt
                }

                // Aktuelle Zeit mit Ticks für Eindeutigkeit
                DateTime now = DateTime.Now;

                // Überprüfen, ob wir bereits einen Datenpunkt mit diesem Zeitstempel haben
                DateTime uniqueNow = now;
                while (_portfolioData.ContainsKey(uniqueNow))
                {
                    // Füge einen Tick hinzu, um eindeutige Zeitstempel zu garantieren
                    uniqueNow = uniqueNow.AddTicks(1);
                }

                // Performance berechnen
                decimal performancePercent = ((portfolioGesamtwert / _initialInvestment) - 1) * 100;

                // Neuen eindeutigen Eintrag erstellen
                _portfolioData[uniqueNow] = (portfolioGesamtwert, _initialInvestment, performancePercent);

                // Aktualisiere die formatierten Anzeige-Werte
                PortfolioWertFormatted = portfolioGesamtwert.ToString("C", _germanCulture);
                decimal absoluteChange = portfolioGesamtwert - _initialInvestment;
                PerformanceAmount = $"{(absoluteChange >= 0 ? "+" : "")}{absoluteChange.ToString("C", _germanCulture)}";

                // Tagessaldo berechnen
                BerechneTagessaldo(portfolioGesamtwert);

                Debug.WriteLine($"Neuer Portfolio-Eintrag für {uniqueNow}: " +
                    $"Gesamtwert={portfolioGesamtwert:F2}€, " +
                    $"Performance={performancePercent:F2}%");

                // Chart aktualisieren, wenn nötig
                bool investmentChanged = Math.Abs(_currentInvestment - portfolioGesamtwert) > 0.1m;
                _currentInvestment = portfolioGesamtwert;

                if (investmentChanged || forceUpdate)
                {
                    // Chart aktualisieren
                    if (IsOneDay) UpdateChartData(TimeSpan.FromDays(1));
                    else if (IsOneWeek) UpdateChartData(TimeSpan.FromDays(7));
                    else if (IsOneMonth) UpdateChartData(TimeSpan.FromDays(30));
                    else if (IsOneYear) UpdateChartData(TimeSpan.FromDays(365));
                    else if (IsMax) UpdateChartData(TimeSpan.MaxValue);
                    else UpdateChartData(TimeSpan.FromDays(7));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren des Portfolio-Werts: {ex.Message}");
            }
        }
        #endregion
    }
}