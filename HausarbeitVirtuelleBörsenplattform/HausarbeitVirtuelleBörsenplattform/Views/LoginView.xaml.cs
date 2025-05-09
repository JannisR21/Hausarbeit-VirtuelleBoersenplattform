using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HausarbeitVirtuelleBörsenplattform.ViewModels;

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

            // Explizit das DataContext setzen, falls es Probleme mit der XAML-Deklaration gibt
            if (this.DataContext == null)
            {
                this.DataContext = new LoginViewModel();
            }

            // Stellen Sie sicher, dass die TextBox immer aktiviert ist
            this.Loaded += (s, e) => {
                if (UsernameTextBox != null)
                {
                    UsernameTextBox.IsEnabled = true;
                    UsernameTextBox.Focus();
                }

                if (StdPasswordBox != null)
                {
                    StdPasswordBox.IsEnabled = true;
                }
            };
        }

        /// <summary>
        /// Einfacher Event-Handler für die PasswordBox
        /// </summary>
        private void StdPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }

        /// <summary>
        /// Debugging-Event-Handler für den Login-Button
        /// </summary>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("LoginButton wurde geklickt");

            // Prüfen, ob das DataContext korrekt ist
            if (DataContext is LoginViewModel viewModel)
            {
                Debug.WriteLine("LoginViewModel gefunden");

                // Prüfen, ob das Command existiert und ausgeführt werden kann
                if (viewModel.LoginCommand != null)
                {
                    Debug.WriteLine("LoginCommand gefunden");

                    if (viewModel.LoginCommand.CanExecute(null))
                    {
                        Debug.WriteLine("LoginCommand kann ausgeführt werden");
                    }
                    else
                    {
                        Debug.WriteLine("LoginCommand kann NICHT ausgeführt werden (CanExecute = false)");
                    }
                }
                else
                {
                    Debug.WriteLine("LoginCommand ist null!");
                }
            }
            else
            {
                Debug.WriteLine($"DataContext ist kein LoginViewModel! Typ: {DataContext?.GetType().Name ?? "null"}");
            }
        }

        /// <summary>
        /// Wechselt zur Registrierungsansicht
        /// </summary>
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Bei den meisten MVVM-Implementierungen sollte das RegisterCommand im ViewModel 
            // die Navigation zur Registrierungsansicht übernehmen
            if (DataContext is LoginViewModel viewModel && viewModel.RegisterCommand != null)
            {
                // Das Command direkt ausführen - dieses sollte die Navigation handhaben
                viewModel.RegisterCommand.Execute(null);
            }
            else
            {
                MessageBox.Show("Navigation zur Registrierung nicht möglich. Das RegisterCommand ist nicht verfügbar.");
            }
        }
    }
}