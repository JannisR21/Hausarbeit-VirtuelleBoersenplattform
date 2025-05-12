using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Repräsentiert einen historischen Kurs einer Aktie für einen bestimmten Tag
    /// </summary>
    public class AktienKursHistorie : ObservableObject
    {
        private int _historieID;
        /// <summary>
        /// Eindeutige ID des Historieneintrags
        /// </summary>
        public int HistorieID
        {
            get => _historieID;
            set => SetProperty(ref _historieID, value);
        }

        private int _aktienID;
        /// <summary>
        /// ID der zugehörigen Aktie
        /// </summary>
        public int AktienID
        {
            get => _aktienID;
            set => SetProperty(ref _aktienID, value);
        }

        private string _aktienSymbol;
        /// <summary>
        /// Symbol der Aktie
        /// </summary>
        public string AktienSymbol
        {
            get => _aktienSymbol;
            set => SetProperty(ref _aktienSymbol, value);
        }

        private DateTime _datum;
        /// <summary>
        /// Datum des Kurses
        /// </summary>
        public DateTime Datum
        {
            get => _datum;
            set => SetProperty(ref _datum, value);
        }

        private decimal _eroeffnungskurs;
        /// <summary>
        /// Eröffnungskurs (Open) des Tages
        /// </summary>
        public decimal Eroeffnungskurs
        {
            get => _eroeffnungskurs;
            set => SetProperty(ref _eroeffnungskurs, value);
        }

        private decimal _hoechstkurs;
        /// <summary>
        /// Höchstkurs (High) des Tages
        /// </summary>
        public decimal Hoechstkurs
        {
            get => _hoechstkurs;
            set => SetProperty(ref _hoechstkurs, value);
        }

        private decimal _tiefstkurs;
        /// <summary>
        /// Tiefstkurs (Low) des Tages
        /// </summary>
        public decimal Tiefstkurs
        {
            get => _tiefstkurs;
            set => SetProperty(ref _tiefstkurs, value);
        }

        private decimal _schlusskurs;
        /// <summary>
        /// Schlusskurs (Close) des Tages
        /// </summary>
        public decimal Schlusskurs
        {
            get => _schlusskurs;
            set => SetProperty(ref _schlusskurs, value);
        }

        private long _volumen;
        /// <summary>
        /// Handelsvolumen des Tages
        /// </summary>
        public long Volumen
        {
            get => _volumen;
            set => SetProperty(ref _volumen, value);
        }

        private decimal _änderungProzent;
        /// <summary>
        /// Kursänderung in Prozent verglichen mit dem Vortag
        /// </summary>
        public decimal ÄnderungProzent
        {
            get => _änderungProzent;
            set => SetProperty(ref _änderungProzent, value);
        }

        /// <summary>
        /// Erstellt eine neue Instanz der AktienKursHistorie
        /// </summary>
        public AktienKursHistorie() 
        {
            Datum = DateTime.Now.Date;
        }

        /// <summary>
        /// Erstellt eine neue Instanz der AktienKursHistorie mit den angegebenen Werten
        /// </summary>
        public AktienKursHistorie(int aktienID, string aktienSymbol, DateTime datum, decimal eroeffnungskurs, 
                                  decimal hoechstkurs, decimal tiefstkurs, decimal schlusskurs, long volumen = 0)
        {
            AktienID = aktienID;
            AktienSymbol = aktienSymbol;
            Datum = datum;
            Eroeffnungskurs = eroeffnungskurs;
            Hoechstkurs = hoechstkurs;
            Tiefstkurs = tiefstkurs;
            Schlusskurs = schlusskurs;
            Volumen = volumen;
            
            // Berechne Prozentänderung nur, wenn ein Vergleichswert bekannt ist (in diesem Fall nicht)
            ÄnderungProzent = 0;
        }
    }
}