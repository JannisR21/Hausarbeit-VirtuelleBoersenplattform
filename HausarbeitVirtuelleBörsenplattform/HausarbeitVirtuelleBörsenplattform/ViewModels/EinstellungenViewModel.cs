using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;

namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    /// <summary>
    /// ViewModel für die Einstellungs-Ansicht
    /// </summary>
    public class EinstellungenViewModel : ObservableObject
    {
        #region Private Felder
        private string _apiKey;
        private bool _isDarkModeEnabled;
        private int _aktualisierungsIntervallInSekunden;
        #endregion

        #region Public Properties
        /// <summary>
        /// API-Schlüssel für Twelve Data
        /// </summary>
        public string ApiKey
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        /// <summary>
        /// Gibt an, ob der Dark Mode aktiviert ist
        /// </summary>
        public bool IsDarkModeEnabled
        {
            get => _isDarkModeEnabled;
            set => SetProperty(ref _isDarkModeEnabled, value);
        }

        /// <summary>
        /// Aktualisierungsintervall für Marktdaten in Sekunden
        /// </summary>
        public int AktualisierungsIntervallInSekunden
        {
            get => _aktualisierungsIntervallInSekunden;
            set => SetProperty(ref _aktualisierungsIntervallInSekunden, value);
        }
        #endregion

        #region Commands
        /// <summary>
        /// Command zum Speichern der Einstellungen
        /// </summary>
        public IRelayCommand SaveSettingsCommand { get; }

        /// <summary>
        /// Command zum Zurücksetzen der Einstellungen
        /// </summary>
        public IRelayCommand ResetSettingsCommand { get; }
        #endregion

        #region Konstruktor
        /// <summary>
        /// Initialisiert eine neue Instanz des EinstellungenViewModel
        /// </summary>
        public EinstellungenViewModel()
        {
            // Standardwerte aus der Anwendung laden
            ApiKey = App.TwelveDataApiKey;
            IsDarkModeEnabled = false; // Standardmäßig deaktiviert
            AktualisierungsIntervallInSekunden = 300; // 5 Minuten als Standard

            // Commands initialisieren
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            ResetSettingsCommand = new RelayCommand(ResetSettings);

            Debug.WriteLine("EinstellungenViewModel initialisiert");
        }
        #endregion

        #region Methoden
        /// <summary>
        /// Speichert die aktuellen Einstellungen
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                Debug.WriteLine("Einstellungen werden gespeichert...");

                // Hier könnten die Einstellungen in der Konfigurationsdatei gespeichert werden
                // In dieser Version zeigen wir nur eine Meldung an

                Debug.WriteLine($"API-Key: {ApiKey}");
                Debug.WriteLine($"Dark Mode: {IsDarkModeEnabled}");
                Debug.WriteLine($"Aktualisierungsintervall: {AktualisierungsIntervallInSekunden} Sekunden");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Speichern der Einstellungen: {ex.Message}");
            }
        }

        /// <summary>
        /// Setzt die Einstellungen auf die Standardwerte zurück
        /// </summary>
        private void ResetSettings()
        {
            try
            {
                Debug.WriteLine("Einstellungen werden zurückgesetzt...");

                // Standardwerte wiederherstellen
                ApiKey = App.TwelveDataApiKey;
                IsDarkModeEnabled = false;
                AktualisierungsIntervallInSekunden = 300;

                Debug.WriteLine("Einstellungen wurden zurückgesetzt");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Zurücksetzen der Einstellungen: {ex.Message}");
            }
        }
        #endregion
    }
}