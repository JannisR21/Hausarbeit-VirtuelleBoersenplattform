using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HausarbeitVirtuelleBörsenplattform.ViewModels;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für HistorischeDatenUserControl.xaml
    /// </summary>
    public partial class HistorischeDatenUserControl : UserControl
    {
        // Flag, um doppelte Initialisierung zu verhindern
        private bool _isInitialized = false;

        /// <summary>
        /// Initialisiert eine neue Instanz von HistorischeDatenUserControl
        /// </summary>
        public HistorischeDatenUserControl()
        {
            InitializeComponent();
            Debug.WriteLine("HistorischeDatenUserControl initialisiert");
        }

        /// <summary>
        /// Event-Handler für das Laden des Controls
        /// </summary>
        private void HistorischeDatenUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("HistorischeDatenUserControl_Loaded ausgelöst");

            // Zuerst alle bestehenden Handler entfernen, um doppelte Registrierung zu vermeiden
            this.Unloaded -= HistorischeDatenUserControl_Unloaded;
            this.Unloaded += HistorischeDatenUserControl_Unloaded;

            if (aktienComboBox != null)
            {
                aktienComboBox.SelectionChanged -= AktienComboBox_SelectionChanged;
                aktienComboBox.SelectionChanged += AktienComboBox_SelectionChanged;
            }

            if (DataContext is HistorischeDatenViewModel viewModel)
            {
                Debug.WriteLine("ViewModel ist ein HistorischeDatenViewModel");

                // Verzögerung für die Initialisierung hinzufügen
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    // Initialisierung nur einmal durchführen
                    if (!_isInitialized)
                    {
                        _isInitialized = true;

                        // Laden der verfügbaren Aktien
                        if (viewModel.VerfügbareAktien == null || viewModel.VerfügbareAktien.Count == 0)
                        {
                            viewModel.LadeVerfügbareAktienCommand.Execute(null);
                        }

                        // Standardzeitraum für die letzten 5 Tage setzen
                        viewModel.VonDatum = DateTime.Now.AddDays(-5);
                        viewModel.BisDatum = DateTime.Now;

                        Debug.WriteLine($"Zeitraum auf {viewModel.VonDatum:yyyy-MM-dd} bis {viewModel.BisDatum:yyyy-MM-dd} gesetzt");

                        // Warten, bis die Aktien geladen sind
                        await Task.Delay(500);

                        // Erste Aktie auswählen, falls noch keine ausgewählt ist
                        if (viewModel.AusgewählteAktie == null && viewModel.VerfügbareAktien.Count > 0)
                        {
                            aktienComboBox.SelectedIndex = 0;
                            Debug.WriteLine("Erste Aktie automatisch ausgewählt");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("HistorischeDatenUserControl wurde bereits initialisiert, überspringe doppelte Initialisierung");
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            else
            {
                Debug.WriteLine($"ViewModel ist kein HistorischeDatenViewModel, sondern: {DataContext?.GetType().Name ?? "null"}");
            }
        }

        /// <summary>
        /// Event-Handler für das Entladen des Controls
        /// </summary>
        private void HistorischeDatenUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("HistorischeDatenUserControl_Unloaded ausgelöst");

            // Alle Event-Handler entfernen
            if (aktienComboBox != null)
            {
                aktienComboBox.SelectionChanged -= AktienComboBox_SelectionChanged;
            }

            // Unload-Handler entfernen
            this.Unloaded -= HistorischeDatenUserControl_Unloaded;

            // Flag zurücksetzen, damit beim nächsten Laden eine vollständige Initialisierung stattfindet
            _isInitialized = false;

            Debug.WriteLine("HistorischeDatenUserControl wurde vollständig entladen");

            // DataContext entfernen, um Speicherlecks zu vermeiden
            this.DataContext = null;

            Debug.WriteLine("HistorischeDatenUserControl entladen");
        }

        /// <summary>
        /// Event-Handler für ComboBox SelectionChanged
        /// </summary>
        private async void AktienComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            Debug.WriteLine($"AktienComboBox SelectionChanged");

            if (DataContext is HistorischeDatenViewModel viewModel && viewModel.AusgewählteAktie != null)
            {
                Debug.WriteLine($"Aktie ausgewählt: {viewModel.AusgewählteAktie.AktienSymbol}");

                // Automatisch Daten laden, wenn eine Aktie ausgewählt wurde
                // Kleiner Verzögerung, um sicherzustellen, dass das UI aktualisiert ist
                await Task.Delay(100);
                if (viewModel.AktualisierenCommand.CanExecute(null))
                {
                    Debug.WriteLine("Führe AktualisierenCommand aus...");
                    viewModel.AktualisierenCommand.Execute(null);
                }
            }
        }
    }
}