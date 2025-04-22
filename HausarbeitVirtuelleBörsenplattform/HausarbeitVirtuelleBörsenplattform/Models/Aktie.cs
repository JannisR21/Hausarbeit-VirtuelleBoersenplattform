using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Repräsentiert eine Aktie im Börsensystem
    /// </summary>
    public class Aktie : ObservableObject
    {
        private int _aktienID;
        /// <summary>
        /// Eindeutige ID der Aktie
        /// </summary>
        public int AktienID
        {
            get => _aktienID;
            set => SetProperty(ref _aktienID, value);
        }

        private string _aktienSymbol;
        /// <summary>
        /// Symbol/Ticker der Aktie (z.B. AAPL für Apple)
        /// </summary>
        public string AktienSymbol
        {
            get => _aktienSymbol;
            set => SetProperty(ref _aktienSymbol, value);
        }

        private string _aktienName;
        /// <summary>
        /// Vollständiger Name der Aktie (z.B. Apple Inc.)
        /// </summary>
        public string AktienName
        {
            get => _aktienName;
            set => SetProperty(ref _aktienName, value);
        }

        private decimal _aktuellerPreis;
        /// <summary>
        /// Aktueller Preis der Aktie
        /// </summary>
        public decimal AktuellerPreis
        {
            get => _aktuellerPreis;
            set => SetProperty(ref _aktuellerPreis, value);
        }

        private decimal _änderung;
        /// <summary>
        /// Absolute Änderung des Kurses (in EUR)
        /// </summary>
        public decimal Änderung
        {
            get => _änderung;
            set => SetProperty(ref _änderung, value);
        }

        private decimal _änderungProzent;
        /// <summary>
        /// Relative Änderung des Kurses (in Prozent)
        /// </summary>
        public decimal ÄnderungProzent
        {
            get => _änderungProzent;
            set => SetProperty(ref _änderungProzent, value);
        }

        private DateTime _letzteAktualisierung;
        /// <summary>
        /// Zeitpunkt der letzten Aktualisierung
        /// </summary>
        public DateTime LetzteAktualisierung
        {
            get => _letzteAktualisierung;
            set => SetProperty(ref _letzteAktualisierung, value);
        }
    }
}
