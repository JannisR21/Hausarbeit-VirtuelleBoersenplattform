using CommunityToolkit.Mvvm.ComponentModel;
using HausarbeitVirtuelleBörsenplattform.Models;
using System;
using System.Configuration;
using System.Diagnostics;

namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    /// <summary>
    /// Haupt-ViewModel für das MainWindow, das alle anderen ViewModels koordiniert
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        #region Private Felder

        private Benutzer _aktuellerBenutzer;
        private MarktStatus _marktStatus;

        #endregion

        #region Public Properties

        /// <summary>
        /// Aktuell angemeldeter Benutzer
        /// </summary>
        public Benutzer AktuellerBenutzer
        {
            get => _aktuellerBenutzer;
            set => SetProperty(ref _aktuellerBenutzer, value);
        }

        /// <summary>
        /// Aktueller Status des Börsenmarktes (offen/geschlossen)
        /// </summary>
        public MarktStatus MarktStatus
        {
            get => _marktStatus;
            set => SetProperty(ref _marktStatus, value);
        }

        /// <summary>
        /// ViewModel für den Portfolio-Bereich
        /// </summary>
        public PortfolioViewModel PortfolioViewModel { get; }

        /// <summary>
        /// ViewModel für den Marktdaten-Bereich
        /// </summary>
        public MarktdatenViewModel MarktdatenViewModel { get; }

        /// <summary>
        /// ViewModel für den Aktienhandel-Bereich
        /// </summary>
        public AktienhandelViewModel AktienhandelViewModel { get; }

        // Alternativ-Property für HandelsViewModel
        public AktienhandelViewModel HandelsViewModel => AktienhandelViewModel;

        #endregion

        #region Konstruktor

        /// <summary>
        /// Initialisiert eine neue Instanz des MainViewModel
        /// </summary>
        public MainViewModel()
        {
            // Benutzer laden oder Beispiele setzen
            if (App.AuthService?.CurrentUser != null)
                AktuellerBenutzer = App.AuthService.CurrentUser;
            else
                InitializeData();

            // API-Key (besser aus Config)
            string apiKey = "cb617aba18ea46b3a974d878d3c7310b";

            // Sub‑ViewModels initialisieren und MainViewModel injizieren
            PortfolioViewModel = new PortfolioViewModel();
            MarktdatenViewModel = new MarktdatenViewModel(this, apiKey);
            AktienhandelViewModel = new AktienhandelViewModel(this);

            // Wenn Marktdaten aktualisiert wurden, synchronisiere Portfolio
            MarktdatenViewModel.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(MarktdatenViewModel.LetzteAktualisierung))
                {
                    Debug.WriteLine("Marktdaten wurden aktualisiert, synchronisiere Portfolio...");
                    PortfolioViewModel.AktualisiereKurseMitMarktdaten(MarktdatenViewModel.AktienListe);
                }
            };
        }

        #endregion

        #region Private Methoden

        /// <summary>
        /// Initialisiert Beispieldaten für Benutzer und Marktstatus
        /// </summary>
        private void InitializeData()
        {
            AktuellerBenutzer = new Benutzer
            {
                BenutzerID = 1,
                Benutzername = "Jannis Ruhland",
                Email = "jannis.ruhland@example.com",
                Kontostand = 10000.00m
            };

            MarktStatus = new MarktStatus
            {
                IsOpen = true,
                StatusText = "Markt geöffnet",
                StatusColor = "#2ecc71",
                LetzteAktualisierung = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            };
        }

        #endregion
    }
}
