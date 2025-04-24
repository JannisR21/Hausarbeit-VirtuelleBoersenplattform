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

            if (DataContext is AktienhandelViewModel viewModel)
            {
                Debug.WriteLine("ViewModel ist ein AktienhandelViewModel");

                // Nach einer kurzen Verzögerung die ComboBox-Bindung überprüfen
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    CheckAndUpdateComboBox();

                    // Wenn ein Symbol in der ComboBox ausgewählt ist, manuell LadeAktienDaten auslösen
                    if (!string.IsNullOrWhiteSpace(aktienComboBox.Text))
                    {
                        Debug.WriteLine($"Aktie in ComboBox ausgewählt: {aktienComboBox.Text}");
                        viewModel.AktienSymbol = aktienComboBox.Text;

                        // Manuelle Aktualisierung der Aktien-Daten
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // Suchen-Button manuell "drücken"
                            if (viewModel.AktienSuchenCommand.CanExecute(null))
                            {
                                viewModel.AktienSuchenCommand.Execute(null);
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
                        viewModel.SelectedAktie = ausgewählteAktie;

                        // Explizit Daten aktualisieren
                        viewModel.AktienSymbol = ausgewählteAktie.AktienSymbol;
                    }
                    else if (!string.IsNullOrWhiteSpace(aktienComboBox.Text))
                    {
                        // Bei manueller Texteingabe
                        Debug.WriteLine($"Text in ComboBox geändert: {aktienComboBox.Text}");
                        viewModel.AktienSymbol = aktienComboBox.Text;
                    }
                };

                // TextChanged-Event für die ComboBox hinzufügen (falls IsEditable=true)
                ((ComboBox)aktienComboBox).AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                    new TextChangedEventHandler((s, args) =>
                    {
                        if (!string.IsNullOrWhiteSpace(aktienComboBox.Text))
                        {
                            Debug.WriteLine($"Text in ComboBox geändert (TextChanged): {aktienComboBox.Text}");
                            viewModel.AktienSymbol = aktienComboBox.Text;
                        }
                    }));
            }
            else
            {
                Debug.WriteLine($"ViewModel ist kein AktienhandelViewModel, sondern: {DataContext?.GetType().Name ?? "null"}");
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

                    // Prüfen, ob App.StandardAktien verfügbar ist
                    if (App.StandardAktien != null && App.StandardAktien.Count > 0)
                    {
                        Debug.WriteLine($"App.StandardAktien enthält {App.StandardAktien.Count} Aktien");

                        // Forced-Binding-Update für die ComboBox
                        BindingOperations.GetBindingExpression(aktienComboBox, ItemsControl.ItemsSourceProperty)?.UpdateTarget();

                        // Kurze Verzögerung, um dem Binding Zeit zu geben
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // Nochmals prüfen
                            if (aktienComboBox.Items.Count == 0)
                            {
                                Debug.WriteLine("ComboBox ist immer noch leer, versuche manuelles Zuweisen");

                                // Manuelles Setzen als letzte Möglichkeit
                                aktienComboBox.ItemsSource = null;
                                aktienComboBox.ItemsSource = App.StandardAktien;

                                // Nochmals prüfen
                                Debug.WriteLine($"ComboBox hat jetzt {aktienComboBox.Items.Count} Elemente");

                                // Wenn nötig, Data Context aktualisieren
                                if (DataContext is AktienhandelViewModel viewModel)
                                {
                                    viewModel.InitializeWithAktien(App.StandardAktien);

                                    // Standard-Aktie auswählen (z.B. erste in der Liste)
                                    if (aktienComboBox.Items.Count > 0)
                                    {
                                        aktienComboBox.SelectedIndex = 0; // Erste Aktie auswählen

                                        if (aktienComboBox.SelectedItem is Aktie ersteAktie)
                                        {
                                            Debug.WriteLine($"Setze erste Aktie: {ersteAktie.AktienSymbol}");
                                            viewModel.SelectedAktie = ersteAktie;
                                            viewModel.AktienSymbol = ersteAktie.AktienSymbol;

                                            // Explizit LadeAktienDaten aufrufen
                                            Dispatcher.BeginInvoke(new Action(() =>
                                            {
                                                if (viewModel.AktienSuchenCommand.CanExecute(null))
                                                {
                                                    viewModel.AktienSuchenCommand.Execute(null);
                                                }
                                            }), System.Windows.Threading.DispatcherPriority.Background);
                                        }
                                    }
                                }
                            }
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                    else
                    {
                        Debug.WriteLine("App.StandardAktien ist null oder leer!");

                        // Wenn keine Standard-Aktien in der App verfügbar sind, eigene erstellen
                        var standardAktien = new System.Collections.ObjectModel.ObservableCollection<Models.Aktie>
                        {
                            new Models.Aktie { AktienID = 1, AktienSymbol = "AAPL", AktienName = "Apple Inc.", AktuellerPreis = 150.00m },
                            new Models.Aktie { AktienID = 2, AktienSymbol = "MSFT", AktienName = "Microsoft Corp.", AktuellerPreis = 320.45m },
                            new Models.Aktie { AktienID = 3, AktienSymbol = "TSLA", AktienName = "Tesla Inc.", AktuellerPreis = 200.20m },
                            new Models.Aktie { AktienID = 4, AktienSymbol = "AMZN", AktienName = "Amazon.com Inc.", AktuellerPreis = 95.10m },
                            new Models.Aktie { AktienID = 5, AktienSymbol = "GOOGL", AktienName = "Alphabet Inc.", AktuellerPreis = 128.75m }
                        };

                        // Direkt zuweisen
                        aktienComboBox.ItemsSource = standardAktien;

                        // Wenn nötig, Data Context aktualisieren
                        if (DataContext is AktienhandelViewModel viewModel)
                        {
                            viewModel.InitializeWithAktien(standardAktien);

                            // Erste Aktie auswählen
                            if (standardAktien.Count > 0)
                            {
                                aktienComboBox.SelectedIndex = 0;
                                viewModel.SelectedAktie = standardAktien[0];
                                viewModel.AktienSymbol = standardAktien[0].AktienSymbol;
                            }
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
                        if (DataContext is AktienhandelViewModel viewModel && aktienComboBox.SelectedItem is Aktie ersteAktie)
                        {
                            viewModel.SelectedAktie = ersteAktie;
                            viewModel.AktienSymbol = ersteAktie.AktienSymbol;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Überprüfen/Aktualisieren der ComboBox: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                try
                {
                    // Notfall-Fallback
                    var fallbackAktien = new System.Collections.ObjectModel.ObservableCollection<Models.Aktie>
                    {
                        new Models.Aktie { AktienID = 1, AktienSymbol = "AAPL", AktienName = "Apple Inc. (Fallback)", AktuellerPreis = 150.00m }
                    };

                    aktienComboBox.ItemsSource = fallbackAktien;

                    if (DataContext is AktienhandelViewModel viewModel)
                    {
                        viewModel.InitializeWithAktien(fallbackAktien);

                        if (fallbackAktien.Count > 0)
                        {
                            aktienComboBox.SelectedIndex = 0;
                            viewModel.SelectedAktie = fallbackAktien[0];
                            viewModel.AktienSymbol = fallbackAktien[0].AktienSymbol;
                        }
                    }
                }
                catch
                {
                    // Ignorieren - letzter Versuch fehlgeschlagen
                }
            }
        }
    }
}