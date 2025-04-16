using CommunityToolkit.Mvvm.ComponentModel;
using HausarbeitVirtuelleBörsenplattform.Models;
using System;
using System.Collections.ObjectModel;

namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    /// <summary>
    /// ViewModel für den Marktdaten-Bereich der Anwendung
    /// </summary>
    public class MarktdatenViewModel : ObservableObject
    {
        #region Private Felder

        private ObservableCollection<Aktie> _aktienListe;
        private Aktie _ausgewählteAktie;
        private MainViewModel _mainViewModel;

        #endregion

        #region Public Properties

        /// <summary>
        /// Liste aller verfügbaren Aktien mit aktuellen Kursinformationen
        /// </summary>
        public ObservableCollection<Aktie> AktienListe
        {
            get => _aktienListe;
            set => SetProperty(ref _aktienListe, value);
        }

        /// <summary>
        /// Aktuell ausgewählte Aktie für Detailansicht
        /// </summary>
        public Aktie AusgewählteAktie
        {
            get => _ausgewählteAktie;
            set => SetProperty(ref _ausgewählteAktie, value);
        }

        #endregion

        #region Konstruktor

        /// <summary>
        /// Initialisiert eine neue Instanz des MarktdatenViewModel
        /// </summary>
        /// <param name="mainViewModel">Hauptinstanz des MainViewModel</param>
        public MarktdatenViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            // Beispieldaten für Marktdaten
            InitializeMarktdaten();

            // Timer für regelmäßige Aktualisierungen
            StartUpdateTimer();
        }

        #endregion

        #region Private Methoden

        /// <summary>
        /// Initialisiert Beispieldaten für die Aktienliste
        /// </summary>
        private void InitializeMarktdaten()
        {
            // Beispieldaten für die Aktienliste
            AktienListe = new ObservableCollection<Aktie>
            {
                new Aktie
                {
                    AktienID = 1,
                    AktienSymbol = "AAPL",
                    AktienName = "Apple Inc.",
                    AktuellerPreis = 150.00m,
                    Änderung = 1.25m,
                    ÄnderungProzent = 0.84m,
                    LetzteAktualisierung = DateTime.Now
                },
                new Aktie
                {
                    AktienID = 2,
                    AktienSymbol = "TSLA",
                    AktienName = "Tesla Inc.",
                    AktuellerPreis = 200.20m,
                    Änderung = -0.70m,
                    ÄnderungProzent = -0.35m,
                    LetzteAktualisierung = DateTime.Now
                },
                new Aktie
                {
                    AktienID = 3,
                    AktienSymbol = "AMZN",
                    AktienName = "Amazon.com Inc.",
                    AktuellerPreis = 95.10m,
                    Änderung = 0.72m,
                    ÄnderungProzent = 0.76m,
                    LetzteAktualisierung = DateTime.Now
                },
                new Aktie
                {
                    AktienID = 4,
                    AktienSymbol = "MSFT",
                    AktienName = "Microsoft Corp.",
                    AktuellerPreis = 320.45m,
                    Änderung = 4.75m,
                    ÄnderungProzent = 1.50m,
                    LetzteAktualisierung = DateTime.Now
                },
                new Aktie
                {
                    AktienID = 5,
                    AktienSymbol = "GOOGL",
                    AktienName = "Alphabet Inc.",
                    AktuellerPreis = 128.75m,
                    Änderung = -0.28m,
                    ÄnderungProzent = -0.22m,
                    LetzteAktualisierung = DateTime.Now
                }
            };
        }

        /// <summary>
        /// Startet einen Timer für die regelmäßige Aktualisierung der Marktdaten
        /// </summary>
        private void StartUpdateTimer()
        {
            // In einer realen Anwendung würde hier ein Timer gestartet werden
            // Für diesen Prototyp wird kein Timer implementiert
        }

        #endregion
    }
}