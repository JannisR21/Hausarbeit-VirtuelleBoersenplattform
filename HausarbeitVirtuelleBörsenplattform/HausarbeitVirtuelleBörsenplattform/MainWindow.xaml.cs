using System.Windows;
using System.Windows.Controls;

namespace HausarbeitVirtuelleBörsenplattform
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initialisiert eine neue Instanz des MainWindow
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Warten, bis das Fenster komplett geladen ist, dann Kontextmenü initialisieren
            this.Loaded += (s, e) => InitializeUserContextMenu();
        }


        /// <summary>
        /// Initialisiert das Kontextmenü für den Benutzer-Button und verbindet die Event-Handler
        /// </summary>
        private void InitializeUserContextMenu()
        {
            // Stellt sicher, dass das Kontextmenü angezeigt wird, wenn der Button geklickt wird
            UserButton.Click += (sender, e) =>
            {
                UserButton.ContextMenu.IsOpen = true;
            };

            // Event-Handler für Menüelemente (können später implementiert werden)
            if (UserButton.ContextMenu != null)
            {
                foreach (var item in UserButton.ContextMenu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        menuItem.Click += UserMenuItem_Click;
                    }
                }
            }
        }

        /// <summary>
        /// Event-Handler für Klicks auf die Menüelemente des Benutzer-Dropdown-Menüs
        /// </summary>
        private void UserMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                switch (menuItem.Header.ToString())
                {
                    case "Profil":
                        // Öffne das Benutzerprofil (noch zu implementieren)
                        MessageBox.Show("Profil öffnen (noch nicht implementiert)");
                        break;
                    case "Konto":
                        // Öffne die Kontoeinstellungen (noch zu implementieren)
                        MessageBox.Show("Kontoeinstellungen öffnen (noch nicht implementiert)");
                        break;
                    case "Ausloggen":
                        // Ausloggen-Logik (noch zu implementieren)
                        MessageBox.Show("Ausloggen (noch nicht implementiert)");
                        break;
                }
            }
        }
    }
}