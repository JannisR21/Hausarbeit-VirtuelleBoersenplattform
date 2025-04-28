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
using System.Windows;
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
        private int _maxApiAnfragenProMinute = 8; // Basic 8 Plan von Twelve Data

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

            // Timer für regelmäßige Aktualisierungen starten
            StartUpdateTimer();

            // Nach der Initialisierung sofort aktualisieren
            // Nach der Initialisierung sofort aktualisieren (verzögert, um UI nicht zu blockieren)
            Application.Current.Dispatcher.BeginInvoke(async () =>
            {
                // Zeit zum Laden des UIs geben
                await Task.Delay(2000);
                await AktualisiereMarktdaten();
            });

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
        /// Setzt die Marktdaten vollständig zurück und leert die Anzeige
        /// </summary>
        /// <param name="clearStandardAktien">Wenn true, werden auch die Standardaktien nicht angezeigt</param>
        /// <returns>Async Task</returns>
        public async Task ResetMarktdatenAsync(bool clearStandardAktien = true)
        {
            try
            {
                Debug.WriteLine("Setze Marktdaten vollständig zurück");

                // UI-Thread verwenden, um die Collection zu leeren
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 1. AktienListe komplett leeren
                    if (AktienListe != null)
                    {
                        AktienListe.Clear();
                    }

                    // 2. Portfolio-Symbole zurücksetzen (interne Collection)
                    _portfolioSymbole.Clear();
                });

                // Status aktualisieren
                StatusText = "Marktdaten zurückgesetzt";
                LetzteAktualisierung = DateTime.Now;

                // Wenn gewünscht, keine Standard-Aktien laden
                if (!clearStandardAktien)
                {
                    // Nur wenn explizit gewünscht, Standard-Aktien laden
                    Debug.WriteLine("Lade Standard-Aktien nach Reset");
                    await AktualisiereMarktdaten();
                }
                else
                {
                    Debug.WriteLine("Keine Standard-Aktien nach Reset geladen");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Zurücksetzen der Marktdaten: {ex.Message}");
                HatFehler = true;
                FehlerText = $"Fehler beim Zurücksetzen der Marktdaten: {ex.Message}";
            }
        }

        /// <summary>
        /// Aktualisiert die Marktdaten über die Twelve Data API
        /// </summary>
        /// <summary>
        /// Aktualisiert die Marktdaten über die Twelve Data API mit Berücksichtigung leerer Portfolios
        /// </summary>
        /// <summary>
        /// Aktualisiert die Marktdaten über die Twelve Data API mit Berücksichtigung der Börsenöffnungszeiten
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
                // Prüfe zuerst, ob die Börse geöffnet ist
                bool istBoerseGeoeffnet = _twelveDatenService.IstBoerseGeoeffnet();

                // Wenn die Börse geschlossen ist, verwenden wir andere Status-Texte
                if (!istBoerseGeoeffnet)
                {
                    Debug.WriteLine("Börse ist derzeit geschlossen.");
                    StatusText = $"Börse geschlossen - Letzte Aktualisierung: {LetzteAktualisierung.ToString("dd.MM.yyyy HH:mm:ss", _germanCulture)}";

                    // Wenn die Börse geschlossen ist, aktualisieren wir nur alle 60 Minuten
                    DateTime naechsteAkt = LetzteAktualisierung.AddMinutes(60);
                    if (DateTime.Now < naechsteAkt)
                    {
                        Debug.WriteLine($"Nächste Aktualisierung während geschlossener Börse erst um {naechsteAkt.ToString("HH:mm", _germanCulture)}");
                        _isUpdating = false;
                        IsLoading = false;
                        return;
                    }

                    FehlerText = "Die Börse ist derzeit geschlossen. Es werden die letzten bekannten Kurse angezeigt ohne Veränderungen.";
                    HatFehler = true;
                }

                // Zuerst die Portfolio-Symbole aktualisieren
                AktualisierePortfolioSymbole();

                // Symbole zum Aktualisieren vorbereiten
                var symboleListe = new List<string>();

                // Priorisieren: Portfolio-Symbole IMMER aktualisieren
                if (_portfolioSymbole.Count > 0)
                {
                    symboleListe.AddRange(_portfolioSymbole);
                    Debug.WriteLine($"Lade Portfolio-Symbole: {string.Join(", ", symboleListe)}");
                }
                else
                {
                    // Wenn kein Portfolio vorhanden, keine Standardsymbole mehr laden - es wird eine leere Liste angezeigt
                    Debug.WriteLine("Kein Portfolio vorhanden, keine Aktien werden geladen");

                    // UI-Thread aktualisieren mit leerer Liste
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (AktienListe != null)
                        {
                            AktienListe.Clear();
                        }
                    });

                    _isUpdating = false;
                    IsLoading = false;
                    StatusText = "Keine Aktien im Portfolio";
                    LetzteAktualisierung = DateTime.Now;
                    NächsteAktualisierung = DateTime.Now.Add(_aktualisierungsIntervall);
                    return; // Früh zurückkehren, da keine Aktien zu laden sind
                }

                // Beschränkung auf max. 5 Symbole pro Anfrage, um die API-Limits zu respektieren
                int maxSymbole = Math.Min(5, symboleListe.Count);
                var priorisierteSymbole = symboleListe.Take(maxSymbole).ToList();

                Debug.WriteLine($"Aktualisiere Symbole: {string.Join(", ", priorisierteSymbole)}");

                // Aktiendaten von der API abrufen
                var aktienDaten = await _twelveDatenService.HoleAktienKurse(priorisierteSymbole);

                // Prüfen, ob ein API-Fehler aufgetreten ist
                if (!string.IsNullOrEmpty(_twelveDatenService.LastErrorMessage))
                {
                    Debug.WriteLine($"API-Fehler erkannt: {_twelveDatenService.LastErrorMessage}");
                    HatFehler = true;
                    FehlerText = $"Twelve Data API meldet: {_twelveDatenService.LastErrorMessage}";

                    // Wenn die Börse geschlossen ist, verwenden wir eine angepasste Fehlermeldung
                    if (!istBoerseGeoeffnet)
                    {
                        FehlerText = "Die Börse ist derzeit geschlossen. Es werden die letzten bekannten Kurse angezeigt.";
                    }

                    _fehlerCounter++; // Zähler erhöhen

                    // Bei API-Limit-Fehler, verlängere das Intervall
                    if (_twelveDatenService.LastErrorMessage.Contains("API credits") &&
                        _twelveDatenService.LastErrorMessage.Contains("limit"))
                    {
                        // Intervall verdoppeln, aber maximal 30 Minuten
                        _aktualisierungsIntervall = TimeSpan.FromMinutes(Math.Min(30, _aktualisierungsIntervall.TotalMinutes * 2));
                        _updateTimer.Interval = _aktualisierungsIntervall;
                        Debug.WriteLine($"API-Limit erreicht. Intervall auf {_aktualisierungsIntervall.TotalMinutes} Minuten erhöht.");

                        // Fehlertext ergänzen
                        FehlerText += $" (API-Limit von {_maxApiAnfragenProMinute} Anfragen/Minute erreicht. Nächste Aktualisierung in {_aktualisierungsIntervall.TotalMinutes} Minuten.)";
                    }
                }
                else
                {
                    // Fehler-Zähler zurücksetzen, wenn keine Fehler auftreten
                    _fehlerCounter = Math.Max(0, _fehlerCounter - 1);
                }

                // UI-Thread-Zugriff sicherstellen
                Application.Current.Dispatcher.Invoke(() =>
                {
                // Aktualisiere die bestehenden Aktien mit den neuen Daten
                foreach (var aktie in AktienListe)
                {
                    var aktienInfo = aktienDaten.FirstOrDefault(a => a.AktienSymbol == aktie.AktienSymbol);

                        if (aktienInfo != null)
                        {
                            // Wenn die Börse geschlossen ist, behalten wir den aktuellen Kurs bei und setzen Änderungen auf 0
                            if (!istBoerseGeoeffnet)
                            {
                                // Änderungen auf 0 setzen, damit keine falschen Bewegungen angezeigt werden
                                aktie.Änderung = 0;
                                aktie.ÄnderungProzent = 0;
                                aktie.LetzteAktualisierung = DateTime.Now;

                                // WICHTIG: Den Preis NICHT aktualisieren bei geschlossener Börse!
                                Debug.WriteLine($"Börse geschlossen: Behalte Preis für {aktie.AktienSymbol} bei {aktie.AktuellerPreis:F2}€");
                            }
                            else
                            {
                                // Normale Aktualisierung bei geöffnetem Markt
                                aktie.AktuellerPreis = aktienInfo.AktuellerPreis;
                                aktie.Änderung = aktienInfo.Änderung;
                                aktie.ÄnderungProzent = aktienInfo.ÄnderungProzent;
                                aktie.LetzteAktualisierung = DateTime.Now;

                                Debug.WriteLine($"Aktie {aktie.AktienSymbol} aktualisiert: Preis={aktie.AktuellerPreis:F2}, Änderung={aktie.Änderung:F2}");
                            }
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
                                Änderung = istBoerseGeoeffnet ? aktienInfo.Änderung : 0,
                                ÄnderungProzent = istBoerseGeoeffnet ? aktienInfo.ÄnderungProzent : 0,
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

                        // Wenn die Börse geschlossen ist, angepasste Statusmeldung
                        if (!istBoerseGeoeffnet)
                        {
                            StatusText = $"Börse geschlossen - Letzte Aktualisierung: {LetzteAktualisierung.ToString("HH:mm:ss", _germanCulture)}";
                        }
                    }
                    else
                    {
                        // Normale Statusmeldung oder Hinweis auf geschlossene Börse
                        if (istBoerseGeoeffnet)
                        {
                            StatusText = $"Daten aktualisiert um {LetzteAktualisierung.ToString("HH:mm:ss", _germanCulture)}";
                        }
                        else
                        {
                            StatusText = $"Börse geschlossen - Letzte Aktualisierung: {LetzteAktualisierung.ToString("HH:mm:ss", _germanCulture)}";
                        }
                    }

                    // Property-Changed-Event manuell auslösen
                    OnPropertyChanged(nameof(AktienListe));
                });

                // Nach der Aktualisierung in Datenbank speichern
                if (!HatFehler && App.DbService != null && istBoerseGeoeffnet)
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
                FehlerText = $"Fehler bei der Aktualisierung: {ex.Message}";
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