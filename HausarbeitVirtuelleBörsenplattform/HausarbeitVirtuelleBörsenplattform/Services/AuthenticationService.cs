using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using HausarbeitVirtuelleBörsenplattform.Models;

namespace HausarbeitVirtuelleBörsenplattform.Services
{
    /// <summary>
    /// Service für die Authentifizierung von Benutzern
    /// </summary>
    public class AuthenticationService
    {
        // Simulierte Benutzer-Datenbank für den Prototyp
        private readonly Dictionary<string, string> _users;

        /// <summary>
        /// Der aktuell angemeldete Benutzer
        /// </summary>
        public Benutzer CurrentUser { get; private set; }

        /// <summary>
        /// Initialisiert eine neue Instanz des AuthenticationService
        /// </summary>
        public AuthenticationService()
        {
            // Simulierte Benutzer für den Prototyp
            // In der echten Anwendung würden wir hier eine Datenbank verwenden
            _users = new Dictionary<string, string>
            {
                { "jannis.ruhland@example.com", HashPassword("passwort123") },
                { "admin", HashPassword("admin") },
                { "demo", HashPassword("demo") }
            };

            Debug.WriteLine("AuthenticationService initialisiert.");
        }

        /// <summary>
        /// Versucht, einen Benutzer anzumelden
        /// </summary>
        /// <param name="username">Benutzername oder E-Mail</param>
        /// <param name="password">Passwort im Klartext</param>
        /// <returns>True, wenn die Anmeldung erfolgreich war, sonst False</returns>
        public bool Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            // Normalisiere Benutzernamen (Kleinbuchstaben)
            username = username.ToLower().Trim();

            // Überprüfe, ob der Benutzer existiert
            if (!_users.TryGetValue(username, out var storedHash))
                return false;

            // Überprüfe das Passwort
            if (!VerifyPassword(password, storedHash))
                return false;

            // Benutzer gefunden und Passwort korrekt
            // In einer echten Anwendung würden wir hier Benutzerdaten aus der Datenbank laden
            CurrentUser = new Benutzer
            {
                BenutzerID = 1,
                Benutzername = username == "admin" ? "Administrator" : (username == "demo" ? "Demo-Benutzer" : "Jannis Ruhland"),
                Email = username.Contains("@") ? username : $"{username}@example.com",
                Kontostand = 10000.00m,
                Erstellungsdatum = DateTime.Now.AddDays(-30)
            };

            Debug.WriteLine($"Benutzer {username} erfolgreich angemeldet.");
            return true;
        }

        /// <summary>
        /// Meldet den aktuellen Benutzer ab
        /// </summary>
        public void Logout()
        {
            Debug.WriteLine("Benutzer abgemeldet.");
            CurrentUser = null;
        }

        /// <summary>
        /// Registriert einen neuen Benutzer
        /// </summary>
        /// <param name="username">Benutzername</param>
        /// <param name="email">E-Mail-Adresse</param>
        /// <param name="password">Passwort im Klartext</param>
        /// <returns>True, wenn die Registrierung erfolgreich war, sonst False</returns>
        public bool Register(string username, string email, string password)
        {
            // In einem echten System würden wir viele weitere Prüfungen durchführen
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return false;

            // Normalisiere E-Mail (Kleinbuchstaben)
            email = email.ToLower().Trim();

            // Prüfe, ob der Benutzer bereits existiert
            if (_users.ContainsKey(email))
                return false;

            // Passwort hashen und Benutzer speichern
            _users[email] = HashPassword(password);

            Debug.WriteLine($"Neuer Benutzer {email} registriert.");
            return true;
        }

        /// <summary>
        /// Erzeugt einen Passwort-Hash mit einem zufälligen Salt
        /// </summary>
        /// <param name="password">Das Passwort im Klartext</param>
        /// <returns>Der erzeugte Hash mit Salt</returns>
        private string HashPassword(string password)
        {
            // In einer echten Anwendung würden wir BCrypt oder PBKDF2 mit einem zufälligen Salt verwenden
            // Für diesen Prototyp verwenden wir einen einfachen SHA256-Hash
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Überprüft, ob ein Passwort zu einem gespeicherten Hash passt
        /// </summary>
        /// <param name="password">Das Passwort im Klartext</param>
        /// <param name="storedHash">Der gespeicherte Hash</param>
        /// <returns>True, wenn das Passwort korrekt ist, sonst False</returns>
        private bool VerifyPassword(string password, string storedHash)
        {
            // In einer echten Anwendung würden wir BCrypt.Verify oder PBKDF2 mit dem gespeicherten Salt verwenden
            // Für diesen Prototyp berechnen wir einfach den SHA256-Hash und vergleichen ihn
            string computedHash = HashPassword(password);
            return computedHash == storedHash;
        }
    }
}