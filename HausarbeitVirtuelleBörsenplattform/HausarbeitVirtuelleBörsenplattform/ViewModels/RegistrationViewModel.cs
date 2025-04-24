using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Services;
using System;
using System.Diagnostics;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    public class RegistrationViewModel : ObservableObject
    {
        private string _vorname;
        private string _nachname;
        private string _email;
        private string _username;
        private string _password;
        private string _passwordConfirm;
        private bool _isRegistering;
        private bool _hasError;
        private string _errorMessage;
        private bool _isPasswordVisible;
        private bool _agbAccepted;

        public string Vorname
        {
            get => _vorname;
            set
            {
                SetProperty(ref _vorname, value);
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }

        public string Nachname
        {
            get => _nachname;
            set
            {
                SetProperty(ref _nachname, value);
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                SetProperty(ref _email, value);
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }

        public string PasswordConfirm
        {
            get => _passwordConfirm;
            set
            {
                SetProperty(ref _passwordConfirm, value);
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }

        public bool IsRegistering
        {
            get => _isRegistering;
            set => SetProperty(ref _isRegistering, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
        }

        public bool AgbAccepted
        {
            get => _agbAccepted;
            set
            {
                SetProperty(ref _agbAccepted, value);
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }

        public IRelayCommand RegisterCommand { get; }
        public IRelayCommand BackToLoginCommand { get; }
        public IRelayCommand TogglePasswordVisibilityCommand { get; }

        public RegistrationViewModel()
        {
            RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
            BackToLoginCommand = new RelayCommand(BackToLogin);
            TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);

            IsPasswordVisible = false;
            HasError = false;
            IsRegistering = false;
            AgbAccepted = false;
        }

        private async Task RegisterAsync()
        {
            try
            {
                IsRegistering = true;
                HasError = false;
                ErrorMessage = string.Empty;

                if (!ValidateInput())
                {
                    IsRegistering = false;
                    return;
                }

                Debug.WriteLine($"Registrierung für {Username} wird gestartet...");

                // Prüfe zunächst explizit, ob E-Mail oder Benutzername bereits existieren
                var existingByEmail = await App.DbService.GetBenutzerByEmailAsync(Email);
                var existingByUsername = await App.DbService.GetBenutzerByUsernameAsync(Username);

                if (existingByEmail != null)
                {
                    HasError = true;
                    ErrorMessage = "Diese E-Mail-Adresse ist bereits vergeben.";
                    IsRegistering = false;
                    return;
                }

                if (existingByUsername != null)
                {
                    HasError = true;
                    ErrorMessage = "Dieser Benutzername ist bereits vergeben.";
                    IsRegistering = false;
                    return;
                }

                bool registrationSuccess = await App.AuthService.RegisterAsync(
                    Username, Email, Password, Vorname, Nachname);

                if (registrationSuccess)
                {
                    Debug.WriteLine("Registrierung erfolgreich!");

                    // E-Mail senden - mit verbessertem Fehlerhandling
                    bool emailSent = false;
                    try
                    {
                        Debug.WriteLine("Sende Registrierungs-E-Mail...");
                        emailSent = await SendRegistrationEmailAsync();
                        Debug.WriteLine($"E-Mail erfolgreich gesendet: {emailSent}");
                    }
                    catch (Exception emailEx)
                    {
                        Debug.WriteLine($"Fehler beim E-Mail-Versand: {emailEx.Message}, Stacktrace: {emailEx.StackTrace}");
                        // Fehler beim E-Mail-Versand sollte die Registrierung nicht verhindern
                    }

                    var messageText = "Die Registrierung war erfolgreich!";
                    if (emailSent)
                    {
                        messageText += " Eine Bestätigungs-E-Mail wurde an Ihre E-Mail-Adresse gesendet.";
                    }
                    else
                    {
                        messageText += " Eine Bestätigungs-E-Mail konnte nicht gesendet werden, aber Sie können sich trotzdem anmelden.";
                    }

                    MessageBox.Show(
                        messageText,
                        "Registrierung erfolgreich",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    BackToLogin();
                }
                else
                {
                    Debug.WriteLine("Registrierung fehlgeschlagen!");
                    HasError = true;
                    ErrorMessage = "Bei der Registrierung ist ein unbekannter Fehler aufgetreten. Bitte versuche es später erneut.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Registrierung: {ex.Message}");
                HasError = true;
                ErrorMessage = $"Ein Fehler ist aufgetreten: {ex.Message}";
            }
            finally
            {
                IsRegistering = false;
            }
        }

        private bool CanRegister()
        {
            return !string.IsNullOrWhiteSpace(Vorname) &&
                !string.IsNullOrWhiteSpace(Nachname) &&
                !string.IsNullOrWhiteSpace(Email) &&
                !string.IsNullOrWhiteSpace(Username) &&
                !string.IsNullOrWhiteSpace(Password) &&
                !string.IsNullOrWhiteSpace(PasswordConfirm) &&
                AgbAccepted;
        }

        private void BackToLogin()
        {
            if (Application.Current.MainWindow is LoginWindow loginWindow)
            {
                loginWindow.ShowLoginView();
            }
        }

        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        private bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                HasError = true;
                ErrorMessage = "Bitte geben Sie eine E-Mail-Adresse ein.";
                return false;
            }

            try
            {
                // Einfache Validierung für E-Mail-Format
                if (!email.Contains("@") || !email.Contains("."))
                {
                    HasError = true;
                    ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse ein.";
                    return false;
                }

                // Versuche, einen MailAddress zu erstellen (wirft eine Exception bei ungültigem Format)
                var mailAddress = new System.Net.Mail.MailAddress(email);
                return true;
            }
            catch
            {
                HasError = true;
                ErrorMessage = "Die eingegebene E-Mail-Adresse ist ungültig.";
                return false;
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(Vorname) ||
                string.IsNullOrWhiteSpace(Nachname) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(PasswordConfirm))
            {
                HasError = true;
                ErrorMessage = "Bitte füllen Sie alle erforderlichen Felder aus.";
                return false;
            }

            try
            {
                var mailAddress = new MailAddress(Email);
            }
            catch
            {
                HasError = true;
                ErrorMessage = "Bitte geben Sie eine gültige E-Mail-Adresse ein.";
                return false;
            }

            if (Password != PasswordConfirm)
            {
                HasError = true;
                ErrorMessage = "Die eingegebenen Passwörter stimmen nicht überein.";
                return false;
            }

            if (Password.Length < 6)
            {
                HasError = true;
                ErrorMessage = "Das Passwort muss mindestens 6 Zeichen lang sein.";
                return false;
            }

            if (!AgbAccepted)
            {
                HasError = true;
                ErrorMessage = "Sie müssen die AGB akzeptieren, um sich zu registrieren.";
                return false;
            }

            return true;
        }

        private async Task<bool> SendRegistrationEmailAsync()
        {
            try
            {
                // Hier verwenden wir den konfigurierten EmailService, um die Mail zu senden
                Debug.WriteLine($"Starte Email-Versand an {Email}");
                var fullName = $"{Vorname} {Nachname}";

                bool result = await App.EmailService.SendRegistrationConfirmationAsync(
                    Email, fullName, Username);

                Debug.WriteLine($"E-Mail-Versand Ergebnis: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Senden der E-Mail: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return false;
            }
        }
    }
}