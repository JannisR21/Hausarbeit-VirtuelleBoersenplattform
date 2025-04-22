using System;
using System.Windows;
using HausarbeitVirtuelleBörsenplattform.Services;

namespace HausarbeitVirtuelleBörsenplattform
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Zentrale Instanz des Authentication Service
        /// </summary>
        public static AuthenticationService AuthService { get; private set; }

        /// <summary>
        /// Wird beim Starten der Anwendung aufgerufen
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // AuthenticationService initialisieren
            AuthService = new AuthenticationService();

            // Starte mit Login-Fenster statt MainWindow
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            // Das StartupUri in App.xaml muss entfernt werden, damit dies funktioniert
            Current.MainWindow = loginWindow;
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
        }

        /// <summary>
        /// Wechselt zum Hauptfenster nach erfolgreicher Anmeldung
        /// </summary>
        public void SwitchToMainWindow()
        {
            // Login-Fenster einfach verstecken, nicht schließen!
            if (Current.MainWindow != null)
            {
                Current.MainWindow.Hide();
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();

            // Jetzt offizielles MainWindow setzen
            Current.MainWindow = mainWindow;
        }

    }
}