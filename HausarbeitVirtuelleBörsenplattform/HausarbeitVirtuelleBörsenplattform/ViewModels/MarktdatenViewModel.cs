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
        private TimeSpan _aktualisierungsIntervall = TimeSpan.FromMinutes(5); // 5 Minuten für normale API-Abfragen
        private readonly MainViewModel _mainViewModel;
        private int _fehlerCounter = 0; // Zählt API-Fehler, um Intervall anzupassen
        private HashSet<string> _portfolioSymbole = new HashSet<string>();
        private int _maxApiAnfragenProMinute = 8; // Basic 8 Plan von Twelve Data
        // Historische Daten wurden entfernt, da sie nun im separaten HistorischeDatenViewModel verwaltet werden

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

        // Historische Daten-Properties wurden entfernt

        /// <summary>
        /// Command zum manuellen Aktualisieren der Marktdaten
        /// </summary>
        public IRelayCommand AktualisierenCommand { get; }

        // Command für historische Daten entfernt

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

            // Nach der Initialisierung sofort aktualisieren (verzögert, um UI nicht zu blockieren)
            Application.Current.Dispatcher.BeginInvoke(async () =>
            {
                // Minimale Zeit zum Laden des UIs geben - beschleunigt
                await Task.Delay(500);
                await AktualisiereMarktdaten();

                // Keine historischen Daten mehr laden
            });

            // Event-Handler registrieren für Portfolio-Änderungen
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
                        // Bei Änderungen im Portfolio sofort die Marktdaten aktualisieren
                        AktualisierePortfolioAktienInMarktdaten();
                    }
                };
            }
        }

        /// <summary>
        /// Aktualisiert die Liste der Portfolio-Symbole
        /// </summary>
        private void AktualisierePortfolioSymbole()
        {
            try
            {
                if (_mainViewModel?.PortfolioViewModel?.PortfolioEintraege != null)
                {
                    // Bisherige Symbole merken
                    var alteListe = new HashSet<string>(_portfolioSymbole);
                    _portfolioSymbole.Clear();

                    foreach (var portfolioEintrag in _mainViewModel.PortfolioViewModel.PortfolioEintraege)
                    {
                        _portfolioSymbole.Add(portfolioEintrag.AktienSymbol.ToUpper());
                    }

                    // Logge nur, wenn sich etwas geändert hat
                    if (!alteListe.SetEquals(_portfolioSymbole))
                    {
                        Debug.WriteLine($"Portfolio-Symbole aktualisiert: {string.Join(", ", _portfolioSymbole)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der Portfolio-Symbole: {ex.Message}");
            }
        }

        /// <summary>
        /// Stellt sicher, dass alle Aktien aus dem Portfolio in den Marktdaten vorhanden sind
        /// </summary>
        private async void AktualisierePortfolioAktienInMarktdaten()
        {
            try
            {
                if (_portfolioSymbole.Count == 0) return;

                // Finde alle Symbole aus dem Portfolio, die noch nicht in den Marktdaten sind
                var vorhandeneSymbole = new HashSet<string>(
                    AktienListe.Select(a => a.AktienSymbol.ToUpper()));

                var fehlendeSymbole = _portfolioSymbole
                    .Where(symbol => !vorhandeneSymbole.Contains(symbol))
                    .ToList();

                if (fehlendeSymbole.Count > 0)
                {
                    Debug.WriteLine($"Es fehlen {fehlendeSymbole.Count} Portfolio-Aktien in den Marktdaten: {string.Join(", ", fehlendeSymbole)}");

                    // Sofort eine Aktualisierung starten, fokussiert auf die fehlenden Symbole
                    if (_twelveDatenService != null)
                    {
                        // Um API-Limits zu respektieren, nur maximal 2 Symbole auf einmal abfragen
                        var zuLadendeSymbole = fehlendeSymbole.Take(2).ToList();
                        Debug.WriteLine($"Lade fehlende Portfolio-Aktien: {string.Join(", ", zuLadendeSymbole)}");

                        // Aktien von der API laden
                        var aktienDaten = await _twelveDatenService.HoleAktienKurse(zuLadendeSymbole);

                        if (aktienDaten != null && aktienDaten.Count > 0)
                        {
                            // Neue Aktien zur Marktdaten-Liste hinzufügen
                            foreach (var aktie in aktienDaten)
                            {
                                var existierendeAktie = AktienListe.FirstOrDefault(a =>
                                    a.AktienSymbol.Equals(aktie.AktienSymbol, StringComparison.OrdinalIgnoreCase));

                                if (existierendeAktie == null)
                                {
                                    // UI-Thread verwenden zum Hinzufügen
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        aktie.AktienID = AktienListe.Count + 1;
                                        AktienListe.Add(aktie);
                                    });

                                    Debug.WriteLine($"Portfolio-Aktie {aktie.AktienSymbol} zu Marktdaten hinzugefügt");
                                }
                                else
                                {
                                    // Aktie aktualisieren
                                    existierendeAktie.AktuellerPreis = aktie.AktuellerPreis;
                                    existierendeAktie.Änderung = aktie.Änderung;
                                    existierendeAktie.ÄnderungProzent = aktie.ÄnderungProzent;
                                    existierendeAktie.LetzteAktualisierung = DateTime.Now;

                                    Debug.WriteLine($"Portfolio-Aktie {aktie.AktienSymbol} in Marktdaten aktualisiert");
                                }
                            }

                            // Wenn noch mehr Aktien fehlen, verzögert erneut aufrufen
                            if (fehlendeSymbole.Count > 2)
                            {
                                Debug.WriteLine($"Es fehlen noch {fehlendeSymbole.Count - 2} weitere Portfolio-Aktien, warte auf nächsten Update");
                                // Nichts tun, wird beim nächsten Timer-Intervall aktualisiert
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Keine Aktien von der API erhalten");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der Portfolio-Aktien in Marktdaten: {ex.Message}");
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
                Interval = _aktualisierungsIntervall
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
                });

                // Status aktualisieren
                StatusText = "Marktdaten zurückgesetzt";
                LetzteAktualisierung = DateTime.Now;

                // Wenn gewünscht, keine Standard-Aktien laden
                if (!clearStandardAktien)
                {
                    // Nach dem Reset die Portfolio-Aktien neu laden
                    AktualisierePortfolioSymbole();

                    // Aktualisiere die Marktdaten mit Fokus auf Portfolio-Aktien
                    Debug.WriteLine("Lade Portfolio-Aktien nach Reset");
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
        /// Aktualisiert die Marktdaten über die Twelve Data API mit Berücksichtigung der Börsenöffnungszeiten
        /// und besonderem Fokus auf Portfolio-Aktien
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

                // WICHTIGE ÄNDERUNG: Portfolio-Symbole immer priorisieren und zuerst laden
                if (_portfolioSymbole.Count > 0)
                {
                    symboleListe.AddRange(_portfolioSymbole);
                    Debug.WriteLine($"Lade Portfolio-Symbole: {string.Join(", ", symboleListe)}");
                }
                else
                {
                    // Wenn kein Portfolio vorhanden, Standard-Aktien laden
                    symboleListe.AddRange(new[] { "AAPL", "MSFT", "GOOGL", "AMZN", "TSLA" });
                    Debug.WriteLine("Kein Portfolio vorhanden, lade Standard-Aktien");
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
                    // Verarbeitete Aktien aus der Liste der Portfolio-Symbole entfernen
                    var verarbeiteteSymbole = new HashSet<string>();

                    // Wenn Daten erfolgreich geladen wurden, diese in die Liste übernehmen
                    if (aktienDaten != null && aktienDaten.Count > 0)
                    {
                        foreach (var aktienInfo in aktienDaten)
                        {
                            verarbeiteteSymbole.Add(aktienInfo.AktienSymbol.ToUpper());

                            // Prüfen, ob die Aktie bereits in der Liste ist
                            var existingAktie = AktienListe.FirstOrDefault(a =>
                                a.AktienSymbol.Equals(aktienInfo.AktienSymbol, StringComparison.OrdinalIgnoreCase));

                            if (existingAktie == null)
                            {
                                // Neue Aktie hinzufügen
                                var neueAktie = new Aktie
                                {
                                    AktienID = AktienListe.Count + 1,
                                    AktienSymbol = aktienInfo.AktienSymbol,
                                    AktienName = aktienInfo.AktienName,
                                    AktuellerPreis = aktienInfo.AktuellerPreis,
                                    Änderung = istBoerseGeoeffnet ? aktienInfo.Änderung : 0,
                                    ÄnderungProzent = istBoerseGeoeffnet ? aktienInfo.ÄnderungProzent : 0,
                                    LetzteAktualisierung = DateTime.Now
                                };

                                AktienListe.Add(neueAktie);
                                Debug.WriteLine($"Neue Aktie {aktienInfo.AktienSymbol} hinzugefügt");
                            }
                            else
                            {
                                // Bestehende Aktie aktualisieren
                                if (!istBoerseGeoeffnet)
                                {
                                    // Bei geschlossener Börse nur Änderungen zurücksetzen
                                    existingAktie.Änderung = 0;
                                    existingAktie.ÄnderungProzent = 0;
                                    existingAktie.LetzteAktualisierung = DateTime.Now;
                                }
                                else
                                {
                                    // Normale Aktualisierung
                                    existingAktie.AktuellerPreis = aktienInfo.AktuellerPreis;
                                    existingAktie.Änderung = aktienInfo.Änderung;
                                    existingAktie.ÄnderungProzent = aktienInfo.ÄnderungProzent;
                                    existingAktie.LetzteAktualisierung = DateTime.Now;
                                }
                                Debug.WriteLine($"Aktie {aktienInfo.AktienSymbol} aktualisiert");
                            }
                        }
                    }

                    // Sind noch Portfolio-Aktien übrig, die nicht geladen wurden?
                    var nochFehlendeSymbole = _portfolioSymbole
                        .Where(s => !verarbeiteteSymbole.Contains(s) &&
                                   !AktienListe.Any(a => a.AktienSymbol.Equals(s, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (nochFehlendeSymbole.Count > 0)
                    {
                        Debug.WriteLine($"Nach der API-Anfrage fehlen noch {nochFehlendeSymbole.Count} Portfolio-Aktien: {string.Join(", ", nochFehlendeSymbole)}");
                        // Diese werden beim nächsten Update priorisiert
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

            // Historische Daten-Funktionalität wurde entfernt
    }
}