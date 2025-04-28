using System;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;

namespace HausarbeitVirtuelleBörsenplattform.Properties
{
    [Serializable]
    public class Settings
    {
        private static Settings _default;

        public static Settings Default
        {
            get
            {
                if (_default == null)
                    _default = new Settings();
                return _default;
            }
        }

        // Einstellungen
        public bool IsDarkModeEnabled { get; set; } = false;
        public int AktualisierungsIntervallInSekunden { get; set; } = 300;
        public string ApiKey { get; set; } = string.Empty;

        public void Save()
        {
            try
            {
                string settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "VirtuelleBörsenplattform",
                    "settings.xml");

                // Verzeichnis erstellen, falls nicht vorhanden
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));

                // Einstellungen serialisieren und speichern
                var serializer = new XmlSerializer(typeof(Settings));
                using (var writer = new StreamWriter(settingsPath))
                {
                    serializer.Serialize(writer, this);
                }

                Debug.WriteLine($"Einstellungen gespeichert in: {settingsPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Speichern der Einstellungen: {ex.Message}");
            }
        }

        public static void Load()
        {
            try
            {
                string settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "VirtuelleBörsenplattform",
                    "settings.xml");

                if (File.Exists(settingsPath))
                {
                    // Einstellungen deserialisieren und laden
                    var serializer = new XmlSerializer(typeof(Settings));
                    using (var reader = new StreamReader(settingsPath))
                    {
                        _default = (Settings)serializer.Deserialize(reader);
                    }

                    Debug.WriteLine($"Einstellungen geladen aus: {settingsPath}");
                }
                else
                {
                    Debug.WriteLine("Keine Einstellungsdatei gefunden, verwende Standardeinstellungen");
                    _default = new Settings();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Einstellungen: {ex.Message}");
                // Bei Fehler Standardeinstellungen verwenden
                _default = new Settings();
            }
        }
    }
}