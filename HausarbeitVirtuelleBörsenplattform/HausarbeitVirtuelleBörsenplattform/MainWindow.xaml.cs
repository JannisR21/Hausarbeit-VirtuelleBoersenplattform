using HausarbeitVirtuelleBörsenplattform.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;

namespace HausarbeitVirtuelleBörsenplattform
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Hinzufügen des Feldes für das Einstellungen-UserControl
        private Views.EinstellungenUserControl _einstellungenControl;

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

                // Event-Handler für UserButton hinzufügen
                UserButton.Click += UserButton_Click;

                // Event-Handler für Logout-MenuItem
                ((MenuItem)UserButton.ContextMenu.Items[3]).Click += LogoutMenuItem_Click;

                // Event-Handler für Einstellungen-Navigation
                var einstellungenButton = FindName("EinstellungenButton") as Button;
                if (einstellungenButton != null)
                {
                    einstellungenButton.Click += EinstellungenButton_Click;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler im MainWindow-Konstruktor: {ex.Message}");
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

            // Nach HandelsUserControl im visuellen Baum suchen
            FindAndCheckHandelsUserControl(this);
        }

        private void UserButton_Click(object sender, RoutedEventArgs e)
        {
            // Kontextmenü beim Klick anzeigen
            UserButton.ContextMenu.IsOpen = true;
        }

        private async void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
        {
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
                Debug.WriteLine("Einstellungs-Button wurde geklickt");

                // Haupt-Grid für Inhalte finden
                var mainGrid = FindMainContentGrid();
                if (mainGrid == null)
                {
                    Debug.WriteLine("Konnte das Haupt-Grid nicht finden");
                    MessageBox.Show("Fehler beim Öffnen der Einstellungen.", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Einstellungs-UserControl erstellen, falls noch nicht vorhanden
                if (_einstellungenControl == null)
                {
                    _einstellungenControl = new Views.EinstellungenUserControl();
                }

                // Einstellungs-Ansicht anzeigen
                mainGrid.Children.Clear();

                // Neues Grid für die Einstellungsansicht erstellen
                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Border für die Einstellungsansicht erstellen (ähnlich wie bei anderen Ansichten)
                var border = new Border
                {
                    Style = (Style)FindResource("CardBorderStyle"),
                    Child = _einstellungenControl
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
                Debug.WriteLine("RestoreMainView aufgerufen");

                // Hauptanwendung holen
                var app = Application.Current;

                // Neues MainWindow erstellen
                var newWindow = new MainWindow();

                // Das neue Fenster anzeigen
                newWindow.Show();

                // Das aktuelle Fenster schließen
                this.Close();

                Debug.WriteLine("Hauptansicht wurde wiederhergestellt");
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
    }
}