using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using HausarbeitVirtuelleBörsenplattform.Models;
using HausarbeitVirtuelleBörsenplattform.ViewModels;
using System.Collections.ObjectModel;
using HausarbeitVirtuelleBörsenplattform.Helpers;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für HandelsUserControl.xaml
    /// </summary>
    public partial class HandelsUserControl : UserControl
    {
        // Flag, um doppelte Initialisierung zu verhindern
        private bool _isInitialized = false;

        // Flag, um doppelte Event-Registrierung für den Watchlist-Button zu verhindern
        private bool _watchlistButtonInitialized = false;

        /// <summary>
        /// Initialisiert eine neue Instanz von HandelsUserControl
        /// </summary>
        public HandelsUserControl()
        {
            InitializeComponent();
            Debug.WriteLine("HandelsUserControl initialisiert");
        }

        /// <summary>
        /// Initialisiert den Watchlist-Button und seine Event-Handler
        /// </summary>
        private void InitializeWatchlistButton()
        {
            // Wenn bereits initialisiert, nicht erneut ausführen
            if (_watchlistButtonInitialized)
                return;

            // Falls der Button existiert, Event-Handler hinzufügen
            if (WatchlistButton != null)
            {
                // Bestehenden Handler entfernen, falls einer existiert
                WatchlistButton.Click -= WatchlistButton_Click;
                // Neuen Handler hinzufügen
                WatchlistButton.Click += WatchlistButton_Click;

                _watchlistButtonInitialized = true;
                Debug.WriteLine("WatchlistButton initialisiert");
            }
        }

        /// <summary>
        /// Bereinigt den Watchlist-Button und entfernt seine Event-Handler
        /// </summary>
        private void CleanupWatchlistButton()
        {
            if (WatchlistButton != null)
            {
                // Event-Handler entfernen
                WatchlistButton.Click -= WatchlistButton_Click;
                _watchlistButtonInitialized = false;
                Debug.WriteLine("WatchlistButton bereinigt");
            }
        }

        /// <summary>
        /// Event-Handler für den Watchlist-Button-Klick
        /// </summary>
        private void WatchlistButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("WatchlistButton_Click wurde aufgerufen");

                // Versuch, das MainWindow zu bekommen
                var mainWindow = Application.Current.MainWindow;

                if (mainWindow == null)
                {
                    Debug.WriteLine("MainWindow konnte nicht gefunden werden");
                    MessageBox.Show("Die Watchlist ist nicht verfügbar.",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Versuche das MainViewModel zu bekommen
                if (mainWindow.DataContext is MainViewModel mainViewModel)
                {
                    Debug.WriteLine("MainViewModel gefunden");

                    // Das WatchlistViewModel aus dem MainViewModel holen
                    var watchlistViewModel = mainViewModel.WatchlistViewModel;

                    // Prüfen ob das WatchlistViewModel vorhanden ist
                    if (watchlistViewModel == null)
                    {
                        Debug.WriteLine("WatchlistViewModel ist null");
                        MessageBox.Show("Die Watchlist ist nicht verfügbar.",
                            "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Das ausgewählte ViewModel und die ausgewählte Aktie holen
                    if (DataContext is AktienhandelViewModel viewModel)
                    {
                        Debug.WriteLine($"DataContext ist AktienhandelViewModel");

                        // Prüfen ob eine Aktie ausgewählt ist
                        if (viewModel.SelectedAktie == null)
                        {
                            Debug.WriteLine("Keine Aktie ausgewählt");
                            MessageBox.Show("Es ist keine Aktie ausgewählt.",
                                "Hinzufügen nicht möglich", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var aktie = viewModel.SelectedAktie;
                        Debug.WriteLine($"Ausgewählte Aktie: {aktie.AktienSymbol} - {aktie.AktienName}");

                        // Über das Command hinzufügen
                        if (watchlistViewModel.AktieHinzufügenCommand != null &&
                            watchlistViewModel.AktieHinzufügenCommand.CanExecute(aktie))
                        {
                            Debug.WriteLine($"Füge Aktie {aktie.AktienSymbol} zur Watchlist hinzu");

                            // Command ausführen
                            watchlistViewModel.AktieHinzufügenCommand.Execute(aktie);

                            // Erfolgsmeldung anzeigen
                            string aktienName = aktie.AktienName;
                            string aktienSymbol = aktie.AktienSymbol;

                            MessageBox.Show($"Die Aktie {aktienName} ({aktienSymbol}) wurde zur Watchlist hinzugefügt.",
                                "Watchlist aktualisiert", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            Debug.WriteLine("Command konnte nicht ausgeführt werden");
                            MessageBox.Show("Die Aktie konnte nicht zur Watchlist hinzugefügt werden.",
                                "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"DataContext ist kein AktienhandelViewModel, sondern: {DataContext?.GetType().Name ?? "null"}");
                        MessageBox.Show("Das Aktienhandel-Feature ist nicht verfügbar.",
                            "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    Debug.WriteLine($"MainWindow.DataContext ist kein MainViewModel, sondern: {mainWindow.DataContext?.GetType().Name ?? "null"}");
                    MessageBox.Show("Die Watchlist ist nicht verfügbar.",
                        "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler in WatchlistButton_Click: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                MessageBox.Show($"Fehler beim Hinzufügen zur Watchlist: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event-Handler für das Laden des Controls
        /// </summary>
        private void HandelsUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("HandelsUserControl_Loaded ausgelöst");

            // Zuerst alle bestehenden Handler entfernen, um doppelte Registrierung zu vermeiden
            this.Unloaded -= HandelsUserControl_Unloaded;
            this.Unloaded += HandelsUserControl_Unloaded;

            // Wichtig: Watchlist-Button initialisieren
            InitializeWatchlistButton();

            if (aktienFilter != null)
            {
                aktienFilter.FilterChanged -= AktienFilter_FilterChanged;
                aktienFilter.FilterChanged += AktienFilter_FilterChanged;
            }

            if (aktienComboBox != null)
            {
                aktienComboBox.SelectionChanged -= ComboBox_SelectionChanged;
                aktienComboBox.SelectionChanged -= AktienComboBox_SelectionChanged;

                // TextChanged-Handler entfernen falls vorhanden
                try
                {
                    ((ComboBox)aktienComboBox).RemoveHandler(
                        System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                        new TextChangedEventHandler(ComboBox_TextChanged));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Entfernen des TextChanged-Handlers: {ex.Message}");
                }
            }

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

                // Verzögerung für die Initialisierung hinzugefügt
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Initialisierung nur einmal durchführen
                    if (!_isInitialized)
                    {
                        _isInitialized = true;

                        // AktienFilterControl initialisieren
                        InitializeFilterControl();

                        // ComboBox aktualisieren
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

                        // Event-Handler für Änderungen in der ComboBox hinzufügen
                        aktienComboBox.SelectionChanged += ComboBox_SelectionChanged;

                        // TextChanged-Event für die ComboBox hinzufügen (falls IsEditable=true)
                        ((ComboBox)aktienComboBox).AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                            new TextChangedEventHandler(ComboBox_TextChanged));
                    }
                    else
                    {
                        Debug.WriteLine("HandelsUserControl wurde bereits initialisiert, überspringe doppelte Initialisierung");
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            else
            {
                Debug.WriteLine($"ViewModel ist kein AktienhandelViewModel, sondern: {DataContext?.GetType().Name ?? "null"}");
            }
        }

        /// <summary>
        /// Event-Handler für das Entladen des Controls
        /// </summary>
        private void HandelsUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("HandelsUserControl_Unloaded ausgelöst");

            // Watchlist-Button bereinigen
            CleanupWatchlistButton();

            // Alle Event-Handler entfernen
            if (aktienFilter != null)
            {
                aktienFilter.FilterChanged -= AktienFilter_FilterChanged;

                // Wichtig: Auch das DataContext zurücksetzen oder zumindest Flags bereinigen
                if (aktienFilter.DataContext is AktienFilterViewModel filterVM)
                {
                    var field = filterVM.GetType().GetField("_isFilteringInProgress",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (field != null)
                    {
                        field.SetValue(filterVM, false);
                        Debug.WriteLine("_isFilteringInProgress bei Unload auf false zurückgesetzt");
                    }
                }

                // Optional: DataContext entfernen
                aktienFilter.DataContext = null;
            }

            // ComboBox-Event-Handler entfernen
            if (aktienComboBox != null)
            {
                aktienComboBox.SelectionChanged -= ComboBox_SelectionChanged;
                aktienComboBox.SelectionChanged -= AktienComboBox_SelectionChanged;

                // TextChanged-Handler entfernen
                try
                {
                    ((ComboBox)aktienComboBox).RemoveHandler(
                        System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                        new TextChangedEventHandler(ComboBox_TextChanged));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Entfernen des TextChanged-Handlers: {ex.Message}");
                }
            }

            // Unload-Handler entfernen
            this.Unloaded -= HandelsUserControl_Unloaded;

            // Flag zurücksetzen, damit beim nächsten Laden eine vollständige Initialisierung stattfindet
            _isInitialized = false;

            Debug.WriteLine("HandelsUserControl wurde vollständig entladen");

            // DataContext entfernen, um Speicherlecks zu vermeiden
            this.DataContext = null;

            Debug.WriteLine("AktienFilterUserControl entladen");
        }

        /// <summary>
        /// Event-Handler für ComboBox SelectionChanged
        /// </summary>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            Debug.WriteLine($"ComboBox SelectionChanged");
            Debug.WriteLine($"Items Count: {comboBox.Items.Count}");
            Debug.WriteLine($"SelectedItem: {comboBox.SelectedItem}");

            if (DataContext is AktienhandelViewModel vm)
            {
                Debug.WriteLine($"ViewModel AktienListe Count: {vm.AktienListe?.Count ?? 0}");
                Debug.WriteLine($"ViewModel GefilterteAktienListe Count: {vm.GefilterteAktienListe?.Count() ?? 0}");

                if (comboBox.SelectedItem is Aktie ausgewählteAktie)
                {
                    Debug.WriteLine($"Aktie ausgewählt: {ausgewählteAktie.AktienSymbol}");
                    vm.SelectedAktie = ausgewählteAktie;

                    // Explizit Daten aktualisieren
                    vm.AktienSymbol = ausgewählteAktie.AktienSymbol;
                }
                else if (!string.IsNullOrWhiteSpace(comboBox.Text))
                {
                    // Bei manueller Texteingabe
                    Debug.WriteLine($"Text in ComboBox geändert: {comboBox.Text}");
                    vm.AktienSymbol = comboBox.Text;
                }
            }
        }

        /// <summary>
        /// Event-Handler für ComboBox TextChanged
        /// </summary>
        private void ComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(aktienComboBox.Text) && DataContext is AktienhandelViewModel vm)
            {
                Debug.WriteLine($"Text in ComboBox geändert (TextChanged): {aktienComboBox.Text}");
                vm.AktienSymbol = aktienComboBox.Text;
            }
        }

        /// <summary>
        /// Initialisiert das AktienFilterControl
        /// </summary>
        private void InitializeFilterControl()
        {
            try
            {
                Debug.WriteLine("Initialisiere AktienFilterControl");

                // Wichtig: Event-Handler explizit entfernen
                if (aktienFilter != null)
                {
                    aktienFilter.FilterChanged -= AktienFilter_FilterChanged;
                }

                // MainViewModel auf konventionellem Weg holen
                var mainWindow = Application.Current.MainWindow as MainWindow;
                var mainViewModel = mainWindow?.DataContext as MainViewModel;

                if (mainViewModel != null && mainViewModel.AktienFilterViewModel != null)
                {
                    Debug.WriteLine("MainViewModel und AktienFilterViewModel gefunden");

                    // Stellen sicher, dass das Flag zurückgesetzt ist
                    var field = mainViewModel.AktienFilterViewModel.GetType().GetField("_isFilteringInProgress",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (field != null)
                    {
                        field.SetValue(mainViewModel.AktienFilterViewModel, false);
                        Debug.WriteLine("_isFilteringInProgress wurde auf false zurückgesetzt");
                    }

                    // Dann erst DataContext setzen
                    aktienFilter.DataContext = mainViewModel.AktienFilterViewModel;

                    // Jetzt den Event-Handler neu hinzufügen
                    aktienFilter.FilterChanged += AktienFilter_FilterChanged;

                    // Aktienlistendaten übertragen
                    if (DataContext is AktienhandelViewModel handelsVM && handelsVM.AktienListe != null)
                    {
                        mainViewModel.AktienFilterViewModel.SetzeAktienListe(handelsVM.AktienListe);
                        Debug.WriteLine("AktienListe an AktienFilterViewModel übergeben");
                    }

                    // Filter explizit aktualisieren
                    mainViewModel.AktienFilterViewModel.FilterAnwendenCommand.Execute(null);
                }
                else
                {
                    Debug.WriteLine("MainViewModel oder AktienFilterViewModel ist null");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Initialisieren des AktienFilterControls: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Behandelt Änderungen der Filteroptionen
        /// </summary>
        private void AktienFilter_FilterChanged(object sender, FilterChangedEventArgs e)
        {
            Debug.WriteLine("AktienFilter_FilterChanged aufgerufen");

            if (DataContext is AktienhandelViewModel aktienViewModel)
            {
                try
                {
                    // Gefilterte Aktien in die ComboBox übernehmen
                    if (e.GefilterteAktien != null)
                    {
                        // Temporäre Liste mit gefilterten Aktien erstellen
                        var tempList = new ObservableCollection<Aktie>();
                        foreach (var aktie in e.GefilterteAktien)
                        {
                            tempList.Add(aktie);
                        }

                        // ComboBox aktualisieren
                        aktienComboBox.ItemsSource = tempList;

                        Debug.WriteLine($"Gefilterte Aktien-Liste mit {tempList.Count} Elementen in ComboBox übernommen");

                        // Auch den SuchText im AktienhandelViewModel aktualisieren
                        if (!string.IsNullOrEmpty(e.SuchText))
                        {
                            aktienViewModel.SuchText = e.SuchText;
                            aktienComboBox.Text = e.SuchText;
                            Debug.WriteLine($"SuchText im ViewModel aktualisiert: {e.SuchText}");
                        }

                        // Wenn nur ein Element, dieses automatisch auswählen
                        if (tempList.Count == 1)
                        {
                            aktienComboBox.SelectedItem = tempList[0];
                            aktienViewModel.SelectedAktie = tempList[0];
                            aktienViewModel.AktienSymbol = tempList[0].AktienSymbol;

                            // Optional: Automatisch Suchen auslösen
                            Dispatcher.BeginInvoke(new Action(async () => {
                                if (aktienViewModel.AktienSuchenCommand.CanExecute(null))
                                {
                                    Debug.WriteLine($"Auto-Suche für gefilterte Aktie: {tempList[0].AktienSymbol}");
                                    await aktienViewModel.AktienSuchenCommand.ExecuteAsync(null);
                                }
                            }));
                        }
                        else if (tempList.Count > 0 && !string.IsNullOrEmpty(e.SuchText))
                        {
                            // Wenn mehrere Elemente und ein Suchtext vorhanden, versuche exakte Übereinstimmung zu finden
                            var exactMatch = tempList.FirstOrDefault(a =>
                                a.AktienSymbol.Equals(e.SuchText, StringComparison.OrdinalIgnoreCase) ||
                                a.AktienName.Equals(e.SuchText, StringComparison.OrdinalIgnoreCase));

                            if (exactMatch != null)
                            {
                                aktienComboBox.SelectedItem = exactMatch;
                                aktienViewModel.SelectedAktie = exactMatch;
                                aktienViewModel.AktienSymbol = exactMatch.AktienSymbol;

                                // Automatisch Suchen auslösen
                                Dispatcher.BeginInvoke(new Action(async () => {
                                    if (aktienViewModel.AktienSuchenCommand.CanExecute(null))
                                    {
                                        Debug.WriteLine($"Auto-Suche für exakt passende Aktie: {exactMatch.AktienSymbol}");
                                        await aktienViewModel.AktienSuchenCommand.ExecuteAsync(null);
                                    }
                                }));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler bei Filteränderung: {ex.Message}");
                }
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