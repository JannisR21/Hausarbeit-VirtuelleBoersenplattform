using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Models;
using HausarbeitVirtuelleBörsenplattform.Helpers;

namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    /// <summary>
    /// ViewModel für die Filterung von Aktien nach verschiedenen Kriterien
    /// </summary>
    public class AktienFilterViewModel : ObservableObject
    {
        #region Private Felder
        private ObservableCollection<Aktie> _ungefilterte​Aktien;
        private ObservableCollection<Aktie> _gefilterteAktien;
        private ObservableCollection<ETF> _verfügbareETFs;
        private AnlageTyp _ausgewählterAnlageTyp = AnlageTyp.EINZELAKTIE;
        private AktienBranche? _ausgewählteBranche = null;
        private ETFTyp? _ausgewählterETFTyp = null;
        private string _suchText = string.Empty;
        private bool _istETFAusschüttend = false;
        private bool _filterAusschüttungAktiv = false;
        private decimal _minPreis = 0;
        private decimal _maxPreis = 10000;
        private bool _preisfilterAktiv = false;
        private bool _istFilterAktiv = false;
        private Dictionary<AnlageTyp, string> _verfügbareAnlageTypen;
        private Dictionary<AktienBranche, string> _verfügbareBranchen;
        private Dictionary<ETFTyp, string> _verfügbareETFTypen;
        // Flag, um rekursive Aufrufe zu verhindern
        private bool _isFilteringInProgress = false;
        #endregion

        #region Events
        // Event für Filteränderungen
        public event EventHandler<FilterChangedEventArgs> FilterChanged;
        #endregion

        #region Konstruktor
        /// <summary>
        /// Initialisiert eine neue Instanz des AktienFilterViewModel
        /// </summary>
        public AktienFilterViewModel()
        {
            // Listen initialisieren
            _ungefilterte​Aktien = new ObservableCollection<Aktie>();
            _gefilterteAktien = new ObservableCollection<Aktie>();
            _verfügbareETFs = new ObservableCollection<ETF>();

            // Anlagetypen, Branchen und ETF-Typen initialisieren
            InitialisiereWertelisten();

            // Commands initialisieren
            FilterZurücksetzenCommand = new RelayCommand(FilterZurücksetzen);
            FilterAnwendenCommand = new RelayCommand(FilterAnwenden);
            LadeVerfügbareAnlagenCommand = new AsyncRelayCommand(LadeVerfügbareAnlagenAsync);

            // Standard-ETFs laden
            LadeVerfügbareETFs();

            Debug.WriteLine("AktienFilterViewModel wurde initialisiert");

            // Verzögerte Initialisierung der Beispiel-Aktien (falls keine geliefert werden)
            Task.Delay(500).ContinueWith(_ =>
            {
                if (Ungefilterte​Aktien.Count == 0)
                {
                    InitialisiereBeispielAktien();
                }
            });
        }
        #endregion

        #region Properties
        /// <summary>
        /// Liste aller ungefilterten Aktien
        /// </summary>
        public ObservableCollection<Aktie> Ungefilterte​Aktien
        {
            get => _ungefilterte​Aktien;
            set => SetProperty(ref _ungefilterte​Aktien, value);
        }

        /// <summary>
        /// Liste der gefilterten Aktien nach aktuellen Filterkriterien
        /// </summary>
        public ObservableCollection<Aktie> GefilterteAktien
        {
            get => _gefilterteAktien;
            set => SetProperty(ref _gefilterteAktien, value);
        }

        /// <summary>
        /// Liste der verfügbaren ETFs
        /// </summary>
        public ObservableCollection<ETF> VerfügbareETFs
        {
            get => _verfügbareETFs;
            set => SetProperty(ref _verfügbareETFs, value);
        }

        /// <summary>
        /// Der ausgewählte Anlagetyp (Aktie, ETF, etc.)
        /// </summary>
        public AnlageTyp AusgewählterAnlageTyp
        {
            get => _ausgewählterAnlageTyp;
            set
            {
                // Überprüfung auf rekursiven Aufruf hinzugefügt
                if (_isFilteringInProgress)
                {
                    Debug.WriteLine("Rekursiver Aufruf in AusgewählterAnlageTyp erkannt und verhindert");
                    return;
                }

                if (SetProperty(ref _ausgewählterAnlageTyp, value) && !_isFilteringInProgress)
                {
                    Debug.WriteLine($"AnlageTyp geändert auf: {value}");
                    OnPropertyChanged(nameof(ZeigeBranchenFilter));
                    OnPropertyChanged(nameof(ZeigeETFFilter));
                    FilterAnwenden();
                }
            }
        }

        /// <summary>
        /// Die ausgewählte Branche für die Aktienfilterung
        /// </summary>
        public AktienBranche? AusgewählteBranche
        {
            get => _ausgewählteBranche;
            set
            {
                // Überprüfung auf rekursiven Aufruf hinzugefügt
                if (_isFilteringInProgress)
                {
                    Debug.WriteLine("Rekursiver Aufruf in AusgewählteBranche erkannt und verhindert");
                    return;
                }

                if (SetProperty(ref _ausgewählteBranche, value) && !_isFilteringInProgress)
                {
                    Debug.WriteLine($"Branche geändert auf: {value}");
                    FilterAnwenden();
                }
            }
        }

        /// <summary>
        /// Der ausgewählte ETF-Typ für die ETF-Filterung
        /// </summary>
        public ETFTyp? AusgewählterETFTyp
        {
            get => _ausgewählterETFTyp;
            set
            {
                // Überprüfung auf rekursiven Aufruf hinzugefügt
                if (_isFilteringInProgress)
                {
                    Debug.WriteLine("Rekursiver Aufruf in AusgewählterETFTyp erkannt und verhindert");
                    return;
                }

                if (SetProperty(ref _ausgewählterETFTyp, value) && !_isFilteringInProgress)
                {
                    Debug.WriteLine($"ETF-Typ geändert auf: {value}");
                    FilterAnwenden();
                }
            }
        }

        /// <summary>
        /// Suchtext zur Filterung nach Namen oder Symbol
        /// </summary>
        public string SuchText
        {
            get => _suchText;
            set
            {
                // Überprüfung auf rekursiven Aufruf hinzugefügt
                if (_isFilteringInProgress)
                {
                    Debug.WriteLine("Rekursiver Aufruf in SuchText erkannt und verhindert");
                    return;
                }

                if (SetProperty(ref _suchText, value) && !_isFilteringInProgress)
                {
                    FilterAnwenden();
                }
            }
        }

        /// <summary>
        /// Gibt an, ob der ETF-Filter für Ausschüttung aktiviert ist
        /// </summary>
        public bool FilterAusschüttungAktiv
        {
            get => _filterAusschüttungAktiv;
            set
            {
                // Überprüfung auf rekursiven Aufruf hinzugefügt
                if (_isFilteringInProgress)
                {
                    Debug.WriteLine("Rekursiver Aufruf in FilterAusschüttungAktiv erkannt und verhindert");
                    return;
                }

                if (SetProperty(ref _filterAusschüttungAktiv, value) && !_isFilteringInProgress)
                {
                    FilterAnwenden();
                }
            }
        }

        /// <summary>
        /// Gibt an, ob nur ausschüttende ETFs angezeigt werden sollen
        /// </summary>
        public bool IstETFAusschüttend
        {
            get => _istETFAusschüttend;
            set
            {
                // Überprüfung auf rekursiven Aufruf hinzugefügt
                if (_isFilteringInProgress)
                {
                    Debug.WriteLine("Rekursiver Aufruf in IstETFAusschüttend erkannt und verhindert");
                    return;
                }

                if (SetProperty(ref _istETFAusschüttend, value) && !_isFilteringInProgress)
                {
                    FilterAnwenden();
                }
            }
        }

        /// <summary>
        /// Minimaler Preis für die Preisfilterung
        /// </summary>
        public decimal MinPreis
        {
            get => _minPreis;
            set
            {
                // Überprüfung auf rekursiven Aufruf hinzugefügt
                if (_isFilteringInProgress)
                {
                    Debug.WriteLine("Rekursiver Aufruf in MinPreis erkannt und verhindert");
                    return;
                }

                if (SetProperty(ref _minPreis, value) && _preisfilterAktiv && !_isFilteringInProgress)
                {
                    FilterAnwenden();
                }
            }
        }

        /// <summary>
        /// Maximaler Preis für die Preisfilterung
        /// </summary>
        public decimal MaxPreis
        {
            get => _maxPreis;
            set
            {
                // Überprüfung auf rekursiven Aufruf hinzugefügt
                if (_isFilteringInProgress)
                {
                    Debug.WriteLine("Rekursiver Aufruf in MaxPreis erkannt und verhindert");
                    return;
                }

                if (SetProperty(ref _maxPreis, value) && _preisfilterAktiv && !_isFilteringInProgress)
                {
                    FilterAnwenden();
                }
            }
        }

        /// <summary>
        /// Gibt an, ob der Preisfilter aktiviert ist
        /// </summary>
        public bool PreisfilterAktiv
        {
            get => _preisfilterAktiv;
            set
            {
                // Überprüfung auf rekursiven Aufruf hinzugefügt
                if (_isFilteringInProgress)
                {
                    Debug.WriteLine("Rekursiver Aufruf in PreisfilterAktiv erkannt und verhindert");
                    return;
                }

                if (SetProperty(ref _preisfilterAktiv, value) && !_isFilteringInProgress)
                {
                    FilterAnwenden();
                }
            }
        }

        /// <summary>
        /// Gibt an, ob momentan mindestens ein Filter aktiv ist
        /// </summary>
        public bool IstFilterAktiv
        {
            get => _istFilterAktiv;
            private set => SetProperty(ref _istFilterAktiv, value);
        }

        /// <summary>
        /// Gibt an, ob der Branchenfilter angezeigt werden soll (nur für Einzelaktien)
        /// </summary>
        public bool ZeigeBranchenFilter => AusgewählterAnlageTyp == AnlageTyp.EINZELAKTIE;

        /// <summary>
        /// Gibt an, ob der ETF-Filter angezeigt werden soll (nur für ETFs)
        /// </summary>
        public bool ZeigeETFFilter => AusgewählterAnlageTyp == AnlageTyp.ETF;

        /// <summary>
        /// Gibt alle verfügbaren Anlagetypen für eine ComboBox zurück
        /// </summary>
        public Dictionary<AnlageTyp, string> VerfügbareAnlageTypen
        {
            get => _verfügbareAnlageTypen;
            private set => SetProperty(ref _verfügbareAnlageTypen, value);
        }

        /// <summary>
        /// Gibt alle verfügbaren Branchen für eine ComboBox zurück
        /// </summary>
        public Dictionary<AktienBranche, string> VerfügbareBranchen
        {
            get => _verfügbareBranchen;
            private set => SetProperty(ref _verfügbareBranchen, value);
        }

        /// <summary>
        /// Gibt alle verfügbaren ETF-Typen für eine ComboBox zurück
        /// </summary>
        public Dictionary<ETFTyp, string> VerfügbareETFTypen
        {
            get => _verfügbareETFTypen;
            private set => SetProperty(ref _verfügbareETFTypen, value);
        }
        #endregion

        #region Commands
        /// <summary>
        /// Command zum Zurücksetzen der Filter
        /// </summary>
        public IRelayCommand FilterZurücksetzenCommand { get; }

        /// <summary>
        /// Command zum Anwenden der Filter
        /// </summary>
        public IRelayCommand FilterAnwendenCommand { get; }

        /// <summary>
        /// Command zum Laden der verfügbaren Anlagen
        /// </summary>
        public IAsyncRelayCommand LadeVerfügbareAnlagenCommand { get; }
        #endregion

        #region Methoden
        /// <summary>
        /// Löst das FilterChanged-Event aus
        /// </summary>
        protected virtual void OnFilterChanged()
        {
            try
            {
                var handler = FilterChanged;
                if (handler != null)
                {
                    var args = new FilterChangedEventArgs(GefilterteAktien, SuchText, IstFilterAktiv);
                    Debug.WriteLine($"Löse FilterChanged-Event aus: {GefilterteAktien.Count} Aktien, SuchText='{SuchText}', FilterAktiv={IstFilterAktiv}");
                    handler(this, args);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Auslösen des FilterChanged-Events: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialisiert die Listen für Anlagetypen, Branchen und ETF-Typen
        /// </summary>
        private void InitialisiereWertelisten()
        {
            // Anlagetypen initialisieren
            _verfügbareAnlageTypen = new Dictionary<AnlageTyp, string>
            {
                { AnlageTyp.EINZELAKTIE, "Einzelaktie" },
                { AnlageTyp.ETF, "ETF" },
                { AnlageTyp.FOND, "Investmentfonds" },
                { AnlageTyp.ANLEIHE, "Anleihe" },
                { AnlageTyp.ZERTIFIKAT, "Zertifikat" }
            };

            // Branchen initialisieren
            _verfügbareBranchen = new Dictionary<AktienBranche, string>
            {
                { AktienBranche.TECHNOLOGIE, "Technologie" },
                { AktienBranche.FINANZEN, "Finanzen" },
                { AktienBranche.GESUNDHEIT, "Gesundheit" },
                { AktienBranche.KONSUMGÜTER, "Konsumgüter" },
                { AktienBranche.ENERGIE, "Energie" },
                { AktienBranche.INDUSTRIE, "Industrie" },
                { AktienBranche.TELEKOMMUNIKATION, "Telekommunikation" },
                { AktienBranche.GRUNDSTOFFE, "Grundstoffe" },
                { AktienBranche.VERSORGUNG, "Versorgung" },
                { AktienBranche.IMMOBILIEN, "Immobilien" }
            };

            // ETF-Typen initialisieren
            _verfügbareETFTypen = new Dictionary<ETFTyp, string>
            {
                { ETFTyp.AKTIEN, "Aktien-ETF" },
                { ETFTyp.ANLEIHEN, "Anleihen-ETF" },
                { ETFTyp.ROHSTOFFE, "Rohstoff-ETF" },
                { ETFTyp.IMMOBILIEN, "Immobilien-ETF" },
                { ETFTyp.MULTI_ASSET, "Multi-Asset-ETF" },
                { ETFTyp.GELDMARKT, "Geldmarkt-ETF" }
            };

            // Properties-Changed-Benachrichtigung auslösen
            OnPropertyChanged(nameof(VerfügbareAnlageTypen));
            OnPropertyChanged(nameof(VerfügbareBranchen));
            OnPropertyChanged(nameof(VerfügbareETFTypen));
        }

        /// <summary>
        /// Initialisiert Beispiel-Aktien, falls keine von außen geliefert werden
        /// </summary>
        private void InitialisiereBeispielAktien()
        {
            try
            {
                if (Ungefilterte​Aktien.Count == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Beispiel-Aktien hinzufügen
                        Ungefilterte​Aktien.Add(new Aktie { AktienID = 1, AktienSymbol = "AAPL", AktienName = "Apple Inc.", AktuellerPreis = 180.95m });
                        Ungefilterte​Aktien.Add(new Aktie { AktienID = 2, AktienSymbol = "MSFT", AktienName = "Microsoft Corporation", AktuellerPreis = 370.50m });
                        Ungefilterte​Aktien.Add(new Aktie { AktienID = 3, AktienSymbol = "GOOGL", AktienName = "Alphabet Inc.", AktuellerPreis = 139.80m });
                        Ungefilterte​Aktien.Add(new Aktie { AktienID = 4, AktienSymbol = "AMZN", AktienName = "Amazon.com Inc.", AktuellerPreis = 142.75m });
                        Ungefilterte​Aktien.Add(new Aktie { AktienID = 5, AktienSymbol = "META", AktienName = "Meta Platforms Inc.", AktuellerPreis = 466.20m });

                        Debug.WriteLine($"Beispiel-Aktien initialisiert: {Ungefilterte​Aktien.Count} Einträge");
                    });

                    // Filter anwenden, um gefilterte Liste zu aktualisieren
                    FilterAnwenden();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Initialisieren der Beispiel-Aktien: {ex.Message}");
            }
        }

        /// <summary>
        /// Setzt alle Filter zurück
        /// </summary>
        private void FilterZurücksetzen()
        {
            // Verhindere rekursive Aufrufe während des Zurücksetzens
            if (_isFilteringInProgress)
            {
                Debug.WriteLine("FilterZurücksetzen wird während FilterAnwenden aufgerufen, überspringe");
                return;
            }

            _isFilteringInProgress = true;

            try
            {
                AusgewählterAnlageTyp = AnlageTyp.EINZELAKTIE;
                AusgewählteBranche = null;
                AusgewählterETFTyp = null;
                SuchText = string.Empty;
                FilterAusschüttungAktiv = false;
                IstETFAusschüttend = false;
                PreisfilterAktiv = false;
                MinPreis = 0;
                MaxPreis = 10000;

                IstFilterAktiv = false;

                // Gefilterte Liste zurücksetzen
                GefilterteAktien.Clear();
                foreach (var aktie in Ungefilterte​Aktien)
                {
                    GefilterteAktien.Add(aktie);
                }

                Debug.WriteLine("Filter wurden zurückgesetzt");
                OnPropertyChanged(nameof(GefilterteAktien));

                // Event auslösen
                OnFilterChanged();
            }
            finally
            {
                _isFilteringInProgress = false;
            }
        }

        /// <summary>
        /// Wendet die aktuellen Filter auf die Aktien/ETFs an
        /// </summary>
        private void FilterAnwenden()
        {
            // Verbesserter Schutz vor rekursiven Aufrufen
            if (_isFilteringInProgress)
            {
                Debug.WriteLine("FilterAnwenden wird bereits ausgeführt, überspringe rekursiven Aufruf");
                return;
            }

            try
            {
                _isFilteringInProgress = true;
                Debug.WriteLine("Wende Filter an...");

                // Sicherstellen, dass wir auf dem UI-Thread sind
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        GefilterteAktien.Clear();

                        // Prüfen, ob Filter aktiv sind
                        bool hatAktiveFilter = !string.IsNullOrWhiteSpace(SuchText) ||
                                            AusgewählteBranche != null ||
                                            AusgewählterETFTyp != null ||
                                            (FilterAusschüttungAktiv && AusgewählterAnlageTyp == AnlageTyp.ETF) ||
                                            PreisfilterAktiv;

                        IstFilterAktiv = hatAktiveFilter;

                        // Quellliste abhängig vom ausgewählten Anlagetyp
                        IEnumerable<Aktie> quellListe;

                        switch (AusgewählterAnlageTyp)
                        {
                            case AnlageTyp.ETF:
                                quellListe = VerfügbareETFs;
                                break;
                            default:
                                quellListe = Ungefilterte​Aktien
                                    .Where(a => !(a is ETF)); // Nur Nicht-ETFs für Einzelaktien
                                break;
                        }

                        // Wenn Quellliste leer ist, nichts weiter tun
                        if (!quellListe.Any())
                        {
                            Debug.WriteLine("Quellliste ist leer");
                            return;
                        }

                        // Filtern nach Suchtext
                        if (!string.IsNullOrWhiteSpace(SuchText))
                        {
                            string suchbegriff = SuchText.ToLower().Trim();
                            quellListe = quellListe.Where(a =>
                                (a.AktienName?.ToLower()?.Contains(suchbegriff) ?? false) ||
                                (a.AktienSymbol?.ToLower()?.Contains(suchbegriff) ?? false));
                        }

                        // Filtern nach Branche (nur für Einzelaktien)
                        if (AusgewählteBranche != null && AusgewählterAnlageTyp == AnlageTyp.EINZELAKTIE)
                        {
                            quellListe = quellListe.Where(a =>
                                AktienBrancheHelper.GetBrancheForSymbol(a.AktienSymbol) == AusgewählteBranche);
                        }

                        // Filtern nach ETF-Typ (nur für ETFs)
                        if (AusgewählterETFTyp != null && AusgewählterAnlageTyp == AnlageTyp.ETF)
                        {
                            quellListe = quellListe.OfType<ETF>()
                                .Where(etf => etf.ETFTyp == AusgewählterETFTyp);
                        }

                        // Filtern nach Ausschüttung (nur für ETFs)
                        if (FilterAusschüttungAktiv && AusgewählterAnlageTyp == AnlageTyp.ETF)
                        {
                            quellListe = quellListe.OfType<ETF>()
                                .Where(etf => etf.IstAusschüttend == IstETFAusschüttend);
                        }

                        // Filtern nach Preis
                        if (PreisfilterAktiv)
                        {
                            quellListe = quellListe.Where(a =>
                                a.AktuellerPreis >= MinPreis && a.AktuellerPreis <= MaxPreis);
                        }

                        // Ergebnisse in die gefilterte Liste übertragen
                        foreach (var aktie in quellListe)
                        {
                            GefilterteAktien.Add(aktie);
                        }

                        Debug.WriteLine($"Filter angewendet, {GefilterteAktien.Count} Ergebnisse gefunden");
                        OnPropertyChanged(nameof(GefilterteAktien));

                        // Event für gefilterte Aktien auslösen
                        OnFilterChanged();
                    });
                }
                else
                {
                    Debug.WriteLine("Application.Current ist null - kann Filter nicht anwenden");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Anwenden des Filters: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                // Wichtig: Flag zurücksetzen IMMER im finally-Block
                _isFilteringInProgress = false;
            }
        }

        /// <summary>
        /// Lädt verfügbare ETFs
        /// </summary>
        private void LadeVerfügbareETFs()
        {
            try
            {
                VerfügbareETFs.Clear();
                var etfs = BekannteBörsenETFs.GetBekannteBörsenETFs();

                foreach (var etf in etfs)
                {
                    VerfügbareETFs.Add(etf);

                    // ETFs auch in Gesamtliste aufnehmen, falls nicht bereits vorhanden
                    if (!Ungefilterte​Aktien.Any(a => a.AktienSymbol == etf.AktienSymbol))
                    {
                        Ungefilterte​Aktien.Add(etf);
                    }
                }

                Debug.WriteLine($"{VerfügbareETFs.Count} ETFs geladen");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der ETFs: {ex.Message}");
            }
        }

        /// <summary>
        /// Lädt alle verfügbaren Anlagen aus externen Quellen
        /// </summary>
        private async Task LadeVerfügbareAnlagenAsync()
        {
            Debug.WriteLine("Lade verfügbare Anlagen...");

            try
            {
                // Hier könnten weitere Anlagen aus der API oder Datenbank geladen werden
                // Z.B. ETFs, Fonds, Anleihen, etc.

                // Beispiel für das Laden von Aktien über die TwelveData API:
                if (App.TwelveDataService != null)
                {
                    // Liste bekannter Aktien-Symbole, die geladen werden sollen
                    var bekannteSymbole = new List<string> {
                        "AAPL", "MSFT", "AMZN", "GOOGL", "META", "TSLA", "BRK.B",
                        "V", "JPM", "JNJ", "WMT", "PG", "MA", "UNH", "HD", "BAC"
                    };

                    // Aktien über die API laden
                    var aktien = await App.TwelveDataService.HoleAktienKurse(bekannteSymbole);

                    if (aktien != null && aktien.Count > 0)
                    {
                        foreach (var aktie in aktien)
                        {
                            // UI-Thread verwenden für Collection-Änderungen
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // Prüfen, ob die Aktie bereits in der Liste ist
                                if (!Ungefilterte​Aktien.Any(a => a.AktienSymbol == aktie.AktienSymbol))
                                {
                                    Ungefilterte​Aktien.Add(aktie);
                                }
                                else
                                {
                                    // Aktualisiere bestehende Aktie
                                    var existingAktie = Ungefilterte​Aktien.FirstOrDefault(a => a.AktienSymbol == aktie.AktienSymbol);
                                    if (existingAktie != null)
                                    {
                                        existingAktie.AktuellerPreis = aktie.AktuellerPreis;
                                        existingAktie.Änderung = aktie.Änderung;
                                        existingAktie.ÄnderungProzent = aktie.ÄnderungProzent;
                                        existingAktie.LetzteAktualisierung = aktie.LetzteAktualisierung;
                                    }
                                }
                            });
                        }

                        Debug.WriteLine($"{aktien.Count} Aktien über API geladen");
                    }
                }

                // Filter anwenden
                FilterAnwenden();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Anlagen: {ex.Message}");
            }
        }

        /// <summary>
        /// Setzt die Liste der ungefilterten Aktien
        /// </summary>
        public void SetzeAktienListe(ObservableCollection<Aktie> aktien)
        {
            if (aktien == null)
                return;

            // Verhindere rekursive Aufrufe bei Aktualisierung
            if (_isFilteringInProgress)
            {
                Debug.WriteLine("SetzeAktienListe während FilterAnwenden aufgerufen, überspringe");
                return;
            }

            try
            {
                _isFilteringInProgress = true;

                // UI-Thread verwenden für Collection-Änderungen
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Ungefilterte​Aktien.Clear();
                    foreach (var aktie in aktien)
                    {
                        Ungefilterte​Aktien.Add(aktie);
                    }

                    // ETFs nicht vergessen
                    foreach (var etf in VerfügbareETFs)
                    {
                        if (!Ungefilterte​Aktien.Any(a => a.AktienSymbol == etf.AktienSymbol))
                        {
                            Ungefilterte​Aktien.Add(etf);
                        }
                    }

                    Debug.WriteLine($"Aktienliste gesetzt mit {Ungefilterte​Aktien.Count} Einträgen");
                });
            }
            finally
            {
                _isFilteringInProgress = false;

                // Jetzt separat die Filter anwenden außerhalb des _isFilteringInProgress-Blocks
                Debug.WriteLine("Aktienlistenaktualisierung abgeschlossen, wende Filter an");
                FilterAnwenden();
            }
        }

        /// <summary>
        /// Aktualisiert die Preisinformationen aller Aktien
        /// </summary>
        public void AktualisierePreise(ObservableCollection<Aktie> aktualisierteAktien)
        {
            if (aktualisierteAktien == null)
                return;

            // Verhindere rekursive Aufrufe bei Preisaktualisierung
            if (_isFilteringInProgress)
            {
                Debug.WriteLine("AktualisierePreise während FilterAnwenden aufgerufen, überspringe");
                return;
            }

            try
            {
                _isFilteringInProgress = true;

                // UI-Thread verwenden für Collection-Änderungen
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var aktie in aktualisierteAktien)
                    {
                        // Aktie in ungefilterten und gefilterten Aktien suchen und aktualisieren
                        var ungefiltert = Ungefilterte​Aktien.FirstOrDefault(a => a.AktienSymbol == aktie.AktienSymbol);
                        var gefiltert = GefilterteAktien.FirstOrDefault(a => a.AktienSymbol == aktie.AktienSymbol);

                        if (ungefiltert != null)
                        {
                            ungefiltert.AktuellerPreis = aktie.AktuellerPreis;
                            ungefiltert.Änderung = aktie.Änderung;
                            ungefiltert.ÄnderungProzent = aktie.ÄnderungProzent;
                            ungefiltert.LetzteAktualisierung = aktie.LetzteAktualisierung;
                        }

                        if (gefiltert != null)
                        {
                            gefiltert.AktuellerPreis = aktie.AktuellerPreis;
                            gefiltert.Änderung = aktie.Änderung;
                            gefiltert.ÄnderungProzent = aktie.ÄnderungProzent;
                            gefiltert.LetzteAktualisierung = aktie.LetzteAktualisierung;
                        }
                    }

                    Debug.WriteLine("Preise aktualisiert");
                });
            }
            finally
            {
                _isFilteringInProgress = false;

                // Jetzt separat die Filter anwenden außerhalb des _isFilteringInProgress-Blocks
                // Aber nur, wenn Preisfilter aktiv ist
                if (PreisfilterAktiv)
                {
                    Debug.WriteLine("Preisaktualisierung abgeschlossen, Preisfilter aktiv, wende Filter an");
                    FilterAnwenden();
                }
            }
        }
        #endregion
    }
}