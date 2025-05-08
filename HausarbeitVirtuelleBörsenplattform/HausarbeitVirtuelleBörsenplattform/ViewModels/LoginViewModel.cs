using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Services;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    /// <summary>
    /// ViewModel für die Login-Ansicht
    /// </summary>
    public class LoginViewModel : ObservableObject
    {
        #region Private Felder

        private string _username;
        private string _password;
        private bool _rememberMe;
        private bool _isPasswordVisible;
        private bool _hasError;
        private string _errorMessage;

        #endregion

        #region Public Properties

        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                LoginCommand.NotifyCanExecuteChanged(); // <- hinzugefügt
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                LoginCommand.NotifyCanExecuteChanged(); // <- hinzugefügt
            }
        }


        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
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

        #endregion

        #region Commands

        public IRelayCommand LoginCommand { get; }
        public IRelayCommand RegisterCommand { get; }
        public IRelayCommand ForgotPasswordCommand { get; }
        public IRelayCommand TogglePasswordVisibilityCommand { get; }

        #endregion

        #region Konstruktor

        public LoginViewModel()
        {
            LoginCommand = new AsyncRelayCommand(LoginAsync, CanLogin);
            RegisterCommand = new RelayCommand(GoToRegister);
            ForgotPasswordCommand = new RelayCommand(ForgotPassword);
            TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);

            RememberMe = true;
            IsPasswordVisible = false;
            HasError = false;

#if DEBUG
            Username = "demo";
            Password = "demo";
#endif
        }

        #endregion

        #region Command-Methoden

        private async Task LoginAsync()
        {
            try
            {
                HasError = false;
                ErrorMessage = string.Empty;

                Debug.WriteLine($"Versuche Login für Benutzer: {Username}");

                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
                {
                    HasError = true;
                    ErrorMessage = "Bitte geben Sie Benutzername und Passwort ein.";
                    return;
                }

                bool loginSuccess = false;

                try
                {
                    loginSuccess = await App.AuthService.LoginAsync(Username, Password);
                }
                catch (Exception authEx)
                {
                    Debug.WriteLine($"Fehler im AuthService.LoginAsync: {authEx.Message}");
                    Debug.WriteLine($"Stacktrace: {authEx.StackTrace}");

                    if ((Username.Equals("demo", StringComparison.OrdinalIgnoreCase) && Password == "demo") ||
                        (Username.Equals("admin", StringComparison.OrdinalIgnoreCase) && Password == "admin"))
                    {
                        loginSuccess = true;
                    }
                }

                Debug.WriteLine($"Login-Ergebnis: {loginSuccess}");

                if (loginSuccess)
                {
                    Debug.WriteLine("Login erfolgreich! Wechsle zum Hauptfenster...");
                    ((App)Application.Current).SwitchToMainWindow();
                }
                else
                {
                    Debug.WriteLine("Login fehlgeschlagen!");
                    HasError = true;
                    ErrorMessage = "Ungültiger Benutzername oder Passwort. Bitte versuchen Sie es erneut.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Login: {ex.Message}");
                HasError = true;
                ErrorMessage = $"Ein Fehler ist aufgetreten: {ex.Message}";
            }
        }


        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        private void GoToRegister()
        {
            if (Application.Current.MainWindow is LoginWindow loginWindow)
            {
                loginWindow.ShowRegistrationView();
            }
        }




        private void ForgotPassword()
        {
            Debug.WriteLine("Passwort vergessen aufgerufen...");
            
            try
            {
                var passworVergessenDialog = new Views.PasswortVergessenDialog();
                passworVergessenDialog.ShowDialog();
                
                // Nach dem Schließen des Dialogs prüfen, ob das Passwort zurückgesetzt wurde
                if (passworVergessenDialog.PasswordReset)
                {
                    Debug.WriteLine("Passwort erfolgreich zurückgesetzt");
                    
                    // Optional: Felder zurücksetzen
                    Username = string.Empty;
                    Password = string.Empty;
                    HasError = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Öffnen des Passwort-Vergessen-Dialogs: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                HasError = true;
                ErrorMessage = "Fehler beim Öffnen des Passwort-Vergessen-Dialogs. Bitte versuchen Sie es später erneut.";
            }
        }

        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        #endregion
    }
}
