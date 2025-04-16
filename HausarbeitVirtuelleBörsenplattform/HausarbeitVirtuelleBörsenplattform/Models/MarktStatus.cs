namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Repräsentiert den aktuellen Status des Börsenmarktes
    /// </summary>
    public class MarktStatus
    {
        /// <summary>
        /// Gibt an, ob der Markt aktuell geöffnet ist
        /// </summary>
        public bool IsOpen { get; set; }

        /// <summary>
        /// Anzeigetext für den Status (z.B. "Markt geöffnet")
        /// </summary>
        public string StatusText { get; set; }

        /// <summary>
        /// Farbcode für die visuelle Darstellung des Status (z.B. grün für geöffnet)
        /// </summary>
        public string StatusColor { get; set; }

        /// <summary>
        /// Zeitpunkt der letzten Aktualisierung der Marktdaten
        /// </summary>
        public string LetzteAktualisierung { get; set; }
    }
}