using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Models;
using HausarbeitVirtuelleBörsenplattform.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    /// <summary>
    /// ViewModel für die Watchlist-Funktionalität
    /// </summary>
    public class WatchlistViewModel : ObservableObject
    {
        #region Private Felder
        private ObservableCollection<WatchlistEintrag> _watchlistEintraege;
        private readonly DatabaseService _databaseService;
        private readonly int _benutzerId;
        private DateTime _letzteAktualisierung;
        private bool _isUpdating = false;
        private string _fehlerText;
        private bool _hatFehler;
        private readonly MainViewModel _mainViewModel;
        #endregion

        #region Public Properties
        /// <summary>
        /// Liste aller Aktien in der Watchlist
        /// </summary>
        public ObservableCollection<WatchlistEintrag> WatchlistEintraege
        {
            get => _watchlistEintraege;
            set => SetProperty(ref _watchlistEintraege, value);
        }

        /// <summary>
        /// Zeitpunkt der letzten Aktualisierung
        /// </summary>
        public DateTime LetzteAktualisierung
        {
            get => _letzteAktualisierung;
            set => SetProperty(ref _letzteAktualisierung, value);
        }

        /// <summary>
        /// Fehlertext für Aktualisierungsprobleme
        /// </summary>
        public string FehlerText
        {
            get => _fehlerText;
            set => SetProperty(ref _fehlerText, value);
        }

        /// <summary>
        /// Gibt an, ob bei der Aktualisierung ein Fehler aufgetreten ist
        /// </summary>
        public bool HatFehler
        {
            get => _hatFehler;
            set => SetProperty(ref _hatFehler, value);
        }

        /// <summary>
        /// Command zum Hinzufügen einer Aktie zur Watchlist
        /// </summary>
        public IRelayCommand<Aktie> AktieHinzufügenCommand { get; }

        /// <summary>
        /// Command zum Entfernen einer Aktie aus der Watchlist
        /// </summary>
        public IRelayCommand<WatchlistEintrag> AktieEntfernenCommand { get; }

        /// <summary>
        /// Command zum manuellen Aktualisieren der Watchlist
        /// </summary>
        public IRelayCommand AktualisierenCommand { get; }
        #endregion

        #region Konstruktor
        /// <summary>
        /// Initialisiert eine neue Instanz des WatchlistViewModel
        /// </summary>
        /// <param name="databaseService">Der zu verwendende DatabaseService</param>
        /// <param name="benutzerId">ID des aktuellen Benutzers</param>
        /// <param name="mainViewModel">Das MainViewModel für den Zugriff auf andere ViewModels</param>
        public WatchlistViewModel(DatabaseService databaseService, int benutzerId, MainViewModel mainViewModel = null)
        {
            _databaseService = databaseService;
            _benutzerId = benutzerId;
            _mainViewModel = mainViewModel;
            WatchlistEintraege = new ObservableCollection<WatchlistEintrag>();

            // Commands initialisieren
            AktieHinzufügenCommand = new RelayCommand<Aktie>(AktieHinzufügen);
            AktieEntfernenCommand = new RelayCommand<WatchlistEintrag>(AktieEntfernen);
            AktualisierenCommand = new RelayCommand(async () => await AktualisiereWatchlist());

            // Daten laden
            _ = LoadWatchlistAsync();
        }
        #endregion

        #region Methoden
        /// <summary>
        /// Lädt die Watchlist-Daten des Benutzers aus der Datenbank
        /// </summary>
        public async Task LoadWatchlistAsync()
        {
            if (_databaseService == null || _benutzerId <= 0)
            {
                Debug.WriteLine("Kein Datenbankzugriff oder Benutzer-ID vorhanden.");
                MessageBox.Show("Watchlist-Daten konnten nicht geladen werden. Bitte melden Sie sich erneut an.",
                    "Fehler beim Laden der Watchlist", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var watchlistData = await _databaseService.GetWatchlistByBenutzerIdAsync(_benutzerId);

                // UI-Thread-Aktualisierung
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WatchlistEintraege.Clear();
                    foreach (var item in watchlistData)
                    {
                        WatchlistEintraege.Add(item);
                    }

                    LetzteAktualisierung = DateTime.Now;
                });

                Debug.WriteLine($"Watchlist-Daten für Benutzer {_benutzerId} geladen: {watchlistData.Count} Einträge");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Watchlist-Daten: {ex.Message}");
                HatFehler = true;
                FehlerText = $"Fehler beim Laden der Watchlist-Daten: {ex.Message}";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Fehler beim Laden der Watchlist-Daten: {ex.Message}",
                        "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// Fügt eine Aktie zur Watchlist hinzu
        /// </summary>
        /// <param name="aktie">Die hinzuzufügende Aktie</param>
        private void AktieHinzufügen(Aktie aktie)
        {
            if (aktie == null || _databaseService == null || _benutzerId <= 0)
            {
                return;
            }

            // Prüfen, ob die Aktie bereits in der Watchlist ist
            if (WatchlistEintraege.Any(w => w.AktienID == aktie.AktienID))
            {
                MessageBox.Show("Diese Aktie ist bereits in Ihrer Watchlist enthalten.",
                    "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // Neuen Watchlist-Eintrag erstellen
                var neuerEintrag = new WatchlistEintrag
                {
                    BenutzerID = _benutzerId,
                    AktienID = aktie.AktienID,
                    AktienSymbol = aktie.AktienSymbol,
                    AktienName = aktie.AktienName,
                    HinzugefuegtAm = DateTime.Now,
                    KursBeimHinzufuegen = aktie.AktuellerPreis,
                    AktuellerKurs = aktie.AktuellerPreis,
                    LetzteAktualisierung = DateTime.Now
                };

                // Zur Datenbank hinzufügen
                _ = _databaseService.AddOrUpdateWatchlistEintragAsync(neuerEintrag);

                // Zur lokalen Collection hinzufügen
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WatchlistEintraege.Add(neuerEintrag);
                });

                Debug.WriteLine($"Aktie {aktie.AktienSymbol} zur Watchlist hinzugefügt");
                MessageBox.Show($"Die Aktie {aktie.AktienName} wurde zu Ihrer Watchlist hinzugefügt.",
                    "Aktie hinzugefügt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Hinzufügen der Aktie zur Watchlist: {ex.Message}");
                MessageBox.Show($"Fehler beim Hinzufügen der Aktie zur Watchlist: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Entfernt eine Aktie aus der Watchlist
        /// </summary>
        /// <param name="eintrag">Der zu entfernende Watchlist-Eintrag</param>
        private void AktieEntfernen(WatchlistEintrag eintrag)
        {
            if (eintrag == null || _databaseService == null || _benutzerId <= 0)
            {
                return;
            }

            try
            {
                // Aus der Datenbank entfernen
                _ = _databaseService.RemoveWatchlistEintragAsync(_benutzerId, eintrag.AktienID);

                // Aus der lokalen Collection entfernen
                Application.Current.Dispatcher.Invoke(() =>
                {
                    WatchlistEintraege.Remove(eintrag);
                });

                Debug.WriteLine($"Aktie {eintrag.AktienSymbol} aus der Watchlist entfernt");
                MessageBox.Show($"Die Aktie {eintrag.AktienName} wurde aus Ihrer Watchlist entfernt.",
                    "Aktie entfernt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Entfernen der Aktie aus der Watchlist: {ex.Message}");
                MessageBox.Show($"Fehler beim Entfernen der Aktie aus der Watchlist: {ex.Message}",
                    "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Aktualisiert die Kurse der Aktien in der Watchlist mit aktuellen Marktdaten
        /// </summary>
        public async Task AktualisiereWatchlist()
        {
            if (_isUpdating || _mainViewModel?.MarktdatenViewModel == null)
                return;

            _isUpdating = true;
            HatFehler = false;
            FehlerText = string.Empty;

            try
            {
                // Prüfen, ob die Börse geöffnet ist
                bool istBoerseGeoeffnet = App.TwelveDataService?.IstBoerseGeoeffnet() ?? false;
                if (!istBoerseGeoeffnet)
                {
                    Debug.WriteLine("Börse ist geschlossen, Watchlist-Kurse werden nicht aktualisiert");
                    FehlerText = "Die Börse ist derzeit geschlossen. Es werden die letzten bekannten Kurse angezeigt.";
                    HatFehler = true;
                    _isUpdating = false;
                    return;
                }

                var marktdaten = _mainViewModel.MarktdatenViewModel.AktienListe;
                if (marktdaten == null || !marktdaten.Any())
                {
                    Debug.WriteLine("Keine Marktdaten verfügbar");
                    FehlerText = "Keine Marktdaten verfügbar. Die Watchlist kann nicht aktualisiert werden.";
                    HatFehler = true;
                    _isUpdating = false;
                    return;
                }

                // Liste der Symbol-IDs erstellen, die wir aktualisieren müssen
                var watchlistSymbole = WatchlistEintraege.Select(w => w.AktienSymbol).ToList();
                var fehlendeSymbole = new List<string>();

                // Aktualisierung der Kurse
                bool wurdeAktualisiert = false;

                foreach (var eintrag in WatchlistEintraege)
                {
                    var aktienInfo = marktdaten.FirstOrDefault(a =>
                        a.AktienSymbol.Equals(eintrag.AktienSymbol, StringComparison.OrdinalIgnoreCase));

                    if (aktienInfo != null && aktienInfo.AktuellerPreis > 0)
                    {
                        decimal alterKurs = eintrag.AktuellerKurs;
                        eintrag.AktuellerKurs = aktienInfo.AktuellerPreis;
                        eintrag.LetzteAktualisierung = DateTime.Now;

                        Debug.WriteLine($"Watchlist-Eintrag {eintrag.AktienSymbol} aktualisiert: {alterKurs:N2}€ -> {aktienInfo.AktuellerPreis:N2}€");
                        wurdeAktualisiert = true;
                    }
                    else
                    {
                        fehlendeSymbole.Add(eintrag.AktienSymbol);
                        Debug.WriteLine($"Keine aktuellen Kursdaten für {eintrag.AktienSymbol} gefunden");
                    }
                }

                if (wurdeAktualisiert)
                {
                    // Änderungen zur Datenbank synchronisieren
                    var kurse = new Dictionary<int, decimal>();
                    foreach (var eintrag in WatchlistEintraege)
                    {
                        kurse[eintrag.AktienID] = eintrag.AktuellerKurs;
                    }

                    await _databaseService.UpdateWatchlistKurseAsync(_benutzerId, kurse);
                    LetzteAktualisierung = DateTime.Now;
                }

                // Warnung anzeigen, wenn nicht alle Aktien aktualisiert werden konnten
                if (fehlendeSymbole.Count > 0)
                {
                    string symbolListe = string.Join(", ", fehlendeSymbole);
                    Debug.WriteLine($"Folgende Aktien konnten nicht aktualisiert werden: {symbolListe}");

                    if (wurdeAktualisiert)
                    {
                        // Wenn teilweise aktualisiert wurde
                        FehlerText = $"Nicht alle Aktien konnten aktualisiert werden. Fehlend: {symbolListe}";
                    }
                    else
                    {
                        // Wenn gar nichts aktualisiert wurde
                        HatFehler = true;
                        FehlerText = $"Keine Watchlist-Positionen konnten aktualisiert werden. Prüfen Sie die Twelve Data API oder Ihre Internetverbindung.";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Aktualisierung der Watchlist: {ex.Message}");
                HatFehler = true;
                FehlerText = $"Fehler bei der Aktualisierung der Watchlist: {ex.Message}";
            }
            finally
            {
                _isUpdating = false;
            }
        }
        #endregion
    }
}