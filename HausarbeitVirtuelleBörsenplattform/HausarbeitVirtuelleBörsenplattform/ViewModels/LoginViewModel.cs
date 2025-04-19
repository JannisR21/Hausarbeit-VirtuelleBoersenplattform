using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Models;
using HausarbeitVirtuelleBörsenplattform.Services;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

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

        /// <summary>
        /// Benutzername oder E-Mail
        /// </summary>
        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        /// <summary>
        /// Passwort des Benutzers
        /// </summary>
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// Gibt an, ob der Benutzer angemeldet bleiben möchte
        /// </summary>
        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        /// <summary>
        /// Gibt an, ob das Passwort im Klartext angezeigt werden soll
        /// </summary>
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
        }

        /// <summary>
        /// Gibt an, ob ein Fehler aufgetreten ist
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        /// <summary>
        /// Fehlermeldung im Falle eines Fehlers
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command zum Einloggen
        /// </summary>
        public IRelayCommand LoginCommand { get; }

        /// <summary>
        /// Command zum Wechseln zur Registrierungsansicht
        /// </summary>
        public IRelayCommand RegisterCommand { get; }

        /// <summary>
        /// Command zum Aufrufen der "Passwort vergessen"-Funktion
        /// </summary>
        public IRelayCommand ForgotPasswordCommand { get; }

        /// <summary>
        /// Command zum Umschalten der Passwort-Sichtbarkeit
        /// </summary>
        public IRelayCommand TogglePasswordVisibilityCommand { get; }

        #endregion

        #region Konstruktor

        /// <summary>
        /// Initialisiert eine neue Instanz des LoginViewModel
        /// </summary>
        public LoginViewModel()
        {
            // Den AuthenticationService benötigen wir nicht mehr als Feld,
            // da wir die zentrale Instanz in App.AuthService verwenden

            // Commands initialisieren
            LoginCommand = new RelayCommand(Login, CanLogin);
            RegisterCommand = new RelayCommand(GoToRegister);
            ForgotPasswordCommand = new RelayCommand(ForgotPassword);
            TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);

            // Standard-Werte
            RememberMe = true;
            IsPasswordVisible = false;
            HasError = false;

            // Für Demo-Zwecke: Standartwerte für Login setzen
#if DEBUG
            Username = "demo";
            Password = "demo";
#endif
        }
        #endregion

        #region Command-Methoden

        /// <summary>
        /// Führt den Login-Vorgang durch
        /// </summary>
        private void Login()
        {
            try
            {
                HasError = false;
                ErrorMessage = string.Empty;

                Debug.WriteLine($"Versuche Login für Benutzer: {Username}");

                // AuthenticationService-Instanz aus App verwenden
                // Wir nutzen die statische Instance, die in App.xaml.cs bereitgestellt wird
                bool loginSuccess = App.AuthService.Login(Username, Password);

                if (loginSuccess)
                {
                    Debug.WriteLine("Login erfolgreich!");

                    // Zur Hauptansicht navigieren
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

        /// <summary>
        /// Prüft, ob ein Login durchgeführt werden kann
        /// </summary>
        private bool CanLogin()
        {
            // Einfache Validierung: Benutzername und Passwort dürfen nicht leer sein
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

        /// <summary>
        /// Wechselt zur Registrierungsansicht
        /// </summary>
        private void GoToRegister()
        {
            Debug.WriteLine("Wechsel zur Registrierungsansicht...");
            // Hier würden wir zur Registrierungsansicht navigieren
        }

        /// <summary>
        /// Ruft die "Passwort vergessen"-Funktion auf
        /// </summary>
        private void ForgotPassword()
        {
            Debug.WriteLine("Passwort vergessen aufgerufen...");
            // Hier würden wir die "Passwort vergessen"-Funktionalität implementieren
        }

        /// <summary>
        /// Schaltet die Sichtbarkeit des Passworts um
        /// </summary>
        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        #endregion
    }
}