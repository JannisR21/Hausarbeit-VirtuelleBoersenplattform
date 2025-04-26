using System;
using System.Windows;
using System.Windows.Input;
using HausarbeitVirtuelleBörsenplattform.ViewModels;
using System.Diagnostics;

namespace HausarbeitVirtuelleBörsenplattform
{
    /// <summary>
    /// Interaktionslogik für HandelsPopupWindow.xaml
    /// </summary>
    public partial class HandelsPopupWindow : Window
    {
        private MainViewModel _mainViewModel;

        /// <summary>
        /// Initialisiert ein neues HandelsPopupWindow
        /// </summary>
        /// <param name="owner">Das Besitzerfenster</param>
        /// <param name="mainViewModel">Das MainViewModel für die Datenbindung</param>
        public HandelsPopupWindow(Window owner, MainViewModel mainViewModel)
        {
            InitializeComponent();
            Owner = owner;
            _mainViewModel = mainViewModel;

            // DataContext des HandelsUserControl setzen
            if (_mainViewModel?.AktienhandelViewModel != null)
            {
                HandelsControl.DataContext = _mainViewModel.AktienhandelViewModel;
                Debug.WriteLine("HandelsUserControl DataContext wurde erfolgreich gesetzt");
            }
            else
            {
                Debug.WriteLine("FEHLER: AktienhandelViewModel ist null!");
            }

            // Fenster nach vorne bringen
            this.Loaded += (s, e) => this.Activate();
        }

        /// <summary>
        /// Behandelt das Ziehen des Fensters mit der Maus
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        /// <summary>
        /// Schließt das Fenster
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}