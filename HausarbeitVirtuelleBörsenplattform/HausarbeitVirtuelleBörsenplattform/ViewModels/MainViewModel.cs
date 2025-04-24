using CommunityToolkit.Mvvm.ComponentModel;
using HausarbeitVirtuelleBörsenplattform.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;

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

        // Alternativ-Property für HandelsViewModel
        public AktienhandelViewModel HandelsViewModel => AktienhandelViewModel;

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
                    InitializeData();
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

                // Explizite Initialisierung der ViewModels
                Debug.WriteLine("Initialisiere MarktdatenViewModel...");
                MarktdatenViewModel = new MarktdatenViewModel(this, apiKey);

                Debug.WriteLine("Initialisiere PortfolioViewModel...");
                PortfolioViewModel = new PortfolioViewModel(App.DbService, AktuellerBenutzer.BenutzerID);
                await PortfolioViewModel.LoadPortfolioDataAsync();

                Debug.WriteLine("Initialisiere AktienhandelViewModel...");

                // Erstelle einige Standard-Aktien für das AktienhandelViewModel
                var standardAktien = new ObservableCollection<Aktie>
                {
                    new Aktie { AktienID = 1, AktienSymbol = "AAPL", AktienName = "Apple Inc.", AktuellerPreis = 150.00m },
                    new Aktie { AktienID = 2, AktienSymbol = "MSFT", AktienName = "Microsoft Corp.", AktuellerPreis = 320.45m },
                    new Aktie { AktienID = 3, AktienSymbol = "TSLA", AktienName = "Tesla Inc.", AktuellerPreis = 200.20m },
                    new Aktie { AktienID = 4, AktienSymbol = "AMZN", AktienName = "Amazon.com Inc.", AktuellerPreis = 95.10m },
                    new Aktie { AktienID = 5, AktienSymbol = "GOOGL", AktienName = "Alphabet Inc.", AktuellerPreis = 128.75m }
                };

                // AktienhandelViewModel mit Standard-Aktien initialisieren
                AktienhandelViewModel = new AktienhandelViewModel(this);
                AktienhandelViewModel.InitializeWithAktien(standardAktien);

                // Timer starten
                StartPortfolioUpdateTimer();

                Debug.WriteLine("Alle ViewModels erfolgreich initialisiert");
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
                Interval = TimeSpan.FromSeconds(10) // Alle 10 Sekunden aktualisieren
            };

            _portfolioUpdateTimer.Tick += (s, e) => UpdatePortfolioWithMarketData();
            _portfolioUpdateTimer.Start();

            // Sofort einmal aktualisieren
            UpdatePortfolioWithMarketData();

            Debug.WriteLine("Portfolio-Update-Timer gestartet");
        }

        /// <summary>
        /// Aktualisiert das Portfolio mit den aktuellen Marktdaten
        /// </summary>
        private void UpdatePortfolioWithMarketData()
        {
            if (MarktdatenViewModel?.AktienListe != null && PortfolioViewModel != null)
            {
                Debug.WriteLine("Aktualisiere Portfolio mit Marktdaten...");
                PortfolioViewModel.AktualisiereKurseMitMarktdaten(MarktdatenViewModel.AktienListe);
            }
        }

        #endregion

        #region Beispiel‑Daten für den Design‑Mode / Fallback

        private void InitializeData()
        {
            // Beispiel-Benutzer
            AktuellerBenutzer = new Benutzer
            {
                BenutzerID = 1,
                Benutzername = "Jannis Ruhland",
                Email = "jannis.ruhland@example.com",
                Kontostand = 10000.00m
            };

            // Kontostand explizit initialisieren
            Kontostand = AktuellerBenutzer.Kontostand;
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
        public void ErhöheKontostand(decimal betrag)
        {
            Debug.WriteLine($"ErhöheKontostand aufgerufen: +{betrag:N2}€");
            Kontostand += betrag;
        }

        /// <summary>
        /// Verringert den Kontostand um den angegebenen Betrag
        /// </summary>
        /// <param name="betrag">Der abzuziehende Betrag</param>
        public void VerringereKontostand(decimal betrag)
        {
            Debug.WriteLine($"VerringereKontostand aufgerufen: -{betrag:N2}€");
            Kontostand -= betrag;
        }

        #endregion
    }
}