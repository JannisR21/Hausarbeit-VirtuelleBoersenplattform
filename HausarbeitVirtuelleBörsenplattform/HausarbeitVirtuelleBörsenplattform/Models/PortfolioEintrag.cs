using System;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Repräsentiert einen Eintrag im Portfolio eines Benutzers
    /// </summary>
    public class PortfolioEintrag
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
        /// Anzahl der Aktien im Besitz
        /// </summary>
        public int Anzahl { get; set; }

        /// <summary>
        /// Aktueller Kurs der Aktie
        /// </summary>
        public decimal AktuellerKurs { get; set; }

        /// <summary>
        /// Durchschnittlicher Einstandspreis (Kaufpreis) der Aktien
        /// </summary>
        public decimal EinstandsPreis { get; set; }

        /// <summary>
        /// Datum der letzten Aktualisierung des Kurses
        /// </summary>
        public DateTime LetzteAktualisierung { get; set; }

        /// <summary>
        /// Berechnet den aktuellen Gesamtwert der Position (Anzahl * aktueller Kurs)
        /// </summary>
        public decimal Wert => Anzahl * AktuellerKurs;

        /// <summary>
        /// Berechnet den absoluten Gewinn oder Verlust (aktueller Wert - Einstandswert)
        /// </summary>
        public decimal GewinnVerlust => Wert - (Anzahl * EinstandsPreis);

        /// <summary>
        /// Berechnet den relativen Gewinn oder Verlust in Prozent
        /// </summary>
        public decimal GewinnVerlustProzent => EinstandsPreis > 0 ? ((AktuellerKurs / EinstandsPreis) - 1) * 100 : 0;
    }
}