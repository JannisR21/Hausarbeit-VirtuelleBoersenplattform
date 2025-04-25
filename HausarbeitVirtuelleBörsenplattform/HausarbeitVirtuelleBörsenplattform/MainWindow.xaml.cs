using HausarbeitVirtuelleBörsenplattform.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            try
            {
                InitializeComponent();
                Debug.WriteLine("MainWindow geladen");

                this.DataContext = new MainViewModel(); // Explizit im Code setzen
                Debug.WriteLine("MainViewModel gesetzt");

                // Event-Handler hinzufügen, der ausgeführt wird, wenn das Fenster vollständig geladen ist
                this.Loaded += MainWindow_Loaded;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Fehler im MainWindow-Konstruktor: {ex.Message}");
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainWindow_Loaded-Event ausgelöst");

            // Nach HandelsUserControl im visuellen Baum suchen
            FindAndCheckHandelsUserControl(this);
        }

        /// <summary>
        /// Durchsucht rekursiv den visuellen Baum nach dem HandelsUserControl und prüft die ComboBox
        /// </summary>
        private void FindAndCheckHandelsUserControl(DependencyObject parent)
        {
            Debug.WriteLine($"Suche HandelsUserControl in {parent.GetType().Name}");

            // Anzahl der Child-Elemente
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                // Prüfen, ob das aktuelle Child ein HandelsUserControl ist
                if (child is Views.HandelsUserControl handelsControl)
                {
                    Debug.WriteLine("HandelsUserControl gefunden!");

                    // CheckAndUpdateComboBox-Methode aufrufen
                    handelsControl.CheckAndUpdateComboBox();
                    return;
                }

                // Rekursiv weitersuchen
                FindAndCheckHandelsUserControl(child);
            }
        }
    }
}