using CommunityToolkit.Mvvm.ComponentModel;
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
    /// ViewModel für den Portfolio-Bereich der Anwendung
    /// </summary>
    public class PortfolioViewModel : ObservableObject
    {
        #region Private Felder

        private ObservableCollection<PortfolioEintrag> _portfolioEintraege;
        private decimal _gesamtwert;
        private decimal _gesamtGewinnVerlust;
        private readonly DatabaseService _databaseService;
        private readonly int _benutzerId;
        private DateTime _letzteAktualisierung;
        private bool _isUpdating = false;
        private string _fehlerText;
        private bool _hatFehler;

        #endregion

        #region Public Properties

        /// <summary>
        /// Liste aller Aktien im Portfolio
        /// </summary>
        public ObservableCollection<PortfolioEintrag> PortfolioEintraege
        {
            get => _portfolioEintraege;
            set => SetProperty(ref _portfolioEintraege, value);
        }

        /// <summary>
        /// Gesamtwert aller Positionen im Portfolio
        /// </summary>
        public decimal Gesamtwert
        {
            get => _gesamtwert;
            set => SetProperty(ref _gesamtwert, value);
        }

        /// <summary>
        /// Gesamter Gewinn oder Verlust im Portfolio
        /// </summary>
        public decimal GesamtGewinnVerlust
        {
            get => _gesamtGewinnVerlust;
            set => SetProperty(ref _gesamtGewinnVerlust, value);
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

        #endregion

        #region Konstruktor

        /// <summary>
        /// Initialisiert eine neue Instanz des PortfolioViewModel
        /// </summary>
        public PortfolioViewModel()
        {
            // Standard-Konstruktor für Designer-Unterstützung
            PortfolioEintraege = new ObservableCollection<PortfolioEintrag>();
            // Keine Beispieldaten mehr, leeres Portfolio wird angezeigt
        }

        /// <summary>
        /// Initialisiert eine neue Instanz des PortfolioViewModel mit Datenbankzugriff
        /// </summary>
        /// <param name="databaseService">Der zu verwendende DatabaseService</param>
        /// <param name="benutzerId">ID des aktuellen Benutzers</param>
        public PortfolioViewModel(DatabaseService databaseService, int benutzerId)
        {
            _databaseService = databaseService;
            _benutzerId = benutzerId;
            PortfolioEintraege = new ObservableCollection<PortfolioEintrag>();

            // Daten werden asynchron geladen in LoadPortfolioDataAsync()
        }

        #endregion

        #region Methoden für Datenzugriff

        /// <summary>
        /// Lädt die Portfolio-Daten des Benutzers aus der Datenbank
        /// </summary>
        public async Task LoadPortfolioDataAsync()
        {
            if (_databaseService == null || _benutzerId <= 0)
            {
                Debug.WriteLine("Kein Datenbankzugriff oder Benutzer-ID vorhanden.");
                MessageBox.Show("Portfolio-Daten konnten nicht geladen werden. Bitte melden Sie sich erneut an.",
                    "Fehler beim Laden des Portfolios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var portfolioData = await _databaseService.GetPortfolioByBenutzerIdAsync(_benutzerId);

                // UI-Thread-Aktualisierung
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PortfolioEintraege.Clear();
                    foreach (var item in portfolioData)
                    {
                        PortfolioEintraege.Add(item);
                    }

                    // Gesamtwerte berechnen
                    BerechneGesamtwerte();
                    LetzteAktualisierung = DateTime.Now;
                });

                Debug.WriteLine($"Portfolio-Daten für Benutzer {_benutzerId} geladen: {portfolioData.Count} Einträge");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Portfolio-Daten: {ex.Message}");
                HatFehler = true;
                FehlerText = $"Fehler beim Laden der Portfolio-Daten: {ex.Message}";

                Application.Current.Dispatcher.Invoke(() => {
                    MessageBox.Show($"Fehler beim Laden der Portfolio-Daten: {ex.Message}",
                        "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        #endregion

        #region Public Methoden

        /// <summary>
        /// Berechnet den Gesamtwert und Gesamtgewinn/-verlust des Portfolios
        /// </summary>
        public void BerechneGesamtwerte()
        {
            // Gesamtwert und Gesamtgewinn/Verlust mit mehr Präzision berechnen
            var portfolioEintraege = PortfolioEintraege.ToList();

            Gesamtwert = portfolioEintraege.Sum(pe => pe.Wert);
            GesamtGewinnVerlust = portfolioEintraege.Sum(pe => pe.GewinnVerlust);

            Debug.WriteLine($"Portfolio-Gesamtwerte neu berechnet:");
            Debug.WriteLine($"Gesamtwert: {Gesamtwert:N2}€");
            Debug.WriteLine($"Gesamtgewinn/-verlust: {GesamtGewinnVerlust:N2}€");

            // Zur Sicherheit PropertyChanged-Events auslösen
            OnPropertyChanged(nameof(Gesamtwert));
            OnPropertyChanged(nameof(GesamtGewinnVerlust));
        }

        /// <summary>
        /// Aktualisiert die Kurse der Portfolio-Einträge basierend auf den aktuellen Marktdaten
        /// </summary>
        /// <param name="aktienListe">Liste aller Aktien mit aktuellen Kursinformationen</param>
        public void AktualisiereKurseMitMarktdaten(IEnumerable<Aktie> aktienListe)
        {
            if (_isUpdating) return;

            try
            {
                _isUpdating = true;
                HatFehler = false;
                FehlerText = string.Empty;

                // Prüfen, ob die Börse geöffnet ist
                bool istBoerseGeoeffnet = App.TwelveDataService?.IstBoerseGeoeffnet() ?? false;
                if (!istBoerseGeoeffnet)
                {
                    Debug.WriteLine("Börse ist geschlossen, Portfolio-Kurse werden nicht aktualisiert");
                    return; // Keine Aktualisierung bei geschlossener Börse
                }

                if (aktienListe == null || !aktienListe.Any())
                {
                    Debug.WriteLine("Keine Marktdaten zum Aktualisieren des Portfolios vorhanden");
                    HatFehler = true;
                    FehlerText = "Keine Marktdaten verfügbar. Das Portfolio kann nicht aktualisiert werden.";
                    return;
                }

                Debug.WriteLine("Aktualisiere Portfolio-Kurse mit Marktdaten");
                bool wurdeAktualisiert = false;
                var missingSymbols = new List<string>();

                foreach (var eintrag in PortfolioEintraege)
                {
                    var aktienInfo = aktienListe.FirstOrDefault(a =>
                        a.AktienSymbol.Equals(eintrag.AktienSymbol, StringComparison.OrdinalIgnoreCase));

                    if (aktienInfo != null && aktienInfo.AktuellerPreis > 0)
                    {
                        decimal alterKurs = eintrag.AktuellerKurs;
                        eintrag.AktuellerKurs = aktienInfo.AktuellerPreis;
                        eintrag.LetzteAktualisierung = DateTime.Now;

                        Debug.WriteLine($"Portfolio-Eintrag {eintrag.AktienSymbol} aktualisiert: {alterKurs:N2}€ -> {aktienInfo.AktuellerPreis:N2}€");

                        // Explizit die Properties triggern
                        eintrag.BerechneWertUndGewinnVerlust();

                        wurdeAktualisiert = true;
                    }
                    else
                    {
                        missingSymbols.Add(eintrag.AktienSymbol);
                        Debug.WriteLine($"Keine aktuellen Kursdaten für {eintrag.AktienSymbol} gefunden");
                    }
                }

                if (wurdeAktualisiert)
                {
                    // Gesamtwerte neu berechnen, wenn mindestens ein Kurs aktualisiert wurde
                    BerechneGesamtwerte();
                    LetzteAktualisierung = DateTime.Now;

                    // Benachrichtigung über PortfolioEintraege auslösen
                    OnPropertyChanged(nameof(PortfolioEintraege));

                    // Änderungen zur Datenbank synchronisieren, wenn möglich
                    _ = SynchronisierenAsync();
                }

                // Warnung anzeigen, wenn nicht alle Aktien aktualisiert werden konnten
                if (missingSymbols.Count > 0)
                {
                    string symbolListe = string.Join(", ", missingSymbols);
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
                        FehlerText = $"Keine Portfolio-Positionen konnten aktualisiert werden. Prüfen Sie die Twelve Data API oder Ihre Internetverbindung.";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Aktualisierung des Portfolios: {ex.Message}");
                HatFehler = true;
                FehlerText = $"Fehler bei der Aktualisierung des Portfolios: {ex.Message}";
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Speichert die Änderungen im Portfolio in der Datenbank
        /// </summary>
        private async Task SynchronisierenAsync()
        {
            if (_databaseService == null || _benutzerId <= 0)
                return;

            try
            {
                // Portfolio-Änderungen speichern
                foreach (var eintrag in PortfolioEintraege)
                {
                    await _databaseService.AddOrUpdatePortfolioEintragAsync(eintrag);
                }

                Debug.WriteLine("Portfolio-Daten mit der Datenbank synchronisiert");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Fehler beim Synchronisieren des Portfolios: {ex.Message}");
                HatFehler = true;
                FehlerText = $"Fehler beim Speichern der Portfolio-Daten: {ex.Message}";
            }
        }

        /// <summary>
        /// Fügt eine neue Aktie zum Portfolio hinzu oder aktualisiert eine vorhandene Position
        /// </summary>
        /// <param name="aktienID">ID der Aktie</param>
        /// <param name="symbol">Symbol/Ticker der Aktie</param>
        /// <param name="name">Name der Aktie</param>
        /// <param name="anzahl">Anzahl der gekauften Aktien</param>
        /// <param name="kaufkurs">Kaufkurs der Aktien</param>
        /// <param name="aktuellerKurs">Aktueller Kurs der Aktie</param>
        public void FügeAktieHinzu(int aktienID, string symbol, string name, int anzahl, decimal kaufkurs, decimal aktuellerKurs)
        {
            // Prüfen, ob die Aktie bereits im Portfolio ist
            var existingEntry = PortfolioEintraege.FirstOrDefault(pe =>
                pe.AktienID == aktienID ||
                pe.AktienSymbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

            if (existingEntry != null)
            {
                // Berechnung des neuen Durchschnittskaufpreises
                decimal gesamtAnzahl = existingEntry.Anzahl + anzahl;
                decimal gesamtEinstandswert =
                    (existingEntry.Anzahl * existingEntry.EinstandsPreis) +
                    (anzahl * kaufkurs);

                // Neuer Durchschnittskaufpreis
                decimal neuerDurchschnittskurs = gesamtEinstandswert / gesamtAnzahl;

                Debug.WriteLine($"Aktienbestand aktualisiert: {symbol}");
                Debug.WriteLine($"Alte Anzahl: {existingEntry.Anzahl}, Neuer Bestand: {gesamtAnzahl}");
                Debug.WriteLine($"Alter Einstandspreis: {existingEntry.EinstandsPreis:F2}€");
                Debug.WriteLine($"Neuer Einstandspreis: {neuerDurchschnittskurs:F2}€");

                // Bestehenden Eintrag aktualisieren
                existingEntry.Anzahl = (int)gesamtAnzahl;
                existingEntry.EinstandsPreis = neuerDurchschnittskurs;
                existingEntry.AktuellerKurs = aktuellerKurs;
                existingEntry.LetzteAktualisierung = DateTime.Now;

                // Aktualisiere berechnete Properties
                existingEntry.BerechneWertUndGewinnVerlust();
            }
            else
            {
                // Wenn die Aktie noch nicht im Portfolio ist, neuen Eintrag erstellen
                var neuerEintrag = new PortfolioEintrag
                {
                    BenutzerID = _benutzerId > 0 ? _benutzerId : 1,
                    AktienID = aktienID,
                    AktienSymbol = symbol,
                    AktienName = name,
                    Anzahl = anzahl,
                    EinstandsPreis = kaufkurs,
                    AktuellerKurs = aktuellerKurs,
                    LetzteAktualisierung = DateTime.Now
                };

                PortfolioEintraege.Add(neuerEintrag);

                // Aktualisiere berechnete Properties
                neuerEintrag.BerechneWertUndGewinnVerlust();
            }

            // Zusätzliche Debugging-Informationen
            Debug.WriteLine($"Portfolio-Update für {symbol}:");
            Debug.WriteLine($"Aktueller Gesamtbestand: {PortfolioEintraege.FirstOrDefault(pe => pe.AktienSymbol == symbol)?.Anzahl ?? 0} Aktien");

            // Gesamtwerte neu berechnen
            BerechneGesamtwerte();
            LetzteAktualisierung = DateTime.Now;

            // Änderungen zur Datenbank synchronisieren
            _ = SynchronisierenAsync();

            // UI durch Benachrichtigung aktualisieren
            OnPropertyChanged(nameof(PortfolioEintraege));
        }

        /// <summary>
        /// Verkauft eine bestimmte Anzahl einer Aktie aus dem Portfolio
        /// </summary>
        /// <param name="aktienID">ID der zu verkaufenden Aktie</param>
        /// <param name="aktienSymbol">Symbol der zu verkaufenden Aktie (als Fallback)</param>
        /// <param name="anzahl">Anzahl der zu verkaufenden Aktien</param>
        /// <returns>True, wenn der Verkauf erfolgreich war, sonst False</returns>
        public bool VerkaufeAktie(int aktienID, string aktienSymbol, int anzahl)
        {
            try
            {
                // Erst versuchen, über ID zu finden
                var portfolioEntry = PortfolioEintraege.FirstOrDefault(pe => pe.AktienID == aktienID);

                // Falls nicht gefunden, über Symbol versuchen
                if (portfolioEntry == null && !string.IsNullOrEmpty(aktienSymbol))
                {
                    Debug.WriteLine($"Aktie mit ID {aktienID} nicht gefunden, versuche über Symbol '{aktienSymbol}'");
                    portfolioEntry = PortfolioEintraege.FirstOrDefault(pe =>
                        pe.AktienSymbol.Equals(aktienSymbol, StringComparison.OrdinalIgnoreCase));

                    if (portfolioEntry != null)
                    {
                        Debug.WriteLine($"Aktie über Symbol gefunden: ID {portfolioEntry.AktienID}, Symbol {portfolioEntry.AktienSymbol}");
                    }
                }

                if (portfolioEntry == null || portfolioEntry.Anzahl < anzahl)
                {
                    Debug.WriteLine($"Verkauf nicht möglich: Aktie (ID: {aktienID}, Symbol: {aktienSymbol}) nicht gefunden oder nicht genug Stück");
                    return false; // Aktie nicht vorhanden oder nicht genug Stück im Portfolio
                }

                // Wichtig: Aktuellen Kurswert speichern, bevor wir den Eintrag entfernen!
                decimal aktuellerKurs = portfolioEntry.AktuellerKurs;
                Debug.WriteLine($"Verkauf: Aktueller Kurs für Aktie {portfolioEntry.AktienSymbol}: {aktuellerKurs:F2}€");

                // Anzahl reduzieren
                portfolioEntry.Anzahl -= anzahl;

                // Wenn alle Aktien verkauft wurden, entferne den Eintrag aus dem Portfolio
                if (portfolioEntry.Anzahl == 0)
                {
                    PortfolioEintraege.Remove(portfolioEntry);

                    // Wenn Datenbankzugriff vorhanden, Eintrag aus der Datenbank entfernen
                    if (_databaseService != null && _benutzerId > 0)
                    {
                        Debug.WriteLine($"Entferne Aktie {portfolioEntry.AktienID} aus Datenbank für Benutzer {_benutzerId}");
                        _ = _databaseService.RemovePortfolioEintragAsync(_benutzerId, portfolioEntry.AktienID);
                    }
                }
                else
                {
                    // Sonst Änderungen zur Datenbank synchronisieren
                    Debug.WriteLine($"Aktualisiere Aktie {portfolioEntry.AktienID}, neue Anzahl: {portfolioEntry.Anzahl}");
                    _ = SynchronisierenAsync();

                    // Aktualisiere berechnete Properties
                    portfolioEntry.BerechneWertUndGewinnVerlust();
                }

                // Gesamtwerte neu berechnen
                BerechneGesamtwerte();
                LetzteAktualisierung = DateTime.Now;

                // UI durch Benachrichtigung aktualisieren
                OnPropertyChanged(nameof(PortfolioEintraege));

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Verkaufen der Aktie: {ex.Message}");
                HatFehler = true;
                FehlerText = $"Fehler beim Verkauf der Aktie: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Aktualisiert die Kurse aller Aktien im Portfolio
        /// </summary>
        /// <param name="aktuelleKurse">Dictionary mit Aktien-IDs und ihren aktuellen Kursen</param>
        public void AktualisiereKurse(Dictionary<int, decimal> aktuelleKurse)
        {
            if (_isUpdating) return;

            try
            {
                _isUpdating = true;
                bool wurdeAktualisiert = false;

                foreach (var eintrag in PortfolioEintraege)
                {
                    if (aktuelleKurse.TryGetValue(eintrag.AktienID, out decimal neuerKurs) && neuerKurs > 0)
                    {
                        eintrag.AktuellerKurs = neuerKurs;
                        eintrag.LetzteAktualisierung = System.DateTime.Now;

                        // Aktualisiere berechnete Properties
                        eintrag.BerechneWertUndGewinnVerlust();

                        wurdeAktualisiert = true;
                    }
                }

                if (wurdeAktualisiert)
                {
                    // Gesamtwerte neu berechnen
                    BerechneGesamtwerte();
                    LetzteAktualisierung = DateTime.Now;

                    // UI durch Benachrichtigung aktualisieren
                    OnPropertyChanged(nameof(PortfolioEintraege));

                    // Änderungen zur Datenbank synchronisieren, wenn möglich
                    if (_databaseService != null && _benutzerId > 0)
                    {
                        _ = _databaseService.UpdatePortfolioKurseAsync(_benutzerId, aktuelleKurse);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der Kurse: {ex.Message}");
                HatFehler = true;
                FehlerText = $"Fehler beim Aktualisieren der Kurse: {ex.Message}";
            }
            finally
            {
                _isUpdating = false;
            }
        }

        public void AktualisierePortfolioMitMarktdaten(List<Aktie> aktuelleMarktdaten)
        {
            if (PortfolioEintraege == null || aktuelleMarktdaten == null)
                return;

            foreach (var eintrag in PortfolioEintraege)
            {
                var aktuelleAktie = aktuelleMarktdaten.FirstOrDefault(a =>
                    a.AktienSymbol.Equals(eintrag.AktienSymbol, StringComparison.OrdinalIgnoreCase));

                if (aktuelleAktie != null)
                {
                    eintrag.AktuellerKurs = aktuelleAktie.AktuellerPreis;
                    eintrag.BerechneWertUndGewinnVerlust();
                }
            }

            BerechneGesamtwerte();
        }

        #endregion
    }
}