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
        private SolidColorBrush _performanceColor = new SolidColorBrush(Colors.Green);
        private readonly MainViewModel _mainViewModel;
        private bool _isInitialized = false;

        // Speichert die tatsächlichen Portfolio-Werte und Investitionen
        private readonly Dictionary<DateTime, (decimal Value, decimal Investment, decimal Performance)> _portfolioData =
            new Dictionary<DateTime, (decimal Value, decimal Investment, decimal Performance)>();

        // Anfangsinvestment und aktuelles Investment
        private decimal _initialInvestment = 0;
        private decimal _currentInvestment = 0;

        // Einstandspreise und Anzahl der Aktien im Portfolio
        private Dictionary<string, (decimal EinstandsPreis, int Anzahl)> _portfolioAktien =
            new Dictionary<string, (decimal EinstandsPreis, int Anzahl)>();
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

        public SolidColorBrush PerformanceColor
        {
            get => _performanceColor;
            set => SetProperty(ref _performanceColor, value);
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
        /// Setzt alle Daten zurück und erstellt eine Kurve mit deutlich sichtbaren negativen Werten
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

                // Nur diese drei wichtigen Punkte setzen:
                // 1. Startdatum (vor 7 Tagen): 0% Performance
                DateTime startDate = DateTime.Now.AddDays(-7);
                _portfolioData[startDate] = (_initialInvestment, _initialInvestment, 0m);

                // 2. Mittendrin (vor 3 Tagen): Hälfte der aktuellen Performance
                DateTime midDate = DateTime.Now.AddDays(-3);
                decimal midPerformance = performance / 2;
                decimal midValue = _initialInvestment * (1 + midPerformance / 100);
                _portfolioData[midDate] = (midValue, _initialInvestment, midPerformance);

                // 3. Jetzt: Aktuelle Performance
                _portfolioData[DateTime.Now] = (aktuellerWert, _initialInvestment, performance);

                _currentInvestment = aktuellerWert;
                _isInitialized = true;

                Debug.WriteLine($"Portfolio-Historie zurückgesetzt mit spezifischen Werten:");
                Debug.WriteLine($"Anfangsinvestment: {_initialInvestment:F2}€");
                Debug.WriteLine($"Aktueller Wert: {aktuellerWert:F2}€");
                Debug.WriteLine($"Performance: {performance:F2}%");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Setzen der extremen Performance: {ex.Message}");
            }
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
                DateTime startDate = DateTime.Now.Subtract(timeSpan);
                if (timeSpan == TimeSpan.MaxValue)
                {
                    // Bei "Max" alle Datenpunkte verwenden
                    startDate = _portfolioData.Keys.Min();
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
                            LineSmoothness = 0,
                            Stroke = new SolidColorBrush(Colors.Green),
                            Fill = new SolidColorBrush(Color.FromArgb(32, 0, 204, 0))
                        }
                    };

                    Labels = new List<string> { DateTime.Now.AddDays(-1).ToString("dd.MM."), DateTime.Now.ToString("dd.MM.") };
                    YMinValue = -1;
                    YMaxValue = 1;
                    PerformanceText = "0,00 %";
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

                // Explizites Padding für bessere Lesbarkeit
                YMinValue = lowestValue * 1.5m; // Mehr Platz nach unten
                YMaxValue = Math.Max(0.5m, highestValue * 1.1m); // Etwas Platz nach oben

                // Aktuelle Performance für die Anzeige
                decimal currentPerformance = performances.Count > 0 ? performances.Last() : 0;

                // Performance-Anzeige aktualisieren
                PerformanceText = $"{(currentPerformance >= 0 ? "+" : "")}{currentPerformance:F2} %";
                PerformanceColor = new SolidColorBrush(currentPerformance >= 0 ? Colors.Green : Colors.Red);

                // Chart-Serie erstellen
                SeriesCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Portfolio-Performance",
                        Values = new ChartValues<decimal>(performances),
                        PointGeometry = null,
                        LineSmoothness = 0, // Kein Smoothing für präzisere Darstellung
                        StrokeThickness = 2,
                        Stroke = currentPerformance >= 0 ?
                            new SolidColorBrush(Colors.Green) :
                            new SolidColorBrush(Colors.Red),
                        Fill = currentPerformance >= 0 ?
                            new SolidColorBrush(Color.FromArgb(32, 0, 204, 0)) :
                            new SolidColorBrush(Color.FromArgb(32, 204, 0, 0))
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
                        LineSmoothness = 0,
                        Stroke = new SolidColorBrush(Colors.Red),
                        Fill = new SolidColorBrush(Color.FromArgb(32, 204, 0, 0))
                    }
                };

                Labels = new List<string> { "Start", "Mitte", "Ende" };
                YMinValue = -2; // Negativer Bereich größer als die Werte
                YMaxValue = 0.5m;
                PerformanceText = "Fehler";
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