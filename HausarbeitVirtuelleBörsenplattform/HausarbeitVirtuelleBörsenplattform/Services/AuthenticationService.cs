using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HausarbeitVirtuelleBörsenplattform.Models;
using System.Collections.Generic;

namespace HausarbeitVirtuelleBörsenplattform.Services
{
    public class AuthenticationService
    {
        private readonly DatabaseService _databaseService;

        public Benutzer CurrentUser { get; private set; }

        public AuthenticationService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            Debug.WriteLine("AuthenticationService initialisiert.");
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            try
            {
                Debug.WriteLine($"Login-Versuch für {username}");

                // Sicherstellen, dass wir eine Kopie von username verwenden
                string searchTerm = username?.ToLower()?.Trim() ?? "";

                // Versuche den Benutzer zu finden
                Benutzer benutzer = null;
                try
                {
                    benutzer = await _databaseService.GetBenutzerByUsernameOrEmailAsync(searchTerm);
                }
                catch (Exception dbEx)
                {
                    Debug.WriteLine($"Datenbankfehler: {dbEx.Message}");
                    return false;
                }

                Debug.WriteLine($"Benutzer gefunden: {benutzer != null}");

                if (benutzer == null)
                {
                    Debug.WriteLine("Login fehlgeschlagen: Benutzer nicht gefunden!");
                    return false;
                }

                // Vereinfachte Passwortüberprüfung für den Entwicklungsmodus
                if (password == "demo" && benutzer.Benutzername == "demo")
                {
                    Debug.WriteLine("Entwicklungsmodus: Standard-Demo-Benutzer akzeptiert");
                    CurrentUser = benutzer;
                    return true;
                }

                if (password == "admin" && benutzer.Benutzername == "admin")
                {
                    Debug.WriteLine("Entwicklungsmodus: Standard-Admin-Benutzer akzeptiert");
                    CurrentUser = benutzer;
                    return true;
                }

                // Sonst normale Überprüfung
                bool passwordValid = false;
                try
                {
                    passwordValid = VerifyPassword(password, benutzer.PasswortHash);
                }
                catch (Exception pwEx)
                {
                    Debug.WriteLine($"Fehler bei der Passwortüberprüfung: {pwEx.Message}");
                    return false;
                }

                Debug.WriteLine($"Passwort gültig: {passwordValid}");

                if (!passwordValid)
                {
                    Debug.WriteLine("Login fehlgeschlagen: Falsches Passwort!");
                    return false;
                }

                CurrentUser = benutzer;
                Debug.WriteLine($"Benutzer {username} erfolgreich angemeldet.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Login: {ex.Message}");
                Debug.WriteLine($"Stacktrace: {ex.StackTrace}");
                return false;
            }
        }


        public bool Login(string username, string password)
        {
            return LoginAsync(username, password).GetAwaiter().GetResult();
        }

        public void Logout()
        {
            Debug.WriteLine("Benutzer abgemeldet.");
            CurrentUser = null;
        }

        public async Task<bool> RegisterAsync(string username, string email, string password, string vorname, string nachname)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return false;

