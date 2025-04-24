using System.Windows;
using System.Windows.Controls;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für RegistrationView.xaml
    /// </summary>
    public partial class RegistrationView : UserControl
    {
        /// <summary>
        /// Initialisiert eine neue Instanz von RegistrationView
        /// </summary>
        public RegistrationView()
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
                // PasswordBox unterstützt keine direkte Datenbindung für das Passwort
                ((dynamic)DataContext).Password = ((PasswordBox)sender).Password;
            }
        }

        /// <summary>
        /// Behandelt die PasswordChanged-Ereignisse der PasswordConfirmBox
        /// und synchronisiert den Wert mit dem ViewModel.
        /// </summary>
        private void PasswordConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext != null)
            {
                // PasswordBox unterstützt keine direkte Datenbindung für das Passwort
                ((dynamic)DataContext).PasswordConfirm = ((PasswordBox)sender).Password;
            }
        }
    }
}