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

        public DatabaseService(BörsenplattformDbContext context)
        {
            _context = context;
        }

        #region Benutzer

        public async Task<Benutzer> GetBenutzerByIdAsync(int id)
        {
            return await _context.Benutzer.FindAsync(id);
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

                // Zuerst versuchen, alle Benutzer zu laden (sollte für kleine Datenmengen funktionieren)
                var alleBenutzer = await _context.Benutzer.ToListAsync();
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
            return await _context.Benutzer
                .FirstOrDefaultAsync(b => b.Benutzername.ToLower() == username.ToLower());
        }

        public async Task<Benutzer> GetBenutzerByEmailAsync(string email)
        {
            return await _context.Benutzer
                .FirstOrDefaultAsync(b => b.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> AddBenutzerAsync(Benutzer benutzer)
        {
            try
            {
                _context.Benutzer.Add(benutzer);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateBenutzerAsync(Benutzer benutzer)
        {
            try
            {
                _context.Benutzer.Update(benutzer);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Aktien

        public async Task<List<Aktie>> GetAllAktienAsync()
        {
            return await _context.Aktien.ToListAsync();
        }

        public async Task<Aktie> GetAktieBySymbolAsync(string symbol)
        {
            return await _context.Aktien
                .FirstOrDefaultAsync(a => a.AktienSymbol.ToUpper() == symbol.ToUpper());
        }

        public async Task<Aktie> GetAktieByIdAsync(int id)
        {
            return await _context.Aktien.FindAsync(id);
        }

        public async Task<bool> UpdateAktieAsync(Aktie aktie)
        {
            try
            {
                var existingAktie = await _context.Aktien.FindAsync(aktie.AktienID);
                if (existingAktie == null)
                {
                    _context.Aktien.Add(aktie);
                }
                else
                {
                    existingAktie.AktuellerPreis = aktie.AktuellerPreis;
                    existingAktie.Änderung = aktie.Änderung;
                    existingAktie.ÄnderungProzent = aktie.ÄnderungProzent;
                    existingAktie.LetzteAktualisierung = aktie.LetzteAktualisierung;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateAktienBatchAsync(List<Aktie> aktien)
        {
            try
            {
                foreach (var aktie in aktien)
                {
                    var existingAktie = await _context.Aktien
                        .FirstOrDefaultAsync(a => a.AktienSymbol == aktie.AktienSymbol);

                    if (existingAktie == null)
                    {
                        _context.Aktien.Add(aktie);
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

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Portfolio

        public async Task<List<PortfolioEintrag>> GetPortfolioByBenutzerIdAsync(int benutzerId)
        {
            return await _context.PortfolioEintraege
                .Where(p => p.BenutzerID == benutzerId)
                .ToListAsync();
        }

        public async Task<bool> AddOrUpdatePortfolioEintragAsync(PortfolioEintrag eintrag)
        {
            try
            {
                var existingEintrag = await _context.PortfolioEintraege
                    .FirstOrDefaultAsync(p => p.BenutzerID == eintrag.BenutzerID && p.AktienID == eintrag.AktienID);

                if (existingEintrag == null)
                {
                    _context.PortfolioEintraege.Add(eintrag);
                }
                else
                {
                    existingEintrag.Anzahl = eintrag.Anzahl;
                    existingEintrag.AktuellerKurs = eintrag.AktuellerKurs;
                    existingEintrag.EinstandsPreis = eintrag.EinstandsPreis;
                    existingEintrag.LetzteAktualisierung = eintrag.LetzteAktualisierung;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdatePortfolioKurseAsync(int benutzerId, Dictionary<int, decimal> aktienKurse)
        {
            try
            {
                var portfolio = await _context.PortfolioEintraege
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

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RemovePortfolioEintragAsync(int benutzerId, int aktienId)
        {
            try
            {
                var eintrag = await _context.PortfolioEintraege
                    .FirstOrDefaultAsync(p => p.BenutzerID == benutzerId && p.AktienID == aktienId);

                if (eintrag != null)
                {
                    _context.PortfolioEintraege.Remove(eintrag);
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
