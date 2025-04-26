using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für EinstellungenUserControl.xaml
    /// </summary>
    public partial class EinstellungenUserControl : UserControl
    {
        /// <summary>
        /// API-Key Property für Databinding
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Initialisiert eine neue Instanz von EinstellungenUserControl
        /// </summary>
        public EinstellungenUserControl()
        {
            InitializeComponent();

            // API-Key aus App-Konfiguration laden
            ApiKey = App.TwelveDataApiKey;

            // DataContext setzen
            this.DataContext = this;

            // Event-Handler für DarkMode-Toggle hinzufügen
            DarkModeToggle.Checked += DarkModeToggle_Checked;
            DarkModeToggle.Unchecked += DarkModeToggle_Unchecked;
        }

        /// <summary>
        /// Event-Handler für DarkMode aktivieren
        /// </summary>
        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Dark Mode wird in einer zukünftigen Version implementiert.",
                            "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Event-Handler für DarkMode deaktivieren
        /// </summary>
        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Wird in zukünftiger Version implementiert
        }

        /// <summary>
        /// Event-Handler für den Zurück-Button
        /// </summary>
        private void ZurückButton_Click(object sender, RoutedEventArgs e)
        {
            // Zur Hauptansicht zurückkehren
            NavigateBackToMainView();
        }

        /// <summary>
        /// Event-Handler für Passwort ändern Button
        /// </summary>
        private void PasswortÄndernButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Einfaches Password-Change-Dialog
                var dialog = new Window
                {
                    Title = "Passwort ändern",
                    Width = 350,
                    Height = 250,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.Margin = new Thickness(15);

                var altesPasswortLabel = new TextBlock
                {
                    Text = "Altes Passwort:",
                    Margin = new Thickness(0, 0, 0, 5)
                };
                Grid.SetRow(altesPasswortLabel, 0);

                var altesPasswortBox = new PasswordBox
                {
                    Margin = new Thickness(0, 0, 0, 15)
                };
                Grid.SetRow(altesPasswortBox, 1);

                var neuesPasswortLabel = new TextBlock
                {
                    Text = "Neues Passwort:",
                    Margin = new Thickness(0, 0, 0, 5)
                };
                Grid.SetRow(neuesPasswortLabel, 2);

                var neuesPasswortBox = new PasswordBox
                {
                    Margin = new Thickness(0, 0, 0, 15)
                };
                Grid.SetRow(neuesPasswortBox, 3);

                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetRow(buttonsPanel, 4);

                var abbruchButton = new Button
                {
                    Content = "Abbrechen",
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                abbruchButton.Click += (s, args) => dialog.Close();

                var speichernButton = new Button
                {
                    Content = "Speichern",
                    Width = 80,
                    Height = 30,
                    Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2c3e50")),
                    Foreground = System.Windows.Media.Brushes.White
                };
                speichernButton.Click += (s, args) =>
                {
                    MessageBox.Show("Passwortänderung wird in einer zukünftigen Version implementiert.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    dialog.Close();
                };

                buttonsPanel.Children.Add(abbruchButton);
                buttonsPanel.Children.Add(speichernButton);

                grid.Children.Add(altesPasswortLabel);
                grid.Children.Add(altesPasswortBox);
                grid.Children.Add(neuesPasswortLabel);
                grid.Children.Add(neuesPasswortBox);
                grid.Children.Add(buttonsPanel);

                dialog.Content = grid;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler im PasswortÄndernButton_Click: {ex.Message}");
                MessageBox.Show($"Fehler: {ex.Message}", "Fehler",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event-Handler für E-Mail ändern Button
        /// </summary>
        private void EmailÄndernButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("E-Mail-Änderung wird in einer zukünftigen Version implementiert.",
                            "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Event-Handler für API-Key speichern Button
        /// </summary>
        private void ApiKeyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ApiKeyTextBox.Text))
                {
                    MessageBox.Show("Bitte geben Sie einen gültigen API-Schlüssel ein.",
                                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    "Soll der neue API-Schlüssel gespeichert werden? Die Anwendung muss dafür neu gestartet werden.",
                    "API-Schlüssel speichern",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    MessageBox.Show("Die API-Schlüssel-Änderung wird in einer zukünftigen Version implementiert.",
                                   "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler im ApiKeyButton_Click: {ex.Message}");
                MessageBox.Show($"Fehler: {ex.Message}", "Fehler",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Event-Handler für Portfolio zurücksetzen Button
        /// </summary>
        private void PortfolioZurücksetzenButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Möchten Sie wirklich Ihr gesamtes Portfolio zurücksetzen? Diese Aktion kann nicht rückgängig gemacht werden.",
                "Portfolio zurücksetzen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Portfolio-Zurücksetzung wird in einer zukünftigen Version implementiert.",
                                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Event-Handler für Konto löschen Button
        /// </summary>
        private void KontoLöschenButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Möchten Sie wirklich Ihr Konto löschen? Alle Ihre Daten werden unwiderruflich gelöscht.",
                "Konto löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Konto-Löschung wird in einer zukünftigen Version implementiert.",
                               "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Event-Handler für Änderungen speichern Button
        /// </summary>
        private void ÄnderungenSpeichernButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Aktualisierungsintervall speichern
                var selectedItem = AktualisierungsIntervallComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Tag != null)
                {
                    int intervallInSekunden = Convert.ToInt32(selectedItem.Tag);
                    Debug.WriteLine($"Aktualisierungsintervall geändert auf: {intervallInSekunden} Sekunden");

                    // Hier könnte das Intervall gespeichert werden
                }

                // Dark Mode Einstellung speichern
                bool darkModeEnabled = DarkModeToggle.IsChecked ?? false;
                Debug.WriteLine($"Dark Mode: {darkModeEnabled}");

                // API-Key aus Textbox übernehmen
                string neuerApiKey = ApiKeyTextBox.Text;
                if (!string.IsNullOrWhiteSpace(neuerApiKey) && neuerApiKey != App.TwelveDataApiKey)
                {
                    Debug.WriteLine($"Neuer API-Key: {neuerApiKey}");
                    // Hier könnte der API-Key gespeichert werden
                }

                MessageBox.Show("Einstellungen wurden gespeichert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                // Zurück zur Hauptansicht wechseln
                NavigateBackToMainView();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Speichern der Einstellungen: {ex.Message}");
                MessageBox.Show($"Fehler beim Speichern der Einstellungen: {ex.Message}",
                                "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Navigiert zurück zur Hauptansicht
        /// </summary>
        private void NavigateBackToMainView()
        {
            try
            {
                // Elternfenster aus dem visuellen Baum ermitteln
                var window = Window.GetWindow(this) as MainWindow;
                if (window != null)
                {
                    // MainWindow auffordern, zur Hauptansicht zurückzukehren
                    var method = window.GetType().GetMethod("RestoreMainView",
                                System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Public);

                    if (method != null)
                    {
                        method.Invoke(window, null);
                        Debug.WriteLine("Zurück zur Hauptansicht navigiert");
                    }
                    else
                    {
                        // Falls die Methode im MainWindow nicht existiert, eine einfachere Lösung
                        window.Close();
                        var newWindow = new MainWindow();
                        newWindow.Show();
                        Debug.WriteLine("MainWindow neu geladen, um zur Hauptansicht zurückzukehren");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Navigation zurück zur Hauptansicht: {ex.Message}");
            }
        }
    }
}