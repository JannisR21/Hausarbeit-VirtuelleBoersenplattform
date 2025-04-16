using System;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Repräsentiert einen Benutzer der Börsenplattform
    /// </summary>
    public class Benutzer
    {
        /// <summary>
        /// Eindeutige ID des Benutzers
        /// </summary>
        public int BenutzerID { get; set; }

        /// <summary>
        /// Anzeigename des Benutzers
        /// </summary>
        public string Benutzername { get; set; }

        /// <summary>
        /// E-Mail-Adresse des Benutzers
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gespeicherter Hash des Benutzerpassworts
        /// </summary>
        public string PasswortHash { get; set; }

        /// <summary>
        /// Datum der Benutzerregistrierung
        /// </summary>
        public DateTime Erstellungsdatum { get; set; }

        /// <summary>
        /// Aktueller Kontostand des Benutzers in Euro
        /// </summary>
        public decimal Kontostand { get; set; }
    }
}