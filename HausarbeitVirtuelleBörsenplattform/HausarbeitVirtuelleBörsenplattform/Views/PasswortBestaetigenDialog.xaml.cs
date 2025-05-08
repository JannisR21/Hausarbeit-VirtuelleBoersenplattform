using System.Windows;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für PasswortBestaetigenDialog.xaml
    /// </summary>
    public partial class PasswortBestaetigenDialog : Window
    {
        /// <summary>
        /// Das eingegebene Passwort
        /// </summary>
        public string Password => PasswordBox.Password;

        /// <summary>
        /// Initialisiert einen neuen PasswortBestaetigenDialog
        /// </summary>
        public PasswortBestaetigenDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event-Handler für den Abbrechen-Button
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Event-Handler für den Bestätigen-Button
        /// </summary>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PasswordBox.Password))
            {
                MessageBox.Show("Bitte geben Sie Ihr Passwort ein.",
                    "Passwort fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}