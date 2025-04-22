using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Models;
using HausarbeitVirtuelleBörsenplattform.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;



namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    /// <summary>
    /// ViewModel für den Marktdaten-Bereich der Anwendung
    /// </summary>
    public class MarktdatenViewModel : ObservableObject
    {
        #region Private Felder

        private ObservableCollection<Aktie> _aktienListe;
        private Aktie _ausgewählteAktie;
        private MainViewModel _mainViewModel;
        private readonly TwelveDataService _twelveDatenService;
        private DispatcherTimer _updateTimer;
        private bool _isUpdating;
        private string _statusText;
        private string _fehlerText;
        private bool _hatFehler;
        private DateTime _letzteAktualisierung;
        private bool _isLoading;
        private TimeSpan _aktualisierungsIntervall = TimeSpan.FromMinutes(2); // 2 Minuten Intervall

        // Kultur für korrekte Formatierung
        private CultureInfo _germanCulture = new CultureInfo("de-DE");
        private string apiKey;

        #endregion

        #region Public Properties

        /// <summary>
        /// Liste aller verfügbaren Aktien mit aktuellen Kursinformationen
        /// </summary>
        public ObservableCollection<Aktie> AktienListe
        {
            get => _aktienListe;
            set => SetProperty(ref _aktienListe, value);
        }

        /// <summary>
        /// Aktuell ausgewählte Aktie für Detailansicht
        /// </summary>
        public Aktie AusgewählteAktie
        {
            get => _ausgewählteAktie;
            set => SetProperty(ref _ausgewählteAktie, value);
        }

        /// <summary>
        /// Statustext für die Aktualisierung
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>
        /// Fehlertext für die Anzeige von API-Fehlern
        /// </summary>
        public string FehlerText
        {
            get => _fehlerText;
            set => SetProperty(ref _fehlerText, value);
        }

        /// <summary>
        /// Gibt an, ob ein Fehler aufgetreten ist
        /// </summary>
        public bool HatFehler
        {
            get => _hatFehler;
            set => SetProperty(ref _hatFehler, value);
        }

        /// <summary>
        /// Zeitpunkt der letzten Aktualisierung
        /// </summary>
        public DateTime LetzteAktualisierung
        {
            get => _letzteAktualisierung;
            set => SetProperty(ref _letzteAktualisierung, value);
        }

        /// <summary>
        /// Gibt an, ob gerade Daten geladen werden
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Nächste geplante Aktualisierung
        /// </summary>
        public DateTime NächsteAktualisierung { get; private set; }

        /// <summary>
        /// Command zum manuellen Aktualisieren der Marktdaten
        /// </summary>
        public IRelayCommand AktualisierenCommand { get; }

        #endregion

        #region Konstruktor

        /// <summary>
        /// Initialisiert eine neue Instanz des MarktdatenViewModel
        /// </summary>
        /// <param name="mainViewModel">Hauptinstanz des MainViewModel</param>
        /// <param name="apiKey">API-Schlüssel für Twelve Data</param>
        public MarktdatenViewModel(MainViewModel mainViewModel, string apiKey = "cb617aba18ea46b3a974d878d3c7310b")
        {
            _mainViewModel = mainViewModel;
            Debug.WriteLine($"MarktdatenViewModel wird initialisiert mit API-Key: {apiKey}");

            _twelveDatenService = new TwelveDataService(apiKey);

            // Kommandos initialisieren
            AktualisierenCommand = new RelayCommand(async () => await AktualisiereMarktdaten(), () => !IsLoading);

            // Collection initialisieren
            AktienListe = new ObservableCollection<Aktie>();

            // Beispieldaten für den Start laden
            InitializeMarktdaten();

            // Timer für regelmäßige Aktualisierungen
            StartUpdateTimer();
        }

        public MarktdatenViewModel(string apiKey)
        {
            this.apiKey = apiKey;
        }

        #endregion

        #region Private Methoden

        /// <summary>
        /// Initialisiert Beispieldaten für die Aktienliste
        /// </summary>
        private void InitializeMarktdaten()
        {
            // Standard-Aktien, die wir überwachen wollen
            var standardAktien = new ObservableCollection<Aktie>
            {
                new Aktie
                {
                    AktienID = 1,
                    AktienSymbol = "AAPL",
                    AktienName = "Apple Inc.",
                    AktuellerPreis = 150.00m,
                    Änderung = 1.25m,
                    ÄnderungProzent = 0.84m,
                    LetzteAktualisierung = DateTime.Now
                },
                new Aktie
                {
                    AktienID = 2,
                    AktienSymbol = "TSLA",
                    AktienName = "Tesla Inc.",
                    AktuellerPreis = 200.20m,
                    Änderung = -0.70m,
                    ÄnderungProzent = -0.35m,
                    LetzteAktualisierung = DateTime.Now
                },
                new Aktie
                {
                    AktienID = 3,
                    AktienSymbol = "AMZN",
                    AktienName = "Amazon.com Inc.",
                    AktuellerPreis = 95.10m,
                    Änderung = 0.72m,
                    ÄnderungProzent = 0.76m,
                    LetzteAktualisierung = DateTime.Now
                },
                new Aktie
                {
                    AktienID = 4,
                    AktienSymbol = "MSFT",
                    AktienName = "Microsoft Corp.",
                    AktuellerPreis = 320.45m,
                    Änderung = 4.75m,
                    ÄnderungProzent = 1.50m,
                    LetzteAktualisierung = DateTime.Now
                },
                new Aktie
                {
                    AktienID = 5,
                    AktienSymbol = "GOOGL",
                    AktienName = "Alphabet Inc.",
                    AktuellerPreis = 128.75m,
                    Änderung = -0.28m,
                    ÄnderungProzent = -0.22m,
                    LetzteAktualisierung = DateTime.Now
                }
            };

            AktienListe = standardAktien;
            LetzteAktualisierung = DateTime.Now;
            NächsteAktualisierung = DateTime.Now.Add(_aktualisierungsIntervall);
            StatusText = "Initiale Daten geladen";

            // Live-Daten sofort laden
            _ = AktualisiereMarktdaten();
        }

        /// <summary>
        /// Startet einen Timer für die regelmäßige Aktualisierung der Marktdaten
        /// </summary>
        private void StartUpdateTimer()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = _aktualisierungsIntervall // Update alle 2 Minuten
            };
            _updateTimer.Tick += async (s, e) => await AktualisiereMarktdaten();
            _updateTimer.Start();

            Debug.WriteLine($"Update-Timer gestartet mit Intervall: {_aktualisierungsIntervall.TotalMinutes} Minuten");
        }

        /// <summary>
        /// Aktualisiert die Marktdaten über die Twelve Data API
        /// </summary>
        private async Task AktualisiereMarktdaten()
        {
            if (_isUpdating) return;

            _isUpdating = true;
            IsLoading = true;
            StatusText = "Daten werden aktualisiert...";
            HatFehler = false;
            FehlerText = "";

            Debug.WriteLine("Starte Aktualisierung der Marktdaten...");

            try
            {
                // Alle Aktien-Symbole aus der aktuellen Liste extrahieren
                var symbole = AktienListe.Select(a => a.AktienSymbol).ToList();
                Debug.WriteLine($"Aktualisiere Symbole: {string.Join(", ", symbole)}");

                // Aktiendaten von der API abrufen
                var aktienDaten = await _twelveDatenService.HoleAktienKurse(symbole);

                // Prüfen, ob ein API-Fehler aufgetreten ist
                if (!string.IsNullOrEmpty(_twelveDatenService.LastErrorMessage))
                {
                    Debug.WriteLine($"API-Fehler erkannt: {_twelveDatenService.LastErrorMessage}");
                    HatFehler = true;
                    FehlerText = _twelveDatenService.LastErrorMessage;

                    // Bei API-Limit-Fehler, verlängere das Intervall
                    if (_twelveDatenService.LastErrorMessage.Contains("API credits") &&
                        _twelveDatenService.LastErrorMessage.Contains("limit"))
                    {
                        // Intervall auf 3 Minuten erhöhen
                        _aktualisierungsIntervall = TimeSpan.FromMinutes(3);
                        _updateTimer.Interval = _aktualisierungsIntervall;
                        Debug.WriteLine($"API-Limit erreicht. Intervall auf {_aktualisierungsIntervall.TotalMinutes} Minuten erhöht.");
                    }
                }

                // UI-Thread-Zugriff sicherstellen
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Aktualisiere die bestehenden Aktien mit den neuen Daten
                    foreach (var aktienInfo in aktienDaten)
                    {
                        var existingAktie = AktienListe.FirstOrDefault(a => a.AktienSymbol == aktienInfo.AktienSymbol);

                        if (existingAktie != null)
                        {
                            existingAktie.AktuellerPreis = aktienInfo.AktuellerPreis;
                            existingAktie.Änderung = aktienInfo.Änderung;
                            existingAktie.ÄnderungProzent = aktienInfo.ÄnderungProzent;
                            existingAktie.AktienName = aktienInfo.AktienName; // Übernehme auch den Namen (wichtig für Fehlermeldungen)
                            existingAktie.LetzteAktualisierung = DateTime.Now;
                        }
                    }

                    LetzteAktualisierung = DateTime.Now;
                    NächsteAktualisierung = DateTime.Now.Add(_aktualisierungsIntervall);

                    if (HatFehler)
                    {
                        StatusText = $"Fehler bei der Aktualisierung um {LetzteAktualisierung.ToString("HH:mm:ss", _germanCulture)}";
                    }
                    else
                    {
                        StatusText = $"Daten aktualisiert um {LetzteAktualisierung.ToString("HH:mm:ss", _germanCulture)}";
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unbehandelte Ausnahme: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                HatFehler = true;
                FehlerText = $"Unbehandelte Ausnahme: {ex.Message}";
                StatusText = $"Fehler bei der Aktualisierung: {ex.Message}";
            }
            finally
            {
                _isUpdating = false;
                IsLoading = false;
                Debug.WriteLine("Aktualisierung der Marktdaten abgeschlossen.");
            }
        }

        #endregion
    }
}