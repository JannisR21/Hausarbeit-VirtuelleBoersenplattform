using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Models;
using System;
using System.Collections.ObjectModel;
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
        private int _menge;
        private decimal _gesamtwert;
        private bool _isKauf = true;
        private string _warnungsText;
        private bool _zeigeWarnung;
        private string _transaktionsText;
        #endregion

        #region Konstruktor
        // Parameterfreier Konstruktor für Initialisierung ohne MainViewModel
        public AktienhandelViewModel(MainViewModel mainViewModel)
        {
            // Commands initialisieren
            AktienSuchenCommand = new RelayCommand(AktienSuchen);
            TransaktionAusführenCommand = new RelayCommand(TransaktionAusführen, KannTransaktionAusführen);
            IncrementMengeCommand = new RelayCommand(IncrementMenge);
            DecrementMengeCommand = new RelayCommand(DecrementMenge);

            // Standardwerte setzen
            Menge = 1;
            _transaktionsText = "Aktien kaufen";

            // PropertyChanged-Handler für IsKauf
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsKauf))
                {
                    PrüfeTransaktionsDurchführbarkeit();
                    TransaktionsText = IsKauf ? "Aktien kaufen" : "Aktien verkaufen";
                }
            };
            _mainViewModel = mainViewModel;
        }

        // Methode zum nachträglichen Setzen des MainViewModel
        public void SetMainViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Verfügbare Aktien zum Auswählen
        /// </summary>
        public ObservableCollection<Aktie> AktienListe =>
            _mainViewModel?.MarktdatenViewModel?.AktienListe;

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
                    LadeAktienDaten();
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
            private set => SetProperty(ref _aktienName, value);
        }

        /// <summary>
        /// Aktueller Kurs der ausgewählten Aktie
        /// </summary>
        public decimal AktuellerKurs
        {
            get => _aktuellerKurs;
            private set
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
            if (string.IsNullOrWhiteSpace(AktienSymbol) || _mainViewModel?.MarktdatenViewModel?.AktienListe == null)
            {
                AktienName = string.Empty;
                AktuellerKurs = 0;
                return;
            }

            var aktie = _mainViewModel.MarktdatenViewModel.AktienListe
                .FirstOrDefault(a => a.AktienSymbol
                    .Equals(AktienSymbol.Trim(), StringComparison.OrdinalIgnoreCase));

            if (aktie != null)
            {
                AktienName = aktie.AktienName;
                AktuellerKurs = aktie.AktuellerPreis;
                OnPropertyChanged(nameof(VerfügbareAktien));
            }
            else
            {
                AktienName = "Aktie nicht gefunden";
                AktuellerKurs = 0;
            }
        }

        /// <summary>
        /// Berechnet den Gesamtwert der Transaktion
        /// </summary>
        private void BerechneGesamtwert() =>
            Gesamtwert = AktuellerKurs * Menge;

        /// <summary>
        /// Sucht die Aktie anhand des eingegebenen Symbols
        /// </summary>
        private void AktienSuchen() =>
            LadeAktienDaten();

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
            try
            {
                Debug.WriteLine($"Transaktion wird ausgeführt: {(IsKauf ? "Kauf" : "Verkauf")} von {Menge} {AktienSymbol} zu {AktuellerKurs:C}");

                if (_mainViewModel?.PortfolioViewModel == null)
                {
                    MessageBox.Show("Es ist ein Fehler aufgetreten: PortfolioViewModel ist nicht verfügbar.",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var portfolio = _mainViewModel.PortfolioViewModel;
                var aktModell = _mainViewModel.MarktdatenViewModel.AktienListe
                    .FirstOrDefault(a => a.AktienSymbol
                        .Equals(AktienSymbol.Trim(), StringComparison.OrdinalIgnoreCase));

                int id = aktModell?.AktienID ?? 0;
                var name = AktienName;
                var kurs = AktuellerKurs;

                if (IsKauf)
                {
                    // Kauf durchführen
                    if (Gesamtwert > AktuellerKontostand)
                    {
                        Debug.WriteLine("Kauf fehlgeschlagen: Nicht genügend Guthaben");
                        MessageBox.Show("Nicht genügend Guthaben für diesen Kauf.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    portfolio.FügeAktieHinzu(id, AktienSymbol.Trim().ToUpper(), name, Menge, kurs, kurs);
                    _mainViewModel.AktuellerBenutzer.Kontostand -= kurs * Menge;

                    MessageBox.Show($"{Menge} Aktien von {AktienSymbol} wurden erfolgreich gekauft.",
                        "Kauf erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Verkauf durchführen
                    if (Menge > VerfügbareAktien)
                    {
                        Debug.WriteLine("Verkauf fehlgeschlagen: Nicht genügend Aktien im Portfolio");
                        MessageBox.Show($"Sie besitzen nur {VerfügbareAktien} Aktien dieses Wertpapiers.",
                            "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (portfolio.VerkaufeAktie(id, Menge))
                    {
                        _mainViewModel.AktuellerBenutzer.Kontostand += kurs * Menge;
                        MessageBox.Show($"{Menge} Aktien von {AktienSymbol} wurden erfolgreich verkauft.",
                            "Verkauf erfolgreich", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                // Formular zurücksetzen
                AktienSymbol = string.Empty;
                Menge = 1;

                // Aktualisierungen benachrichtigen
                OnPropertyChanged(nameof(AktuellerKontostand));
                Debug.WriteLine($"Neuer Kontostand: {AktuellerKontostand:C}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei Transaktion: {ex.Message}");
                MessageBox.Show($"Bei der Transaktion ist ein Fehler aufgetreten: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (string.IsNullOrWhiteSpace(AktienSymbol) || AktuellerKurs <= 0 || Menge <= 0)
                return false;

            if (IsKauf)
            {
                // Beim Kauf prüfen, ob genug Geld vorhanden ist
                return Gesamtwert <= AktuellerKontostand;
            }
            else
            {
                // Beim Verkauf prüfen, ob genug Aktien vorhanden sind
                return Menge <= VerfügbareAktien;
            }
        }
        #endregion
    }
}