using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using HausarbeitVirtuelleBörsenplattform.Models;
using HausarbeitVirtuelleBörsenplattform.ViewModels;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Linq;

namespace HausarbeitVirtuelleBörsenplattform.Views
{
    /// <summary>
    /// Interaktionslogik für MarktdatenUserControl.xaml
    /// </summary>
    public partial class MarktdatenUserControl : UserControl
    {
        private DispatcherTimer _uiUpdateTimer;
        private MarktdatenViewModel _viewModel;

        /// <summary>
        /// Initialisiert eine neue Instanz des MarktdatenUserControl
        /// </summary>
        public MarktdatenUserControl()
        {
            InitializeComponent();

            // Nach dem Laden Daten aktualisieren
            this.Loaded += MarktdatenUserControl_Loaded;

            // Beim Entladen den Timer stoppen
            this.Unloaded += MarktdatenUserControl_Unloaded;

            // Timer für regelmäßige UI-Updates starten
            StartUIUpdateTimer();
        }

        /// <summary>
        /// Startet einen Timer für die regelmäßige UI-Aktualisierung
        /// </summary>
        private void StartUIUpdateTimer()
        {
            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) // Alle 2 Sekunden aktualisieren
            };
            _uiUpdateTimer.Tick += (s, e) => RefreshUI();
            _uiUpdateTimer.Start();

