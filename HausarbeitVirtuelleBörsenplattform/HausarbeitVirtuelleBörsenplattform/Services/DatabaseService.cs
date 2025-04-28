using System;
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
            return new BörsenplattformDbContext(_options);
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
                context.Benutzer.Update(benutzer);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren des Benutzers: {ex.Message}");
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

        #endregion




        #endregion
    }
}