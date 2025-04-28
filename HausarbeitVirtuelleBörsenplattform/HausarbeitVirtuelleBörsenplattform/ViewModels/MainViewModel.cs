using CommunityToolkit.Mvvm.ComponentModel;
using HausarbeitVirtuelleBörsenplattform.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HausarbeitVirtuelleBörsenplattform.Services;

namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    /// <summary>
    /// Haupt-ViewModel für das MainWindow, das alle anderen ViewModels koordiniert
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        #region Private Felder

        private Benutzer _aktuellerBenutzer;
        private MarktStatus _marktStatus;
        private DispatcherTimer _portfolioUpdateTimer;
        private decimal _kontostand; // Neues Feld für den Kontostand
        private AktienFilterService _aktienFilterService;

        #endregion

        #region Public Properties

        /// <summary>
        /// Aktuell angemeldeter Benutzer
        /// </summary>
        public Benutzer AktuellerBenutzer
        {
            get => _aktuellerBenutzer;
            set
            {
                if (SetProperty(ref _aktuellerBenutzer, value))
                {
                    // Wenn sich der Benutzer ändert, aktualisiere auch den Kontostand
                    Kontostand = value?.Kontostand ?? 0;
                }
            }
        }

        /// <summary>
        /// Aktueller Kontostand des Benutzers - als separate Property
        /// </summary>
        public decimal Kontostand
        {
            get => _kontostand;
            set
            {
                // Nur aktualisieren, wenn sich der Wert geändert hat
                if (SetProperty(ref _kontostand, value))
                {
                    Debug.WriteLine($"Kontostand aktualisiert: {value:N2}€");

                    // Benutzer-Objekt ebenfalls aktualisieren
                    if (_aktuellerBenutzer != null)
                    {
                        _aktuellerBenutzer.Kontostand = value;
                    }

                    // Alle UI-Komponenten über die Änderung informieren
                    OnPropertyChanged(nameof(AktuellerBenutzer));
                }
            }
        }

        /// <summary>
        /// Aktueller Status des Börsenmarktes (offen/geschlossen)
        /// </summary>
        public MarktStatus MarktStatus
        {
            get => _marktStatus;
            set => SetProperty(ref _marktStatus, value);
        }

        /// <summary>
        /// ViewModel für den Portfolio-Bereich
        /// </summary>
        public PortfolioViewModel PortfolioViewModel { get; private set; }

        /// <summary>
        /// ViewModel für den Marktdaten-Bereich
        /// </summary>
        public MarktdatenViewModel MarktdatenViewModel { get; private set; }

        /// <summary>
        /// ViewModel für den Aktienhandel-Bereich
        /// </summary>
        public AktienhandelViewModel AktienhandelViewModel { get; private set; }

        /// <summary>
        /// ViewModel für den Portfolio-Chart
        /// </summary>
        public PortfolioChartViewModel PortfolioChartViewModel { get; private set; }

        /// <summary>
        /// ViewModel für den Aktienfilter
        /// </summary>
        public AktienFilterViewModel AktienFilterViewModel { get; private set; }

        /// <summary>
        /// Service für die Aktienfilterung
        /// </summary>
        public AktienFilterService AktienFilterService
        {
            get => _aktienFilterService;
            private set => SetProperty(ref _aktienFilterService, value);
        }

        // Alternativ-Property für HandelsViewModel
        public AktienhandelViewModel HandelsViewModel => AktienhandelViewModel;

        /// <summary>
        /// ViewModel für die Watchlist
        /// </summary>
        public WatchlistViewModel WatchlistViewModel { get; private set; }

        #endregion

        #region Konstruktor

        /// <summary>
        /// Initialisiert eine neue Instanz des MainViewModel
        /// </summary>
        public MainViewModel()
        {
            try
            {
                InitializeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FEHLER im MainViewModel Konstruktor: " + ex);
                MessageBox.Show("Fataler Fehler bei der Initialisierung:\n" + ex.Message);
            }
        }

        #endregion

        #region Asynchrone Initialisierung

        private async void InitializeAsync()
        {
            try
            {
                if (App.AuthService?.CurrentUser != null)
                {
                    AktuellerBenutzer = App.AuthService.CurrentUser;
                }
                else
                {
                    // Wenn kein angemeldeter Benutzer, einen Fehler anzeigen
                    throw new InvalidOperationException("Kein angemeldeter Benutzer gefunden.");
                }

                if (AktuellerBenutzer == null || AktuellerBenutzer.BenutzerID <= 0)
                {
                    throw new InvalidOperationException("Aktueller Benutzer ist ungültig.");
                }

                // Kontostand initialisieren
                Kontostand = AktuellerBenutzer.Kontostand;

                // Standardwerte initialisieren
                InitializeMarktStatus();

                // API-Key abrufen
                string apiKey = App.TwelveDataApiKey;

                // AktienFilterService aus App übernehmen
                AktienFilterService = App.AktienFilterService;

                // Explizite Initialisierung der ViewModels in der richtigen Reihenfolge
                // WICHTIG: AktienhandelViewModel sollte erst nach MarktdatenViewModel erstellt werden
                Debug.WriteLine("Initialisiere MarktdatenViewModel...");
                MarktdatenViewModel = new MarktdatenViewModel(this, apiKey);

                Debug.WriteLine("Initialisiere PortfolioViewModel...");
                PortfolioViewModel = new PortfolioViewModel(App.DbService, AktuellerBenutzer.BenutzerID);
                await PortfolioViewModel.LoadPortfolioDataAsync();

                Debug.WriteLine("Initialisiere AktienFilterViewModel...");
                AktienFilterViewModel = new AktienFilterViewModel();
                if (MarktdatenViewModel?.AktienListe != null)
                {
                    // Aktien-Liste an den Filter übergeben
                    AktienFilterViewModel.SetzeAktienListe(MarktdatenViewModel.AktienListe);
                }
                OnPropertyChanged(nameof(AktienFilterViewModel));

                Debug.WriteLine("Initialisiere AktienhandelViewModel...");
                AktienhandelViewModel = new AktienhandelViewModel(this);
                if (AktienhandelViewModel != null)
                {
                    // Explizit das MainViewModel setzen
                    AktienhandelViewModel.SetMainViewModel(this);
                }
                else
                {
                    Debug.WriteLine("FEHLER: AktienhandelViewModel konnte nicht erstellt werden!");
                }

                // Neues PortfolioChartViewModel initialisieren
                Debug.WriteLine("Initialisiere PortfolioChartViewModel...");
                PortfolioChartViewModel = new PortfolioChartViewModel(this);

                // Timer starten
                StartPortfolioUpdateTimer();

                Debug.WriteLine("Alle ViewModels erfolgreich initialisiert");

                // Explizit eine PropertyChanged-Benachrichtigung für AktienhandelViewModel auslösen
                OnPropertyChanged(nameof(AktienhandelViewModel));
                // Und für das neue PortfolioChartViewModel
                OnPropertyChanged(nameof(PortfolioChartViewModel));

                Debug.WriteLine("Initialisiere WatchlistViewModel...");
                WatchlistViewModel = new WatchlistViewModel(App.DbService, AktuellerBenutzer.BenutzerID, this);
                OnPropertyChanged(nameof(WatchlistViewModel));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Initialisierung: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show("Fehler beim Initialisieren der Hauptansicht:\n" + ex.Message, "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Initialisiert den Marktstatus
        /// </summary>
        private void InitializeMarktStatus()
        {
            MarktStatus = new MarktStatus
            {
                IsOpen = true,
                StatusText = "Markt geöffnet",
                StatusColor = "#2ecc71",
                LetzteAktualisierung = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            };
        }

        #endregion

        #region Portfolio-Update-Timer

        /// <summary>
        /// Startet einen Timer, um das Portfolio regelmäßig mit aktuellen Marktdaten zu aktualisieren
        /// </summary>
        private void StartPortfolioUpdateTimer()
        {
            _portfolioUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30) // Von 10 auf 30 Sekunden erhöht
            };

            _portfolioUpdateTimer.Tick += (s, e) => UpdatePortfolioWithMarketData();
            _portfolioUpdateTimer.Start();

            // Sofort einmal aktualisieren, aber nur wenn die Börse geöffnet ist
            bool istBoerseGeoeffnet = App.TwelveDataService?.IstBoerseGeoeffnet() ?? false;
            if (istBoerseGeoeffnet)
            {
                UpdatePortfolioWithMarketData();
            }

            Debug.WriteLine("Portfolio-Update-Timer gestartet");
        }

        /// <summary>
        /// Aktualisiert das Portfolio mit den aktuellen Marktdaten
        /// </summary>
        private void UpdatePortfolioWithMarketData()
        {
            if (MarktdatenViewModel?.AktienListe != null && PortfolioViewModel != null)
            {
                // Prüfen, ob die Börse geöffnet ist, bevor das Portfolio aktualisiert wird
                bool istBoerseGeoeffnet = App.TwelveDataService?.IstBoerseGeoeffnet() ?? false;

                if (!istBoerseGeoeffnet)
                {
                    Debug.WriteLine("Börse geschlossen: Portfolio wird nicht mit Marktdaten aktualisiert");
                    return; // Keine Aktualisierung bei geschlossener Börse
                }

                Debug.WriteLine("Aktualisiere Portfolio mit Marktdaten...");
                PortfolioViewModel.AktualisiereKurseMitMarktdaten(MarktdatenViewModel.AktienListe);

                // Auch Aktienfilterliste aktualisieren
                if (AktienFilterViewModel != null)
                {
                    AktienFilterViewModel.AktualisierePreise(MarktdatenViewModel.AktienListe);
                }
            }
        }

        #endregion

        #region Öffentliche Wrapper‑Methoden

        /// <summary>
        /// Erlaubt externen Aufrufern (z.B. dem AktienhandelViewModel), 
        /// die PropertyChanged-Benachrichtigung für 'AktuellerBenutzer' auszulösen.
        /// </summary>
        public void NotifyAktuellerBenutzerChanged()
        {
            // Kontostand-Property aktualisieren
            if (AktuellerBenutzer != null)
            {
                Kontostand = AktuellerBenutzer.Kontostand;
            }

            // Benachrichtigen, dass sich der Benutzer geändert hat
            OnPropertyChanged(nameof(AktuellerBenutzer));
        }

        /// <summary>
        /// Aktualisiert den Kontostand des Benutzers
        /// </summary>
        /// <param name="neuerKontostand">Der neue Kontostand</param>
        public void AktualisiereKontostand(decimal neuerKontostand)
        {
            Debug.WriteLine($"AktualisiereKontostand aufgerufen: alt={Kontostand:N2}€, neu={neuerKontostand:N2}€");
            Kontostand = neuerKontostand;
        }

        /// <summary>
        /// Erhöht den Kontostand um den angegebenen Betrag
        /// </summary>
        /// <param name="betrag">Der zu addierende Betrag</param>
        public async void ErhöheKontostand(decimal betrag)
        {
            Debug.WriteLine($"ErhöheKontostand aufgerufen: +{betrag:N2}€");
            Kontostand += betrag;

            // Aktualisiere den Benutzer in der Datenbank
            if (AktuellerBenutzer != null && App.DbService != null)
            {
                // Kontostand im Benutzer-Objekt aktualisieren
                AktuellerBenutzer.Kontostand = Kontostand;

                // In der Datenbank speichern
                bool erfolg = await App.DbService.UpdateBenutzerAsync(AktuellerBenutzer);
                if (!erfolg)
                {
                    Debug.WriteLine("FEHLER: Kontostand konnte nicht in der Datenbank aktualisiert werden!");
                }
            }
        }

        /// <summary>
        /// Verringert den Kontostand um den angegebenen Betrag
        /// </summary>
        /// <param name="betrag">Der abzuziehende Betrag</param>
        public async void VerringereKontostand(decimal betrag)
        {
            Debug.WriteLine($"VerringereKontostand aufgerufen: -{betrag:N2}€");
            Kontostand -= betrag;

            // Aktualisiere den Benutzer in der Datenbank
            if (AktuellerBenutzer != null && App.DbService != null)
            {
                // Kontostand im Benutzer-Objekt aktualisieren
                AktuellerBenutzer.Kontostand = Kontostand;

                // In der Datenbank speichern
                bool erfolg = await App.DbService.UpdateBenutzerAsync(AktuellerBenutzer);
                if (!erfolg)
                {
                    Debug.WriteLine("FEHLER: Kontostand konnte nicht in der Datenbank aktualisiert werden!");
                }
            }
        }

        #endregion
    }
}