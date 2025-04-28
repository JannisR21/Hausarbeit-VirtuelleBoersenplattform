using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Data;
using HausarbeitVirtuelleBörsenplattform.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    /// <summary>
    /// ViewModel für den Aktienhandel-Bereich
    /// </summary>
    public class AktienhandelViewModel : ObservableObject
    {
        #region Private Felder
        private MainViewModel _mainViewModel; // Wird nach der Initialisierung gesetzt
        private string _aktienSymbol;
        private string _aktienName;
        private decimal _aktuellerKurs;
        private int _menge = 1; // Mit 1 vorinitialisieren!
        private decimal _gesamtwert;
        private bool _isKauf = true;
        private string _warnungsText;
        private bool _zeigeWarnung;
        private string _transaktionsText = "Aktien kaufen"; // Mit Standardwert initialisieren
        private ObservableCollection<Aktie> _aktienListe;
        private Aktie _selectedAktie; // Property für ausgewählte Aktie
        private bool _isLoading; // Zeigt an, ob Kursdaten geladen werden
        private string _suchText = string.Empty; // Suchtext für die Filterung der Aktien
        #endregion

        #region Konstruktor
        /// <summary>
        /// Initialisiert eine neue Instanz des AktienhandelViewModel
        /// </summary>
        /// <param name="mainViewModel">Das MainViewModel, das die anderen ViewModels verwaltet</param>
        public AktienhandelViewModel(MainViewModel mainViewModel = null)
        {
            // Debugging-Info
            Debug.WriteLine("AktienhandelViewModel wird initialisiert...");

            // KRITISCH: Sofort die AktienListe initialisieren, um NullReferenceException zu vermeiden
            _aktienListe = new ObservableCollection<Aktie>();
            Debug.WriteLine("Leere AktienListe wurde erstellt");

            // Commands initialisieren - Namen ohne "Async"-Suffix
            AktienSuchenCommand = new AsyncRelayCommand(AktienSuchen);
            TransaktionAusführenCommand = new AsyncRelayCommand(TransaktionAusführenAsync, KannTransaktionAusführen);
            IncrementMengeCommand = new RelayCommand(IncrementMenge);
            DecrementMengeCommand = new RelayCommand(DecrementMenge);

            // PropertyChanged-Handler für IsKauf
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsKauf))
                {
                    PrüfeTransaktionsDurchführbarkeit();
                    TransaktionsText = IsKauf ? "Aktien kaufen" : "Aktien verkaufen";
                }
            };

            // MainViewModel setzen
            _mainViewModel = mainViewModel;

            // Aktien-Liste initialisieren mit bekannten Börsensymbolen
            InitializeAktienListe();

            // Listener für SelectedAktie-Änderungen
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedAktie) && SelectedAktie != null)
                {
                    // Wenn eine Aktie ausgewählt wird, übernehmen wir die Daten
                    AktienSymbol = SelectedAktie.AktienSymbol;
                    AktienName = SelectedAktie.AktienName;

                    // Da wir Lazy-Loading implementieren, laden wir jetzt erst die Kursdaten
                    LadeAktienKursdaten(SelectedAktie.AktienSymbol);
                }
                if (_aktienListe == null || _aktienListe.Count == 0)
                {
                    MessageBox.Show("Achtung: Es wurden keine Aktien geladen.", "Ladefehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            // Event-Handler für Änderungen an MainViewModel registrieren
            if (_mainViewModel != null)
            {
                _mainViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(_mainViewModel.AktuellerBenutzer))
                    {
                        // Kontostand hat sich möglicherweise geändert
                        OnPropertyChanged(nameof(AktuellerKontostand));
                        PrüfeTransaktionsDurchführbarkeit();
                    }
                };

                // Event-Handler für Änderungen in den Marktdaten
                if (_mainViewModel.MarktdatenViewModel != null)
                {
                    _mainViewModel.MarktdatenViewModel.PropertyChanged += MarktdatenViewModel_PropertyChanged;
                }
            }
        }

        // Event-Handler für PropertyChanged des MarktdatenViewModels
        private void MarktdatenViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine($"MarktdatenViewModel PropertyChanged: {e.PropertyName}");

            if (e.PropertyName == nameof(_mainViewModel.MarktdatenViewModel.AktienListe))
            {
                // Aktuelle Aktie aktualisieren, falls ausgewählt
                AktualisiereAktuelleAktie();
            }
        }

        // Aktualisiert den Kurs der aktuell ausgewählten Aktie basierend auf Marktdaten
        private void AktualisiereAktuelleAktie()
        {
            if (!string.IsNullOrEmpty(AktienSymbol) && _mainViewModel?.MarktdatenViewModel?.AktienListe != null)
            {
                var aktieAusMarktdaten = _mainViewModel.MarktdatenViewModel.AktienListe
                    .FirstOrDefault(a => a.AktienSymbol.Equals(AktienSymbol, StringComparison.OrdinalIgnoreCase));

                if (aktieAusMarktdaten != null && aktieAusMarktdaten.AktuellerPreis > 0)
                {
                    // Wenn der Preis sich geändert hat, aktualisieren
                    if (AktuellerKurs != aktieAusMarktdaten.AktuellerPreis)
                    {
                        Debug.WriteLine($"Aktienhandel: Aktualisiere Kurs für {AktienSymbol} von {AktuellerKurs:F2}€ auf {aktieAusMarktdaten.AktuellerPreis:F2}€");
                        AktuellerKurs = aktieAusMarktdaten.AktuellerPreis;
                        BerechneGesamtwert();
                        PrüfeTransaktionsDurchführbarkeit();
                    }
                }
            }
        }

        /// <summary>
        /// Initialisiert die AktienListe mit Basisinformationen (ohne Kursdaten)
        /// </summary>
        private void InitializeAktienListe()
        {
            Debug.WriteLine("Initialisiere erweiterte Aktien-Liste...");

            try
            {
                // WICHTIG: Prüfe, ob _aktienListe null ist und initialisiere sie in diesem Fall
                if (_aktienListe == null)
                {
                    Debug.WriteLine("AktienListe war null und wird neu erstellt");
                    _aktienListe = new ObservableCollection<Aktie>();
                }
                else
                {
                    Debug.WriteLine($"AktienListe bereits vorhanden mit {_aktienListe.Count} Elementen");
                }

                // Falls bereits Daten vorhanden sind, diese beibehalten
                if (_aktienListe.Count > 0)
                {
                    Debug.WriteLine("AktienListe hat bereits Elemente, überspringe erneutes Laden");
                    OnPropertyChanged(nameof(AktienListe));
                    OnPropertyChanged(nameof(GefilterteAktienListe));
                    return;
                }

                // Bekannte Aktien aus dem FTSE All-World und anderen wichtigen Indizes laden
                var bekannteBörsenAktien = Data.AktienListe.GetBekannteBörsenAktien();

                Debug.WriteLine($"Anzahl bekannter Börsenaktien: {bekannteBörsenAktien.Count}");

                // In die ObservableCollection übertragen
                foreach (var aktie in bekannteBörsenAktien)
                {
                    _aktienListe.Add(aktie);
                }

                Debug.WriteLine($"AktienListe erfolgreich mit {_aktienListe.Count} Aktien initialisiert");

                // Explizite PropertyChanged-Benachrichtigungen
                OnPropertyChanged(nameof(AktienListe));
                OnPropertyChanged(nameof(GefilterteAktienListe));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Initialisieren der Aktien-Liste: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Fallback-Initialisierung mit Beispieldaten
                if (_aktienListe == null)
                {
                    _aktienListe = new ObservableCollection<Aktie>();
                }

                if (_aktienListe.Count == 0)
                {
                    _aktienListe.Add(new Aktie { AktienID = 1, AktienSymbol = "AAPL", AktienName = "Apple Inc." });
                    _aktienListe.Add(new Aktie { AktienID = 2, AktienSymbol = "MSFT", AktienName = "Microsoft Corporation" });
                    _aktienListe.Add(new Aktie { AktienID = 3, AktienSymbol = "GOOGL", AktienName = "Alphabet Inc." });

                    Debug.WriteLine("Fallback-Initialisierung mit 3 Standard-Aktien durchgeführt");
                    OnPropertyChanged(nameof(AktienListe));
                    OnPropertyChanged(nameof(GefilterteAktienListe));
                }
            }

            if (_aktienListe == null || !_aktienListe.Any())
            {
                MessageBox.Show("AktienListe ist leer!", "Debug", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Public Properties
        /// <summary>
        /// Verfügbare Aktien zum Auswählen
        /// </summary>
        public ObservableCollection<Aktie> AktienListe
        {
            get
            {
                // SCHUTZMASSNAHME: Stelle sicher, dass AktienListe nie null zurückgibt
                if (_aktienListe == null)
                {
                    Debug.WriteLine("WARNUNG: AktienListe wurde null abgerufen, erstelle neue Liste");
                    _aktienListe = new ObservableCollection<Aktie>();

                    // Füllen mit Fallback-Daten
                    _aktienListe.Add(new Aktie { AktienID = 1, AktienSymbol = "AAPL", AktienName = "Apple Inc." });
                    _aktienListe.Add(new Aktie { AktienID = 2, AktienSymbol = "MSFT", AktienName = "Microsoft Corporation" });
                    _aktienListe.Add(new Aktie { AktienID = 3, AktienSymbol = "GOOGL", AktienName = "Alphabet Inc." });
                }
                return _aktienListe;
            }
            private set
            {
                // Sicherstellung, dass _aktienListe nie null wird
                if (value == null)
                {
                    Debug.WriteLine("WARNUNG: Versuch, AktienListe auf null zu setzen, wurde verhindert");
                    return;
                }
                SetProperty(ref _aktienListe, value);
            }
        }

        /// <summary>
        /// Suchtext für die Filterung der Aktien
        /// </summary>
        public string SuchText
        {
            get => _suchText;
            set
            {
                if (SetProperty(ref _suchText, value))
                {
                    // Bei Änderung des Suchtexts filtern
                    OnPropertyChanged(nameof(GefilterteAktienListe));
                }
            }
        }

        /// <summary>
        /// Gefilterte Liste der Aktien basierend auf dem Suchtext
        /// </summary>
        public IEnumerable<Aktie> GefilterteAktienListe
        {
            get
            {
                Debug.WriteLine($"GefilterteAktienListe aufgerufen. Suchtext: '{SuchText}'");

                if (AktienListe == null)
                {
                    Debug.WriteLine("WARNUNG: AktienListe ist null in GefilterteAktienListe");
                    return Enumerable.Empty<Aktie>();
                }

                if (string.IsNullOrWhiteSpace(SuchText))
                    return AktienListe;

                var suchBegriff = SuchText.Trim().ToLower();
                return AktienListe.Where(a =>
                    a.AktienSymbol.ToLower().Contains(suchBegriff) ||
                    a.AktienName.ToLower().Contains(suchBegriff));
            }
        }

        /// <summary>
        /// Zeigt an, ob Kursdaten geladen werden
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Ausgewählte Aktie aus der ComboBox
        /// </summary>
        public Aktie SelectedAktie
        {
            get => _selectedAktie;
            set => SetProperty(ref _selectedAktie, value);
        }

        /// <summary>
        /// Symbol/Ticker der ausgewählten Aktie
        /// </summary>
        public string AktienSymbol
        {
            get => _aktienSymbol;
            set
            {
                if (SetProperty(ref _aktienSymbol, value))
                {
                    Debug.WriteLine($"AktienSymbol geändert auf: {value}");
                    // Wenn sich das Symbol ändert, prüfen wir, ob es eine passende Aktie in der Liste gibt
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var aktie = AktienListe.FirstOrDefault(a =>
                            a.AktienSymbol.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (aktie != null && aktie != SelectedAktie)
                        {
                            // Wir haben eine passende Aktie gefunden
                            SelectedAktie = aktie;
                        }
                    }
                    else
                    {
                        AktienName = string.Empty;
                        AktuellerKurs = 0;
                    }

                    TransaktionAusführenCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IsAktieAusgewählt));
                    PrüfeTransaktionsDurchführbarkeit();
                }
            }
        }

        /// <summary>
        /// Vollständiger Name der ausgewählten Aktie
        /// </summary>
        public string AktienName
        {
            get => _aktienName;
            set => SetProperty(ref _aktienName, value);
        }

        /// <summary>
        /// Aktueller Kurs der ausgewählten Aktie
        /// </summary>
        public decimal AktuellerKurs
        {
            get => _aktuellerKurs;
            set
            {
                if (SetProperty(ref _aktuellerKurs, value))
                {
                    BerechneGesamtwert();
                    TransaktionAusführenCommand.NotifyCanExecuteChanged();
                    PrüfeTransaktionsDurchführbarkeit();
                }
            }
        }

        /// <summary>
        /// Anzahl der zu kaufenden/verkaufenden Aktien
        /// </summary>
        public int Menge
        {
            get => _menge;
            set
            {
                // Sicherstellen, dass die Menge immer mindestens 1 ist
                value = Math.Max(1, value);

                if (SetProperty(ref _menge, value))
                {
                    BerechneGesamtwert();
                    TransaktionAusführenCommand.NotifyCanExecuteChanged();
                    PrüfeTransaktionsDurchführbarkeit();
                }
            }
        }

        /// <summary>
        /// Gesamtwert der Transaktion
        /// </summary>
        public decimal Gesamtwert
        {
            get => _gesamtwert;
            private set => SetProperty(ref _gesamtwert, value);
        }

        /// <summary>
        /// Gibt an, ob die Operation ein Kauf (true) oder Verkauf (false) ist
        /// </summary>
        public bool IsKauf
        {
            get => _isKauf;
            set
            {
                if (SetProperty(ref _isKauf, value))
                {
                    Debug.WriteLine($"IsKauf geändert auf: {value}");
                    TransaktionAusführenCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(TransaktionsText));
                }
            }
        }

        /// <summary>
        /// Gibt an, ob die Warnungsmeldung angezeigt werden soll
        /// </summary>
        public bool ZeigeWarnung
        {
            get => _zeigeWarnung;
            set => SetProperty(ref _zeigeWarnung, value);
        }

        /// <summary>
        /// Text für die Warnungsmeldung
        /// </summary>
        public string WarnungsText
        {
            get => _warnungsText;
            set => SetProperty(ref _warnungsText, value);
        }

        /// <summary>
        /// Text für den Transaktion-Button
        /// </summary>
        public string TransaktionsText
        {
            get => _transaktionsText;
            set => SetProperty(ref _transaktionsText, value);
        }

        /// <summary>
        /// Aktueller Kontostand des Benutzers
        /// </summary>
        public decimal AktuellerKontostand => _mainViewModel?.AktuellerBenutzer?.Kontostand ?? 0;

        /// <summary>
        /// Anzahl der verfügbaren Aktien im Portfolio (für Verkauf)
        /// </summary>
        public int VerfügbareAktien
        {
            get
            {
                if (string.IsNullOrEmpty(AktienSymbol) || _mainViewModel?.PortfolioViewModel == null)
                    return 0;

                var aktieImPortfolio = _mainViewModel.PortfolioViewModel.PortfolioEintraege
                    .FirstOrDefault(p => p.AktienSymbol.Equals(AktienSymbol, StringComparison.OrdinalIgnoreCase));

                return aktieImPortfolio?.Anzahl ?? 0;
            }
        }

        /// <summary>
        /// Gibt an, ob eine Aktie ausgewählt ist
        /// </summary>
        public bool IsAktieAusgewählt => !string.IsNullOrEmpty(AktienSymbol) && AktuellerKurs > 0;
        #endregion

        #region Commands
        /// <summary>
        /// Command zum Suchen/Laden von Aktieninformationen
        /// </summary>
        public IAsyncRelayCommand AktienSuchenCommand { get; }

        /// <summary>
        /// Command zum Ausführen der Transaktion
        /// </summary>
        public IAsyncRelayCommand TransaktionAusführenCommand { get; }

        /// <summary>
        /// Command zum Erhöhen der Menge
        /// </summary>
        public IRelayCommand IncrementMengeCommand { get; }

        /// <summary>
        /// Command zum Verringern der Menge
        /// </summary>
        public IRelayCommand DecrementMengeCommand { get; }
        #endregion

        #region Methoden

        /// <summary>
        /// Methode zum nachträglichen Setzen des MainViewModel
        /// </summary>
        public void SetMainViewModel(MainViewModel mainViewModel)
        {
            Debug.WriteLine("SetMainViewModel aufgerufen");

            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            // Für zukünftige Änderungen in Marktdaten registrieren
            if (_mainViewModel.MarktdatenViewModel != null)
            {
                _mainViewModel.MarktdatenViewModel.PropertyChanged += MarktdatenViewModel_PropertyChanged;
            }

            // Wenn AktienListe leer ist, neu initialisieren
            if (_aktienListe == null || _aktienListe.Count == 0)
            {
                InitializeAktienListe();
            }

            // Explizit PropertyChanged für alle relevanten Properties auslösen
            OnPropertyChanged(nameof(AktienListe));
            OnPropertyChanged(nameof(GefilterteAktienListe));
            OnPropertyChanged(nameof(AktuellerKontostand));
        }

        /// <summary>
        /// Führt die Aktiensuche durch - KORRIGIERT ohne "Async" im Namen
        /// </summary>
        private async Task AktienSuchen()
        {
            Debug.WriteLine($"AktienSuchenCommand aufgerufen für Symbol: {AktienSymbol}");
            if (string.IsNullOrWhiteSpace(AktienSymbol))
            {
                Debug.WriteLine("Kein Symbol eingegeben");
                return;
            }

            // Zuerst nach einer passenden Aktie in der Liste suchen
            var aktie = AktienListe.FirstOrDefault(a =>
                a.AktienSymbol.Equals(AktienSymbol.Trim(), StringComparison.OrdinalIgnoreCase));

            if (aktie != null)
            {
                Debug.WriteLine($"Aktie in Liste gefunden: {aktie.AktienSymbol} - {aktie.AktienName}");
                SelectedAktie = aktie;
                AktienName = aktie.AktienName;

                // Kursdaten laden
                await LadeAktienKursdaten(AktienSymbol);
            }
            else
            {
                Debug.WriteLine($"Aktie mit Symbol {AktienSymbol} nicht in der Liste gefunden");

                // Versuchen, Daten direkt über API zu laden
                IsLoading = true;
                try
                {
                    var aktienDaten = await App.TwelveDataService.HoleAktienKurse(new List<string> { AktienSymbol });

                    if (aktienDaten != null && aktienDaten.Count > 0)
                    {
                        var aktienInfo = aktienDaten.FirstOrDefault();
                        if (aktienInfo != null)
                        {
                            Debug.WriteLine($"Aktie über API gefunden: {aktienInfo.AktienSymbol} - {aktienInfo.AktienName}");
                            AktienName = aktienInfo.AktienName;
                            AktuellerKurs = aktienInfo.AktuellerPreis;
                            BerechneGesamtwert();

                            // Neue Aktie zur Liste hinzufügen
                            if (!AktienListe.Any(a => a.AktienSymbol.Equals(aktienInfo.AktienSymbol, StringComparison.OrdinalIgnoreCase)))
                            {
                                aktienInfo.AktienID = AktienListe.Count + 1;
                                AktienListe.Add(aktienInfo);
                                Debug.WriteLine($"Neue Aktie zur AktienListe hinzugefügt: {aktienInfo.AktienSymbol}");
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Die Aktie mit dem Symbol '{AktienSymbol}' wurde nicht gefunden.",
                                "Aktie nicht gefunden", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Die Aktie mit dem Symbol '{AktienSymbol}' wurde nicht gefunden.",
                            "Aktie nicht gefunden", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Laden der Aktie: {ex.Message}");
                    MessageBox.Show($"Fehler beim Laden der Aktie: {ex.Message}",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// Lädt Kursdaten für eine spezifische Aktie und stellt sicher, dass echte Preise verwendet werden
        /// </summary>
        private async Task LadeAktienKursdaten(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return;

            try
            {
                IsLoading = true;
                Debug.WriteLine($"Lade Kursdaten für Aktie: {symbol}");

                // Aktie in der lokalen Liste finden und Basisinformationen übernehmen
                var aktieInAktienListe = AktienListe.FirstOrDefault(a =>
                    a.AktienSymbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

                if (aktieInAktienListe != null)
                {
                    // Temporäre Zuweisung des Namens und der ID für den Fall, dass API-Abfrage fehlschlägt
                    AktienName = aktieInAktienListe.AktienName;
                }

                // Kurs über TwelveData API abrufen
                if (App.TwelveDataService != null)
                {
                    Debug.WriteLine($"Rufe aktuellen Kurs für {symbol} von Twelve Data API ab");

                    // Korrektes Symbol für die API ermitteln (ohne .DE Suffix, falls nötig)
                    string apiSymbol = symbol;
                    if (symbol.EndsWith(".DE", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Deutsche Aktie erkannt: {symbol}, verwende Symbol ohne Suffix für API-Abfrage");
                        apiSymbol = symbol; // Tatsächlich werden wir das Symbol mit .DE belassen, falls die API damit umgehen kann
                    }

                    var aktienDaten = await App.TwelveDataService.HoleAktienKurse(new List<string> { apiSymbol });

                    if (aktienDaten != null && aktienDaten.Count > 0)
                    {
                        var aktienInfo = aktienDaten.FirstOrDefault();
                        if (aktienInfo != null)
                        {
                            Debug.WriteLine($"Aktie über API geladen: {aktienInfo.AktienSymbol} - {aktienInfo.AktienName} - Kurs: {aktienInfo.AktuellerPreis:F2}€");

                            // ÜBERPRÜFUNG: Stellen sicher, dass wir einen gültigen Preis haben (nicht 0)
                            if (aktienInfo.AktuellerPreis <= 0)
                            {
                                Debug.WriteLine($"WARNUNG: API hat ungültigen Preis zurückgegeben ({aktienInfo.AktuellerPreis}).");
                                MessageBox.Show($"Für die Aktie {symbol} konnte kein gültiger Kurs ermittelt werden. Bitte versuchen Sie es später erneut.",
                                    "Ungültiger Kurs", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            else
                            {
                                // Den echten Preis von der API verwenden
                                AktuellerKurs = aktienInfo.AktuellerPreis;
                                AktienName = aktienInfo.AktienName; // Den Namen von der API übernehmen

                                // Aktie in der lokalen Liste aktualisieren
                                if (aktieInAktienListe != null)
                                {
                                    Debug.WriteLine($"Aktualisiere Aktie in AktienListe: {aktieInAktienListe.AktienSymbol} mit Kurs {aktienInfo.AktuellerPreis:F2}€");
                                    aktieInAktienListe.AktuellerPreis = aktienInfo.AktuellerPreis;
                                    aktieInAktienListe.Änderung = aktienInfo.Änderung;
                                    aktieInAktienListe.ÄnderungProzent = aktienInfo.ÄnderungProzent;
                                    aktieInAktienListe.LetzteAktualisierung = DateTime.Now;
                                }

                                // Auch in Marktdaten aktualisieren
                                if (_mainViewModel?.MarktdatenViewModel?.AktienListe != null)
                                {
                                    var aktieInMarktdaten = _mainViewModel.MarktdatenViewModel.AktienListe
                                        .FirstOrDefault(a => a.AktienSymbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

                                    if (aktieInMarktdaten != null)
                                    {
                                        Debug.WriteLine($"Aktualisiere Aktie in Marktdaten: {aktieInMarktdaten.AktienSymbol} mit Kurs {aktienInfo.AktuellerPreis:F2}€");
                                        aktieInMarktdaten.AktuellerPreis = aktienInfo.AktuellerPreis;
                                        aktieInMarktdaten.Änderung = aktienInfo.Änderung;
                                        aktieInMarktdaten.ÄnderungProzent = aktienInfo.ÄnderungProzent;
                                        aktieInMarktdaten.LetzteAktualisierung = DateTime.Now;
                                    }
                                }

                                BerechneGesamtwert();
                                IsLoading = false;
                                Debug.WriteLine($"Kursdaten für {symbol} erfolgreich geladen und aktualisiert");
                                return;
                            }
                        }
                    }

                    // Wenn die Aktie nicht über die API gefunden wurde, aber in Marktdaten existiert
                    if (_mainViewModel?.MarktdatenViewModel?.AktienListe != null)
                    {
                        var aktieInMarktdaten = _mainViewModel.MarktdatenViewModel.AktienListe
                            .FirstOrDefault(a => a.AktienSymbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

                        if (aktieInMarktdaten != null && aktieInMarktdaten.AktuellerPreis > 0)
                        {
                            Debug.WriteLine($"Kurs aus Marktdaten verwendet: {aktieInMarktdaten.AktienSymbol} - {aktieInMarktdaten.AktuellerPreis:F2}€");
                            AktuellerKurs = aktieInMarktdaten.AktuellerPreis;
                            AktienName = aktieInMarktdaten.AktienName;
                            BerechneGesamtwert();
                            IsLoading = false;
                            return;
                        }
                    }
                }

                // Wenn keine Daten gefunden wurden
                Debug.WriteLine($"Keine Kursdaten für {symbol} gefunden");
                MessageBox.Show($"Für die Aktie {symbol} konnten keine aktuellen Kursdaten abgerufen werden. " +
                                $"Bitte versuchen Sie es später erneut oder wählen Sie eine andere Aktie.",
                               "Keine Kursdaten verfügbar", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Kursdaten: {ex.Message}\nStackTrace: {ex.StackTrace}");

                // Benutzerfreundliche Fehlermeldung
                MessageBox.Show($"Beim Abrufen der Kursdaten für {symbol} ist ein Fehler aufgetreten: {ex.Message}",
                               "Fehler beim Laden der Kursdaten", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Berechnet den Gesamtwert der Transaktion
        /// </summary>
        private void BerechneGesamtwert()
        {
            var neuerGesamtwert = AktuellerKurs * Menge;
            Debug.WriteLine($"Berechne Gesamtwert: {AktuellerKurs} € x {Menge} Stück = {neuerGesamtwert} €");

            Gesamtwert = neuerGesamtwert;

            // Explizite PropertyChanged-Auslösung für Gesamtwert
            OnPropertyChanged(nameof(Gesamtwert));

            // Nach Gesamtwertberechnung prüfen, ob Transaktion durchführbar ist
            PrüfeTransaktionsDurchführbarkeit();
            TransaktionAusführenCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Prüft, ob genügend Geld/Aktien für die Transaktion vorhanden sind
        /// </summary>
        private void PrüfeTransaktionsDurchführbarkeit()
        {
            ZeigeWarnung = false;
            WarnungsText = string.Empty;

            if (!IsAktieAusgewählt || Menge <= 0)
                return;

            if (IsKauf)
            {
                // Prüfe, ob genug Geld vorhanden ist
                if (Gesamtwert > AktuellerKontostand)
                {
                    ZeigeWarnung = true;
                    WarnungsText = "Nicht genügend Guthaben für diesen Kauf.";
                }
            }
            else
            {
                // Prüfe, ob genug Aktien im Portfolio sind
                if (Menge > VerfügbareAktien)
                {
                    ZeigeWarnung = true;
                    WarnungsText = $"Sie besitzen nur {VerfügbareAktien} Aktien dieses Wertpapiers.";
                }
            }
        }

        /// <summary>
        /// Führt die Transaktion (Kauf oder Verkauf) durch - Asynchrone Version
        /// </summary>
        private async Task TransaktionAusführenAsync()
        {
            Debug.WriteLine($"TransaktionAusführenCommand aufgerufen: {(IsKauf ? "Kauf" : "Verkauf")} von {Menge} {AktienSymbol}");

            if (string.IsNullOrWhiteSpace(AktienSymbol) || Menge <= 0)
            {
                Debug.WriteLine("Ungültige Transaktionsparameter");
                MessageBox.Show("Bitte wählen Sie eine Aktie aus und geben Sie eine Menge an.", "Eingabefehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Zuerst prüfen, ob die API Daten für diese Aktie bereitstellt
            IsLoading = true;
            try
            {
                bool istAktieVerfügbar = await App.TwelveDataService.IstAktieVerfügbar(AktienSymbol);
                if (!istAktieVerfügbar)
                {
                    Debug.WriteLine($"Keine Daten für Aktie {AktienSymbol} verfügbar");
                    MessageBox.Show($"Für die Aktie {AktienSymbol} stehen momentan keine Daten zur Verfügung. Bitte wählen Sie eine andere Aktie oder versuchen Sie es später erneut.",
                        "Keine Daten verfügbar", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Verfügbarkeitsprüfung: {ex.Message}");
                // Fahren wir trotzdem fort, da dies nur eine zusätzliche Sicherheit ist
            }
            finally
            {
                IsLoading = false;
            }

            // Zusätzliche Prüfung auf gültigen Kurs
            if (AktuellerKurs <= 0)
            {
                Debug.WriteLine("Ungültiger Kurs: " + AktuellerKurs);
                MessageBox.Show("Der aktuelle Kurs für diese Aktie ist nicht verfügbar oder ungültig. Bitte wählen Sie eine andere Aktie oder versuchen Sie es später erneut.",
                    "Ungültiger Kurs", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var portfolio = _mainViewModel?.PortfolioViewModel;
                if (portfolio == null)
                {
                    Debug.WriteLine("PortfolioViewModel ist null");
                    MessageBox.Show("Portfolio konnte nicht geladen werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Zuerst in Marktdaten suchen für aktuellste Daten
                Aktie aktModell = null;
                if (_mainViewModel?.MarktdatenViewModel?.AktienListe != null)
                {
                    aktModell = _mainViewModel.MarktdatenViewModel.AktienListe
                        .FirstOrDefault(a => a.AktienSymbol.Equals(AktienSymbol.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                // Falls nicht in Marktdaten, in AktienListe suchen
                if (aktModell == null)
                {
                    aktModell = AktienListe
                        .FirstOrDefault(a => a.AktienSymbol
                            .Equals(AktienSymbol.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                // Wenn Aktie nicht gefunden wurde, eine neue erstellen
                if (aktModell == null)
                {
                    Debug.WriteLine($"Aktie {AktienSymbol} nicht in der AktienListe gefunden, erstelle neue Aktie");

                    // Neue Aktie erstellen mit den aktuellen Daten
                    aktModell = new Aktie
                    {
                        AktienID = AktienListe.Count + 1,
                        AktienSymbol = AktienSymbol.Trim().ToUpper(),
                        AktienName = AktienName,
                        AktuellerPreis = AktuellerKurs,
                        LetzteAktualisierung = DateTime.Now
                    };
                }

                int id = aktModell.AktienID;
                var name = string.IsNullOrEmpty(aktModell.AktienName) ? AktienName : aktModell.AktienName;

                // WICHTIG: Den aktuell angezeigten Kurs verwenden, der von der API geladen wurde
                var kurs = AktuellerKurs;
                var gesamtWert = kurs * Menge;

                Debug.WriteLine($"Transaktion für Aktie: ID={id}, Name={name}, Kurs={kurs}, Gesamtwert={gesamtWert:N2}€");

                if (IsKauf)
                {
                    Debug.WriteLine($"Kaufe {Menge} Aktien von {name} zum Kurs {kurs}");

                    // Zuerst prüfen, ob genug Geld vorhanden ist
                    if (_mainViewModel.Kontostand < gesamtWert)
                    {
                        MessageBox.Show($"Nicht genügend Guthaben für diesen Kauf. Benötigt: {gesamtWert:N2}€, Verfügbar: {_mainViewModel.Kontostand:N2}€",
                            "Unzureichendes Guthaben", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // WICHTIG: Vor dem Kauf Bestätigung einholen mit allen Details
                    var kaufBestätigt = MessageBox.Show(
                        $"Möchten Sie {Menge} Aktien von {name} ({AktienSymbol}) zum Kurs von {kurs:N2}€ pro Aktie kaufen?\n\n" +
                        $"Gesamtbetrag: {gesamtWert:N2}€",
                        "Kauf bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (kaufBestätigt != MessageBoxResult.Yes)
                    {
                        Debug.WriteLine("Kauf wurde vom Benutzer abgebrochen");
                        return;
                    }

                    // Aktie zum Portfolio hinzufügen
                    portfolio.FügeAktieHinzu(id, AktienSymbol.Trim().ToUpper(), name, Menge, kurs, kurs);

                    // Kontostand direkt im MainViewModel aktualisieren
                    _mainViewModel.VerringereKontostand(gesamtWert);

                    // NEU: Prüfen, ob Aktie bereits in Marktdaten vorhanden ist, sonst hinzufügen
                    if (_mainViewModel.MarktdatenViewModel != null)
                    {
                        // Aktie zu Marktdaten hinzufügen, wenn sie noch nicht vorhanden ist
                        bool bereitsInMarktdaten = _mainViewModel.MarktdatenViewModel.AktienListe.Any(
                            a => a.AktienSymbol.Equals(AktienSymbol.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (!bereitsInMarktdaten)
                        {
                            Debug.WriteLine($"Füge Aktie {AktienSymbol} zu Marktdaten hinzu, da sie gekauft wurde");

                            // Neue Aktie für Marktdaten erstellen
                            var neueAktie = new Aktie
                            {
                                AktienID = id,
                                AktienSymbol = AktienSymbol.Trim().ToUpper(),
                                AktienName = name,
                                AktuellerPreis = kurs,
                                Änderung = 0,  // Zunächst keine Änderung
                                ÄnderungProzent = 0,
                                LetzteAktualisierung = DateTime.Now
                            };

                            // Aktie zu Marktdaten hinzufügen - per Dispatcher, da dies eine UI-Operation sein könnte
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _mainViewModel.MarktdatenViewModel.AktienListe.Add(neueAktie);
                            });
                        }
                    }

                    // WICHTIG: PortfolioChartViewModel informieren, dass eine Transaktion stattfand
                    if (_mainViewModel.PortfolioChartViewModel != null)
                    {
                        // Die Änderung wird automatisch durch die PropertyChanged-Events
                        // des PortfolioViewModels erkannt und verarbeitet
                        Debug.WriteLine("Kauf durchgeführt - PortfolioChartViewModel wird aktualisiert");
                    }

                    // Erfolgsmeldung anzeigen
                    MessageBox.Show($"{Menge} Aktien von {name} wurden erfolgreich gekauft für {gesamtWert:N2}€.",
                        "Kauf erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Debug.WriteLine($"Verkaufe {Menge} Aktien von {name} zum Kurs {kurs}");

                    // Prüfen, ob genug Aktien vorhanden sind
                    if (VerfügbareAktien < Menge)
                    {
                        MessageBox.Show($"Nicht genügend Aktien verfügbar. Sie besitzen nur {VerfügbareAktien} Aktien.",
                            "Unzureichender Bestand", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // WICHTIG: Vor dem Verkauf Bestätigung einholen mit allen Details
                    var verkaufBestätigt = MessageBox.Show(
                        $"Möchten Sie {Menge} Aktien von {name} ({AktienSymbol}) zum Kurs von {kurs:N2}€ pro Aktie verkaufen?\n\n" +
                        $"Gesamterlös: {gesamtWert:N2}€",
                        "Verkauf bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (verkaufBestätigt != MessageBoxResult.Yes)
                    {
                        Debug.WriteLine("Verkauf wurde vom Benutzer abgebrochen");
                        return;
                    }

                    // Debug-Ausgabe aller Portfolio-Einträge
                    Debug.WriteLine("Alle Aktien im Portfolio vor dem Verkauf:");
                    foreach (var pe in portfolio.PortfolioEintraege)
                    {
                        Debug.WriteLine($"ID: {pe.AktienID}, Symbol: {pe.AktienSymbol}, Name: {pe.AktienName}, Anzahl: {pe.Anzahl}");
                    }

                    // Verkaufen und prüfen, ob erfolgreich - ANGEPASSTE METHODE VERWENDEN
                    if (portfolio.VerkaufeAktie(id, AktienSymbol.Trim().ToUpper(), Menge))
                    {
                        // Kontostand direkt im MainViewModel aktualisieren
                        _mainViewModel.ErhöheKontostand(gesamtWert);

                        // WICHTIG: PortfolioChartViewModel informieren, dass eine Transaktion stattfand
                        if (_mainViewModel.PortfolioChartViewModel != null)
                        {
                            // Die Änderung wird automatisch durch die PropertyChanged-Events
                            // des PortfolioViewModels erkannt und verarbeitet
                            Debug.WriteLine("Verkauf durchgeführt - PortfolioChartViewModel wird aktualisiert");
                        }

                        // Erfolgsmeldung anzeigen
                        MessageBox.Show($"{Menge} Aktien von {name} wurden erfolgreich verkauft für {gesamtWert:N2}€.",
                            "Verkauf erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        Debug.WriteLine("Verkauf fehlgeschlagen");
                        MessageBox.Show("Der Verkauf konnte nicht durchgeführt werden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Zurücksetzen der Eingabefelder nach erfolgreicher Transaktion
                AktienSymbol = string.Empty;
                SelectedAktie = null;
                Menge = 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Transaktion: {ex.Message}");
                MessageBox.Show($"Bei der Transaktion ist ein Fehler aufgetreten: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Erhöht die Menge um 1
        /// </summary>
        private void IncrementMenge()
        {
            Menge += 1;
        }

        /// <summary>
        /// Verringert die Menge um 1 (nicht unter 1)
        /// </summary>
        private void DecrementMenge()
        {
            if (Menge > 1)
                Menge -= 1;
        }

        /// <summary>
        /// Prüft, ob eine Transaktion durchgeführt werden kann
        /// </summary>
        private bool KannTransaktionAusführen()
        {
            bool kannAusführen = !string.IsNullOrWhiteSpace(AktienSymbol) && AktuellerKurs > 0 && Menge > 0;

            if (kannAusführen)
            {
                if (IsKauf)
                {
                    // Beim Kauf prüfen, ob genug Geld vorhanden ist
                    kannAusführen = Gesamtwert <= AktuellerKontostand;
                }
                else
                {
                    // Beim Verkauf prüfen, ob genug Aktien vorhanden sind
                    kannAusführen = Menge <= VerfügbareAktien;
                }
            }

            Debug.WriteLine($"KannTransaktionAusführen: {kannAusführen}, AktienSymbol: {AktienSymbol}, Kurs: {AktuellerKurs}, Menge: {Menge}");
            return kannAusführen;
        }
        #endregion
    }
}