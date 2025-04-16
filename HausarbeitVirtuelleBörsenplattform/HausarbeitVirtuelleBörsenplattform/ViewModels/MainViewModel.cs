using CommunityToolkit.Mvvm.ComponentModel;
using HausarbeitVirtuelleBörsenplattform.Models;

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
        public PortfolioViewModel PortfolioViewModel { get; private set; }

        #endregion

        #region Konstruktor

        /// <summary>
        /// Initialisiert eine neue Instanz des MainViewModel
        /// </summary>
        public MainViewModel()
        {
            // Beispieldaten initialisieren
            InitializeData();

            // ViewModel für den Portfolio-Bereich erstellen
            PortfolioViewModel = new PortfolioViewModel();
        }

        #endregion

        #region Private Methoden

        /// <summary>
        /// Initialisiert Beispieldaten für das MainViewModel
        /// </summary>
        private void InitializeData()
        {
            // Beispiel-Benutzer erstellen
            AktuellerBenutzer = new Benutzer
            {
                BenutzerID = 1,
                Benutzername = "Jannis Ruhland",
                Email = "jannis.ruhland@example.com",
                Kontostand = 10000.00m
            };

            // Beispiel-Marktstatus erstellen
            MarktStatus = new MarktStatus
            {
                IsOpen = true,
                StatusText = "Markt geöffnet",
                StatusColor = "#2ecc71",
                LetzteAktualisierung = System.DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            };
        }

        #endregion
    }
}