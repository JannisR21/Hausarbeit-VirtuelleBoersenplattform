using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für PasswortVergessenDialog.xaml
    /// </summary>
    public partial class PasswortVergessenDialog : Window
    {
        /// <summary>
        /// Gibt an, ob das Passwort erfolgreich zurückgesetzt wurde
        /// </summary>
        public bool PasswordReset { get; private set; } = false;

        /// <summary>
        /// Initialisiert eine neue Instanz des PasswortVergessenDialog
        /// </summary>
        public PasswortVergessenDialog()
        {
            InitializeComponent();
            
            // Dialog im Besitzer-Fenster zentrieren
            if (Application.Current.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
            }
        }

        /// <summary>
        /// Event-Handler für den Abbrechen-Button
        /// </summary>
        private void AbbrechenButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Event-Handler für den Passwort-Zusenden-Button
        /// </summary>
        private async void PasswortZusendenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // UI während der Verarbeitung deaktivieren
                this.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;
                
                // Error-Container zurücksetzen und ausblenden
                ErrorBorder.Visibility = Visibility.Collapsed;
                ErrorText.Text = string.Empty;
                
                // Success-Container ausblenden
                SuccessBorder.Visibility = Visibility.Collapsed;

                // Eingaben prüfen
                var email = EmailTextBox.Text?.Trim();
                
                if (string.IsNullOrEmpty(email))
                {
                    ShowError("Bitte geben Sie Ihre E-Mail-Adresse ein.");
                    EmailTextBox.Focus();
                    return;
                }

                if (!IsValidEmail(email))
                {
                    ShowError("Bitte geben Sie eine gültige E-Mail-Adresse ein.");
                    EmailTextBox.Focus();
                    return;
                }

                // Prüfen, ob die E-Mail in der Datenbank existiert
                var benutzer = await App.DbService.GetBenutzerByEmailAsync(email);
                
                if (benutzer == null)
                {
                    ShowError("Es wurde kein Konto mit dieser E-Mail-Adresse gefunden.");
                    EmailTextBox.Focus();
                    return;
                }

                // Neues zufälliges Passwort generieren
                string newPassword = GenerateRandomPassword();
                Debug.WriteLine($"Neues Passwort für Benutzer {benutzer.Benutzername}: {newPassword}");

                // Passwort in der Datenbank aktualisieren
                bool passwordUpdated = await App.AuthService.ChangePasswordAsync(benutzer.BenutzerID, newPassword);
                
                if (!passwordUpdated)
                {
                    ShowError("Fehler beim Zurücksetzen des Passworts. Bitte versuchen Sie es später erneut.");
                    return;
                }

                // E-Mail mit neuem Passwort senden
                bool emailSent = await SendPasswordResetEmail(benutzer, newPassword);
                
                if (!emailSent)
                {
                    ShowError("Fehler beim Senden der E-Mail. Das Passwort wurde zurückgesetzt, konnte aber nicht per E-Mail zugestellt werden.");
                    return;
                }

                // Erfolg anzeigen
                SuccessBorder.Visibility = Visibility.Visible;
                PasswordReset = true;
                
                // Nach kurzer Verzögerung Dialog schließen
                await Task.Delay(2000);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Zurücksetzen des Passworts: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                ShowError($"Fehler beim Zurücksetzen des Passworts: {ex.Message}");
            }
            finally
            {
                // UI-Status wiederherstellen
                this.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Zeigt eine Fehlermeldung im Dialog an
        /// </summary>
        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Überprüft, ob eine E-Mail-Adresse gültig ist
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generiert ein zufälliges sicheres Passwort
        /// </summary>
        private string GenerateRandomPassword(int length = 10)
        {
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numbers = "0123456789";
            const string specialChars = "!@#$%^&*()-_=+[]";
            
            string allChars = lowerChars + upperChars + numbers + specialChars;
            
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                
                var result = new StringBuilder(length);
                
                // Sicherstellen, dass mindestens ein Zeichen aus jeder Kategorie verwendet wird
                result.Append(lowerChars[bytes[0] % lowerChars.Length]);
                result.Append(upperChars[bytes[1] % upperChars.Length]);
                result.Append(numbers[bytes[2] % numbers.Length]);
                result.Append(specialChars[bytes[3] % specialChars.Length]);
                
                // Restliche Zeichen zufällig ausfüllen
                for (int i = 4; i < length; i++)
                {
                    result.Append(allChars[bytes[i] % allChars.Length]);
                }
                
                // Zeichen mischen, um zu vermeiden, dass das Passwort immer mit den gleichen Kategorien beginnt
                return MixString(result.ToString());
            }
        }

        /// <summary>
        /// Mischt die Zeichen eines Strings
        /// </summary>
        private string MixString(string input)
        {
            var characters = input.ToCharArray();
            var random = new Random();
            
            for (int i = characters.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var temp = characters[i];
                characters[i] = characters[j];
                characters[j] = temp;
            }
            
            return new string(characters);
        }

        /// <summary>
        /// Sendet eine E-Mail mit dem neuen Passwort an den Benutzer
        /// </summary>
        private async Task<bool> SendPasswordResetEmail(Models.Benutzer benutzer, string newPassword)
        {
            try
            {
                Debug.WriteLine($"Sende Passwort-Reset-E-Mail an: {benutzer.Email}");
                
                // Den EmailService aus App.EmailService verwenden
                bool result = await App.EmailService.SendPasswordResetEmailAsync(
                    benutzer.Email,
                    benutzer.VollName,
                    benutzer.Benutzername,
                    newPassword);
                
                if (result)
                {
                    Debug.WriteLine("Passwort-Reset-E-Mail erfolgreich gesendet!");
                }
                else
                {
                    Debug.WriteLine("Fehler beim Senden der Passwort-Reset-E-Mail über den EmailService.");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Senden der Passwort-Reset-E-Mail: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }
    }
}