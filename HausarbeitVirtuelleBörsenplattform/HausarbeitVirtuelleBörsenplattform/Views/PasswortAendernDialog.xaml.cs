using HausarbeitVirtuelleBörsenplattform.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für PasswortAendernDialog.xaml
    /// </summary>
    public partial class PasswortAendernDialog : Window
    {
        /// <summary>
        /// Gibt an, ob das Passwort erfolgreich geändert wurde
        /// </summary>
        public bool PasswordChanged { get; private set; } = false;

        /// <summary>
        /// Das neue Passwort, falls die Änderung erfolgreich war
        /// </summary>
        public string NewPassword { get; private set; }

        /// <summary>
        /// Initialisiert eine neue Instanz des PasswortAendernDialog
        /// </summary>
        public PasswortAendernDialog()
        {
            InitializeComponent();

            // Fokus auf das erste Eingabefeld setzen
            Loaded += (s, e) => AktuellesPasswortBox.Focus();
        }

        /// <summary>
        /// Ereignishandler für den Abbrechen-Button
        /// </summary>
        private void AbbrechenButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Ereignishandler für den Speichern-Button
        /// </summary>
        private async void SpeichernButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ErrorMessage.Text = string.Empty;

                // Eingaben validieren
                string aktuellesPasswort = AktuellesPasswortBox.Password;
                string neuesPasswort = NeuesPasswortBox.Password;
                string wiederholungsPasswort = PasswortWiederholenBox.Password;

                if (string.IsNullOrEmpty(aktuellesPasswort))
                {
                    ErrorMessage.Text = "Bitte geben Sie Ihr aktuelles Passwort ein.";
                    AktuellesPasswortBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(neuesPasswort))
                {
                    ErrorMessage.Text = "Bitte geben Sie ein neues Passwort ein.";
                    NeuesPasswortBox.Focus();
                    return;
                }

                if (neuesPasswort.Length < 6)
                {
                    ErrorMessage.Text = "Das neue Passwort muss mindestens 6 Zeichen lang sein.";
                    NeuesPasswortBox.Focus();
                    return;
                }

                if (neuesPasswort != wiederholungsPasswort)
                {
                    ErrorMessage.Text = "Die Passwörter stimmen nicht überein.";
                    PasswortWiederholenBox.Focus();
                    return;
                }

                // Aktuelles Passwort überprüfen
                var mainWindow = Window.GetWindow(this.Owner) as MainWindow;
                var mainViewModel = mainWindow?.DataContext as MainViewModel;

                if (mainViewModel == null || mainViewModel.AktuellerBenutzer == null)
                {
                    ErrorMessage.Text = "Fehler: Kein aktiver Benutzer gefunden.";
                    return;
                }

                string benutzername = mainViewModel.AktuellerBenutzer.Benutzername;

                // Demo/Admin-Benutzer gesondert behandeln
                bool isPasswordValid = false;

                if (benutzername.Equals("demo", StringComparison.OrdinalIgnoreCase) && aktuellesPasswort.Equals("demo"))
                {
                    isPasswordValid = true;
                }
                else if (benutzername.Equals("admin", StringComparison.OrdinalIgnoreCase) && aktuellesPasswort.Equals("admin"))
                {
                    isPasswordValid = true;
                }
                else
                {
                    // Bei normalen Benutzern die Authentifizierung über den Service durchführen
                    isPasswordValid = await App.AuthService.VerifyPasswordAsync(benutzername, aktuellesPasswort);
                }

                if (!isPasswordValid)
                {
                    ErrorMessage.Text = "Das aktuelle Passwort ist nicht korrekt.";
                    AktuellesPasswortBox.Password = string.Empty;
                    AktuellesPasswortBox.Focus();
                    return;
                }

                // Passwort ändern
                int benutzerId = mainViewModel.AktuellerBenutzer.BenutzerID;
                bool success = await App.AuthService.ChangePasswordAsync(benutzerId, neuesPasswort);

                if (success)
                {
                    NewPassword = neuesPasswort;
                    PasswordChanged = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ErrorMessage.Text = "Fehler beim Ändern des Passworts. Bitte versuchen Sie es später erneut.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Ändern des Passworts: {ex.Message}");
                ErrorMessage.Text = $"Ein Fehler ist aufgetreten: {ex.Message}";
            }
        }
    }
}