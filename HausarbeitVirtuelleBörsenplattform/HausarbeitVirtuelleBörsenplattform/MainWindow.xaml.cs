using HausarbeitVirtuelleBörsenplattform.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using System.ComponentModel;

namespace HausarbeitVirtuelleBörsenplattform
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                Debug.WriteLine("MainWindow geladen");

                // Prüfen, ob wir im Designer-Modus sind
                if (DesignerProperties.GetIsInDesignMode(this))
                {
                    // Im Designer-Modus setzen wir ein Mock-ViewModel, 
                    // das keine Abhängigkeit vom angemeldeten Benutzer hat
                    Debug.WriteLine("Designer-Modus erkannt, setze Mock-ViewModel");

                    // Hier würde ein spezielles DesignerViewModel verwendet werden,
                    // das wir hier der Einfachheit halber weglassen
                }
                else
                {
                    // In der Laufzeit setzen wir das echte ViewModel
                    this.DataContext = new MainViewModel();
                    Debug.WriteLine("MainViewModel gesetzt");

                    // Nur in der Laufzeit die Inhalte laden
                    LoadRuntimeContent();
                }

                // Event-Handler hinzufügen, der ausgeführt wird, wenn das Fenster vollständig geladen ist
                this.Loaded += MainWindow_Loaded;

                // Event-Handler für UserButton hinzufügen
                UserButton.Click += UserButton_Click;

                // Event-Handler für Logout-MenuItem
                var logoutMenuItem = UserButton.ContextMenu.Items[3] as MenuItem;
                if (logoutMenuItem != null)
                {
                    logoutMenuItem.Click += LogoutMenuItem_Click;
                }

                // Event-Handler für Einstellungen-Navigation
                var einstellungenButton = FindName("EinstellungenButton") as Button;
                if (einstellungenButton != null)
                {
                    einstellungenButton.Click += EinstellungenButton_Click;
                }

                // Event-Handler für Watchlist-Navigation hinzufügen
                var watchlistButton = FindName("WatchlistButton") as Button;
                if (watchlistButton != null)
                {
                    watchlistButton.Click += WatchlistButton_Click;
                }

                // Event-Handler für Dashboard-Navigation hinzufügen
                var dashboardButton = FindName("DashboardButton") as Button;
                if (dashboardButton != null)
                {
                    dashboardButton.Click += DashboardButton_Click;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler im MainWindow-Konstruktor: {ex.Message}");
            }
        }

        // Methode zum Laden der Inhalte nur in der Laufzeit, nicht im Designer
        private void LoadRuntimeContent()
        {
            try
            {
                // Hier können wir die Inhalte für die Laufzeit laden
                if (MainContentGrid != null && DataContext is MainViewModel viewModel)
                {
                    // Portfolio-Bereich
                    var portfolioBorder = new Border
                    {
                        Style = (Style)FindResource("CardBorderStyle"),
                        Child = new Views.PortfolioUserControl
                        {
                            DataContext = viewModel.PortfolioViewModel
                        }
                    };
                    Grid.SetRow(portfolioBorder, 0);
                    Grid.SetColumn(portfolioBorder, 0);
                    Grid.SetRowSpan(portfolioBorder, 2);

                    // Marktdaten-Bereich
                    var marktdatenBorder = new Border
                    {
                        Style = (Style)FindResource("CardBorderStyle"),
                        Child = new Views.MarktdatenUserControl
                        {
                            DataContext = viewModel.MarktdatenViewModel
                        }
                    };
                    Grid.SetRow(marktdatenBorder, 0);
                    Grid.SetColumn(marktdatenBorder, 1);
                    Grid.SetRowSpan(marktdatenBorder, 2);

                    // Zu MainContentGrid hinzufügen
                    MainContentGrid.Children.Add(portfolioBorder);
                    MainContentGrid.Children.Add(marktdatenBorder);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Inhalte: {ex.Message}");
            }
        }

        /// <summary>
        /// Handler für den Klick auf den Handels-Button
        /// </summary>
        private void HandelsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("HandelsButton wurde geklickt");

                // MainViewModel aus dem DataContext abrufen
                if (DataContext is MainViewModel mainViewModel)
                {
                    // Neues HandelsPopupWindow erstellen
                    var handelsPopup = new HandelsPopupWindow(this, mainViewModel);

                    // Fenster anzeigen
                    handelsPopup.ShowDialog();

                    Debug.WriteLine("HandelsPopupWindow wurde angezeigt");
                }
                else
                {
                    Debug.WriteLine("FEHLER: DataContext ist kein MainViewModel");
                    MessageBox.Show("Fehler beim Öffnen des Aktienhandels.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Öffnen des HandelsPopupWindow: {ex.Message}");
                MessageBox.Show($"Fehler beim Öffnen des Aktienhandels: {ex.Message}",
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handler für den Klick auf den Dashboard-Button
        /// </summary>
        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("Dashboard-Button wurde geklickt");

                // Einfach die Hauptansicht wiederherstellen
                RestoreMainView();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Öffnen des Dashboards: {ex.Message}");
                MessageBox.Show($"Fehler beim Öffnen des Dashboards: {ex.Message}",
                              "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handler für den Klick auf den Watchlist-Button
        /// </summary>
        private void WatchlistButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Im Designer-Modus nichts tun
                if (DesignerProperties.GetIsInDesignMode(this))
                    return;

                Debug.WriteLine("Watchlist-Button wurde geklickt");

                // Haupt-Grid für Inhalte finden
                var mainGrid = FindMainContentGrid();
                if (mainGrid == null)
                {
                    Debug.WriteLine("Konnte das Haupt-Grid nicht finden");
                    MessageBox.Show("Fehler beim Öffnen der Watchlist.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Watchlist-Ansicht anzeigen
                mainGrid.Children.Clear();

                // Immer eine neue Instanz des WatchlistUserControl erstellen, um Probleme mit der visuellen Hierarchie zu vermeiden
                var watchlistControl = new Views.WatchlistUserControl();

                // DataContext setzen
                if (DataContext is MainViewModel vm1)
                {
                    watchlistControl.DataContext = vm1.WatchlistViewModel;
                }

                // Neues Grid für die Watchlist-Ansicht erstellen
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.VerticalAlignment = VerticalAlignment.Stretch;
                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                grid.MinHeight = 600; // Mindesthöhe setzen

                // Border für die Watchlist-Ansicht erstellen
                var border = new Border
                {
                    Style = (Style)FindResource("CardBorderStyle"),
                    Child = watchlistControl,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    MinHeight = 580
                };

                Grid.SetRow(border, 0);
                Grid.SetColumn(border, 0);
                grid.Children.Add(border);

                // Grid zum Haupt-Grid hinzufügen
                mainGrid.Children.Add(grid);

                Debug.WriteLine("Watchlist-Ansicht wurde angezeigt");

                // Aktualisiere die Watchlist beim Anzeigen
                if (DataContext is MainViewModel vm2 && vm2.WatchlistViewModel != null)
                {
                    _ = vm2.WatchlistViewModel.AktualisiereWatchlist();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Öffnen der Watchlist: {ex.Message}");
                MessageBox.Show($"Fehler beim Öffnen der Watchlist: {ex.Message}",
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Hinzufügen der FindMainContentGrid-Methode
        private Grid FindMainContentGrid()
        {
            try
            {
                // Debuggen der visuellen Hierarchie
                var allGrids = FindVisualChildren<Grid>(this).ToList();
                Debug.WriteLine($"Gefundene Grids: {allGrids.Count}");

                foreach (var grid in allGrids)
                {
                    Debug.WriteLine($"Grid Name: {grid.Name}, Children: {grid.Children.Count}");
                }

                // Versuch, das ScrollViewer-Grid zu finden
                var scrollViewer = FindVisualChildren<ScrollViewer>(this)
                    .FirstOrDefault(sv => sv.Name == "MainContentScrollViewer");

                if (scrollViewer != null)
                {
                    Debug.WriteLine("ScrollViewer gefunden!");

                    // Wenn der Content ein Grid ist, gib dieses zurück
                    if (scrollViewer.Content is Grid contentGrid)
                    {
                        Debug.WriteLine("Grid im ScrollViewer gefunden!");
                        return contentGrid;
                    }
                }

                // Suche ein Grid mit dem Namen "MainContentGrid"
                var mainGrid = FindVisualChildren<Grid>(this)
                    .FirstOrDefault(g => g.Name == "MainContentGrid" || g.Name == "MainGrid");

                if (mainGrid != null)
                {
                    Debug.WriteLine("MainContentGrid gefunden!");
                    return mainGrid;
                }

                // Letzte Option: Versuche das erste große Grid zu finden
                var largestGrid = FindVisualChildren<Grid>(this)
                    .OrderByDescending(g => g.Children.Count)
                    .FirstOrDefault();

                if (largestGrid != null)
                {
                    Debug.WriteLine($"Größtes Grid gefunden mit {largestGrid.Children.Count} Elementen!");
                    return largestGrid;
                }

                Debug.WriteLine("Kein passendes Grid gefunden!");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler in FindMainContentGrid: {ex.Message}");
                return null;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MainWindow_Loaded-Event ausgelöst");

            // Im Designer-Modus nichts tun
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            // Nach HandelsUserControl im visuellen Baum suchen
            FindAndCheckHandelsUserControl(this);

            // ScrollViewer-Eigenschaften setzen für besseres Layout
            if (MainContentScrollViewer != null)
            {
                MainContentScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                MainContentScrollViewer.VerticalAlignment = VerticalAlignment.Stretch;
                MainContentScrollViewer.HorizontalAlignment = HorizontalAlignment.Stretch;
                MainContentScrollViewer.MinHeight = 600;
            }

            // MainContentGrid-Eigenschaften setzen
            if (MainContentGrid != null)
            {
                MainContentGrid.VerticalAlignment = VerticalAlignment.Stretch;
                MainContentGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                MainContentGrid.MinHeight = 600;
            }
        }

        private void UserButton_Click(object sender, RoutedEventArgs e)
        {
            // Kontextmenü beim Klick anzeigen
            UserButton.ContextMenu.IsOpen = true;
        }

        private async void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Im Designer-Modus nichts tun
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            // Benutzer abmelden
            var result = MessageBox.Show("Möchten Sie sich wirklich abmelden?",
                "Abmelden", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Debug.WriteLine("Benutzer wird abgemeldet...");

                try
                {
                    // Aktuellen Kontostand in der Datenbank speichern
                    if (DataContext is MainViewModel viewModel &&
                        viewModel.AktuellerBenutzer != null &&
                        App.DbService != null)
                    {
                        // Sicherstellen, dass der aktuelle Kontostand im Benutzer-Objekt gespeichert ist
                        viewModel.AktuellerBenutzer.Kontostand = viewModel.Kontostand;

                        // In der Datenbank aktualisieren
                        await App.DbService.UpdateBenutzerAsync(viewModel.AktuellerBenutzer);
                        Debug.WriteLine("Kontostand vor dem Ausloggen gespeichert");
                    }

                    // AuthService.Logout() aufrufen
                    if (App.AuthService != null)
                    {
                        App.AuthService.Logout();
                    }

                    // Zurück zum Login-Fenster wechseln
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();

                    // Aktuelles Fenster schließen
                    this.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Ausloggen: {ex.Message}");
                    MessageBox.Show($"Ein Fehler ist aufgetreten: {ex.Message}", "Fehler beim Ausloggen",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EinstellungenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Im Designer-Modus nichts tun
                if (DesignerProperties.GetIsInDesignMode(this))
                    return;

                Debug.WriteLine("Einstellungs-Button wurde geklickt");

                // Haupt-Grid für Inhalte finden
                var mainGrid = FindMainContentGrid();
                if (mainGrid == null)
                {
                    Debug.WriteLine("Konnte das Haupt-Grid nicht finden");
                    MessageBox.Show("Fehler beim Öffnen der Einstellungen.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Immer eine neue Instanz des EinstellungenUserControl erstellen,
                // damit es nicht zu Parent-Child-Konflikten kommt
                var einstellungenControl = new Views.EinstellungenUserControl();
                
                // Einstellungs-Ansicht anzeigen
                mainGrid.Children.Clear();

                // Neues Grid für die Einstellungsansicht erstellen
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.VerticalAlignment = VerticalAlignment.Stretch;
                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                grid.MinHeight = 600;

                // Border für die Einstellungsansicht erstellen (ähnlich wie bei anderen Ansichten)
                var border = new Border
                {
                    Style = (Style)FindResource("CardBorderStyle"),
                    Child = einstellungenControl,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    MinHeight = 580
                };

                Grid.SetRow(border, 0);
                Grid.SetColumn(border, 0);
                grid.Children.Add(border);

                // Grid zum Haupt-Grid hinzufügen
                mainGrid.Children.Add(grid);

                Debug.WriteLine("Einstellungsansicht wurde angezeigt");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Öffnen der Einstellungen: {ex.Message}");
                MessageBox.Show($"Fehler beim Öffnen der Einstellungen: {ex.Message}",
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RestoreMainView()
        {
            try
            {
                // Im Designer-Modus nichts tun
                if (DesignerProperties.GetIsInDesignMode(this))
                    return;

                Debug.WriteLine("RestoreMainView aufgerufen");

                // Haupt-Grid für Inhalte finden
                var mainGrid = FindMainContentGrid();
                if (mainGrid == null)
                {
                    Debug.WriteLine("Konnte das Haupt-Grid nicht finden");
                    MessageBox.Show("Fehler beim Öffnen des Dashboards.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Alles löschen und Hauptansicht wiederherstellen
                mainGrid.Children.Clear();

                // Das ursprüngliche Dashboard-Layout wiederherstellen
                if (DataContext is MainViewModel viewModel)
                {
                    // Portfolio-Bereich
                    var portfolioBorder = new Border
                    {
                        Style = (Style)FindResource("CardBorderStyle"),
                        Child = new Views.PortfolioUserControl
                        {
                            DataContext = viewModel.PortfolioViewModel
                        }
                    };
                    Grid.SetRow(portfolioBorder, 0);
                    Grid.SetColumn(portfolioBorder, 0);
                    Grid.SetRowSpan(portfolioBorder, 2);

                    // Marktdaten-Bereich
                    var marktdatenBorder = new Border
                    {
                        Style = (Style)FindResource("CardBorderStyle"),
                        Child = new Views.MarktdatenUserControl
                        {
                            DataContext = viewModel.MarktdatenViewModel
                        }
                    };
                    Grid.SetRow(marktdatenBorder, 0);
                    Grid.SetColumn(marktdatenBorder, 1);
                    Grid.SetRowSpan(marktdatenBorder, 2);

                    // Zu MainContentGrid hinzufügen
                    mainGrid.Children.Add(portfolioBorder);
                    mainGrid.Children.Add(marktdatenBorder);

                    Debug.WriteLine("Dashboard-Ansicht wurde wiederhergestellt");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Wiederherstellen der Hauptansicht: {ex.Message}");
                MessageBox.Show($"Fehler beim Zurückkehren zur Hauptansicht: {ex.Message}",
                             "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Findet alle visuellen Kinder eines bestimmten Typs in der visuellen Hierarchie
        /// </summary>
        /// <typeparam name="T">Der zu suchende Typ</typeparam>
        /// <param name="depObj">Das Ausgangsobjekt, von dem aus gesucht wird</param>
        /// <returns>Eine Aufzählung aller gefundenen Kinder vom Typ T</returns>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null)
                yield break;

            // Anzahl der visuellen Kinder des Objekts
            int childCount = VisualTreeHelper.GetChildrenCount(depObj);

            for (int i = 0; i < childCount; i++)
            {
                // Aktuelles Kind holen
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                // Prüfen, ob das Kind vom gesuchten Typ ist
                if (child != null && child is T typedChild)
                {
                    // Wenn ja, zurückgeben
                    yield return typedChild;
                }

                // Rekursiv in die Kinder des aktuellen Kindes absteigen
                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    // Gefundene Kinder weitergeben
                    yield return childOfChild;
                }
            }
        }

        private void FindAndCheckHandelsUserControl(DependencyObject parent)
        {
            // Im Designer-Modus nichts tun
            if (DesignerProperties.GetIsInDesignMode(parent))
                return;

            Debug.WriteLine($"Suche HandelsUserControl in {parent.GetType().Name}");

            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is Views.HandelsUserControl handelsControl)
                {
                    Debug.WriteLine("HandelsUserControl gefunden!");
                    handelsControl.CheckAndUpdateComboBox();
                    return;
                }

                FindAndCheckHandelsUserControl(child);
            }
        }

        private void PortfolioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Im Designer-Modus nichts tun
                if (DesignerProperties.GetIsInDesignMode(this))
                    return;

                Debug.WriteLine("Portfolio-Button wurde geklickt");

                // Haupt-Grid für Inhalte finden
                var mainGrid = FindMainContentGrid();
                if (mainGrid == null)
                {
                    Debug.WriteLine("Konnte das Haupt-Grid nicht finden");
                    MessageBox.Show("Fehler beim Öffnen des Portfolios.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Portfolio-Ansicht anzeigen
                mainGrid.Children.Clear();

                // Neues Grid für die Portfolio-Ansicht erstellen
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.VerticalAlignment = VerticalAlignment.Stretch;
                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                grid.MinHeight = 600;

                // Border für die Portfolio-Ansicht erstellen
                var border = new Border
                {
                    Style = (Style)FindResource("CardBorderStyle"),
                    Child = new Views.PortfolioView
                    {
                        DataContext = this.DataContext,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        MinHeight = 550
                    },
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    MinHeight = 580
                };

                Grid.SetRow(border, 0);
                Grid.SetColumn(border, 0);
                grid.Children.Add(border);

                // Grid zum Haupt-Grid hinzufügen
                mainGrid.Children.Add(grid);

                Debug.WriteLine("Portfolio-Ansicht wurde angezeigt");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Öffnen des Portfolios: {ex.Message}");
                MessageBox.Show($"Fehler beim Öffnen des Portfolios: {ex.Message}",
                               "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}