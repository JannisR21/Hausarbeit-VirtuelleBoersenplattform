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
        private readonly Random _random = new Random();
        private readonly List<decimal> _portfolioHistoryValues = new List<decimal>();
        private MainViewModel _mainViewModel;
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
            YFormatter = value => value.ToString("N2") + " €";

            // Initialisierung der Chartwerte
            GenerateInitialPortfolioHistory();
            UpdateChartData(TimeSpan.FromDays(7)); // 1 Woche als Standard

            // Timer für das periodische Update
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10) // Aktualisierung alle 10 Sekunden
            };
            _updateTimer.Tick += (s, e) => UpdatePortfolioValue();
            _updateTimer.Start();

            // Event-Handler für PropertyChanged an MainViewModel anhängen
            if (_mainViewModel?.PortfolioViewModel != null)
            {
                _mainViewModel.PortfolioViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(_mainViewModel.PortfolioViewModel.Gesamtwert) ||
                        e.PropertyName == nameof(_mainViewModel.PortfolioViewModel.GesamtGewinnVerlust))
                    {
                        UpdatePortfolioValue();
                    }
                };
            }

            Debug.WriteLine("PortfolioChartViewModel wurde initialisiert");
        }
        #endregion

        #region Methoden
        /// <summary>
        /// Erzeugt anfängliche Verlaufsdaten für das Portfolio
        /// </summary>
        private void GenerateInitialPortfolioHistory()
        {
            _portfolioHistoryValues.Clear();

            // Startkapital 10.000€
            decimal baseValue = 10000m;

            // Wir erzeugen Daten für 365 Tage
            for (int i = 0; i < 365; i++)
            {
                // Tägliche Änderung zwischen -2% und +2%
                decimal dailyChange = (decimal)(_random.NextDouble() * 4 - 2) / 100;
                baseValue = baseValue * (1 + dailyChange);
                _portfolioHistoryValues.Add(baseValue);
            }

            Debug.WriteLine($"Anfängliche Portfoliodaten mit {_portfolioHistoryValues.Count} Einträgen erzeugt");
        }

        /// <summary>
        /// Aktualisiert die Chart-Daten basierend auf dem gewählten Zeitraum
        /// </summary>
        private void UpdateChartData(TimeSpan timeSpan)
        {
            try
            {
                // Anzahl der anzuzeigenden Tage bestimmen
                int daysToShow;
                int labelInterval;

                if (timeSpan == TimeSpan.MaxValue)
                {
                    daysToShow = _portfolioHistoryValues.Count;
                    labelInterval = daysToShow / 10; // 10 Labels für den gesamten Zeitraum
                }
                else
                {
                    daysToShow = (int)Math.Min(timeSpan.TotalDays, _portfolioHistoryValues.Count);

                    // Label-Intervall je nach Zeitraum anpassen
                    if (daysToShow <= 1) labelInterval = 1; // Stunden für 1 Tag
                    else if (daysToShow <= 7) labelInterval = 1; // Täglich für 1 Woche
                    else if (daysToShow <= 30) labelInterval = 2; // Alle 2 Tage für 1 Monat
                    else if (daysToShow <= 365) labelInterval = 30; // Monatlich für 1 Jahr
                    else labelInterval = 90; // Alle 3 Monate für Maximum
                }

                // Werte für den angezeigten Zeitraum auswählen
                var values = _portfolioHistoryValues
                    .Skip(Math.Max(0, _portfolioHistoryValues.Count - daysToShow))
                    .ToList();

                // Labels generieren
                var dateLabels = new List<string>();
                DateTime endDate = DateTime.Now;

                for (int i = 0; i < daysToShow; i++)
                {
                    if (i % labelInterval == 0 || i == daysToShow - 1)
                    {
                        DateTime labelDate = endDate.AddDays(-(daysToShow - 1 - i));

                        // Format je nach Zeitraum anpassen
                        string labelFormat;
                        if (daysToShow <= 1) labelFormat = "HH:mm";
                        else if (daysToShow <= 30) labelFormat = "dd.MM.";
                        else labelFormat = "MMM yy";

                        dateLabels.Add(labelDate.ToString(labelFormat));
                    }
                    else
                    {
                        dateLabels.Add("");
                    }
                }

                // Y-Achsen-Grenzen festlegen
                var minValue = values.Min();
                var maxValue = values.Max();
                var padding = (maxValue - minValue) * 0.1m; // 10% Padding

                YMinValue = Math.Max(0, minValue - padding); // Minimum nicht unter 0
                YMaxValue = maxValue + padding;

                // Performance berechnen
                decimal performance = 0;
                if (values.Count > 1)
                {
                    decimal initialValue = values.First();
                    decimal currentValue = values.Last();
                    performance = initialValue > 0 ? (currentValue / initialValue - 1) * 100 : 0;
                }

                // Performance-Text und -Farbe aktualisieren
                PerformanceText = $"{(performance >= 0 ? "+" : "")}{performance:F2} %";
                PerformanceColor = new SolidColorBrush(performance >= 0 ? Colors.Green : Colors.Red);

                // Chartserie erstellen
                SeriesCollection = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Portfolio",
                        Values = new ChartValues<decimal>(values),
                        PointGeometry = null,
                        LineSmoothness = 0.5,
                        StrokeThickness = 2,
                        Stroke = performance >= 0 ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red),
                        Fill = new SolidColorBrush(Color.FromArgb(32, 0, 204, 0))
                    }
                };

                // Labels übernehmen
                Labels = dateLabels;

                Debug.WriteLine($"Chart für Zeitraum {timeSpan.TotalDays} Tage aktualisiert");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der Chart-Daten: {ex.Message}");
            }
        }

        /// <summary>
        /// Aktualisiert den aktuellen Portfoliowert
        /// </summary>
        private void UpdatePortfolioValue()
        {
            try
            {
                // Aktuellen Portfoliowert abrufen, falls MainViewModel verfügbar
                decimal currentValue;
                if (_mainViewModel?.PortfolioViewModel != null)
                {
                    currentValue = _mainViewModel.PortfolioViewModel.Gesamtwert;

                    // Wenn der Wert 0 ist und wir haben historische Daten, verwenden wir den letzten Wert
                    if (currentValue == 0 && _portfolioHistoryValues.Count > 0)
                    {
                        decimal lastValue = _portfolioHistoryValues.Last();
                        decimal dailyChange = (decimal)(_random.NextDouble() * 1 - 0.5) / 100; // ±0.5%
                        currentValue = lastValue * (1 + dailyChange);
                    }
                }
                else
                {
                    // Sonst simulieren wir eine kleine Änderung im letzten Wert
                    decimal lastValue = _portfolioHistoryValues.Count > 0 ? _portfolioHistoryValues.Last() : 10000m;
                    decimal dailyChange = (decimal)(_random.NextDouble() * 1 - 0.5) / 100; // ±0.5%
                    currentValue = lastValue * (1 + dailyChange);
                }

                // Vermeiden wir Duplikate, indem wir nur hinzufügen, wenn sich der Wert ändert
                if (_portfolioHistoryValues.Count == 0 || _portfolioHistoryValues.Last() != currentValue)
                {
                    // Neuen Wert zum Verlauf hinzufügen
                    _portfolioHistoryValues.Add(currentValue);

                    // Bei zu vielen Datenpunkten ältere entfernen
                    if (_portfolioHistoryValues.Count > 1000)
                    {
                        _portfolioHistoryValues.RemoveAt(0);
                    }

                    // Aktualisierten Zeitraum anzeigen
                    if (IsOneDay) UpdateChartData(TimeSpan.FromDays(1));
                    else if (IsOneWeek) UpdateChartData(TimeSpan.FromDays(7));
                    else if (IsOneMonth) UpdateChartData(TimeSpan.FromDays(30));
                    else if (IsOneYear) UpdateChartData(TimeSpan.FromDays(365));
                    else if (IsMax) UpdateChartData(TimeSpan.MaxValue);

                    Debug.WriteLine($"Portfoliowert aktualisiert auf {currentValue:F2}€");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren des Portfoliowerts: {ex.Message}");
            }
        }
        #endregion
    }
}