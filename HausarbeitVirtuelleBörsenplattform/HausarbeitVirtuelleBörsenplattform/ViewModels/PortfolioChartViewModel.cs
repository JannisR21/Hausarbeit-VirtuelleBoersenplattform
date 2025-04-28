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
        private decimal _yMinValue;
        private decimal _yMaxValue;
        private string _performanceText = "+0,00 %";
        private SolidColorBrush _performanceColor = new SolidColorBrush(Colors.Green);
        private readonly MainViewModel _mainViewModel;

        // Speichert die tatsächlichen Portfolio-Werte und Investitionen
        private readonly Dictionary<DateTime, (decimal Value, decimal Investment)> _portfolioData =
            new Dictionary<DateTime, (decimal Value, decimal Investment)>();

        // Investitionsverläufe (zur Berücksichtigung bei der Performance-Berechnung)
        private decimal _totalInvestment = 0;
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

            // Euro-Formatierung für die Y-Achse
            YFormatter = value => value.ToString("N2") + " %";

            // Timer für das periodische Update, aber mit seltenem Intervall
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1) // Aktualisierung nur jede Minute
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
                        UpdatePortfolioValue(true); // Direkte Aktualisierung bei Portfolio-Änderungen
                    }
                };
            }

            // Initialen Portfolio-Wert erfassen
            UpdatePortfolioValue(false);

            // Initialen Chart erstellen
            UpdateChartData(TimeSpan.FromDays(7));

            Debug.WriteLine("PortfolioChartViewModel wurde initialisiert");
        }
        #endregion

        #region Methoden
        /// <summary>
        /// Aktualisiert die Chart-Daten basierend auf dem gewählten Zeitraum
        /// </summary>
        private void UpdateChartData(TimeSpan timeSpan)
        {
            try
            {
                if (_portfolioData.Count == 0)
                {
                    Debug.WriteLine("Keine Portfolio-Werte vorhanden, Chart kann nicht aktualisiert werden");

                    // Löschen des vorhandenen Charts, um leeren Zustand anzuzeigen
                    SeriesCollection = new SeriesCollection();
                    Labels = new List<string>();
                    return;
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

                if (relevantPoints.Count == 0)
                {
                    Debug.WriteLine("Keine Datenpunkte im gewählten Zeitraum gefunden");

                    // Leeren Chart anzeigen
                    SeriesCollection = new SeriesCollection();
                    Labels = new List<string>();
                    return;
                }

                // Labels und Werte für den Chart vorbereiten
                List<string> labels = new List<string>();
                List<decimal> performances = new List<decimal>();

                // Maximal 30 Punkte anzeigen, um Übersichtlichkeit zu wahren
                int skipFactor = Math.Max(1, relevantPoints.Count / 30);

                // Berechne die Performance relativ zum Ausgangspunkt
                var firstPoint = relevantPoints.First();
                decimal initialInvestment = firstPoint.Value.Investment;
                decimal initialValue = firstPoint.Value.Value;

                for (int i = 0; i < relevantPoints.Count; i += skipFactor)
                {
                    var point = relevantPoints[i];
                    var (value, investment) = point.Value;

                    // Format für die Zeitpunkt-Anzeige wählen
                    string format;
                    if (timeSpan.TotalDays <= 1) format = "HH:mm";
                    else if (timeSpan.TotalDays <= 7) format = "dd.MM.";
                    else if (timeSpan.TotalDays <= 30) format = "dd.MM.";
                    else format = "MMM yy";

                    labels.Add(point.Key.ToString(format));

                    // Berechne Performance als Prozentsatz zwischen Wert und Investment
                    decimal performancePercent;
                    if (initialInvestment == investment)
                    {
                        // Wenn sich das Investment nicht geändert hat, einfache relative Performance
                        performancePercent = initialValue > 0 ?
                            (value / initialValue - 1) * 100 : 0;
                    }
                    else
                    {
                        // Bei Änderungen im Investment (Käufe/Verkäufe) komplexere Berechnung
                        // Hier verwenden wir eine angepasste time-weighted return Berechnung
                        decimal adjustedStartValue = initialValue;
                        decimal adjustedCurrentValue = value;

                        // Bei Investment-Änderungen seit Beginn
                        if (investment > initialInvestment)
                        {
                            // Bei Zukäufen: Anfangswert entsprechend anpassen
                            adjustedStartValue = initialValue + (investment - initialInvestment);
                        }
                        else if (investment < initialInvestment)
                        {
                            // Bei Verkäufen: Aktuellen Wert entsprechend anpassen
                            adjustedCurrentValue = value + (initialInvestment - investment);
                        }

                        // Bereinigte Performance berechnen
                        performancePercent = adjustedStartValue > 0 ?
                            (adjustedCurrentValue / adjustedStartValue - 1) * 100 : 0;
                    }

                    performances.Add(performancePercent);
                }

                // Immer den letzten Punkt hinzufügen (aktueller Wert)
                if (relevantPoints.Count > 0 && performances.Count > 0 &&
                    !performances.Contains(CalculatePerformance(relevantPoints.Last().Value, firstPoint.Value)))
                {
                    var lastPoint = relevantPoints.Last();

                    string format = timeSpan.TotalDays <= 1 ? "HH:mm" : "dd.MM.";
                    labels.Add(lastPoint.Key.ToString(format));

                    // Letzte Performance
                    decimal lastPerformance = CalculatePerformance(lastPoint.Value, firstPoint.Value);
                    performances.Add(lastPerformance);
                }

                // Y-Achsen-Grenzen mit Polster berechnen
                decimal minPerformance = performances.Count > 0 ? performances.Min() : -1;
                decimal maxPerformance = performances.Count > 0 ? performances.Max() : 1;
                decimal padding = Math.Max(1, (maxPerformance - minPerformance) * 0.1m); // 10% Polster, mind. ±1%

                YMinValue = Math.Min(-1, minPerformance - padding);
                YMaxValue = Math.Max(1, maxPerformance + padding);

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
                        LineSmoothness = 0.5,
                        StrokeThickness = 2,
                        Stroke = currentPerformance >= 0 ?
                            new SolidColorBrush(Colors.Green) :
                            new SolidColorBrush(Colors.Red),
                        Fill = new SolidColorBrush(Color.FromArgb(32, 0, 204, 0))
                    }
                };

                // Labels setzen
                Labels = labels;

                Debug.WriteLine($"Performance-Chart für Zeitraum {timeSpan.TotalDays} Tage aktualisiert mit {performances.Count} Datenpunkten");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der Chart-Daten: {ex.Message}");
            }
        }

        /// <summary>
        /// Berechnet die Performance zwischen zwei Portfolio-Zuständen
        /// </summary>
        private decimal CalculatePerformance((decimal Value, decimal Investment) current, (decimal Value, decimal Investment) initial)
        {
            decimal initialInvestment = initial.Investment;
            decimal initialValue = initial.Value;
            decimal currentValue = current.Value;
            decimal currentInvestment = current.Investment;

            // Bei gleichem Investment: einfache relative Performance
            if (initialInvestment == currentInvestment)
            {
                return initialValue > 0 ? (currentValue / initialValue - 1) * 100 : 0;
            }

            // Bei Änderungen im Investment: bereinigte Berechnung
            decimal adjustedStartValue = initialValue;
            decimal adjustedCurrentValue = currentValue;

            if (currentInvestment > initialInvestment)
            {
                // Bei Zukäufen: Anfangswert entsprechend anpassen
                adjustedStartValue = initialValue + (currentInvestment - initialInvestment);
            }
            else if (currentInvestment < initialInvestment)
            {
                // Bei Verkäufen: Aktuellen Wert entsprechend anpassen
                adjustedCurrentValue = currentValue + (initialInvestment - currentInvestment);
            }

            // Bereinigte Performance berechnen
            return adjustedStartValue > 0 ? (adjustedCurrentValue / adjustedStartValue - 1) * 100 : 0;
        }

        /// <summary>
        /// Aktualisiert den aktuellen Portfoliowert und berücksichtigt Investitionsänderungen
        /// </summary>
        /// <param name="forceUpdate">Erzwingt eine sofortige Chart-Aktualisierung</param>
        private void UpdatePortfolioValue(bool forceUpdate = false)
        {
            try
            {
                // Aktuellen Gesamtwert aus dem PortfolioViewModel holen
                decimal portfolioValue = _mainViewModel?.PortfolioViewModel?.Gesamtwert ?? 0;

                // Aktuelle Investments aufsummieren (Einstandswerte)
                decimal currentInvestment = 0;
                if (_mainViewModel?.PortfolioViewModel?.PortfolioEintraege != null)
                {
                    foreach (var position in _mainViewModel.PortfolioViewModel.PortfolioEintraege)
                    {
                        currentInvestment += position.Anzahl * position.EinstandsPreis;
                    }
                }

                // Aktuellen Zeitpunkt verwenden
                DateTime now = DateTime.Now;

                // Eintrag speichern oder aktualisieren
                if (_portfolioData.ContainsKey(now))
                {
                    var (oldValue, oldInvestment) = _portfolioData[now];

                    // Nur bei signifikanter Änderung aktualisieren
                    if (Math.Abs(oldValue - portfolioValue) > 0.1m ||
                        Math.Abs(oldInvestment - currentInvestment) > 0.1m)
                    {
                        _portfolioData[now] = (portfolioValue, currentInvestment);
                        Debug.WriteLine($"Portfolio-Wert für {now} aktualisiert: Wert={portfolioValue:F2}€, Investment={currentInvestment:F2}€");
                    }
                }
                else
                {
                    // Neuen Eintrag erstellen
                    _portfolioData[now] = (portfolioValue, currentInvestment);
                    Debug.WriteLine($"Neuer Portfolio-Eintrag für {now}: Wert={portfolioValue:F2}€, Investment={currentInvestment:F2}€");
                }

                // Wenn Investment-Änderung erkannt wurde oder erzwungenes Update
                bool investmentChanged = Math.Abs(_totalInvestment - currentInvestment) > 0.1m;
                _totalInvestment = currentInvestment;

                if (investmentChanged || forceUpdate)
                {
                    // Chart aktualisieren, um die Performance korrekt darzustellen
                    if (IsOneDay) UpdateChartData(TimeSpan.FromDays(1));
                    else if (IsOneWeek) UpdateChartData(TimeSpan.FromDays(7));
                    else if (IsOneMonth) UpdateChartData(TimeSpan.FromDays(30));
                    else if (IsOneYear) UpdateChartData(TimeSpan.FromDays(365));
                    else if (IsMax) UpdateChartData(TimeSpan.MaxValue);
                    else UpdateChartData(TimeSpan.FromDays(7)); // Standardfall
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