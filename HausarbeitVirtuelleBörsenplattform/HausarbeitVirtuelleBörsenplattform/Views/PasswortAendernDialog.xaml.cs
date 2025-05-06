using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using BCrypt.Net;
using HausarbeitVirtuelleBörsenplattform.Data;
using HausarbeitVirtuelleBörsenplattform.Models;
using HausarbeitVirtuelleBörsenplattform.ViewModels;

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
        /// Initialisiert eine neue Instanz des PasswortAendernDialog
        /// </summary>
        public PasswortAendernDialog()
        {
            InitializeComponent();
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
        /// Event-Handler für den Passwort-Ändern-Button
        /// </summary>
        private async void PasswortAendernButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // UI während der Verarbeitung deaktivieren
                this.IsEnabled = false;
                Mouse.OverrideCursor = Cursors.Wait;

                // Eingaben prüfen
                if (string.IsNullOrEmpty(CurrentPasswordBox.Password))
                {
                    MessageBox.Show("Bitte geben Sie Ihr aktuelles Passwort ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    CurrentPasswordBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(NewPasswordBox.Password))
                {
                    MessageBox.Show("Bitte geben Sie ein neues Passwort ein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    NewPasswordBox.Focus();
                    return;
                }

                if (NewPasswordBox.Password.Length < 6)
                {
                    MessageBox.Show("Das Passwort muss mindestens 6 Zeichen lang sein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    NewPasswordBox.Focus();
                    return;
                }

                if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("Die Passwörter stimmen nicht überein.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    ConfirmPasswordBox.Focus();
                    return;
                }

                // Aktuellen Benutzer abrufen
                var mainWindow = Application.Current.MainWindow as MainWindow;
                var mainViewModel = mainWindow?.DataContext as MainViewModel;

                if (mainViewModel == null || mainViewModel.AktuellerBenutzer == null)
                {
                    MessageBox.Show("Fehler: Kein angemeldeter Benutzer gefunden.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Benutzer benutzer = mainViewModel.AktuellerBenutzer;
                Debug.WriteLine($"Versuche Passwort zu ändern für Benutzer: {benutzer.Benutzername} (ID: {benutzer.BenutzerID})");

                // Prüfen, ob das aktuelle Passwort korrekt ist
                bool isCurrentPasswordValid = false;

                // Für Demo-Benutzer: hardcoded Vergleich
                if (benutzer.Benutzername.Equals("demo", StringComparison.OrdinalIgnoreCase) &&
                    CurrentPasswordBox.Password.Equals("demo"))
                {
                    isCurrentPasswordValid = true;
                }
                else if (benutzer.Benutzername.Equals("admin", StringComparison.OrdinalIgnoreCase) &&
                         CurrentPasswordBox.Password.Equals("admin"))
                {
                    isCurrentPasswordValid = true;
                }
                else
                {
                    // Für normale Benutzer: BCrypt-Vergleich
                    isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(CurrentPasswordBox.Password, benutzer.PasswortHash);
                }

                if (!isCurrentPasswordValid)
                {
                    MessageBox.Show("Das aktuelle Passwort ist nicht korrekt.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    CurrentPasswordBox.Focus();
                    return;
                }

                // Neues Passwort-Hash generieren
                string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(NewPasswordBox.Password, workFactor: 12);
                Debug.WriteLine("Neuer Passwort-Hash wurde generiert");

                // Passwort im Benutzer-Objekt aktualisieren und speichern
                benutzer.PasswortHash = newPasswordHash;

                // App.DbService verwenden, um die Änderungen zu speichern
                bool updateSuccess = await App.DbService.UpdateBenutzerAsync(benutzer);

                if (updateSuccess)
                {
                    Debug.WriteLine("Passwort wurde erfolgreich in der Datenbank aktualisiert");
                    PasswordChanged = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    throw new Exception("Fehler beim Speichern des neuen Passworts in der Datenbank");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Ändern des Passworts: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Fehler beim Ändern des Passworts: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // UI-Status wiederherstellen
                this.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }
    }
}