            Debug.WriteLine("UI-Update-Timer gestartet (2 Sekunden Intervall)");
        }

        /// <summary>
        /// Aktualisiert die UI-Elemente
        /// </summary>
        private void RefreshUI()
        {
            try
            {
                // Prüfen, ob das DataGrid existiert und eine Datenquelle hat
                if (AktienDataGrid != null && AktienDataGrid.ItemsSource != null)
                {
                    // DataGrid aktualisieren
                    this.Dispatcher.Invoke(() =>
                    {
                        AktienDataGrid.Items.Refresh();
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der UI-Aktualisierung: {ex.Message}");
            }
        }

        /// <summary>
        /// Event-Handler für das Laden des UserControl
        /// </summary>
        private void MarktdatenUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MarktdatenUserControl wurde geladen");

            // Verzögerte Aktualisierung, um UI-Thread nicht zu blockieren
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // ViewModel aus dem DataContext holen
                    if (DataContext is MarktdatenViewModel viewModel)
                    {
                        _viewModel = viewModel;
                        Debug.WriteLine("MarktdatenViewModel gefunden, richte CollectionChanged-Event ein");

                        // Auf Änderungen in der AktienListe reagieren
                        if (viewModel.AktienListe is INotifyCollectionChanged notifyCollection)
                        {
                            notifyCollection.CollectionChanged += (s, args) =>
                            {
                                Debug.WriteLine("AktienListe hat sich geändert, aktualisiere UI");
                                RefreshUI();
                            };
                        }

                        // Prüfen, ob wir Aktien aus dem Portfolio haben
                        var mainViewModel = ((App)Application.Current).MainWindow?.DataContext as MainViewModel;
                        if (mainViewModel?.PortfolioViewModel?.PortfolioEintraege != null &&
                            mainViewModel.PortfolioViewModel.PortfolioEintraege.Any())
                        {
                            Debug.WriteLine("Portfolio gefunden, prüfe nach Portfolio-Aktien in der Marktdatenliste");

                            // Prüfen, ob alle Portfolio-Aktien in der Marktdatenliste vorhanden sind
                            var portfolioSymbole = mainViewModel.PortfolioViewModel.PortfolioEintraege
                                .Select(p => p.AktienSymbol.ToUpper())
                                .ToHashSet();

                            var marktdatenSymbole = viewModel.AktienListe
                                .Select(a => a.AktienSymbol.ToUpper())
                                .ToHashSet();

                            var fehlendeSymbole = portfolioSymbole.Except(marktdatenSymbole).ToList();

                            if (fehlendeSymbole.Any())
                            {
                                Debug.WriteLine($"Fehlende Portfolio-Aktien in Marktdaten: {string.Join(", ", fehlendeSymbole)}");

                                // Aktualisieren, um Portfolio-Aktien zu laden
                                if (viewModel.AktualisierenCommand.CanExecute(null))
                                {
                                    Debug.WriteLine("Führe AktualisierenCommand aus, um Portfolio-Aktien zu laden");
                                    viewModel.AktualisierenCommand.Execute(null);
                                }
                            }
                        }

                        // Verzögerung, damit UI-Elemente zuerst geladen werden
                        Dispatcher.BeginInvoke(new Action(async () =>
                        {
                            // Minimale Wartezeit für das Laden des UIs
                            await Task.Delay(100);

                            // Wenn Aktien vorhanden sind, erste Aktie auswählen
                            if (viewModel.AktienListe != null && viewModel.AktienListe.Count > 0 && viewModel.AusgewählteAktie == null)
                            {
                                viewModel.AusgewählteAktie = viewModel.AktienListe.FirstOrDefault();
                                Debug.WriteLine($"Erste Aktie automatisch ausgewählt: {viewModel.AusgewählteAktie?.AktienSymbol}");
                            }
                        }), DispatcherPriority.Background);
                    }
                    else
                    {
                        Debug.WriteLine("Kein MarktdatenViewModel im DataContext gefunden");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler bei der Initialisierung des MarktdatenUserControl: {ex.Message}");
                }
            }), DispatcherPriority.Background);
        }

        /// <summary>
        /// Event-Handler für das Entladen des UserControl
        /// </summary>
        private void MarktdatenUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Timer stoppen, wenn Control entladen wird
            if (_uiUpdateTimer != null)
            {
                _uiUpdateTimer.Stop();
                _uiUpdateTimer = null;
                Debug.WriteLine("UI-Update-Timer gestoppt");
            }
        }

        /// <summary>
        /// Event-Handler für das Laden der TextBlocks in der Änderung-Spalte
        /// </summary>
        private void ÄnderungTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is Aktie aktie)
            {
                try
                {
                    // Farbe basierend auf dem Wert setzen
                    if (aktie.Änderung > 0)
                    {
                        textBlock.Foreground = Brushes.Green;
                    }
                    else if (aktie.Änderung < 0)
                    {
                        textBlock.Foreground = Brushes.Red;
                    }
                    else
                    {
                        textBlock.Foreground = Brushes.Black;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Setzen der Textfarbe: {ex.Message}");
                    textBlock.Foreground = Brushes.Black; // Standardfarbe im Fehlerfall
                }
            }
        }

        /// <summary>
        /// Event-Handler für das Laden der TextBlocks in der Änderung-Prozent-Spalte
        /// </summary>
        private void ÄnderungProzentTextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                try
                {
                    // Erkennen, welche Art von Objekt das DataContext ist (Aktie oder AktienKursHistorie)
                    if (textBlock.DataContext is Aktie aktie)
                    {
                        // Farbe basierend auf dem Wert setzen
                        if (aktie.ÄnderungProzent > 0)
                        {
                            textBlock.Foreground = Brushes.Green;
                        }
                        else if (aktie.ÄnderungProzent < 0)
                        {
                            textBlock.Foreground = Brushes.Red;
                        }
                        else
                        {
                            textBlock.Foreground = Brushes.Black;
                        }
                    }
                    else if (textBlock.DataContext is AktienKursHistorie historie)
                    {
                        // Farbe basierend auf dem Wert setzen
                        if (historie.ÄnderungProzent > 0)
                        {
                            textBlock.Foreground = Brushes.Green;
                        }
                        else if (historie.ÄnderungProzent < 0)
                        {
                            textBlock.Foreground = Brushes.Red;
                        }
                        else
                        {
                            textBlock.Foreground = Brushes.Black;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Setzen der Textfarbe: {ex.Message}");
                    textBlock.Foreground = Brushes.Black; // Standardfarbe im Fehlerfall
                }
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn eine Aktie in der DataGrid ausgewählt wird
        /// </summary>
        private void AktienDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel != null && _viewModel.AusgewählteAktie != null)
            {
                Debug.WriteLine($"Aktie ausgewählt: {_viewModel.AusgewählteAktie.AktienSymbol}");
            }
        }

        // Methode HistorieExpander_Expanded entfernt
    }
}