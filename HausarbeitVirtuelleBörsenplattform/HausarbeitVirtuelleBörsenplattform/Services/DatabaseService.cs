﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HausarbeitVirtuelleBörsenplattform.Data;
using HausarbeitVirtuelleBörsenplattform.Models;
using System.Diagnostics;

namespace HausarbeitVirtuelleBörsenplattform.Services
{
    public class DatabaseService
    {
        private readonly BörsenplattformDbContext _context;
        private readonly DbContextOptions<BörsenplattformDbContext> _options;

        public DatabaseService(BörsenplattformDbContext context, DbContextOptions<BörsenplattformDbContext> options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context), "Der Datenbankkontext darf nicht null sein.");
            _options = options ?? throw new ArgumentNullException(nameof(options), "Die Datenbankkontext-Optionen dürfen nicht null sein.");
        }

        // Hilfsmethode, um einen neuen Kontext für jede Operation zu erstellen
        private BörsenplattformDbContext CreateContext()
        {
            try
            {
                var context = new BörsenplattformDbContext(_options);

                // Aktiviere detaillierte Fehlerprotokollierung für SQL Server
                context.Database.SetCommandTimeout(60); // Längeres Timeout für alle Befehle

                return context;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Erstellen des Datenbankkontexts: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Innere Ausnahme: {ex.InnerException.Message}");
                }

                // Trotz Fehler einen neuen Kontext zurückgeben - dieser wird mit Standardwerten initialisiert
                return new BörsenplattformDbContext(_options);
            }
        }

        /// <summary>
        /// Stellt sicher, dass die Datenbank existiert und erstellt Tabellen falls notwendig
        /// </summary>
        public async Task EnsureDatabaseCreatedAsync()
        {
            try
            {
                using var context = CreateContext();

                // Stellt sicher, dass die Datenbank existiert
                bool created = await context.Database.EnsureCreatedAsync();

                if (created)
                {
                    Debug.WriteLine("Datenbank wurde neu erstellt");
                }
                else
                {
                    Debug.WriteLine("Datenbank existiert bereits");
                }

                // Benutzer-Tabelle explizit erstellen/überprüfen (wichtig!)
                await EnsureBenutzerTabelleExistiertAsync();

                // Aktien-Tabelle explizit erstellen/überprüfen
                await EnsureAktienTabelleExistiertAsync();

                // Portfolio-Tabelle explizit erstellen/überprüfen
                await EnsurePortfolioEintraegeExistiertAsync();

                // Watchlist-Tabelle explizit erstellen/überprüfen
                await EnsureWatchlistEintraegeExistiertAsync();

                // AktienKursHistorie-Tabelle explizit erstellen/überprüfen
                await EnsureAktienKursHistorieExistiertAsync();

                // Historische Daten Tabelle explizit erstellen/überprüfen
                await EnsureHistorischeDatenTabelleExistiertAsync();

                // Sicherstellen, dass alle Standard-Aktien in der Datenbank vorhanden sind
                await EnsureAllStandardAktienExistAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Sicherstellen der Datenbank: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Innere Ausnahme: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Stellt sicher, dass die Tabelle für historische Daten existiert
        /// </summary>
        private async Task EnsureHistorischeDatenTabelleExistiertAsync()
        {
            try
            {
                using var context = CreateContext();

                // SQL zur Überprüfung und Erstellung der Tabelle
                var sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'HistorischeDatenErweitert')
                BEGIN
                    CREATE TABLE HistorischeDatenErweitert (
                        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        AktieId int NOT NULL,
                        Datum datetime2 NOT NULL,
                        Eröffnungskurs decimal(18,2) NOT NULL,
                        Höchstkurs decimal(18,2) NOT NULL,
                        Tiefstkurs decimal(18,2) NOT NULL,
                        Schlusskurs decimal(18,2) NOT NULL,
                        ÄnderungProzent decimal(18,2) NOT NULL,
                        Volumen bigint NULL,
                        Intervall nvarchar(20) NOT NULL,
                        ErstelltAm datetime2 NOT NULL DEFAULT GETDATE(),
                        AktualisiertAm datetime2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_HistorischeDatenErweitert_Aktien FOREIGN KEY (AktieId) REFERENCES Aktien(AktienID)
                    );

                    -- Indizes erstellen
                    CREATE INDEX IX_HistorischeDatenErweitert_AktieId ON HistorischeDatenErweitert(AktieId);
                    CREATE INDEX IX_HistorischeDatenErweitert_Datum ON HistorischeDatenErweitert(Datum);
                    CREATE INDEX IX_HistorischeDatenErweitert_AktieId_Datum ON HistorischeDatenErweitert(AktieId, Datum);

                    PRINT 'Tabelle HistorischeDatenErweitert wurde erstellt';
                END
                ELSE
                BEGIN
                    PRINT 'Tabelle HistorischeDatenErweitert existiert bereits';
                END";

                await context.Database.ExecuteSqlRawAsync(sql);
                Debug.WriteLine("SQL zur Erstellung der HistorischeDatenErweitert-Tabelle wurde ausgeführt");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Erstellen der HistorischeDatenErweitert-Tabelle: {ex.Message}");
                // Fehler protokollieren, aber App weiterlaufen lassen
            }
        }

        /// <summary>
        /// Stellt sicher, dass die Benutzer-Tabelle existiert
        /// </summary>
        private async Task EnsureBenutzerTabelleExistiertAsync()
        {
            try
            {
                using var context = CreateContext();

                // Zuerst prüfen, ob die Tabelle existiert
                var checkSql = @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Benutzer')
                    SELECT 1
                ELSE
                    SELECT 0";

                // Verbindung öffnen und Abfrage ausführen
                var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = checkSql;

                if (command.Connection.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();

                var result = await command.ExecuteScalarAsync();
                bool tableExists = Convert.ToBoolean(result);

                if (!tableExists)
                {
                    Debug.WriteLine("Benutzer-Tabelle existiert nicht, erstelle sie...");

                    // Die Benutzer-Tabelle manuell erstellen
                    var createTableCmd = context.Database.GetDbConnection().CreateCommand();
                    createTableCmd.CommandText = @"
                    CREATE TABLE Benutzer (
                        BenutzerID int IDENTITY(1,1) PRIMARY KEY,
                        Benutzername nvarchar(50) NOT NULL,
                        Email nvarchar(100) NOT NULL,
                        PasswortHash nvarchar(255) NOT NULL,
                        Kontostand decimal(18,2) NOT NULL DEFAULT 10000.00,
                        Erstellungsdatum datetime NOT NULL DEFAULT GETDATE(),
                        Vorname nvarchar(50) NULL,
                        Nachname nvarchar(50) NULL,
                        VollName nvarchar(100) NULL
                    );

                    -- Unique-Indices
                    CREATE UNIQUE INDEX IX_Benutzer_Benutzername ON Benutzer(Benutzername);
                    CREATE UNIQUE INDEX IX_Benutzer_Email ON Benutzer(Email);

                    -- Demo- und Admin-Benutzer einfügen
                    INSERT INTO Benutzer (Benutzername, Email, PasswortHash, Kontostand, Erstellungsdatum, Vorname, Nachname, VollName)
                    VALUES ('admin', 'admin@example.com', '$2a$12$eTxedgRvWVqcV9gOJ5ZOz.zqbTLwc7E0gIOZTSLVMPzb0OFaZqNQK', 10000.00, DATEADD(day, -30, GETDATE()), 'Admin', 'User', 'Admin User');

                    INSERT INTO Benutzer (Benutzername, Email, PasswortHash, Kontostand, Erstellungsdatum, Vorname, Nachname, VollName)
                    VALUES ('demo', 'demo@example.com', '$2a$12$T30V4QZDsHRbGHqLPBPwleF0K27z0CFkFRgYLBVT8G3V36Ou.wJbu', 10000.00, DATEADD(day, -15, GETDATE()), 'Demo', 'User', 'Demo User');";

                    await createTableCmd.ExecuteNonQueryAsync();
                    Debug.WriteLine("Benutzer-Tabelle erfolgreich erstellt mit Admin- und Demo-Benutzer");
                }
                else
                {
                    Debug.WriteLine("Benutzer-Tabelle existiert bereits");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Erstellen der Benutzer-Tabelle: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Innerer Fehler: {ex.InnerException.Message}");
                }
                // Fehler protokollieren, aber App weiterlaufen lassen
            }
        }

        /// <summary>
        /// Stellt sicher, dass die Aktien-Tabelle existiert
        /// </summary>
        private async Task EnsureAktienTabelleExistiertAsync()
        {
            try
            {
                using var context = CreateContext();

                // Zuerst prüfen, ob die Tabelle existiert
                var checkSql = @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Aktien')
                    SELECT 1
                ELSE
                    SELECT 0";

                // Verbindung öffnen und Abfrage ausführen
                var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = checkSql;

                if (command.Connection.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();

                var result = await command.ExecuteScalarAsync();
                bool tableExists = Convert.ToBoolean(result);

                if (!tableExists)
                {
                    Debug.WriteLine("Aktien-Tabelle existiert nicht, erstelle sie...");

                    // Die Aktien-Tabelle manuell erstellen
                    var createTableCmd = context.Database.GetDbConnection().CreateCommand();
                    createTableCmd.CommandText = @"
                    CREATE TABLE Aktien (
                        AktienID int IDENTITY(1,1) PRIMARY KEY,
                        AktienSymbol nvarchar(10) NOT NULL,
                        AktienName nvarchar(100) NOT NULL,
                        AktuellerPreis decimal(18,2) NOT NULL DEFAULT 0,
                        Änderung decimal(18,2) NOT NULL DEFAULT 0,
                        ÄnderungProzent decimal(18,2) NOT NULL DEFAULT 0,
                        LetzteAktualisierung datetime NOT NULL DEFAULT GETDATE()
                    );

                    -- Unique-Index
                    CREATE UNIQUE INDEX IX_Aktien_AktienSymbol ON Aktien(AktienSymbol);";

                    await createTableCmd.ExecuteNonQueryAsync();
                    Debug.WriteLine("Aktien-Tabelle erfolgreich erstellt");
                }
                else
                {
                    Debug.WriteLine("Aktien-Tabelle existiert bereits");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Erstellen der Aktien-Tabelle: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Innerer Fehler: {ex.InnerException.Message}");
                }
                // Fehler protokollieren, aber App weiterlaufen lassen
            }
        }

        /// <summary>
        /// Stellt sicher, dass die Portfolio-Tabelle existiert
        /// </summary>
        private async Task EnsurePortfolioEintraegeExistiertAsync()
        {
            try
            {
                using var context = CreateContext();

                // Zuerst prüfen, ob die Tabelle existiert
                var checkSql = @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PortfolioEintraege')
                    SELECT 1
                ELSE
                    SELECT 0";

                // Verbindung öffnen und Abfrage ausführen
                var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = checkSql;

                if (command.Connection.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();

                var result = await command.ExecuteScalarAsync();
                bool tableExists = Convert.ToBoolean(result);

                if (!tableExists)
                {
                    Debug.WriteLine("PortfolioEintraege-Tabelle existiert nicht, erstelle sie...");

                    // Die PortfolioEintraege-Tabelle manuell erstellen
                    var createTableCmd = context.Database.GetDbConnection().CreateCommand();
                    createTableCmd.CommandText = @"
                    CREATE TABLE PortfolioEintraege (
                        BenutzerID int NOT NULL,
                        AktienID int NOT NULL,
                        AktienSymbol nvarchar(10) NOT NULL,
                        AktienName nvarchar(100) NOT NULL,
                        Anzahl int NOT NULL DEFAULT 0,
                        EinstandsPreis decimal(18,2) NOT NULL DEFAULT 0,
                        AktuellerKurs decimal(18,2) NOT NULL DEFAULT 0,
                        LetzteAktualisierung datetime NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT PK_PortfolioEintraege PRIMARY KEY (BenutzerID, AktienID),
                        CONSTRAINT FK_PortfolioEintraege_Benutzer FOREIGN KEY (BenutzerID) REFERENCES Benutzer(BenutzerID) ON DELETE CASCADE,
                        CONSTRAINT FK_PortfolioEintraege_Aktien FOREIGN KEY (AktienID) REFERENCES Aktien(AktienID) ON DELETE NO ACTION
                    );";

                    await createTableCmd.ExecuteNonQueryAsync();
                    Debug.WriteLine("PortfolioEintraege-Tabelle erfolgreich erstellt");
                }
                else
                {
                    Debug.WriteLine("PortfolioEintraege-Tabelle existiert bereits");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Erstellen der PortfolioEintraege-Tabelle: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Innerer Fehler: {ex.InnerException.Message}");
                }
                // Fehler protokollieren, aber App weiterlaufen lassen
            }
        }

        /// <summary>
        /// Stellt sicher, dass die Watchlist-Tabelle existiert
        /// </summary>
        private async Task EnsureWatchlistEintraegeExistiertAsync()
        {
            try
            {
                using var context = CreateContext();

                // Zuerst prüfen, ob die Tabelle existiert
                var checkSql = @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'WatchlistEintraege')
                    SELECT 1
                ELSE
                    SELECT 0";

                // Verbindung öffnen und Abfrage ausführen
                var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = checkSql;

                if (command.Connection.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();

                var result = await command.ExecuteScalarAsync();
                bool tableExists = Convert.ToBoolean(result);

                if (!tableExists)
                {
                    Debug.WriteLine("WatchlistEintraege-Tabelle existiert nicht, erstelle sie...");

                    // Die WatchlistEintraege-Tabelle manuell erstellen
                    var createTableCmd = context.Database.GetDbConnection().CreateCommand();
                    createTableCmd.CommandText = @"
                    CREATE TABLE WatchlistEintraege (
                        BenutzerID int NOT NULL,
                        AktienID int NOT NULL,
                        AktienSymbol nvarchar(10) NOT NULL,
                        AktienName nvarchar(100) NOT NULL,
                        HinzugefuegtAm datetime NOT NULL DEFAULT GETDATE(),
                        KursBeimHinzufuegen decimal(18,2) NOT NULL DEFAULT 0,
                        AktuellerKurs decimal(18,2) NOT NULL DEFAULT 0,
                        LetzteAktualisierung datetime NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT PK_WatchlistEintraege PRIMARY KEY (BenutzerID, AktienID),
                        CONSTRAINT FK_WatchlistEintraege_Benutzer FOREIGN KEY (BenutzerID) REFERENCES Benutzer(BenutzerID) ON DELETE CASCADE,
                        CONSTRAINT FK_WatchlistEintraege_Aktien FOREIGN KEY (AktienID) REFERENCES Aktien(AktienID) ON DELETE NO ACTION
                    );";

                    await createTableCmd.ExecuteNonQueryAsync();
                    Debug.WriteLine("WatchlistEintraege-Tabelle erfolgreich erstellt");
                }
                else
                {
                    Debug.WriteLine("WatchlistEintraege-Tabelle existiert bereits");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Erstellen der WatchlistEintraege-Tabelle: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Innerer Fehler: {ex.InnerException.Message}");
                }
                // Fehler protokollieren, aber App weiterlaufen lassen
            }
        }

        /// <summary>
        /// Stellt sicher, dass die AktienKursHistorie-Tabelle existiert
        /// </summary>
        private async Task EnsureAktienKursHistorieExistiertAsync()
        {
            try
            {
                using var context = CreateContext();

                // Zuerst prüfen, ob die Tabelle existiert
                var checkSql = @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AktienKursHistorie')
                    SELECT 1
                ELSE
                    SELECT 0";

                // Verbindung öffnen und Abfrage ausführen
                var command = context.Database.GetDbConnection().CreateCommand();
                command.CommandText = checkSql;

                if (command.Connection.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();

                var result = await command.ExecuteScalarAsync();
                bool tableExists = Convert.ToBoolean(result);

                if (!tableExists)
                {
                    Debug.WriteLine("AktienKursHistorie-Tabelle existiert nicht, erstelle sie...");

                    // Die AktienKursHistorie-Tabelle manuell erstellen
                    var createTableCmd = context.Database.GetDbConnection().CreateCommand();
                    createTableCmd.CommandText = @"
                    CREATE TABLE AktienKursHistorie (
                        HistorieID int IDENTITY(1,1) PRIMARY KEY,
                        AktienID int NOT NULL,
                        AktienSymbol nvarchar(10) NOT NULL,
                        Datum datetime NOT NULL,
                        Eroeffnungskurs decimal(18,2) NOT NULL,
                        Hoechstkurs decimal(18,2) NOT NULL,
                        Tiefstkurs decimal(18,2) NOT NULL,
                        Schlusskurs decimal(18,2) NOT NULL,
                        Volumen bigint NULL,
                        ÄnderungProzent decimal(18,2) NOT NULL DEFAULT 0,
                        ErstelltAm datetime NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_AktienKursHistorie_Aktien FOREIGN KEY (AktienID) REFERENCES Aktien(AktienID) ON DELETE NO ACTION
                    );

                    -- Indices für Performance
                    CREATE INDEX IX_AktienKursHistorie_AktienID ON AktienKursHistorie(AktienID);
                    CREATE INDEX IX_AktienKursHistorie_Datum ON AktienKursHistorie(Datum);
                    CREATE INDEX IX_AktienKursHistorie_AktienSymbol_Datum ON AktienKursHistorie(AktienSymbol, Datum);";

                    await createTableCmd.ExecuteNonQueryAsync();
                    Debug.WriteLine("AktienKursHistorie-Tabelle erfolgreich erstellt");
                }
                else
                {
                    Debug.WriteLine("AktienKursHistorie-Tabelle existiert bereits");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Erstellen der AktienKursHistorie-Tabelle: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Innerer Fehler: {ex.InnerException.Message}");
                }
                // Fehler protokollieren, aber App weiterlaufen lassen
            }
        }

        #region Benutzer

        public async Task<Benutzer> GetBenutzerByIdAsync(int id)
        {
            using var context = CreateContext();
            return await context.Benutzer.FindAsync(id);
        }

        public async Task<Benutzer> GetBenutzerByUsernameOrEmailAsync(string usernameOrEmail)
        {
            try
            {
                Debug.WriteLine($"Suche Benutzer mit Username/Email: {usernameOrEmail}");

                if (string.IsNullOrEmpty(usernameOrEmail))
                {
                    Debug.WriteLine("Leerer Suchbegriff übergeben");
                    return null;
                }

                using var context = CreateContext();

                // Zuerst versuchen, alle Benutzer zu laden (sollte für kleine Datenmengen funktionieren)
                var alleBenutzer = await context.Benutzer.ToListAsync();
                Debug.WriteLine($"Alle Benutzer geladen: {alleBenutzer.Count} gefunden");

                // Direkter Vergleich im Speicher
                var benutzer = alleBenutzer.FirstOrDefault(b =>
                    string.Equals(b.Benutzername, usernameOrEmail, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(b.Email, usernameOrEmail, StringComparison.OrdinalIgnoreCase));

                Debug.WriteLine($"Benutzer gefunden: {benutzer != null}");
                return benutzer;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Abrufen des Benutzers: {ex.Message}");
                Debug.WriteLine($"Stacktrace: {ex.StackTrace}");

                // Im Fehlerfall einen Notfall-Dummy-Benutzer zurückgeben, wenn es sich um den Demo-Benutzer handelt
                if (usernameOrEmail.Equals("demo", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine("Erstelle Notfall-Demo-Benutzer");
                    return new Benutzer
                    {
                        BenutzerID = 2,
                        Benutzername = "demo",
                        Email = "demo@example.com",
                        PasswortHash = "$2a$12$T30V4QZDsHRbGHqLPBPwleF0K27z0CFkFRgYLBVT8G3V36Ou.wJbu", // Hash für "demo"
                        Kontostand = 10000.00m,
                        Erstellungsdatum = DateTime.Now.AddDays(-15),
                        Vorname = "Demo",
                        Nachname = "User",
                        VollName = "Demo User"
                    };
                }
                return null;
            }
        }

        public async Task<Benutzer> GetBenutzerByUsernameAsync(string username)
        {
            using var context = CreateContext();
            return await context.Benutzer
                .FirstOrDefaultAsync(b => b.Benutzername.ToLower() == username.ToLower());
        }

        public async Task<Benutzer> GetBenutzerByEmailAsync(string email)
        {
            using var context = CreateContext();
            return await context.Benutzer
                .FirstOrDefaultAsync(b => b.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> AddBenutzerAsync(Benutzer benutzer)
        {
            try
            {
                using var context = CreateContext();
                context.Benutzer.Add(benutzer);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Hinzufügen des Benutzers: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateBenutzerAsync(Benutzer benutzer)
        {
            try
            {
                using var context = CreateContext();

                // Den Benutzer zuerst aus der Datenbank holen
                var existingBenutzer = await context.Benutzer.FindAsync(benutzer.BenutzerID);

                if (existingBenutzer == null)
                {
                    Debug.WriteLine($"Benutzer mit ID {benutzer.BenutzerID} nicht gefunden");
                    return false;
                }

                // Eigenschaften aktualisieren
                existingBenutzer.PasswortHash = benutzer.PasswortHash;
                existingBenutzer.Email = benutzer.Email;
                existingBenutzer.Vorname = benutzer.Vorname;
                existingBenutzer.Nachname = benutzer.Nachname;
                existingBenutzer.VollName = benutzer.VollName;
                existingBenutzer.Kontostand = benutzer.Kontostand;

                // Änderungen speichern
                await context.SaveChangesAsync();

                Debug.WriteLine($"Benutzer mit ID {benutzer.BenutzerID} erfolgreich aktualisiert");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren des Benutzers: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // In DatabaseService eine neue Methode hinzufügen
        public async Task<bool> DeleteBenutzerAsync(int benutzerId)
        {
            try
            {
                using var context = CreateContext();

                // Benutzer suchen
                var benutzer = await context.Benutzer.FindAsync(benutzerId);

                if (benutzer == null)
                {
                    Debug.WriteLine($"Benutzer mit ID {benutzerId} nicht gefunden");
                    return false;
                }

                // Benutzer löschen
                context.Benutzer.Remove(benutzer);
                await context.SaveChangesAsync();

                Debug.WriteLine($"Benutzer mit ID {benutzerId} wurde gelöscht");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Löschen des Benutzers: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Aktien

        public async Task<List<Aktie>> GetAllAktienAsync()
        {
            using var context = CreateContext();
            return await context.Aktien.ToListAsync();
        }

        public async Task<Aktie> GetAktieBySymbolAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                Debug.WriteLine("GetAktieBySymbolAsync: Symbol ist leer oder null");
                return null;
            }

            try
            {
                using var context = CreateContext();
                var aktie = await context.Aktien
                    .FirstOrDefaultAsync(a => a.AktienSymbol.ToUpper() == symbol.ToUpper());

                if (aktie == null)
                {
                    Debug.WriteLine($"GetAktieBySymbolAsync: Keine Aktie für Symbol {symbol} gefunden");

                    // Wenn keine Aktie gefunden wurde, prüfen wir die Standard-Aktienliste
                    var standardAktie = AktienListe.GetBekannteBörsenAktien()
                        .FirstOrDefault(a => a.AktienSymbol.ToUpper() == symbol.ToUpper());

                    if (standardAktie != null)
                    {
                        Debug.WriteLine($"GetAktieBySymbolAsync: Aktie {symbol} in Standard-Liste gefunden, füge sie zur Datenbank hinzu");

                        // Aktie aus Standard-Liste zur Datenbank hinzufügen
                        context.Aktien.Add(standardAktie);
                        await context.SaveChangesAsync();

                        // Aktie erneut aus Datenbank holen, um die generierte ID zu erhalten
                        return await context.Aktien
                            .FirstOrDefaultAsync(a => a.AktienSymbol.ToUpper() == symbol.ToUpper());
                    }
                }
                else
                {
                    Debug.WriteLine($"GetAktieBySymbolAsync: Aktie {symbol} gefunden mit ID {aktie.AktienID}");
                }

                return aktie;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler in GetAktieBySymbolAsync für Symbol {symbol}: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        // Neue Methode: GetOrCreateAktieBySymbolAsync
        public async Task<Aktie> GetOrCreateAktieBySymbolAsync(string symbol, string name = null, decimal initialPrice = 0)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                Debug.WriteLine("GetOrCreateAktieBySymbolAsync: Symbol ist leer oder null");
                return null;
            }

            try
            {
                // Zuerst versuchen, die Aktie zu finden
                var aktie = await GetAktieBySymbolAsync(symbol);

                // Wenn keine Aktie gefunden wurde, erstellen wir eine neue
                if (aktie == null)
                {
                    Debug.WriteLine($"GetOrCreateAktieBySymbolAsync: Erstelle neue Aktie für Symbol {symbol}");

                    using var context = CreateContext();
                    var neueAktie = new Aktie
                    {
                        AktienSymbol = symbol.ToUpper(),
                        AktienName = name ?? symbol.ToUpper(),
                        AktuellerPreis = initialPrice,
                        Änderung = 0,
                        ÄnderungProzent = 0,
                        LetzteAktualisierung = DateTime.Now
                    };

                    // Neue Aktie zur Datenbank hinzufügen
                    context.Aktien.Add(neueAktie);
                    await context.SaveChangesAsync();

                    // Aktie mit generierter ID zurückholen
                    aktie = await GetAktieBySymbolAsync(symbol);

                    if (aktie == null)
                    {
                        Debug.WriteLine($"GetOrCreateAktieBySymbolAsync: Fehler beim Erstellen der Aktie {symbol}");
                    }
                    else
                    {
                        Debug.WriteLine($"GetOrCreateAktieBySymbolAsync: Aktie {symbol} erstellt mit ID {aktie.AktienID}");
                    }
                }
                else
                {
                    Debug.WriteLine($"GetOrCreateAktieBySymbolAsync: Aktie {symbol} gefunden mit ID {aktie.AktienID}");

                    // Wenn die Aktie gefunden wurde, aber der Preis 0 ist, aktualisieren wir ihn
                    if (aktie.AktuellerPreis <= 0 && initialPrice > 0)
                    {
                        Debug.WriteLine($"GetOrCreateAktieBySymbolAsync: Aktualisiere Preis für {symbol} auf {initialPrice}");
                        aktie.AktuellerPreis = initialPrice;
                        await UpdateAktieAsync(aktie);
                    }
                }

                return aktie;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler in GetOrCreateAktieBySymbolAsync für Symbol {symbol}: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<Aktie> GetAktieByIdAsync(int id)
        {
            using var context = CreateContext();
            return await context.Aktien.FindAsync(id);
        }

        public async Task<bool> UpdateAktieAsync(Aktie aktie)
        {
            try
            {
                using var context = CreateContext();
                var existingAktie = await context.Aktien.FindAsync(aktie.AktienID);
                if (existingAktie == null)
                {
                    context.Aktien.Add(aktie);
                }
                else
                {
                    existingAktie.AktuellerPreis = aktie.AktuellerPreis;
                    existingAktie.Änderung = aktie.Änderung;
                    existingAktie.ÄnderungProzent = aktie.ÄnderungProzent;
                    existingAktie.LetzteAktualisierung = aktie.LetzteAktualisierung;
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der Aktie: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAktienBatchAsync(List<Aktie> aktien)
        {
            try
            {
                using var context = CreateContext();
                foreach (var aktie in aktien)
                {
                    var existingAktie = await context.Aktien
                        .FirstOrDefaultAsync(a => a.AktienSymbol == aktie.AktienSymbol);

                    if (existingAktie == null)
                    {
                        context.Aktien.Add(aktie);
                    }
                    else
                    {
                        existingAktie.AktienName = aktie.AktienName;
                        existingAktie.AktuellerPreis = aktie.AktuellerPreis;
                        existingAktie.Änderung = aktie.Änderung;
                        existingAktie.ÄnderungProzent = aktie.ÄnderungProzent;
                        existingAktie.LetzteAktualisierung = aktie.LetzteAktualisierung;
                    }
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Batch-Update der Aktien: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Portfolio

        public async Task<List<PortfolioEintrag>> GetPortfolioByBenutzerIdAsync(int benutzerId)
        {
            using var context = CreateContext();
            return await context.PortfolioEintraege
                .Where(p => p.BenutzerID == benutzerId)
                .ToListAsync();
        }

        public async Task<bool> AddOrUpdatePortfolioEintragAsync(PortfolioEintrag eintrag)
        {
            try
            {
                using var context = CreateContext();
                var existingEintrag = await context.PortfolioEintraege
                    .FirstOrDefaultAsync(p => p.BenutzerID == eintrag.BenutzerID && p.AktienID == eintrag.AktienID);

                if (existingEintrag == null)
                {
                    context.PortfolioEintraege.Add(eintrag);
                }
                else
                {
                    existingEintrag.Anzahl = eintrag.Anzahl;
                    existingEintrag.AktuellerKurs = eintrag.AktuellerKurs;
                    existingEintrag.EinstandsPreis = eintrag.EinstandsPreis;
                    existingEintrag.LetzteAktualisierung = eintrag.LetzteAktualisierung;
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Hinzufügen/Aktualisieren des Portfolio-Eintrags: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdatePortfolioKurseAsync(int benutzerId, Dictionary<int, decimal> aktienKurse)
        {
            try
            {
                using var context = CreateContext();
                var portfolio = await context.PortfolioEintraege
                    .Where(p => p.BenutzerID == benutzerId)
                    .ToListAsync();

                foreach (var eintrag in portfolio)
                {
                    if (aktienKurse.TryGetValue(eintrag.AktienID, out decimal neuerKurs))
                    {
                        eintrag.AktuellerKurs = neuerKurs;
                        eintrag.LetzteAktualisierung = DateTime.Now;
                    }
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der Portfolio-Kurse: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemovePortfolioEintragAsync(int benutzerId, int aktienId)
        {
            try
            {
                using var context = CreateContext();
                var eintrag = await context.PortfolioEintraege
                    .FirstOrDefaultAsync(p => p.BenutzerID == benutzerId && p.AktienID == aktienId);

                if (eintrag != null)
                {
                    context.PortfolioEintraege.Remove(eintrag);
                    await context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Entfernen des Portfolio-Eintrags: {ex.Message}");
                return false;
            }
        }

        // In der bestehenden DatabaseService.cs-Datei hinzufügen:

        #region Watchlist

        /// <summary>
        /// Ruft die Watchlist eines Benutzers aus der Datenbank ab
        /// </summary>
        /// <param name="benutzerId">ID des Benutzers</param>
        /// <returns>Liste der Watchlist-Einträge des Benutzers</returns>
        public async Task<List<WatchlistEintrag>> GetWatchlistByBenutzerIdAsync(int benutzerId)
        {
            using var context = CreateContext();
            return await context.WatchlistEintraege
                .Where(w => w.BenutzerID == benutzerId)
                .ToListAsync();
        }

        /// <summary>
        /// Fügt eine Aktie zur Watchlist eines Benutzers hinzu oder aktualisiert einen bestehenden Eintrag
        /// </summary>
        /// <param name="eintrag">Der Watchlist-Eintrag</param>
        /// <returns>True, wenn der Vorgang erfolgreich war, sonst False</returns>
        public async Task<bool> AddOrUpdateWatchlistEintragAsync(WatchlistEintrag eintrag)
        {
            try
            {
                using var context = CreateContext();
                var existingEintrag = await context.WatchlistEintraege
                    .FirstOrDefaultAsync(w => w.BenutzerID == eintrag.BenutzerID && w.AktienID == eintrag.AktienID);

                if (existingEintrag == null)
                {
                    context.WatchlistEintraege.Add(eintrag);
                }
                else
                {
                    existingEintrag.AktuellerKurs = eintrag.AktuellerKurs;
                    existingEintrag.LetzteAktualisierung = eintrag.LetzteAktualisierung;
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Hinzufügen/Aktualisieren des Watchlist-Eintrags: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Entfernt eine Aktie aus der Watchlist eines Benutzers
        /// </summary>
        /// <param name="benutzerId">ID des Benutzers</param>
        /// <param name="aktienId">ID der Aktie</param>
        /// <returns>True, wenn der Vorgang erfolgreich war, sonst False</returns>
        public async Task<bool> RemoveWatchlistEintragAsync(int benutzerId, int aktienId)
        {
            try
            {
                using var context = CreateContext();
                var eintrag = await context.WatchlistEintraege
                    .FirstOrDefaultAsync(w => w.BenutzerID == benutzerId && w.AktienID == aktienId);

                if (eintrag != null)
                {
                    context.WatchlistEintraege.Remove(eintrag);
                    await context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Entfernen des Watchlist-Eintrags: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Aktualisiert die Kurse aller Aktien in der Watchlist eines Benutzers
        /// </summary>
        /// <param name="benutzerId">ID des Benutzers</param>
        /// <param name="aktienKurse">Dictionary mit Aktien-IDs und ihren aktuellen Kursen</param>
        /// <returns>True, wenn der Vorgang erfolgreich war, sonst False</returns>
        public async Task<bool> UpdateWatchlistKurseAsync(int benutzerId, Dictionary<int, decimal> aktienKurse)
        {
            try
            {
                using var context = CreateContext();
                var watchlist = await context.WatchlistEintraege
                    .Where(w => w.BenutzerID == benutzerId)
                    .ToListAsync();

                foreach (var eintrag in watchlist)
                {
                    if (aktienKurse.TryGetValue(eintrag.AktienID, out decimal neuerKurs))
                    {
                        eintrag.AktuellerKurs = neuerKurs;
                        eintrag.LetzteAktualisierung = DateTime.Now;
                    }
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der Watchlist-Kurse: {ex.Message}");
                return false;
            }
        }


        public async Task CreateWatchlistTableIfNotExists()
        {
            try
            {
                using var context = CreateContext();

                // Prüfen, ob die Tabelle bereits existiert
                var sql = @"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WatchlistEintraege')
        BEGIN
            CREATE TABLE WatchlistEintraege (
                BenutzerID int NOT NULL,
                AktienID int NOT NULL,
                AktienSymbol nvarchar(10) NOT NULL,
                AktienName nvarchar(100) NOT NULL,
                HinzugefuegtAm datetime2 NOT NULL DEFAULT GETDATE(),
                KursBeimHinzufuegen decimal(18,2) NOT NULL,
                AktuellerKurs decimal(18,2) NOT NULL,
                LetzteAktualisierung datetime2 NOT NULL DEFAULT GETDATE(),
                CONSTRAINT PK_WatchlistEintraege PRIMARY KEY (BenutzerID, AktienID),
                CONSTRAINT FK_WatchlistEintraege_Benutzer FOREIGN KEY (BenutzerID) REFERENCES Benutzer(BenutzerID) ON DELETE CASCADE,
                CONSTRAINT FK_WatchlistEintraege_Aktien FOREIGN KEY (AktienID) REFERENCES Aktien(AktienID) ON DELETE NO ACTION
            );

            -- Default-Werte loggen
            PRINT 'Watchlist-Tabelle wurde erfolgreich erstellt';
        END
        ELSE
        BEGIN
            PRINT 'Watchlist-Tabelle existiert bereits';
        END";

                // SQL ausführen und nicht auf Fehler warten
                await context.Database.ExecuteSqlRawAsync(sql);
                Debug.WriteLine("SQL zur Erstellung der Watchlist-Tabelle wurde ausgeführt");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Erstellen der Watchlist-Tabelle: {ex.Message}");
                // Fehler protokollieren, aber App weiterlaufen lassen
            }
        }

        #region AktienKursHistorie

        /// <summary>
        /// Stellt sicher, dass alle Aktien aus der Standard-Aktienliste in der Datenbank existieren
        /// und für historische Daten verwendet werden können
        /// </summary>
        public async Task EnsureAllStandardAktienExistAsync()
        {
            try
            {
                using var context = CreateContext();
                var standardAktien = AktienListe.GetBekannteBörsenAktien();
                var dbAktien = await context.Aktien.ToListAsync();

                int hinzugefügt = 0;

                // Alle Standard-Aktien durchgehen
                foreach (var aktie in standardAktien)
                {
                    // Prüfen, ob die Aktie bereits in der Datenbank existiert
                    if (!dbAktien.Any(a => a.AktienSymbol.Equals(aktie.AktienSymbol, StringComparison.OrdinalIgnoreCase)))
                    {
                        // Neue Aktie zur Datenbank hinzufügen
                        context.Aktien.Add(new Aktie
                        {
                            AktienSymbol = aktie.AktienSymbol,
                            AktienName = aktie.AktienName,
                            AktuellerPreis = aktie.AktuellerPreis > 0 ? aktie.AktuellerPreis : 0.01m, // Sicherstellen, dass Preis > 0
                            Änderung = 0,
                            ÄnderungProzent = 0,
                            LetzteAktualisierung = DateTime.Now
                        });

                        hinzugefügt++;
                    }
                }

                if (hinzugefügt > 0)
                {
                    await context.SaveChangesAsync();
                    Debug.WriteLine($"{hinzugefügt} Standard-Aktien wurden zur Datenbank hinzugefügt");
                }
                else
                {
                    Debug.WriteLine("Alle Standard-Aktien sind bereits in der Datenbank vorhanden");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Sicherstellen der Standard-Aktien: {ex.Message}");
            }
        }

        /// <summary>
        /// Speichert einen neuen historischen Kurseintrag in der Datenbank
        /// </summary>
        /// <param name="historie">Der zu speichernde Kursverlauf</param>
        /// <returns>True, wenn erfolgreich gespeichert wurde, andernfalls false</returns>
        public async Task<bool> AddAktienKursHistorieAsync(AktienKursHistorie historie)
        {
            if (historie == null)
                return false;

            try
            {
                using var context = CreateContext();

                // Prüfen, ob bereits ein Eintrag für dieses Datum und diese Aktie existiert
                var existingHistorie = await context.AktienKursHistorie
                    .FirstOrDefaultAsync(h => h.AktienID == historie.AktienID &&
                                              h.Datum.Date == historie.Datum.Date);

                if (existingHistorie != null)
                {
                    // Eintrag aktualisieren
                    existingHistorie.Eroeffnungskurs = historie.Eroeffnungskurs;
                    existingHistorie.Hoechstkurs = historie.Hoechstkurs;
                    existingHistorie.Tiefstkurs = historie.Tiefstkurs;
                    existingHistorie.Schlusskurs = historie.Schlusskurs;
                    existingHistorie.Volumen = historie.Volumen;
                    existingHistorie.ÄnderungProzent = historie.ÄnderungProzent;
                }
                else
                {
                    // Neuen Eintrag hinzufügen
                    context.AktienKursHistorie.Add(historie);
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Speichern der Kurshistorie: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ruft die Kurshistorie für eine bestimmte Aktie in einem Zeitraum ab
        /// </summary>
        /// <param name="aktienID">Die ID der Aktie</param>
        /// <param name="startDatum">Das Startdatum</param>
        /// <param name="endDatum">Das Enddatum</param>
        /// <returns>Liste der historischen Kurse, nach Datum sortiert</returns>
        public async Task<List<AktienKursHistorie>> GetAktienKursHistorieAsync(int aktienID,
                                                                              DateTime startDatum,
                                                                              DateTime endDatum)
        {
            try
            {
                using var context = CreateContext();

                var historie = await context.AktienKursHistorie
                    .Where(h => h.AktienID == aktienID &&
                                h.Datum >= startDatum.Date &&
                                h.Datum <= endDatum.Date)
                    .OrderBy(h => h.Datum)
                    .ToListAsync();

                return historie;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Abrufen der Kurshistorie: {ex.Message}");
                return new List<AktienKursHistorie>();
            }
        }

        /// <summary>
        /// Ruft die Kurshistorie für eine bestimmte Aktie nach Symbol in einem Zeitraum ab
        /// </summary>
        /// <param name="aktienSymbol">Das Symbol der Aktie</param>
        /// <param name="startDatum">Das Startdatum</param>
        /// <param name="endDatum">Das Enddatum</param>
        /// <returns>Liste der historischen Kurse, nach Datum sortiert</returns>
        public async Task<List<AktienKursHistorie>> GetAktienKursHistorieBySymbolAsync(string aktienSymbol,
                                                                                     DateTime startDatum,
                                                                                     DateTime endDatum)
        {
            if (string.IsNullOrWhiteSpace(aktienSymbol))
                return new List<AktienKursHistorie>();

            try
            {
                using var context = CreateContext();

                var historie = await context.AktienKursHistorie
                    .Where(h => h.AktienSymbol.ToUpper() == aktienSymbol.ToUpper() &&
                                h.Datum >= startDatum.Date &&
                                h.Datum <= endDatum.Date)
                    .OrderBy(h => h.Datum)
                    .ToListAsync();

                return historie;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Abrufen der Kurshistorie für Symbol {aktienSymbol}: {ex.Message}");
                return new List<AktienKursHistorie>();
            }
        }

        /// <summary>
        /// Ruft die Kurshistorie für die letzten X Tage für eine Aktie ab
        /// </summary>
        /// <param name="aktienID">Die ID der Aktie</param>
        /// <param name="anzahlTage">Die Anzahl der Tage</param>
        /// <returns>Liste der historischen Kurse, nach Datum sortiert</returns>
        public async Task<List<AktienKursHistorie>> GetLetzteXTageKursHistorieAsync(int aktienID, int anzahlTage)
        {
            if (anzahlTage <= 0)
                anzahlTage = 5; // Standardwert: 5 Tage

            var endDatum = DateTime.Now.Date;
            var startDatum = endDatum.AddDays(-anzahlTage);

            return await GetAktienKursHistorieAsync(aktienID, startDatum, endDatum);
        }

        /// <summary>
        /// Speichert den aktuellen Kurs einer Aktie als historischen Eintrag für heute
        /// </summary>
        /// <param name="aktie">Die Aktie mit den aktuellen Kursdaten</param>
        /// <returns>True, wenn erfolgreich gespeichert wurde, andernfalls false</returns>
        public async Task<bool> SpeichereAktuellenKursAlsHistorieAsync(Aktie aktie)
        {
            if (aktie == null || aktie.AktienID <= 0)
                return false;

            try
            {
                // Schlusskurs als Referenzwert für alle Felder verwenden (vereinfachte Variante)
                var historieEintrag = new AktienKursHistorie
                {
                    AktienID = aktie.AktienID,
                    AktienSymbol = aktie.AktienSymbol,
                    Datum = DateTime.Now.Date,
                    Eroeffnungskurs = aktie.AktuellerPreis, // Vereinfacht - in einer realen API hätten wir OHLC-Daten
                    Hoechstkurs = aktie.AktuellerPreis,
                    Tiefstkurs = aktie.AktuellerPreis,
                    Schlusskurs = aktie.AktuellerPreis,
                    Volumen = 0, // Wir haben keine Volumendaten
                    ÄnderungProzent = aktie.ÄnderungProzent
                };

                return await AddAktienKursHistorieAsync(historieEintrag);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Speichern des aktuellen Kurses als Historie: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Speichert die aktuellen Kurse aller übergebenen Aktien als historische Einträge für heute
        /// </summary>
        /// <param name="aktien">Liste der Aktien mit aktuellen Kursdaten</param>
        /// <returns>Anzahl der erfolgreich gespeicherten Einträge</returns>
        public async Task<int> SpeichereAktuelleKurseAlsHistorieBatchAsync(List<Aktie> aktien)
        {
            if (aktien == null || !aktien.Any())
                return 0;

            int erfolgreich = 0;

            try
            {
                using var context = CreateContext();
                var heutigesDatum = DateTime.Now.Date;

                foreach (var aktie in aktien)
                {
                    if (aktie.AktienID <= 0 || string.IsNullOrEmpty(aktie.AktienSymbol))
                        continue;

                    try
                    {
                        // Prüfen, ob bereits ein Eintrag für heute existiert
                        var existingHistorie = await context.AktienKursHistorie
                            .FirstOrDefaultAsync(h => h.AktienID == aktie.AktienID &&
                                                      h.Datum.Date == heutigesDatum);

                        if (existingHistorie != null)
                        {
                            // Eintrag aktualisieren
                            existingHistorie.Schlusskurs = aktie.AktuellerPreis;
                            existingHistorie.ÄnderungProzent = aktie.ÄnderungProzent;

                            // Höchst- und Tiefstkurs aktualisieren, falls nötig
                            if (aktie.AktuellerPreis > existingHistorie.Hoechstkurs)
                                existingHistorie.Hoechstkurs = aktie.AktuellerPreis;

                            if (aktie.AktuellerPreis < existingHistorie.Tiefstkurs)
                                existingHistorie.Tiefstkurs = aktie.AktuellerPreis;
                        }
                        else
                        {
                            // Neuen Eintrag erstellen
                            var neuerEintrag = new AktienKursHistorie
                            {
                                AktienID = aktie.AktienID,
                                AktienSymbol = aktie.AktienSymbol,
                                Datum = heutigesDatum,
                                Eroeffnungskurs = aktie.AktuellerPreis,
                                Hoechstkurs = aktie.AktuellerPreis,
                                Tiefstkurs = aktie.AktuellerPreis,
                                Schlusskurs = aktie.AktuellerPreis,
                                Volumen = 0,
                                ÄnderungProzent = aktie.ÄnderungProzent
                            };

                            context.AktienKursHistorie.Add(neuerEintrag);
                        }

                        erfolgreich++;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Fehler beim Speichern der Historie für Aktie {aktie.AktienSymbol}: {ex.Message}");
                    }
                }

                await context.SaveChangesAsync();
                return erfolgreich;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Batch-Speichern der Kurshistorie: {ex.Message}");
                return erfolgreich;
            }
        }

        #endregion

        #endregion




        #endregion
    }
}