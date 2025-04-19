using System.Windows;
using System.Windows.Input;

namespace HausarbeitVirtuelleBörsenplattform
{
    /// <summary>
    /// Interaktionslogik für LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        /// <summary>
        /// Initialisiert eine neue Instanz des LoginWindow
        /// </summary>
        public LoginWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Behandelt Mausklick-Ereignisse in der Titelleiste zum Verschieben des Fensters
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove(); // Erlaube das Verschieben des Fensters durch Ziehen der Titelleiste
        }

        /// <summary>
        /// Behandelt das Klick-Ereignis des Minimieren-Buttons
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Behandelt das Klick-Ereignis des Schließen-Buttons
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}