using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Repräsentiert einen Benutzer der Börsenplattform
    /// </summary>
    public class Benutzer : ObservableObject
    {
        public int BenutzerID { get; set; }
        public string Benutzername { get; set; }
        public string Email { get; set; }
        public string PasswortHash { get; set; }
        public DateTime Erstellungsdatum { get; set; }

        private decimal _kontostand;
        /// <summary>
        /// Aktueller Kontostand des Benutzers in Euro
        /// </summary>
        public decimal Kontostand
        {
            get => _kontostand;
            set => SetProperty(ref _kontostand, value);
        }
    }
}