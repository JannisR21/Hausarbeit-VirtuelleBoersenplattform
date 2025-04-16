using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Models;
using System;
using System.Windows.Input;

namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    /// ViewModel für den Aktienhandel-Bereich
    public class AktienhandelViewModel : ObservableObject
    {
        #region Private Felder

        private string _aktienSymbol;
        private string _aktienName;
        private decimal _aktuellerKurs;
        private int _menge;
        private decimal _gesamtwert;
        private bool _isKauf = true;
        private MainViewModel _mainViewModel;

        #endregion

        #region Public Properties

        /// Symbol/Ticker der ausgewählten Aktie
        public string AktienSymbol
        {
            get => _aktienSymbol;
            set
            {
                if (SetProperty(ref _aktienSymbol, value))
                {
                    // Bei Änderung des Symbols Aktiendaten abrufen
                    LadeAktienDaten();
                }
            }
        }

        /// Name der ausgewählten Aktie
        public string AktienName
        {
            get => _aktienName;
            set => SetProperty(ref _aktienName, value);
        }

        // Aktueller Kurs der ausgewählten Aktie
        public decimal AktuellerKurs
        {
            get => _aktuellerKurs;
            set
            {
                if (SetProperty(ref _aktuellerKurs, value))
                {
                    // Bei Kursänderung Gesamtwert neu berechnen
                    BerechneGesamtwert();
                }
            }
        }

        /// Anzahl der zu handelnden Aktien
        public int Menge
        {
            get => _menge;
            set
            {
                if (SetProperty(ref _menge, value))
                {
                    // Bei Mengenänderung Gesamtwert neu berechnen
                    BerechneGesamtwert();
                }
            }
        }

        /// Gesamtwert der Transaktion
        public decimal Gesamtwert
        {
            get => _gesamtwert;
            set => SetProperty(ref _gesamtwert, value);
        }

        /// Art der Transaktion (Kauf = true, Verkauf = false)
        public bool IsKauf
        {
            get => _isKauf;
            set => SetProperty(ref _isKauf, value);
        }

        /// Command zum Suchen einer Aktie
        public IRelayCommand AktienSuchenCommand { get; }

        /// Command zum Ausführen einer Transaktion
        public IRelayCommand TransaktionAusführenCommand { get; }

        #endregion

        #region Konstruktor

        /// Initialisiert eine neue Instanz des AktienhandelViewModel
        /// <param name="mainViewModel">Hauptinstanz des MainViewModel</param>
        public AktienhandelViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            // Commands initialisieren
            AktienSuchenCommand = new RelayCommand(AktienSuchen);
            TransaktionAusführenCommand = new RelayCommand(TransaktionAusführen, KannTransaktionAusführen);

            // Standardwerte setzen
            Menge = 1;
        }

        #endregion

        #region Private Methoden

        /// Lädt Aktiendaten basierend auf dem eingegebenen Symbol
        private void LadeAktienDaten()
        {
            // Hier würden normalerweise die Daten über eine API geladen
            // Für diesen Prototyp verwenden wir Beispieldaten

            if (string.IsNullOrEmpty(AktienSymbol))
                return;

            switch (AktienSymbol.ToUpper())
            {
                case "AAPL":
                    AktienName = "Apple Inc.";
                    AktuellerKurs = 150.00m;
                    break;
                case "TSLA":
                    AktienName = "Tesla Inc.";
                    AktuellerKurs = 200.20m;
                    break;
                case "AMZN":
                    AktienName = "Amazon.com Inc.";
                    AktuellerKurs = 95.10m;
                    break;
                case "MSFT":
                    AktienName = "Microsoft Corp.";
                    AktuellerKurs = 320.45m;
                    break;
                case "GOOGL":
                    AktienName = "Alphabet Inc.";
                    AktuellerKurs = 128.75m;
                    break;
                default:
                    AktienName = "Aktie nicht gefunden";
                    AktuellerKurs = 0.00m;
                    break;
            }
        }

        /// Berechnet den Gesamtwert der aktuellen Transaktion
        private void BerechneGesamtwert()
        {
            Gesamtwert = AktuellerKurs * Menge;
        }

        /// Sucht nach Aktien basierend auf dem eingegebenen Symbol
        private void AktienSuchen()
        {
            LadeAktienDaten();
        }

        /// Führt eine Kauf- oder Verkaufstransaktion durch
        private void TransaktionAusführen()
        {
            if (_mainViewModel == null)
                return;

        }

        /// Prüft, ob eine Transaktion ausgeführt werden kann
        private bool KannTransaktionAusführen()
        {
            if (string.IsNullOrEmpty(AktienSymbol) || AktuellerKurs <= 0 || Menge <= 0)
                return false;


            return true;
        }

        #endregion
    }
}