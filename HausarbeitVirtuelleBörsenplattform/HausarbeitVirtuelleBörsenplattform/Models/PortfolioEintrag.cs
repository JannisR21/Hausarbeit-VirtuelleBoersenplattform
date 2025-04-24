using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Repräsentiert einen Eintrag im Portfolio eines Benutzers
    /// </summary>
    public class PortfolioEintrag : ObservableObject
    {
        private int _benutzerID;
        private int _aktienID;
        private string _aktienSymbol;
        private string _aktienName;
        private int _anzahl;
        private decimal _aktuellerKurs;
        private decimal _einstandsPreis;
        private DateTime _letzteAktualisierung;

        /// <summary>
        /// ID des Benutzers, dem dieses Portfolio-Element gehört
        /// </summary>
        public int BenutzerID
        {
            get => _benutzerID;
            set => SetProperty(ref _benutzerID, value);
        }

        public int AktienID
        {
            get => _aktienID;
            set => SetProperty(ref _aktienID, value);
        }

        public string AktienSymbol
        {
            get => _aktienSymbol;
            set => SetProperty(ref _aktienSymbol, value);
        }

        public string AktienName
        {
            get => _aktienName;
            set => SetProperty(ref _aktienName, value);
        }

        public int Anzahl
        {
            get => _anzahl;
            set
            {
                if (SetProperty(ref _anzahl, value))
                {
                    OnPropertyChanged(nameof(Wert));
                    OnPropertyChanged(nameof(GewinnVerlust));
                    OnPropertyChanged(nameof(GewinnVerlustProzent));
                }
            }
        }

        public decimal AktuellerKurs
        {
            get => _aktuellerKurs;
            set
            {
                if (SetProperty(ref _aktuellerKurs, value))
                {
                    OnPropertyChanged(nameof(Wert));
                    OnPropertyChanged(nameof(GewinnVerlust));
                    OnPropertyChanged(nameof(GewinnVerlustProzent));
                }
            }
        }

        public decimal EinstandsPreis
        {
            get => _einstandsPreis;
            set
            {
                if (SetProperty(ref _einstandsPreis, value))
                {
                    OnPropertyChanged(nameof(GewinnVerlust));
                    OnPropertyChanged(nameof(GewinnVerlustProzent));
                }
            }
        }

        public DateTime LetzteAktualisierung
        {
            get => _letzteAktualisierung;
            set => SetProperty(ref _letzteAktualisierung, value);
        }

        public decimal Wert => Anzahl * AktuellerKurs;

        public decimal GewinnVerlust => Wert - (Anzahl * EinstandsPreis);

        public decimal GewinnVerlustProzent =>
            EinstandsPreis > 0 ? ((AktuellerKurs / EinstandsPreis) - 1) * 100 : 0;
    }
}