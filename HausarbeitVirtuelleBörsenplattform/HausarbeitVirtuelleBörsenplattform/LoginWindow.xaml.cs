using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HausarbeitVirtuelleBörsenplattform.ViewModels;

namespace HausarbeitVirtuelleBörsenplattform
{
    /// <summary>
    /// Interaktionslogik für LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private UserControl _loginView;
        private UserControl _registrationView;
        private UserControl _currentView;

        /// <summary>
        /// Initialisiert eine neue Instanz des LoginWindow
        /// </summary>
public LoginWindow()
{
    InitializeComponent();

    MainContent.Content = new Views.LoginView
    {
        DataContext = new LoginViewModel()
    };
}


        /// <summary>
        /// Zeigt die Login-Ansicht an
        /// </summary>
        public void ShowLoginView()
        {
            MainContent.Content = new Views.LoginView
            {
                DataContext = new LoginViewModel()
            };
        }


        /// <summary>
        /// Zeigt die Registrierungs-Ansicht an
        /// </summary>
        public void ShowRegistrationView()
        {
            MainContent.Content = new Views.RegistrationView();
        }



        /// <summary>
        /// Setzt den Hauptinhalt des Fensters
        /// </summary>
        private void SetMainContent(UserControl content)
        {
            MainContent.Content = content;
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loginView?.DataContext is ViewModels.LoginViewModel viewModel)
            {
                viewModel.TogglePasswordVisibilityCommand.Execute(null);
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }



        /// <summary>
        /// Behandelt Mausklick-Ereignisse in der Titelleiste zum Verschieben des Fensters
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
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
