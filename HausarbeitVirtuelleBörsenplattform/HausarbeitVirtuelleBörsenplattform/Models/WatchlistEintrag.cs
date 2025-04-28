using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Repräsentiert einen Eintrag in der Watchlist eines Benutzers
    /// </summary>
    public class WatchlistEintrag : ObservableObject
    {
        private int _benutzerID;
        private int _aktienID;
        private string _aktienSymbol;
        private string _aktienName;
        private DateTime _hinzugefuegtAm;
        private decimal _kursBeimHinzufuegen;
        private decimal _aktuellerKurs;
        private DateTime _letzteAktualisierung;

        /// <summary>
        /// ID des Benutzers, dem dieser Watchlist-Eintrag gehört
        /// </summary>
        public int BenutzerID
        {
            get => _benutzerID;
            set => SetProperty(ref _benutzerID, value);
        }

        /// <summary>
        /// ID der Aktie in der Watchlist
        /// </summary>
        public int AktienID
        {
            get => _aktienID;
            set => SetProperty(ref _aktienID, value);
        }

        /// <summary>
        /// Symbol/Ticker der Aktie
        /// </summary>
        public string AktienSymbol
        {
            get => _aktienSymbol;
            set => SetProperty(ref _aktienSymbol, value);
        }

        /// <summary>
        /// Name der Aktie
        /// </summary>
        public string AktienName
        {
            get => _aktienName;
            set => SetProperty(ref _aktienName, value);
        }

        /// <summary>
        /// Datum und Uhrzeit, wann die Aktie zur Watchlist hinzugefügt wurde
        /// </summary>
        public DateTime HinzugefuegtAm
        {
            get => _hinzugefuegtAm;
            set => SetProperty(ref _hinzugefuegtAm, value);
        }

        /// <summary>
        /// Kurs der Aktie zum Zeitpunkt des Hinzufügens zur Watchlist
        /// </summary>
        public decimal KursBeimHinzufuegen
        {
            get => _kursBeimHinzufuegen;
            set => SetProperty(ref _kursBeimHinzufuegen, value);
        }

        /// <summary>
        /// Aktueller Kurs der Aktie
        /// </summary>
        public decimal AktuellerKurs
        {
            get => _aktuellerKurs;
            set
            {
                if (SetProperty(ref _aktuellerKurs, value))
                {
                    OnPropertyChanged(nameof(KursÄnderung));
                    OnPropertyChanged(nameof(KursÄnderungProzent));
                }
            }
        }

        /// <summary>
        /// Zeitpunkt der letzten Kursaktualisierung
        /// </summary>
        public DateTime LetzteAktualisierung
        {
            get => _letzteAktualisierung;
            set => SetProperty(ref _letzteAktualisierung, value);
        }

        /// <summary>
        /// Absolute Kursänderung seit dem Hinzufügen zur Watchlist
        /// </summary>
        public decimal KursÄnderung => AktuellerKurs - KursBeimHinzufuegen;

        /// <summary>
        /// Prozentuale Kursänderung seit dem Hinzufügen zur Watchlist
        /// </summary>
        public decimal KursÄnderungProzent =>
            KursBeimHinzufuegen > 0 ? ((AktuellerKurs / KursBeimHinzufuegen) - 1) * 100 : 0;
    }
}