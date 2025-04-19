using System.Windows;
using System.Windows.Controls;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        /// <summary>
        /// Initialisiert eine neue Instanz von LoginView
        /// </summary>
        public LoginView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Behandelt die PasswordChanged-Ereignisse der PasswordBox
        /// und synchronisiert den Wert mit dem ViewModel.
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                // Wir binden die Passwort-Eigenschaft des ViewModels an das Tag der PasswordBox,
                // da PasswordBox keine direkte Datenbindung für das Passwort unterstützt
                ((dynamic)DataContext).Password = ((PasswordBox)sender).Password;
            }
        }
    }
}