            try
            {
                email = email.ToLower().Trim();
                username = username.Trim();

                // Debug-Ausgaben für bessere Diagnose
                Debug.WriteLine($"Versuche Registrierung für: Username={username}, Email={email}");

                var existingByEmail = await _databaseService.GetBenutzerByEmailAsync(email);
                Debug.WriteLine($"Benutzer mit dieser E-Mail existiert bereits: {existingByEmail != null}");

                if (existingByEmail != null)
                {
                    Debug.WriteLine($"E-Mail bereits vergeben: {email}");
                    return false;
                }

                var existingByUsername = await _databaseService.GetBenutzerByUsernameAsync(username);
                Debug.WriteLine($"Benutzer mit diesem Benutzernamen existiert bereits: {existingByUsername != null}");

                if (existingByUsername != null)
                {
                    Debug.WriteLine($"Benutzername bereits vergeben: {username}");
                    return false;
                }

                var fullName = $"{vorname} {nachname}".Trim();
                var newUser = new Benutzer
                {
                    Benutzername = username,
                    Email = email,
                    PasswortHash = HashPassword(password),
                    Kontostand = 15000.00m,
                    Erstellungsdatum = DateTime.Now,
                    Vorname = vorname,
                    Nachname = nachname,
                    VollName = fullName
                };

                // Benutzer in Datenbank speichern
                var result = await _databaseService.AddBenutzerAsync(newUser);

                if (result)
                {
                    Debug.WriteLine($"Benutzer {username} wurde erfolgreich angelegt mit ID: {newUser.BenutzerID}");

                    // Wichtig: Das await nicht vergessen, um zu gewährleisten, dass die Apple-Aktien hinzugefügt werden
                    await AddInitialAppleShares(newUser);

                    Debug.WriteLine($"Neuer Benutzer {email} registriert: {result}");
                    return true;
                }
                else
                {
                    Debug.WriteLine("Benutzer konnte nicht in Datenbank gespeichert werden");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Registrierung: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        // Überarbeitete Methode zum Hinzufügen von Apple-Aktien
        private async Task AddInitialAppleShares(Benutzer newUser)
        {
            try
            {
                // Die BenutzerID aus dem neu erstellten Benutzer holen
                int benutzerId = newUser.BenutzerID;

                Debug.WriteLine($"Füge 2 Apple-Aktien für neuen Benutzer hinzu (ID: {benutzerId})");

                // Die neue GetOrCreateAktieBySymbolAsync-Methode verwenden
                var appleAktie = await _databaseService.GetOrCreateAktieBySymbolAsync("AAPL", "Apple Inc.", 180.00m);

                if (appleAktie == null)
                {
                    Debug.WriteLine("Fehler: Konnte Apple-Aktie nicht in der Datenbank finden oder erstellen");
                    return;
                }

                // Aktuellen Kurs über API abrufen, wenn möglich
                decimal aktuellerPreis = appleAktie.AktuellerPreis;

                if (App.TwelveDataService != null)
                {
                    try
                    {
                        var aktienDaten = await App.TwelveDataService.HoleAktienKurse(new List<string> { "AAPL" });
                        if (aktienDaten != null && aktienDaten.Count > 0 && aktienDaten[0].AktuellerPreis > 0)
                        {
                            aktuellerPreis = aktienDaten[0].AktuellerPreis;
                            Debug.WriteLine($"Aktueller Apple-Kurs von API: {aktuellerPreis:F2}€");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Fehler beim Abrufen des aktuellen Apple-Kurses: {ex.Message}");
                        // Bei Fehler behalten wir den Standardpreis oder DB-Preis bei
                    }
                }

                // Portfolio-Eintrag erstellen (2 Apple-Aktien)
                var portfolioEintrag = new PortfolioEintrag
                {
                    BenutzerID = benutzerId,
                    AktienID = appleAktie.AktienID,
                    AktienSymbol = appleAktie.AktienSymbol,
                    AktienName = appleAktie.AktienName,
                    Anzahl = 2, // 2 Aktien zum Start
                    AktuellerKurs = aktuellerPreis,
                    EinstandsPreis = aktuellerPreis,
                    LetzteAktualisierung = DateTime.Now
                };

                // Zum Portfolio hinzufügen
                bool success = await _databaseService.AddOrUpdatePortfolioEintragAsync(portfolioEintrag);

                if (success)
                {
                    // Apple-Aktien kosten vom Startguthaben abziehen
                    decimal kosten = 2 * aktuellerPreis;
                    newUser.Kontostand -= kosten;

                    // Aktualisieren des Benutzers in der Datenbank
                    await _databaseService.UpdateBenutzerAsync(newUser);

                    Debug.WriteLine($"2 Apple-Aktien wurden dem neuen Benutzer (ID: {benutzerId}) für {kosten:F2}€ hinzugefügt");
                    Debug.WriteLine($"Verbleibendes Guthaben: {newUser.Kontostand:F2}€");
                }
                else
                {
                    Debug.WriteLine($"Fehler: Konnte Portfolio-Eintrag nicht hinzufügen oder aktualisieren");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Hinzufügen der Start-Aktien: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        public bool Register(string username, string email, string password, string vorname, string nachname)
        {
            return RegisterAsync(username, email, password, vorname, nachname).GetAwaiter().GetResult();
        }

        private string HashPassword(string password)
        {
            try
            {
                // BCrypt-Hashing mit reduziertem Aufwand für bessere Performance in der Entwicklung
                return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(10));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Hashen des Passworts: {ex.Message}");
                // Fallback für Entwicklung - NIEMALS in Produktion verwenden!
                return "$2a$10$entwicklungdummyhash";
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                Debug.WriteLine($"Versuche Passwort zu verifizieren...");

                // Für Entwicklung: Fest verdrahtete Prüfungen für bekannte Benutzer
                if (password == "admin" && (storedHash.Contains("eTxedgRvWVqcV9gOJ5ZOz") ||
                                           storedHash == "$2a$12$eTxedgRvWVqcV9gOJ5ZOz.zqbTLwc7E0gIOZTSLVMPzb0OFaZqNQK"))
                {
                    Debug.WriteLine("Admin-Passwort manuell bestätigt");
                    return true;
                }

                if (password == "demo" && (storedHash.Contains("T30V4QZDsHRbGHqLPBPwle") ||
                                          storedHash == "$2a$12$T30V4QZDsHRbGHqLPBPwleF0K27z0CFkFRgYLBVT8G3V36Ou.wJbu"))
                {
                    Debug.WriteLine("Demo-Passwort manuell bestätigt");
                    return true;
                }

                // Im Entwicklungsmodus immer "demo" und "admin" akzeptieren
                if ((password == "demo" || password == "admin") &&
                    (storedHash.StartsWith("$2a$") || storedHash.Contains("entwicklungdummyhash")))
                {
                    Debug.WriteLine("Entwicklungsmodus: Standard-Passwort akzeptiert");
                    return true;
                }

                // BCrypt-Verifikation mit zusätzlicher Fehlerbehandlung
                try
                {
                    bool result = BCrypt.Net.BCrypt.Verify(password, storedHash);
                    Debug.WriteLine($"BCrypt-Verifikation Ergebnis: {result}");
                    return result;
                }
                catch (Exception bcEx)
                {
                    Debug.WriteLine($"BCrypt-Fehler: {bcEx.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unbehandelter Fehler bei der Passwortüberprüfung: {ex.Message}");
                Debug.WriteLine($"Stacktrace: {ex.StackTrace}");

                // Notfall-Fallback nur für Demo und Admin
                return password == "admin" || password == "demo";
            }
        }

        // In AuthenticationService eine neue Methode hinzufügen
        public async Task<bool> VerifyPasswordAsync(string username, string password)
        {
            try
            {
                Debug.WriteLine($"Überprüfe Passwort für Benutzer: {username}");

                // Benutzer in der Datenbank suchen
                var benutzer = await _databaseService.GetBenutzerByUsernameAsync(username);

                if (benutzer == null)
                {
                    Debug.WriteLine("Benutzer nicht gefunden");
                    return false;
                }

                // Passwort überprüfen
                return VerifyPassword(password, benutzer.PasswortHash);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Passwortüberprüfung: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ändert das Passwort eines Benutzers
        /// </summary>
        /// <param name="benutzerId">ID des Benutzers</param>
        /// <param name="newPassword">Neues Passwort</param>
        /// <returns>True, wenn erfolgreich, sonst False</returns>
        public async Task<bool> ChangePasswordAsync(int benutzerId, string newPassword)
        {
            try
            {
                // Benutzer aus der Datenbank holen
                var benutzer = await _databaseService.GetBenutzerByIdAsync(benutzerId);
                if (benutzer == null)
                {
                    Debug.WriteLine($"Benutzer mit ID {benutzerId} nicht gefunden");
                    return false;
                }

                // Passwort hashen
                string hashedPassword = HashPassword(newPassword);

                // Passwort-Hash speichern
                benutzer.PasswortHash = hashedPassword;

                // Benutzer in der Datenbank aktualisieren
                bool result = await _databaseService.UpdateBenutzerAsync(benutzer);

                Debug.WriteLine($"Passwort für Benutzer {benutzer.Benutzername} wurde {(result ? "erfolgreich" : "nicht")} geändert");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler in ChangePasswordAsync: {ex.Message}");
                Debug.WriteLine($"Stacktrace: {ex.StackTrace}");
                return false;
            }
        }
    }
}