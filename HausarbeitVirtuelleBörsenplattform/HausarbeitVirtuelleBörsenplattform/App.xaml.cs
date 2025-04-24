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

namespace HausarbeitVirtuelleBörsenplattform
{
    public partial class App : Application
    {
        public static AuthenticationService AuthService { get; private set; }
        public static DatabaseService DbService { get; private set; }
        public static EmailService EmailService { get; private set; }
        public static string TwelveDataApiKey { get; private set; }
        public static TwelveDataService TwelveDataService { get; private set; }

        // Statische Liste von Standard-Aktien, die überall in der App verwendet werden kann
        public static ObservableCollection<Aktie> StandardAktien { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Konfiguration laden
            LoadConfiguration();

            // Standard-Aktien initialisieren (leer, werden später gefüllt)
            StandardAktien = new ObservableCollection<Aktie>();

            // Services initialisieren
            InitializeServices();

            // Marktdaten beim Start laden
            _ = LadeAktuelleMarktdaten();

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
                    InitializeDefaultAktien();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Marktdaten: {ex.Message}");
                InitializeDefaultAktien();
            }
        }

        /// <summary>
        /// Initialisiert Dummy-Aktien als Fallback
        /// </summary>
        private void InitializeDefaultAktien()
        {
            StandardAktien.Clear();
            StandardAktien.Add(new Aktie { AktienID = 1, AktienSymbol = "AAPL", AktienName = "Apple Inc.", AktuellerPreis = 150.00m, Änderung = 1.25m, ÄnderungProzent = 0.84m, LetzteAktualisierung = DateTime.Now });
            StandardAktien.Add(new Aktie { AktienID = 2, AktienSymbol = "MSFT", AktienName = "Microsoft Corp.", AktuellerPreis = 320.45m, Änderung = 4.75m, ÄnderungProzent = 1.50m, LetzteAktualisierung = DateTime.Now });
            StandardAktien.Add(new Aktie { AktienID = 3, AktienSymbol = "TSLA", AktienName = "Tesla Inc.", AktuellerPreis = 200.20m, Änderung = -0.70m, ÄnderungProzent = -0.35m, LetzteAktualisierung = DateTime.Now });
            StandardAktien.Add(new Aktie { AktienID = 4, AktienSymbol = "AMZN", AktienName = "Amazon.com Inc.", AktuellerPreis = 95.10m, Änderung = 0.72m, ÄnderungProzent = 0.76m, LetzteAktualisierung = DateTime.Now });
            StandardAktien.Add(new Aktie { AktienID = 5, AktienSymbol = "GOOGL", AktienName = "Alphabet Inc.", AktuellerPreis = 128.75m, Änderung = -0.28m, ÄnderungProzent = -0.22m, LetzteAktualisierung = DateTime.Now });
        }

        private void LoadConfiguration()
        {
            try
            {
                TwelveDataApiKey = ConfigurationManager.AppSettings["TwelveDataApiKey"];
                if (string.IsNullOrEmpty(TwelveDataApiKey))
                    TwelveDataApiKey = "cb617aba18ea46b3a974d878d3c7310b"; // Fallback
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Konfiguration: {ex.Message}", "Konfigurationsfehler",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void InitializeServices()
        {
            try
            {
                var options = new DbContextOptionsBuilder<BörsenplattformDbContext>()
                    .UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=VirtuelleBörsenplattform;Trusted_Connection=True;TrustServerCertificate=True")
                    .EnableSensitiveDataLogging()
                    .Options;

                var dbContext = new BörsenplattformDbContext(options);

                Debug.WriteLine("Prüfe Datenbankverbindung...");
                if (dbContext.Database.CanConnect())
                {
                    Debug.WriteLine("Datenbankverbindung ist möglich.");
                    DbService = new DatabaseService(dbContext);
                    AuthService = new AuthenticationService(DbService);
                }
                else
                {
                    throw new Exception("Datenbankverbindung fehlgeschlagen.");
                }

                // Konfiguriere EmailService mit den korrekten SMTP-Einstellungen
                EmailService = new EmailService(
                    "smtp.web.de", 587,
                    "Jannisr32@web.de", "KSIYD2GR4AIKOMIH7ZCK",
                    "Jannisr32@web.de", "Virtuelle Börsenplattform",
                    false); // isTestMode auf false setzen, um echte E-Mails zu senden

                // TwelveDataService initialisieren
                TwelveDataService = new TwelveDataService(TwelveDataApiKey);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler bei der Initialisierung der Services: {ex.Message}",
                    "Initialisierungsfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                AuthService = new AuthenticationService(null);
                EmailService = new EmailService("localhost", 25, "", "", "", "", true);
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

                oldWindow?.Close(); // vollständig schließen statt nur verstecken

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