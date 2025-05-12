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
        public static BörsenplattformDbContext DbContext { get; private set; }

        // Diese Collection enthält nur API-geladene Aktien, keine Dummy-Daten mehr
        public static ObservableCollection<Aktie> StandardAktien { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
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

            // Setze IsEnabled für Eingabefelder immer auf true
            // (wird in allen Builds angewendet)
            Debug.WriteLine("Stelle sicher, dass Eingabefelder in allen Build-Konfigurationen aktiviert sind");

            // Services initialisieren
            // Asynchrone Initialisierung als Task ausführen
            await InitializeServices();

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

        private async Task InitializeServices()
        {
            try
            {
                // Explizites Laden der Konfiguration
                LoadConfiguration();
                Debug.WriteLine($"Geladener API-Key: {TwelveDataApiKey}");

                // Ensure UI input fields always work regardless of build mode
                // No need to explicitly enable input method as we've set IsEnabled=true on the controls

                var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                Debug.WriteLine($"Verbindungszeichenfolge: {connectionString}");

                var options = new DbContextOptionsBuilder<BörsenplattformDbContext>()
                    .UseSqlServer(connectionString, sqlOptions => {
                        // Explizites Setzen der SQL Server-Kompatibilitätsebene
                        sqlOptions.CommandTimeout(60); // Längeres Timeout für langsamere Verbindungen
                        sqlOptions.EnableRetryOnFailure(3); // Wiederholungsversuche bei Verbindungsproblemen
                    })
                    .EnableSensitiveDataLogging()
                    .Options;

                var dbContext = new BörsenplattformDbContext(options);

                Debug.WriteLine("Prüfe Datenbankverbindung...");

                // Prüfen, ob die Datenbank existiert, falls nicht, erstellen
                try
                {
                    // DbService initialisieren
                    DbService = new DatabaseService(dbContext, options);

                    // Explizit überprüfen, ob Datenbank bereits existiert
                    if (!dbContext.Database.CanConnect())
                    {
                        Debug.WriteLine("Datenbank existiert nicht, versuche sie zu erstellen...");

                        // Zuerst mit Migrate versuchen für bessere Kompatibilität
                        try
                        {
                            dbContext.Database.Migrate();
                            Debug.WriteLine("Datenbank wurde erfolgreich mit Migrate erstellt!");
                        }
                        catch (Exception migrateEx)
                        {
                            Debug.WriteLine($"Fehler bei Migrate: {migrateEx.Message}, versuche EnsureCreated...");

                            // Fallback auf EnsureCreated
                            dbContext.Database.EnsureCreated();
                            Debug.WriteLine("Datenbank wurde erfolgreich mit EnsureCreated erstellt!");
                        }

                        // Zusätzlich noch unsere benutzerdefinierte Methode aufrufen
                        await DbService.EnsureDatabaseCreatedAsync();
                    }
                    else
                    {
                        Debug.WriteLine("Datenbank existiert bereits, prüfe auf Updates...");

                        // Zuerst mit GetPendingMigrations prüfen
                        try
                        {
                            // Prüfe, ob Migrationen angewendet werden müssen
                            var pendingMigrations = dbContext.Database.GetPendingMigrations();
                            if (pendingMigrations.Any())
                            {
                                Debug.WriteLine($"Es gibt {pendingMigrations.Count()} ausstehende Migrationen, wende sie an...");
                                dbContext.Database.Migrate();
                                Debug.WriteLine("Alle ausstehenden Migrationen wurden angewendet.");
                            }
                            else
                            {
                                Debug.WriteLine("Datenbank existiert und ist auf dem neuesten Stand.");
                            }
                        }
                        catch (Exception migrationEx)
                        {
                            Debug.WriteLine($"Fehler beim Prüfen/Anwenden von Migrationen: {migrationEx.Message}");
                        }

                        // Zusätzlich noch unsere benutzerdefinierte Methode aufrufen
                        await DbService.EnsureDatabaseCreatedAsync();
                    }
                }
                catch (Exception dbCreateEx)
                {
                    Debug.WriteLine($"Fehler beim Erstellen der Datenbank: {dbCreateEx.Message}");
                    if (dbCreateEx.InnerException != null)
                        Debug.WriteLine($"Inner Exception: {dbCreateEx.InnerException.Message}");
                    throw; // Neu werfen, um den äußeren Catch-Block zu erreichen
                }

                // Erneute Prüfung, ob die Verbindung jetzt möglich ist
                if (dbContext.Database.CanConnect())
                {
                    Debug.WriteLine("Datenbankverbindung ist möglich.");
                    DbContext = dbContext; // DbContext für direkten Zugriff speichern
                    DbService = new DatabaseService(dbContext, options);
                    AuthService = new AuthenticationService(DbService);

                    // Überprüfe und erstelle historische Daten-Tabelle, falls noch nicht vorhanden
                    Task.Run(async () => {
                        try {
                            Debug.WriteLine("Prüfe die Tabelle für historische Aktiendaten...");
                            var tableExists = false;

                            // Prüfe, ob die Tabelle bereits existiert
                            using (var command = dbContext.Database.GetDbConnection().CreateCommand())
                            {
                                command.CommandText = "SELECT CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES " +
                                                      "WHERE TABLE_NAME = 'AktienKursHistorie') THEN 1 ELSE 0 END";

                                await dbContext.Database.OpenConnectionAsync();
                                var result = await command.ExecuteScalarAsync();
                                tableExists = Convert.ToInt32(result) == 1;
                                Debug.WriteLine("Datenbankverbindung erfolgreich geöffnet und Abfrage ausgeführt");
                            }

                            Debug.WriteLine($"Historische Daten-Tabelle existiert: {tableExists}");

                            // Falls nicht existierend, führe Migratioinen aus
                            if (!tableExists)
                            {
                                Debug.WriteLine("Historische Daten-Tabelle wird erstellt...");
                                dbContext.Database.ExecuteSqlRaw(@"
                                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AktienKursHistorie')
                                    BEGIN
                                        CREATE TABLE AktienKursHistorie (
                                            HistorieID int IDENTITY(1,1) PRIMARY KEY,
                                            AktienID int NOT NULL,
                                            AktienSymbol nvarchar(10) NOT NULL,
                                            Datum datetime2 NOT NULL,
                                            Eroeffnungskurs decimal(18,2) NOT NULL,
                                            Hoechstkurs decimal(18,2) NOT NULL,
                                            Tiefstkurs decimal(18,2) NOT NULL,
                                            Schlusskurs decimal(18,2) NOT NULL,
                                            Volumen bigint NOT NULL,
                                            ÄnderungProzent decimal(18,2) NOT NULL,
                                            CONSTRAINT FK_AktienKursHistorie_Aktien
                                                FOREIGN KEY (AktienID) REFERENCES Aktien(AktienID)
                                                ON DELETE NO ACTION,
                                            INDEX IX_AktienKursHistorie_Symbol (AktienSymbol),
                                            INDEX IX_AktienKursHistorie_Datum (Datum)
                                        );

                                        PRINT 'Historische Daten-Tabelle wurde erfolgreich erstellt';
                                    END
                                    ELSE
                                    BEGIN
                                        PRINT 'Historische Daten-Tabelle existiert bereits';
                                    END");
                                Debug.WriteLine("Historische Daten-Tabelle wurde erstellt.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Fehler beim Prüfen/Erstellen der historischen Daten-Tabelle: {ex.Message}");
                            // Fehler protokollieren, aber App weiterlaufen lassen
                        }
                    });

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

                    // Lade einige Standard-Aktien beim Start
                    Task.Run(async () => {
                        await LadeAktuelleMarktdaten();
                    });

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
                    throw new Exception("Datenbankverbindung fehlgeschlagen, auch nach Versuch der Erstellung.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Initialisierung: {ex.Message}");
                if (ex.InnerException != null)
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");

                // Prüfen, ob es sich um ein SQL Server-Versionsproblem handelt
                bool isSqlVersionError = ex.Message.Contains("version") ||
                                       (ex.InnerException != null && ex.InnerException.Message.Contains("version"));

                if (isSqlVersionError)
                {
                    Debug.WriteLine("SQL Server-Versionsproblem erkannt. Versuche alternative Verbindung...");

                    try
                    {
                        // Alternativer Versuch mit anderer Kompatibilitätseinstellung
                        // Verbindungszeichenfolge aus ConnectionStrings abrufen
                        string dbConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

                        var fallbackOptions = new DbContextOptionsBuilder<BörsenplattformDbContext>()
                            .UseSqlServer(dbConnectionString, sqlServerOptions => {
                                sqlServerOptions.CommandTimeout(90);
                                sqlServerOptions.EnableRetryOnFailure(5);
                                // Einfachere Konfiguration ohne spezifische ExecutionStrategy
                            })
                            .EnableDetailedErrors()
                            .Options;

                        var fallbackContext = new BörsenplattformDbContext(fallbackOptions);
                        fallbackContext.Database.Migrate();

                        // Wenn erfolgreich, setze diese Optionen und Context als Standard
                        DbService = new DatabaseService(fallbackContext, fallbackOptions);
                        AuthService = new AuthenticationService(DbService);

                        Debug.WriteLine("Alternative Verbindung erfolgreich hergestellt!");

                        MessageBox.Show("Es wurde eine ältere SQL Server-Version erkannt. " +
                                       "Die Anwendung wurde in den Kompatibilitätsmodus versetzt und sollte nun funktionieren.",
                                       "SQL Server-Kompatibilität", MessageBoxButton.OK, MessageBoxImage.Information);

                        // Services weiter initialisieren
                        TwelveDataService = new TwelveDataService(TwelveDataApiKey);
                        AktienFilterService = new AktienFilterService(TwelveDataApiKey, TwelveDataService);

                        if (StandardAktien == null)
                        {
                            StandardAktien = new ObservableCollection<Aktie>();
                        }

                        return; // Frühzeitig beenden, da wir erfolgreich waren
                    }
                    catch (Exception fallbackEx)
                    {
                        Debug.WriteLine($"Auch der Fallback-Versuch ist fehlgeschlagen: {fallbackEx.Message}");
                        // Weiter zum normalen Fehlerhandling
                    }
                }

                MessageBox.Show($"Fehler bei der Initialisierung: {ex.Message}\n\n" +
                                $"Details: {ex.InnerException?.Message ?? "Keine weiteren Details verfügbar."}\n\n" +
                                "Bitte stellen Sie sicher, dass SQL Server LocalDB installiert ist und die Verbindungszeichenfolge korrekt ist.",
                                "Initialisierungsfehler",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
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