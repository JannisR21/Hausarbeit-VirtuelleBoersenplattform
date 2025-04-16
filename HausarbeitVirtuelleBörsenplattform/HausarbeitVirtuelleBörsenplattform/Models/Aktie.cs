using System;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Repräsentiert eine Aktie im Börsensystem
    /// </summary>
    public class Aktie
    {
        /// <summary>
        /// Eindeutige ID der Aktie
        /// </summary>
        public int AktienID { get; set; }

        /// <summary>
        /// Symbol/Ticker der Aktie (z.B. AAPL für Apple)
        /// </summary>
        public string AktienSymbol { get; set; }

        /// <summary>
        /// Vollständiger Name der Aktie (z.B. Apple Inc.)
        /// </summary>
        public string AktienName { get; set; }

        /// <summary>
        /// Aktueller Preis der Aktie
        /// </summary>
        public decimal AktuellerPreis { get; set; }

        /// <summary>
        /// Absolute Änderung des Kurses (in EUR)
        /// </summary>
        public decimal Änderung { get; set; }

        /// <summary>
        /// Relative Änderung des Kurses (in Prozent)
        /// </summary>
        public decimal ÄnderungProzent { get; set; }

        /// <summary>
        /// Zeitpunkt der letzten Aktualisierung
        /// </summary>
        public DateTime LetzteAktualisierung { get; set; }
    }
}