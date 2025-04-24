using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        private Aktie _selectedAktie; // Neue Property für ausgewählte Aktie
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

            // Commands initialisieren
            AktienSuchenCommand = new RelayCommand(AktienSuchen);
            TransaktionAusführenCommand = new RelayCommand(TransaktionAusführen, KannTransaktionAusführen);
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

            // Aktien-Liste initialisieren
            _aktienListe = new ObservableCollection<Aktie>();

            // Listener für SelectedAktie-Änderungen
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedAktie) && SelectedAktie != null)
                {
                    // Wenn eine Aktie ausgewählt wird, übernehmen wir die Daten
                    AktienSymbol = SelectedAktie.AktienSymbol;
                    AktienName = SelectedAktie.AktienName;

                    // Hier verwenden wir die aktuellen Marktdaten, falls verfügbar
                    if (_mainViewModel?.MarktdatenViewModel?.AktienListe != null)
                    {
                        var aktieAusMarktdaten = _mainViewModel.MarktdatenViewModel.AktienListe
                            .FirstOrDefault(a => a.AktienSymbol.Equals(AktienSymbol, StringComparison.OrdinalIgnoreCase));

                        if (aktieAusMarktdaten != null && aktieAusMarktdaten.AktuellerPreis > 0)
                        {
                            // Verwende den aktuellen Marktpreis
                            Debug.WriteLine($"Aktienhandel: Verwende aktuellen Marktpreis für {AktienSymbol}: {aktieAusMarktdaten.AktuellerPreis:F2}€");
                            AktuellerKurs = aktieAusMarktdaten.AktuellerPreis;
                        }
                        else
                        {
                            // Fallback auf den Preis aus der ComboBox-Aktie
                            AktuellerKurs = SelectedAktie.AktuellerPreis;
                        }
                    }
                    else
                    {
                        // Fallback auf den Preis aus der ComboBox-Aktie wenn keine Marktdaten verfügbar
                        AktuellerKurs = SelectedAktie.AktuellerPreis;
                    }

                    BerechneGesamtwert();
                    PrüfeTransaktionsDurchführbarkeit();
                }
            };

            // Wenn App.StandardAktien vorhanden ist, verwenden wir diese
            if (App.StandardAktien != null && App.StandardAktien.Count > 0)
            {
                InitializeWithAktien(App.StandardAktien);
            }

            // Listener für Änderungen an MainViewModel registrieren
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
                // Aktien-Liste aktualisieren, wenn sich die Marktdaten ändern
                SynchronisiereAktienListe();

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
        /// Aktualisiert die lokale AktienListe aus dem MarktdatenViewModel
        /// </summary>
        private void SynchronisiereAktienListe()
        {
            if (_mainViewModel?.MarktdatenViewModel?.AktienListe == null ||
                _mainViewModel.MarktdatenViewModel.AktienListe.Count == 0)
            {
                Debug.WriteLine("Keine Aktien im MarktdatenViewModel vorhanden.");
                return;
            }

            Debug.WriteLine("Synchronisiere AktienListe im AktienhandelViewModel");

            // UI-Thread verwenden
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Vorhandene Elemente löschen, aber nur wenn wir neue haben
                    var marktdatenAktien = _mainViewModel.MarktdatenViewModel.AktienListe;
                    if (marktdatenAktien.Count > 0)
                    {
                        Debug.WriteLine($"Aktuelle AktienListe: {_aktienListe.Count} Einträge, MarktdatenViewModel hat {marktdatenAktien.Count} Einträge");

                        _aktienListe.Clear();

                        // Neue Elemente hinzufügen
                        foreach (var aktie in marktdatenAktien)
                        {
                            _aktienListe.Add(aktie);
                        }

                        Debug.WriteLine($"AktienListe aktualisiert, jetzt {_aktienListe.Count} Einträge");

                        // Benachrichtigung auslösen
                        OnPropertyChanged(nameof(AktienListe));
                    }
                    else
                    {
                        Debug.WriteLine("MarktdatenViewModel hat keine Aktien. Behalte vorhandene Liste.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler bei der Aktualisierung der AktienListe: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Initialisiert die AktienListe mit den übergebenen Aktien
        /// </summary>
        /// <param name="aktien">Die Aktien, mit denen die Liste gefüllt werden soll</param>
        public void InitializeWithAktien(ObservableCollection<Aktie> aktien)
        {
            if (aktien == null || aktien.Count == 0)
            {
                Debug.WriteLine("Keine Aktien zum Initialisieren übergeben");
                return;
            }

            Debug.WriteLine($"Initialisiere AktienListe mit {aktien.Count} Aktien");

            // UI-Thread verwenden
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Liste leeren
                    _aktienListe.Clear();

                    // Neue Aktien hinzufügen
                    foreach (var aktie in aktien)
                    {
                        _aktienListe.Add(aktie);
                    }

                    Debug.WriteLine($"AktienListe erfolgreich mit {_aktienListe.Count} Aktien initialisiert");

                    // Benachrichtigung auslösen
                    OnPropertyChanged(nameof(AktienListe));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Initialisieren der AktienListe: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Methode zum nachträglichen Setzen des MainViewModel
        /// </summary>
        public void SetMainViewModel(MainViewModel mainViewModel)
        {
            Debug.WriteLine("SetMainViewModel aufgerufen");

            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            // Aktien-Liste neu laden
            if (_mainViewModel.MarktdatenViewModel != null)
            {
                SynchronisiereAktienListe();
                // Für zukünftige Änderungen registrieren
                _mainViewModel.MarktdatenViewModel.PropertyChanged += MarktdatenViewModel_PropertyChanged;
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Verfügbare Aktien zum Auswählen
        /// </summary>
        public ObservableCollection<Aktie> AktienListe
        {
            get => _aktienListe;
            private set => SetProperty(ref _aktienListe, value);
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
                        else
                        {
                            // Manuelles Laden der Aktiendaten
                            LadeAktienDaten();
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
        public IRelayCommand AktienSuchenCommand { get; }

        /// <summary>
        /// Command zum Ausführen der Transaktion
        /// </summary>
        public IRelayCommand TransaktionAusführenCommand { get; }

        /// <summary>
        /// Command zum Erhöhen der Menge
        /// </summary>
        public IRelayCommand IncrementMengeCommand { get; }

        /// <summary>
        /// Command zum Verringern der Menge
        /// </summary>
        public IRelayCommand DecrementMengeCommand { get; }
        #endregion

        #region Private Methoden
        /// <summary>
        /// Lädt Daten für die ausgewählte Aktie
        /// </summary>
        private void LadeAktienDaten()
        {
            if (string.IsNullOrWhiteSpace(AktienSymbol))
            {
                AktienName = string.Empty;
                AktuellerKurs = 0;
                return;
            }

            Debug.WriteLine($"Lade Daten für Aktie: {AktienSymbol}");

            // Zuerst in Marktdaten suchen (höchste Priorität für aktuelle Kurse)
            Aktie aktie = null;
            if (_mainViewModel?.MarktdatenViewModel?.AktienListe != null)
            {
                aktie = _mainViewModel.MarktdatenViewModel.AktienListe
                    .FirstOrDefault(a => a.AktienSymbol.Equals(AktienSymbol.Trim(), StringComparison.OrdinalIgnoreCase));

                if (aktie != null)
                {
                    Debug.WriteLine($"Aktie in Marktdaten gefunden: {aktie.AktienSymbol} - {aktie.AktienName} - Kurs: {aktie.AktuellerPreis}");
                }
            }

            // Wenn nicht in Marktdaten, dann in lokaler AktienListe suchen
            if (aktie == null)
            {
                Debug.WriteLine($"AktienListe enthält {AktienListe.Count} Einträge");

                aktie = AktienListe
                    .FirstOrDefault(a => a.AktienSymbol
                        .Equals(AktienSymbol.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            // Fallback auf App.StandardAktien
            if (aktie == null && App.StandardAktien != null)
            {
                aktie = App.StandardAktien
                    .FirstOrDefault(a => a.AktienSymbol
                        .Equals(AktienSymbol.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (aktie != null)
            {
                Debug.WriteLine($"Aktie gefunden: {aktie.AktienSymbol} - {aktie.AktienName} - Kurs: {aktie.AktuellerPreis}");
                AktienName = aktie.AktienName;
                AktuellerKurs = aktie.AktuellerPreis;

                // Explizit den Gesamtwert neu berechnen
                BerechneGesamtwert();

                // CanExecuteChanged explizit auslösen
                TransaktionAusführenCommand.NotifyCanExecuteChanged();

                OnPropertyChanged(nameof(VerfügbareAktien));
                OnPropertyChanged(nameof(IsAktieAusgewählt));

                // Aktualisiere auch SelectedAktie, wenn sie sich geändert hat
                if (SelectedAktie == null || !SelectedAktie.AktienSymbol.Equals(AktienSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    SelectedAktie = aktie;
                }
            }
            else
            {
                Debug.WriteLine($"Aktie nicht gefunden: {AktienSymbol}");
                AktienName = "Aktie nicht gefunden";
                AktuellerKurs = 0;

                // Sicherstellen, dass Commands aktualisiert werden
                BerechneGesamtwert();
                TransaktionAusführenCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(IsAktieAusgewählt));
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
        /// Sucht die Aktie anhand des eingegebenen Symbols
        /// </summary>
        private void AktienSuchen()
        {
            Debug.WriteLine($"AktienSuchenCommand aufgerufen für Symbol: {AktienSymbol}");
            if (string.IsNullOrWhiteSpace(AktienSymbol))
            {
                Debug.WriteLine("Kein Symbol eingegeben");
                return;
            }

            // Hier explizit LadeAktienDaten aufrufen
            LadeAktienDaten();

            // Wenn keine Aktie gefunden wurde, eine Meldung anzeigen
            if (AktuellerKurs <= 0)
            {
                MessageBox.Show($"Die Aktie mit dem Symbol '{AktienSymbol}' wurde nicht gefunden.",
                    "Aktie nicht gefunden",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
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
        /// Führt die Transaktion (Kauf oder Verkauf) durch
        /// </summary>
        private void TransaktionAusführen()
        {
            Debug.WriteLine($"TransaktionAusführenCommand aufgerufen: {(IsKauf ? "Kauf" : "Verkauf")} von {Menge} {AktienSymbol}");

            if (string.IsNullOrWhiteSpace(AktienSymbol) || AktuellerKurs <= 0 || Menge <= 0)
            {
                Debug.WriteLine("Ungültige Transaktionsparameter");
                MessageBox.Show("Bitte wählen Sie eine Aktie aus und geben Sie eine Menge an.", "Eingabefehler", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                if (aktModell == null)
                {
                    Debug.WriteLine($"Aktie {AktienSymbol} nicht in der AktienListe gefunden");

                    // Als Fallback in der MarktdatenViewModel-Liste suchen
                    aktModell = _mainViewModel.MarktdatenViewModel?.AktienListe?
                        .FirstOrDefault(a => a.AktienSymbol
                            .Equals(AktienSymbol.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (aktModell == null)
                    {
                        // Wenn immer noch nicht gefunden, erstellen wir eine neue Aktie mit den aktuellen Werten
                        aktModell = new Aktie
                        {
                            AktienID = AktienListe.Count + 1,
                            AktienSymbol = AktienSymbol.Trim().ToUpper(),
                            AktienName = AktienName,
                            AktuellerPreis = AktuellerKurs,
                            LetzteAktualisierung = DateTime.Now
                        };
                    }
                }

                int id = aktModell.AktienID;
                var name = aktModell.AktienName;

                // WICHTIG: Hier verwenden wir explizit den aktuellen Kurs aus unserer View
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

                    // Aktie zum Portfolio hinzufügen
                    portfolio.FügeAktieHinzu(id, AktienSymbol.Trim().ToUpper(), name, Menge, kurs, kurs);

                    // Kontostand direkt im MainViewModel aktualisieren
                    _mainViewModel.VerringereKontostand(gesamtWert);

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

                    // Verkaufen und prüfen, ob erfolgreich
                    if (portfolio.VerkaufeAktie(id, Menge))
                    {
                        // Kontostand direkt im MainViewModel aktualisieren
                        _mainViewModel.ErhöheKontostand(gesamtWert);

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