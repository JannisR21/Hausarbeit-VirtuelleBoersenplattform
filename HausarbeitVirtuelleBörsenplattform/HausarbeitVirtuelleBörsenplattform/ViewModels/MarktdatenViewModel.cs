using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Models;
using HausarbeitVirtuelleBörsenplattform.Services;
using System;
using System.Collections.Generic;
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
        private readonly TwelveDataService _twelveDatenService;
        private DispatcherTimer _updateTimer;
        private bool _isUpdating;
        private string _statusText;
        private string _fehlerText;
        private bool _hatFehler;
        private DateTime _letzteAktualisierung;
        private bool _isLoading;
        private TimeSpan _aktualisierungsIntervall = TimeSpan.FromMinutes(15); // Von 5 auf 15 Minuten erhöht
        private readonly MainViewModel _mainViewModel;
        private int _fehlerCounter = 0; // Zählt API-Fehler, um Intervall anzupassen
        private HashSet<string> _portfolioSymbole = new HashSet<string>();

        // Kultur für korrekte Formatierung
        private CultureInfo _germanCulture = new CultureInfo("de-DE");

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
        /// <param name="apiKey">API-Schlüssel für Twelve Data</param>
        // In MarktdatenViewModel.cs Konstruktor anpassen:
        public MarktdatenViewModel(MainViewModel mainViewModel = null, string apiKey = null)
        {
            _mainViewModel = mainViewModel;

            // Wenn kein API-Key übergeben wurde, aus der App-Konfiguration lesen
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = App.TwelveDataApiKey;
                Debug.WriteLine($"API-Key aus App-Konfiguration geladen: {apiKey ?? "null"}");
            }

            Debug.WriteLine($"MarktdatenViewModel wird initialisiert mit API-Key: {apiKey}");

            _twelveDatenService = new TwelveDataService(apiKey);

            // Kommandos initialisieren
            AktualisierenCommand = new RelayCommand(async () => await AktualisiereMarktdaten(), () => !IsLoading);

            // Collection initialisieren
            AktienListe = new ObservableCollection<Aktie>();

            // Keine automatische Datenladung mehr
            // Entferne InitializeMarktdatenAsync()-Aufruf

            // Timer für regelmäßige Aktualisierungen entfernen oder deaktivieren
            // StartUpdateTimer();

            // Event-Handler registrieren für Portfolio-Änderungen (falls MainViewModel existiert)
            if (_mainViewModel?.PortfolioViewModel != null)
            {
                // Initialen Zustand erfassen
                AktualisierePortfolioSymbole();

                // Auf Property-Changed-Events reagieren
                _mainViewModel.PortfolioViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(_mainViewModel.PortfolioViewModel.PortfolioEintraege))
                    {
                        AktualisierePortfolioSymbole();
                    }
                };
            }
        }

        /// <summary>
        /// Aktualisiert die Liste der Portfolio-Symbole
        /// </summary>
        private void AktualisierePortfolioSymbole()
        {
            if (_mainViewModel?.PortfolioViewModel?.PortfolioEintraege != null)
            {
                _portfolioSymbole.Clear();
                foreach (var portfolioEintrag in _mainViewModel.PortfolioViewModel.PortfolioEintraege)
                {
                    _portfolioSymbole.Add(portfolioEintrag.AktienSymbol.ToUpper());
                }
                Debug.WriteLine($"Portfolio-Symbole aktualisiert: {string.Join(", ", _portfolioSymbole)}");
            }
        }

        #endregion

        #region Private Methoden

        /// <summary>
        /// Asynchrone Version der Initialisierungsmethode
        /// </summary>
        private async void InitializeMarktdatenAsync()
        {
            try
            {
                // Erst versuchen, Aktien aus der Datenbank zu laden
                if (App.DbService != null)
                {
                    var aktienAusDatenbank = await App.DbService.GetAllAktienAsync();
                    Debug.WriteLine($"Aktien aus Datenbank geladen: {aktienAusDatenbank.Count}");

                    if (aktienAusDatenbank.Count > 0)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => {
                            AktienListe.Clear();
                            foreach (var aktie in aktienAusDatenbank)
                            {
                                AktienListe.Add(aktie);
                            }
                        });

                        LetzteAktualisierung = DateTime.Now;
                        NächsteAktualisierung = DateTime.Now.Add(_aktualisierungsIntervall);
                        StatusText = "Daten aus Datenbank geladen";

                        // Auch wenn wir Daten aus der DB haben, aktualisieren wir sie trotzdem,
                        // aber mit Verzögerung, um nicht sofort API-Anfragen zu machen
                        await Task.Delay(5000); // 5 Sekunden warten
                        await AktualisiereMarktdaten();
                        return;
                    }
                }

                // Wenn keine Daten aus der Datenbank geladen werden konnten, sofort API anfragen
                await AktualisiereMarktdaten();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Aktien aus der Datenbank: {ex.Message}");
                // Sofort API verwenden, da Datenbankzugriff fehlgeschlagen ist
                await AktualisiereMarktdaten();
            }
        }

        /// <summary>
        /// Startet einen Timer für die regelmäßige Aktualisierung der Marktdaten
        /// </summary>
        private void StartUpdateTimer()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = _aktualisierungsIntervall // Auf 15 Minuten erhöht (siehe oben)
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
                // Zuerst die Portfolio-Symbole aktualisieren
                AktualisierePortfolioSymbole();

                // Nur Portfolio-Symbole laden, wenn vorhanden
                var symboleListe = new List<string>();

                if (_portfolioSymbole.Count > 0)
                {
                    symboleListe.AddRange(_portfolioSymbole);
                    Debug.WriteLine($"Lade nur Portfolio-Symbole: {string.Join(", ", symboleListe)}");
                }
                else
                {
                    // Wenn kein Portfolio vorhanden, lade nur Standardsymbole als Beispiel
                    string[] standardSymbole = new[] { "AAPL", "MSFT" };
                    symboleListe.AddRange(standardSymbole);
                    Debug.WriteLine($"Kein Portfolio vorhanden, lade Standardsymbole: {string.Join(", ", symboleListe)}");
                }

                // Beschränkung auf max. 2 Symbole pro Anfrage
                int maxSymbole = Math.Min(2, symboleListe.Count);
                var priorisierteSymbole = symboleListe.Take(maxSymbole).ToList();

                Debug.WriteLine($"Aktualisiere Symbole: {string.Join(", ", priorisierteSymbole)}");

                // Aktiendaten von der API abrufen
                var aktienDaten = await _twelveDatenService.HoleAktienKurse(priorisierteSymbole);

                // Prüfen, ob ein API-Fehler aufgetreten ist
                if (!string.IsNullOrEmpty(_twelveDatenService.LastErrorMessage))
                {
                    Debug.WriteLine($"API-Fehler erkannt: {_twelveDatenService.LastErrorMessage}");
                    HatFehler = true;
                    FehlerText = _twelveDatenService.LastErrorMessage;
                    _fehlerCounter++; // Zähler erhöhen

                    // Bei API-Limit-Fehler, verlängere das Intervall
                    if (_twelveDatenService.LastErrorMessage.Contains("API credits") &&
                        _twelveDatenService.LastErrorMessage.Contains("limit"))
                    {
                        // Intervall verdoppeln, aber maximal 30 Minuten
                        _aktualisierungsIntervall = TimeSpan.FromMinutes(Math.Min(30, _aktualisierungsIntervall.TotalMinutes * 2));
                        _updateTimer.Interval = _aktualisierungsIntervall;
                        Debug.WriteLine($"API-Limit erreicht. Intervall auf {_aktualisierungsIntervall.TotalMinutes} Minuten erhöht.");
                    }
                }
                else
                {
                    // Fehler-Zähler zurücksetzen, wenn keine Fehler auftreten
                    _fehlerCounter = Math.Max(0, _fehlerCounter - 1);
                }

                // UI-Thread-Zugriff sicherstellen
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    // Aktualisiere die bestehenden Aktien mit den neuen Daten
                    foreach (var aktienInfo in aktienDaten)
                    {
                        // Finde die bestehende Aktie nach Symbol
                        var aktie = AktienListe.FirstOrDefault(a => a.AktienSymbol == aktienInfo.AktienSymbol);

                        if (aktie != null)
                        {
                            // Aktualisiere die Eigenschaften direkt
                            aktie.AktuellerPreis = aktienInfo.AktuellerPreis;
                            aktie.Änderung = aktienInfo.Änderung;
                            aktie.ÄnderungProzent = aktienInfo.ÄnderungProzent;
                            aktie.LetzteAktualisierung = DateTime.Now;

                            Debug.WriteLine($"Aktie {aktie.AktienSymbol} aktualisiert: Preis={aktie.AktuellerPreis:F2}, Änderung={aktie.Änderung:F2}");
                        }
                        else
                        {
                            // Neue Aktie hinzufügen, wenn sie noch nicht existiert
                            Debug.WriteLine($"Neue Aktie {aktienInfo.AktienSymbol} wird hinzugefügt");
                            AktienListe.Add(new Aktie
                            {
                                AktienID = AktienListe.Count + 1, // Einfache ID-Generierung
                                AktienSymbol = aktienInfo.AktienSymbol,
                                AktienName = aktienInfo.AktienName,
                                AktuellerPreis = aktienInfo.AktuellerPreis,
                                Änderung = aktienInfo.Änderung,
                                ÄnderungProzent = aktienInfo.ÄnderungProzent,
                                LetzteAktualisierung = DateTime.Now
                            });
                        }
                    }

                    LetzteAktualisierung = DateTime.Now;
                    NächsteAktualisierung = DateTime.Now.Add(_aktualisierungsIntervall);

                    // Status aktualisieren
                    if (HatFehler)
                    {
                        StatusText = $"Fehler bei der Aktualisierung um {LetzteAktualisierung.ToString("HH:mm:ss", _germanCulture)}";
                    }
                    else
                    {
                        StatusText = $"Daten aktualisiert um {LetzteAktualisierung.ToString("HH:mm:ss", _germanCulture)}";
                    }

                    // Property-Changed-Event manuell auslösen
                    OnPropertyChanged(nameof(AktienListe));
                });

                // Nach der Aktualisierung in Datenbank speichern
                if (!HatFehler && App.DbService != null)
                {
                    try
                    {
                        var aktienListe = AktienListe.ToList();
                        await App.DbService.UpdateAktienBatchAsync(aktienListe);
                        Debug.WriteLine("Aktualisierte Aktien in Datenbank gespeichert");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Fehler beim Speichern der Aktien in der Datenbank: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unbehandelte Ausnahme: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                HatFehler = true;
                FehlerText = $"Unbehandelte Ausnahme: {ex.Message}";
                StatusText = $"Fehler bei der Aktualisierung: {ex.Message}";

                // Fehler-Zähler erhöhen
                _fehlerCounter++;
            }
            finally
            {
                _isUpdating = false;
                IsLoading = false;
                Debug.WriteLine("Aktualisierung der Marktdaten abgeschlossen.");

                // Eventuell das Portfolio aktualisieren
                if (_mainViewModel?.PortfolioViewModel != null)
                {
                    Debug.WriteLine("Aktualisiere Portfolio nach Marktdaten-Update");
                    _mainViewModel.PortfolioViewModel.AktualisiereKurseMitMarktdaten(AktienListe);
                }
            }
        }

        #endregion
    }
}