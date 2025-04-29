using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using HausarbeitVirtuelleBörsenplattform.Data;
using HausarbeitVirtuelleBörsenplattform.Services;
using System.Diagnostics;
using HausarbeitVirtuelleBörsenplattform.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using HausarbeitVirtuelleBörsenplattform.Properties;
using HausarbeitVirtuelleBörsenplattform.Helpers;

namespace HausarbeitVirtuelleBörsenplattform
{
    public partial class App : Application
    {
        public static AuthenticationService AuthService { get; private set; }
        public static DatabaseService DbService { get; private set; }
        public static EmailService EmailService { get; private set; }
        public static string TwelveDataApiKey { get; private set; }
        public static TwelveDataService TwelveDataService { get; private set; }
        public static AktienFilterService AktienFilterService { get; private set; }

        // Diese Collection enthält nur API-geladene Aktien, keine Dummy-Daten mehr
        public static ObservableCollection<Aktie> StandardAktien { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Einstellungen laden
            Settings.Load();

            // DarkAndLightMode initialisieren
            DarkAndLightMode.Initialize();

            // Falls Dark Mode aktiviert ist, entsprechendes Theme setzen
            if (Settings.Default.IsDarkModeEnabled)
            {
                DarkAndLightMode.SetDarkTheme();
            }

            // Services initialisieren
            InitializeServices();

            // Nur das LoginWindow starten!
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            Current.MainWindow = loginWindow;
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
        }

        /// <summary>
        /// Lädt die aktuellen Marktdaten von Twelve Data
        /// </summary>
        private async Task LadeAktuelleMarktdaten()
        {
            try
            {
                Debug.WriteLine("Lade aktuelle Marktdaten beim Anwendungsstart...");

                // Standard-Aktien-Symbole, die wir abfragen wollen
                var symbole = new List<string> { "AAPL", "MSFT", "TSLA", "AMZN", "GOOGL" };

                // TwelveDataService erstellen und Daten abrufen
                if (TwelveDataService == null)
                {
                    TwelveDataService = new TwelveDataService(TwelveDataApiKey);
                }

                var aktienDaten = await TwelveDataService.HoleAktienKurse(symbole);

                if (aktienDaten != null && aktienDaten.Count > 0)
                {
                    // IDs den Aktien zuweisen
                    for (int i = 0; i < aktienDaten.Count; i++)
                    {
                        aktienDaten[i].AktienID = i + 1;
                    }

                    // UI-Thread-Zugriff sicherstellen
                    Current.Dispatcher.Invoke(() =>
                    {
                        StandardAktien.Clear();
                        foreach (var aktie in aktienDaten)
                        {
                            StandardAktien.Add(aktie);
                            Debug.WriteLine($"Aktie geladen: {aktie.AktienSymbol} - {aktie.AktienName} - {aktie.AktuellerPreis:F2}€");
                        }
                    });

                    // In Datenbank speichern
                    if (DbService != null)
                    {
                        await DbService.UpdateAktienBatchAsync(aktienDaten);
                        Debug.WriteLine("Aktien in Datenbank gespeichert");
                    }

                    Debug.WriteLine($"Erfolgreich {aktienDaten.Count} Aktien geladen");
                }
                else
                {
                    Debug.WriteLine("Keine Aktien von Twelve Data erhalten");
                    // Anstatt Dummy-Daten zu laden, zeigen wir eine Warnung an
                    MessageBox.Show("Keine Aktien konnten von der API geladen werden. Bitte überprüfen Sie Ihre Internetverbindung.",
                        "API-Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Marktdaten: {ex.Message}");
                MessageBox.Show($"Fehler beim Laden der Marktdaten: {ex.Message}\nDie Anwendung benötigt eine Internetverbindung.",
                    "API-Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                // Explizites Laden der Konfiguration
                var configFileMap = new ExeConfigurationFileMap();
                configFileMap.ExeConfigFilename = "App.config";
                var config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);

                TwelveDataApiKey = config.AppSettings.Settings["TwelveDataApiKey"]?.Value;

                Debug.WriteLine($"Konfigurationspfad: {config.FilePath}");
                Debug.WriteLine($"Geladener API-Key: {TwelveDataApiKey}");

                if (string.IsNullOrEmpty(TwelveDataApiKey))
                {
                    TwelveDataApiKey = "cb617aba18ea46b3a974d878d3c7310b";
                    Debug.WriteLine("Fallback-API-Key wurde gesetzt");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Konfiguration: {ex.Message}");
                MessageBox.Show($"Fehler beim Laden der Konfiguration: {ex.Message}",
                    "Konfigurationsfehler", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Fallback-Key setzen
                TwelveDataApiKey = "cb617aba18ea46b3a974d878d3c7310b";
            }
        }

        private void InitializeServices()
        {
            try
            {
                // Explizites Laden der Konfiguration
                LoadConfiguration();

                Debug.WriteLine($"Geladener API-Key: {TwelveDataApiKey}");

                var options = new DbContextOptionsBuilder<BörsenplattformDbContext>()
                    .UseSqlServer(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString)
                    .EnableSensitiveDataLogging()
                    .Options;

                var dbContext = new BörsenplattformDbContext(options);

                Debug.WriteLine("Prüfe Datenbankverbindung...");
                if (dbContext.Database.CanConnect())
                {
                    Debug.WriteLine("Datenbankverbindung ist möglich.");
                    DbService = new DatabaseService(dbContext, options);
                    AuthService = new AuthenticationService(DbService);

                    // Sicherstellen, dass der API-Key nicht null ist
                    if (string.IsNullOrEmpty(TwelveDataApiKey))
                    {
                        TwelveDataApiKey = "cb617aba18ea46b3a974d878d3c7310b";
                        Debug.WriteLine("Fallback-API-Key wurde gesetzt");
                    }

                    TwelveDataService = new TwelveDataService(TwelveDataApiKey);
                    Debug.WriteLine($"TwelveDataService initialisiert mit API-Key: {TwelveDataApiKey}");

                    // Initialisiere den AktienFilterService mit dem bestehenden TwelveDataService
                    AktienFilterService = new AktienFilterService(TwelveDataApiKey, TwelveDataService);
                    Debug.WriteLine($"AktienFilterService initialisiert");

                    // StandardAktien Collection initialisieren
                    if (StandardAktien == null)
                    {
                        StandardAktien = new ObservableCollection<Aktie>();
                    }

                    // EmailService mit den konkreten SMTP-Daten initialisieren
                    EmailService = new EmailService(
                        smtpServer: "smtp.web.de",
                        smtpPort: 587,
                        smtpUsername: "Jannisr32@web.de",
                        smtpPassword: "KSIYD2GR4AIKOMIH7ZCK",
                        senderEmail: "Jannisr32@web.de",
                        senderName: "Virtuelle Börsenplattform",
                        isTestMode: false    // Echte E-Mails versenden
                    );

                    Debug.WriteLine("EmailService mit echten SMTP-Daten initialisiert");
                }
                else
                {
                    throw new Exception("Datenbankverbindung fehlgeschlagen.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Initialisierung: {ex.Message}");
                MessageBox.Show($"Fehler bei der Initialisierung: {ex.Message}",
                    "Initialisierungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SwitchToMainWindow()
        {
            try
            {
                Debug.WriteLine("Wechsle zum Hauptfenster...");

                var oldWindow = Current.MainWindow;
                var mainWindow = new MainWindow();

                mainWindow.Show();
                Current.MainWindow = mainWindow;

                oldWindow?.Close(); // Komplett schließen

                Debug.WriteLine("Hauptfenster geöffnet.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Wechsel zum Hauptfenster: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}