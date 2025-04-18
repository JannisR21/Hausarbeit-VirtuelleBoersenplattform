using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using HausarbeitVirtuelleBörsenplattform.Models;

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
            // Beispieldaten für das Portfolio laden
            InitializePortfolioData();
        }

        #endregion

        #region Private Methoden

        /// <summary>
        /// Lädt Beispieldaten für das Portfolio
        /// </summary>
        private void InitializePortfolioData()
        {
            // Beispieldaten für das Portfolio
            PortfolioEintraege = new ObservableCollection<PortfolioEintrag>
            {
                new PortfolioEintrag
                {
                    AktienID = 1,
                    AktienSymbol = "AAPL",
                    AktienName = "Apple Inc.",
                    Anzahl = 10,
                    AktuellerKurs = 150.00m,
                    EinstandsPreis = 145.00m
                },
                new PortfolioEintrag
                {
                    AktienID = 2,
                    AktienSymbol = "TSLA",
                    AktienName = "Tesla Inc.",
                    Anzahl = 5,
                    AktuellerKurs = 200.20m,
                    EinstandsPreis = 210.00m
                },
                new PortfolioEintrag
                {
                    AktienID = 3,
                    AktienSymbol = "MSFT",
                    AktienName = "Microsoft Corp.",
                    Anzahl = 8,
                    AktuellerKurs = 320.45m,
                    EinstandsPreis = 305.80m
                }
            };

            // Gesamtwerte berechnen
            BerechneGesamtwerte();
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
                PortfolioEintraege.Add(new PortfolioEintrag
                {
                    AktienID = aktienID,
                    AktienSymbol = symbol,
                    AktienName = name,
                    Anzahl = anzahl,
                    EinstandsPreis = kaufkurs,
                    AktuellerKurs = aktuellerKurs
                });
            }

            // Gesamtwerte neu berechnen
            BerechneGesamtwerte();
        }

        /// <summary>
        /// Verkauft eine bestimmte Anzahl einer Aktie aus dem Portfolio
        /// </summary>
        /// <param name="aktienID">ID der zu verkaufenden Aktie</param>
        /// <param name="anzahl">Anzahl der zu verkaufenden Aktien</param>
        /// <returns>True, wenn der Verkauf erfolgreich war, sonst False</returns>
        public bool VerkaufeAktie(int aktienID, int anzahl)
        {
            var portfolioEntry = PortfolioEintraege.FirstOrDefault(pe => pe.AktienID == aktienID);

            if (portfolioEntry == null || portfolioEntry.Anzahl < anzahl)
            {
                return false; // Aktie nicht vorhanden oder nicht genug Stück im Portfolio
            }

            // Anzahl reduzieren
            portfolioEntry.Anzahl -= anzahl;

            // Wenn alle Aktien verkauft wurden, entferne den Eintrag aus dem Portfolio
            if (portfolioEntry.Anzahl == 0)
            {
                PortfolioEintraege.Remove(portfolioEntry);
            }

            // Gesamtwerte neu berechnen
            BerechneGesamtwerte();
            return true;
        }

        /// <summary>
        /// Aktualisiert die Kurse aller Aktien im Portfolio
        /// </summary>
        /// <param name="aktuelleKurse">Dictionary mit Aktien-IDs und ihren aktuellen Kursen</param>
        public void AktualisiereKurse(Dictionary<int, decimal> aktuelleKurse)
        {
            foreach (var eintrag in PortfolioEintraege)
            {
                if (aktuelleKurse.TryGetValue(eintrag.AktienID, out decimal neuerKurs))
                {
                    eintrag.AktuellerKurs = neuerKurs;
                }
            }

            // Gesamtwerte neu berechnen
            BerechneGesamtwerte();
        }

        #endregion
    }
}