using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using HausarbeitVirtuelleBörsenplattform.Models;
using HausarbeitVirtuelleBörsenplattform.ViewModels;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für HandelsUserControl.xaml
    /// </summary>
    public partial class HandelsUserControl : UserControl
    {
        /// <summary>
        /// Initialisiert eine neue Instanz von HandelsUserControl
        /// </summary>
        public HandelsUserControl()
        {
            InitializeComponent();
            Debug.WriteLine("HandelsUserControl initialisiert");
        }

        /// <summary>
        /// Event-Handler für das Laden des Controls
        /// </summary>
        private void HandelsUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("HandelsUserControl_Loaded ausgelöst");

            if (DataContext is AktienhandelViewModel aktienViewModel)
            {
                Debug.WriteLine("ViewModel ist ein AktienhandelViewModel");

                // Wichtig: Prüfen, ob AktienListe Daten enthält
                if (aktienViewModel.AktienListe == null || aktienViewModel.AktienListe.Count == 0)
                {
                    Debug.WriteLine("AktienListe ist leer oder null, initialisiere erneut");

                    // Explizit die Aktienliste initialisieren
                    Dispatcher.BeginInvoke(new Action(() => {
                        try
                        {
                            // Methode zum Neuinitialisieren aufrufen
                            var methode = aktienViewModel.GetType().GetMethod("InitializeAktienListe",
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);

                            if (methode != null)
                            {
                                Debug.WriteLine("Rufe InitializeAktienListe explizit auf");
                                methode.Invoke(aktienViewModel, null);
                            }
                            else
                            {
                                Debug.WriteLine("InitializeAktienListe Methode nicht gefunden");
                            }

                            // Property-Changed auslösen
                            aktienViewModel.GetType().GetMethod("OnPropertyChanged",
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance)?.Invoke(aktienViewModel,
                                new object[] { nameof(aktienViewModel.AktienListe) });

                            aktienViewModel.GetType().GetMethod("OnPropertyChanged",
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance)?.Invoke(aktienViewModel,
                                new object[] { nameof(aktienViewModel.GefilterteAktienListe) });

                            // Nach der Initialisierung ComboBox aktualisieren
                            CheckAndUpdateComboBox();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Fehler beim Initialisieren der AktienListe: {ex.Message}");
                        }
                    }));
                }

                // Nach einer kurzen Verzögerung die ComboBox-Bindung überprüfen
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    CheckAndUpdateComboBox();

                    // Wenn ein Symbol in der ComboBox ausgewählt ist, manuell AktienSuchenCommand auslösen
                    if (!string.IsNullOrWhiteSpace(aktienComboBox.Text))
                    {
                        Debug.WriteLine($"Aktie in ComboBox ausgewählt: {aktienComboBox.Text}");
                        aktienViewModel.AktienSymbol = aktienComboBox.Text;

                        // Manuelle Aktualisierung der Aktien-Daten
                        Dispatcher.BeginInvoke(new Action(async () =>
                        {
                            // Suchen-Button manuell "drücken"
                            if (aktienViewModel.AktienSuchenCommand.CanExecute(null))
                            {
                                await aktienViewModel.AktienSuchenCommand.ExecuteAsync(null);
                            }
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);

                // Event-Handler für Änderungen in der ComboBox hinzufügen
                aktienComboBox.SelectionChanged += (s, args) =>
                {
                    Debug.WriteLine("ComboBox SelectionChanged ausgelöst");

                    if (aktienComboBox.SelectedItem is Aktie ausgewählteAktie)
                    {
                        Debug.WriteLine($"Aktie ausgewählt: {ausgewählteAktie.AktienSymbol}");
                        aktienViewModel.SelectedAktie = ausgewählteAktie;

                        // Explizit Daten aktualisieren
                        aktienViewModel.AktienSymbol = ausgewählteAktie.AktienSymbol;
                    }
                    else if (!string.IsNullOrWhiteSpace(aktienComboBox.Text))
                    {
                        // Bei manueller Texteingabe
                        Debug.WriteLine($"Text in ComboBox geändert: {aktienComboBox.Text}");
                        aktienViewModel.AktienSymbol = aktienComboBox.Text;
                    }
                };

                // TextChanged-Event für die ComboBox hinzufügen (falls IsEditable=true)
                ((ComboBox)aktienComboBox).AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                    new TextChangedEventHandler((s, args) =>
                    {
                        if (!string.IsNullOrWhiteSpace(aktienComboBox.Text))
                        {
                            Debug.WriteLine($"Text in ComboBox geändert (TextChanged): {aktienComboBox.Text}");
                            aktienViewModel.AktienSymbol = aktienComboBox.Text;
                        }
                    }));
            }
            else
            {
                Debug.WriteLine($"ViewModel ist kein AktienhandelViewModel, sondern: {DataContext?.GetType().Name ?? "null"}");
            }
        }


        private void AktienComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            Debug.WriteLine($"ComboBox SelectionChanged");
            Debug.WriteLine($"Items Count: {comboBox.Items.Count}");
            Debug.WriteLine($"SelectedItem: {comboBox.SelectedItem}");

            if (DataContext is AktienhandelViewModel vm)
            {
                Debug.WriteLine($"ViewModel AktienListe Count: {vm.AktienListe?.Count ?? 0}");
                Debug.WriteLine($"ViewModel GefilterteAktienListe Count: {vm.GefilterteAktienListe?.Count() ?? 0}");
            }
        }

        /// <summary>
        /// Prüft den Status des ComboBox-Inhalts und aktualisiert ihn bei Bedarf
        /// </summary>
        public void CheckAndUpdateComboBox()
        {
            try
            {
                Debug.WriteLine($"CheckAndUpdateComboBox aufgerufen, aktienComboBox.Items.Count = {aktienComboBox.Items.Count}");

                // Prüfen, ob die ComboBox Elemente hat
                if (aktienComboBox.Items.Count == 0)
                {
                    Debug.WriteLine("ComboBox ist leer, versuche zu aktualisieren");

                    // Logging für DataContext und ViewModel
                    if (DataContext is AktienhandelViewModel vm)
                    {
                        Debug.WriteLine($"AktienListe Count: {vm.AktienListe?.Count ?? 0}");
                        Debug.WriteLine($"GefilterteAktienListe Count: {vm.GefilterteAktienListe?.Count() ?? 0}");
                        Debug.WriteLine($"SuchText: '{vm.SuchText}'");
                    }

                    // Erzwinge die Aktualisierung der ItemsSource-Bindung
                    BindingExpression bindingExpression = BindingOperations.GetBindingExpression(aktienComboBox, ItemsControl.ItemsSourceProperty);
                    if (bindingExpression != null)
                    {
                        Debug.WriteLine("Binding Expression gefunden, aktualisiere Target");
                        bindingExpression.UpdateTarget();
                    }
                    else
                    {
                        Debug.WriteLine("Keine Binding Expression gefunden!");
                    }

                    // Wenn nach der Aktualisierung immer noch keine Items vorhanden sind,
                    // dann haben wir möglicherweise Probleme mit der Datenquelle
                    if (aktienComboBox.Items.Count == 0)
                    {
                        Debug.WriteLine("ComboBox ist immer noch leer nach Binding-Aktualisierung");

                        // Prüfen, ob wir die AktienListe direkt setzen können
                        if (DataContext is AktienhandelViewModel aktienVm && aktienVm.AktienListe != null && aktienVm.AktienListe.Count > 0)
                        {
                            Debug.WriteLine("Setze ItemsSource direkt auf AktienListe");
                            aktienComboBox.ItemsSource = aktienVm.AktienListe;
                        }
                        else
                        {
                            MessageBox.Show("Es konnten keine Aktien geladen werden. Bitte versuchen Sie es später erneut.",
                                "Keine Aktien verfügbar", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"ComboBox hat bereits {aktienComboBox.Items.Count} Elemente, keine Aktualisierung nötig");

                    // Prüfen, ob bereits eine Aktie ausgewählt ist
                    if (aktienComboBox.SelectedItem == null && aktienComboBox.Items.Count > 0)
                    {
                        Debug.WriteLine("Keine Aktie ausgewählt, wähle die erste");
                        aktienComboBox.SelectedIndex = 0;

                        // ViewModel aktualisieren
                        if (DataContext is AktienhandelViewModel aktienVm && aktienComboBox.SelectedItem is Aktie ersteAktie)
                        {
                            aktienVm.SelectedAktie = ersteAktie;
                            aktienVm.AktienSymbol = ersteAktie.AktienSymbol;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Überprüfen/Aktualisieren der ComboBox: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                MessageBox.Show($"Beim Laden der Aktienauswahl ist ein Fehler aufgetreten: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}