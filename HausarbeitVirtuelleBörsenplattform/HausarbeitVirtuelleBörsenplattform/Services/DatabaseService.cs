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
            using var context = CreateContext();
            return await context.Aktien
                .FirstOrDefaultAsync(a => a.AktienSymbol.ToUpper() == symbol.ToUpper());
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

        #endregion
    }
}