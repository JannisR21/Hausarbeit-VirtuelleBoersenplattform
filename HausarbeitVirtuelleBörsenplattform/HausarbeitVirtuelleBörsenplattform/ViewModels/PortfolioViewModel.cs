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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    PortfolioEintraege.Clear();
                    foreach (var item in portfolioData)
                    {
                        PortfolioEintraege.Add(item);
                    }

                    // Gesamtwerte berechnen
                    BerechneGesamtwerte();
                });

                Debug.WriteLine($"Portfolio-Daten für Benutzer {_benutzerId} geladen: {portfolioData.Count} Einträge");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der Portfolio-Daten: {ex.Message}");
                MessageBox.Show($"Fehler beim Laden der Portfolio-Daten: {ex.Message}",
                    "Datenbankfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                // Keine Beispieldaten mehr laden
            }
        }

        #endregion

        #region Public Methoden

        /// <summary>
        /// Berechnet den Gesamtwert und Gesamtgewinn/-verlust des Portfolios
        /// </summary>
        public void BerechneGesamtwerte()
        {
            Gesamtwert = PortfolioEintraege.Sum(pe => pe.Wert);
            GesamtGewinnVerlust = PortfolioEintraege.Sum(pe => pe.GewinnVerlust);
            Debug.WriteLine($"Portfolio-Gesamtwerte neu berechnet: Wert={Gesamtwert:N2}€, GewinnVerlust={GesamtGewinnVerlust:N2}€");
        }

        /// <summary>
        /// Aktualisiert die Kurse der Portfolio-Einträge basierend auf den aktuellen Marktdaten
        /// </summary>
        /// <param name="aktienListe">Liste aller Aktien mit aktuellen Kursinformationen</param>
        public void AktualisiereKurseMitMarktdaten(IEnumerable<Aktie> aktienListe)
        {
            if (aktienListe == null)
                return;

            Debug.WriteLine("Aktualisiere Portfolio-Kurse mit Marktdaten");
            bool wurdeAktualisiert = false;

            foreach (var eintrag in PortfolioEintraege)
            {
                var aktienInfo = aktienListe.FirstOrDefault(a => a.AktienSymbol == eintrag.AktienSymbol);
                if (aktienInfo != null && aktienInfo.AktuellerPreis > 0)
                {
                    decimal alterKurs = eintrag.AktuellerKurs;
                    eintrag.AktuellerKurs = aktienInfo.AktuellerPreis;
                    eintrag.LetzteAktualisierung = aktienInfo.LetzteAktualisierung;

                    Debug.WriteLine($"Portfolio-Eintrag {eintrag.AktienSymbol} aktualisiert: {alterKurs:N2}€ -> {aktienInfo.AktuellerPreis:N2}€");
                    wurdeAktualisiert = true;
                }
            }

            if (wurdeAktualisiert)
            {
                // Gesamtwerte neu berechnen, wenn mindestens ein Kurs aktualisiert wurde
                BerechneGesamtwerte();

                // Änderungen zur Datenbank synchronisieren, wenn möglich
                SynchronisierenAsync().ConfigureAwait(false);
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
            var existingEntry = PortfolioEintraege.FirstOrDefault(pe => pe.AktienID == aktienID);

            if (existingEntry != null)
            {
                // Wenn die Aktie bereits existiert, aktualisiere die Anzahl und berechne den Durchschnittskaufpreis
                decimal gesamtEinstandswert = (existingEntry.Anzahl * existingEntry.EinstandsPreis) + (anzahl * kaufkurs);
                existingEntry.Anzahl += anzahl;
                existingEntry.EinstandsPreis = gesamtEinstandswert / existingEntry.Anzahl;
                existingEntry.AktuellerKurs = aktuellerKurs; // Aktuellen Kurs aktualisieren
            }
            else
            {
                // Wenn die Aktie noch nicht im Portfolio ist, füge einen neuen Eintrag hinzu
                var neuerEintrag = new PortfolioEintrag
                {
                    BenutzerID = _benutzerId > 0 ? _benutzerId : 1,
                    AktienID = aktienID,
                    AktienSymbol = symbol,
                    AktienName = name,
                    Anzahl = anzahl,
                    EinstandsPreis = kaufkurs,
                    AktuellerKurs = aktuellerKurs,
                    LetzteAktualisierung = System.DateTime.Now
                };

                PortfolioEintraege.Add(neuerEintrag);
            }

            // Gesamtwerte neu berechnen
            BerechneGesamtwerte();

            // Änderungen zur Datenbank synchronisieren, wenn möglich
            SynchronisierenAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Verkauft eine bestimmte Anzahl einer Aktie aus dem Portfolio
        /// </summary>
        /// <param name="aktienID">ID der zu verkaufenden Aktie</param>
        /// <param name="anzahl">Anzahl der zu verkaufenden Aktien</param>
        /// <returns>True, wenn der Verkauf erfolgreich war, sonst False</returns>
        public bool VerkaufeAktie(int aktienID, int anzahl)
        {
            try
            {
                var portfolioEntry = PortfolioEintraege.FirstOrDefault(pe => pe.AktienID == aktienID);

                if (portfolioEntry == null || portfolioEntry.Anzahl < anzahl)
                {
                    Debug.WriteLine($"Verkauf nicht möglich: AktienID {aktienID} nicht gefunden oder nicht genug Stück");
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
                        Debug.WriteLine($"Entferne Aktie {aktienID} aus Datenbank für Benutzer {_benutzerId}");
                        _ = _databaseService.RemovePortfolioEintragAsync(_benutzerId, aktienID);
                    }
                }
                else
                {
                    // Sonst Änderungen zur Datenbank synchronisieren
                    Debug.WriteLine($"Aktualisiere Aktie {aktienID}, neue Anzahl: {portfolioEntry.Anzahl}");
                    _ = SynchronisierenAsync();
                }

                // Gesamtwerte neu berechnen
                BerechneGesamtwerte();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Verkaufen der Aktie: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Aktualisiert die Kurse aller Aktien im Portfolio
        /// </summary>
        /// <param name="aktuelleKurse">Dictionary mit Aktien-IDs und ihren aktuellen Kursen</param>
        public void AktualisiereKurse(Dictionary<int, decimal> aktuelleKurse)
        {
            bool wurdeAktualisiert = false;

            foreach (var eintrag in PortfolioEintraege)
            {
                if (aktuelleKurse.TryGetValue(eintrag.AktienID, out decimal neuerKurs))
                {
                    eintrag.AktuellerKurs = neuerKurs;
                    eintrag.LetzteAktualisierung = System.DateTime.Now;
                    wurdeAktualisiert = true;
                }
            }

            if (wurdeAktualisiert)
            {
                // Gesamtwerte neu berechnen
                BerechneGesamtwerte();

                // Änderungen zur Datenbank synchronisieren, wenn möglich
                if (_databaseService != null && _benutzerId > 0)
                {
                    _databaseService.UpdatePortfolioKurseAsync(_benutzerId, aktuelleKurse).ConfigureAwait(false);
                }
            }
        }

        #endregion
    }
}