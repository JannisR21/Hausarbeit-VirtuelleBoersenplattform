using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HausarbeitVirtuelleBörsenplattform.Helpers;
using HausarbeitVirtuelleBörsenplattform.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HausarbeitVirtuelleBörsenplattform.Services
{
    /// <summary>
    /// Service für die erweiterte Suche und Filterung von Aktien und ETFs
    /// Erweitert die Funktionalität des TwelveDataService
    /// </summary>
    public class AktienFilterService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly TwelveDataService _twelveDataService;
        private readonly RateLimiter _rateLimiter;
        private readonly int _maxCallsPerMinute = 8; // Dem TwelveDataService angepasst

        /// <summary>
        /// Letzte Fehlermeldung für Debugging
        /// </summary>
        public string LastErrorMessage { get; private set; }

        /// <summary>
        /// Initialisiert eine neue Instanz des AktienFilterService
        /// </summary>
        /// <param name="apiKey">Der API-Schlüssel für Twelve Data</param>
        /// <param name="twelveDataService">Referenz auf den bestehenden TwelveDataService</param>
        public AktienFilterService(string apiKey, TwelveDataService twelveDataService)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
            _twelveDataService = twelveDataService ?? throw new ArgumentNullException(nameof(twelveDataService));
            _rateLimiter = new RateLimiter(_maxCallsPerMinute, TimeSpan.FromMinutes(1));

            Debug.WriteLine($"AktienFilterService initialisiert mit API-Key: {apiKey}");
        }

        /// <summary>
        /// Sucht nach ETFs basierend auf verschiedenen Filterkriterien
        /// </summary>
        /// <param name="query">Suchbegriff (optional)</param>
        /// <param name="etfTyp">ETF-Typ (optional)</param>
        /// <param name="ausschüttend">Nur ausschüttende ETFs? (optional)</param>
        /// <returns>Liste der gefilterten ETFs</returns>
        public async Task<List<ETF>> SucheETFsAsync(string query = null, ETFTyp? etfTyp = null, bool? ausschüttend = null)
        {
            Debug.WriteLine($"SucheETFsAsync aufgerufen mit Query: {query}, ETF-Typ: {etfTyp}, Ausschüttend: {ausschüttend}");

            try
            {
                // Zuerst lokale ETF-Liste holen (kann später durch API-Aufruf ersetzt werden)
                var alleETFs = BekannteBörsenETFs.GetBekannteBörsenETFs();

                // Filter anwenden
                IEnumerable<ETF> ergebnisse = alleETFs;

                // Nach Suchbegriff filtern
                if (!string.IsNullOrWhiteSpace(query))
                {
                    string suchbegriff = query.ToLower().Trim();
                    ergebnisse = ergebnisse.Where(e =>
                        e.AktienName.ToLower().Contains(suchbegriff) ||
                        e.AktienSymbol.ToLower().Contains(suchbegriff) ||
                        e.AbgebildeterIndex.ToLower().Contains(suchbegriff) ||
                        e.Fondsgesellschaft.ToLower().Contains(suchbegriff));
                }

                // Nach ETF-Typ filtern
                if (etfTyp.HasValue)
                {
                    ergebnisse = ergebnisse.Where(e => e.ETFTyp == etfTyp.Value);
                }

                // Nach Ausschüttungsart filtern
                if (ausschüttend.HasValue)
                {
                    ergebnisse = ergebnisse.Where(e => e.IstAusschüttend == ausschüttend.Value);
                }

                // Für jeden ETF aktuelle Kursdaten laden
                var ergebnisListe = ergebnisse.ToList();
                if (ergebnisListe.Any())
                {
                    await LadeAktuelleKurseAsync(ergebnisListe);
                }

                Debug.WriteLine($"SucheETFsAsync liefert {ergebnisListe.Count} Ergebnisse zurück");
                return ergebnisListe;
            }
            catch (Exception ex)
            {
                LastErrorMessage = $"Fehler bei der ETF-Suche: {ex.Message}";
                Debug.WriteLine(LastErrorMessage);
                return new List<ETF>();
            }
        }

        /// <summary>
        /// Lädt aktuelle Kurse für eine Liste von ETFs
        /// </summary>
        private async Task LadeAktuelleKurseAsync(List<ETF> etfs)
        {
            // Wir nutzen den TwelveDataService für die Kursabfrage
            if (_twelveDataService != null && etfs != null && etfs.Any())
            {
                try
                {
                    // Symbole extrahieren
                    var symbole = etfs.Select(e => e.AktienSymbol).ToList();

                    // Kurse über TwelveDataService laden
                    var aktienKurse = await _twelveDataService.HoleAktienKurse(symbole);

                    // Kurse in ETFs eintragen
                    if (aktienKurse != null && aktienKurse.Any())
                    {
                        foreach (var etf in etfs)
                        {
                            var kurs = aktienKurse.FirstOrDefault(a =>
                                a.AktienSymbol.Equals(etf.AktienSymbol, StringComparison.OrdinalIgnoreCase));

                            if (kurs != null)
                            {
                                etf.AktuellerPreis = kurs.AktuellerPreis;
                                etf.Änderung = kurs.Änderung;
                                etf.ÄnderungProzent = kurs.ÄnderungProzent;
                                etf.LetzteAktualisierung = DateTime.Now;

                                Debug.WriteLine($"Kurs für ETF {etf.AktienSymbol} aktualisiert: {etf.AktuellerPreis:F2}€");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Laden der ETF-Kurse: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Sucht nach Aktien basierend auf der Branche
        /// </summary>
        /// <param name="branche">Die zu suchende Branche</param>
        /// <returns>Liste der Aktien in der angegebenen Branche</returns>
        public async Task<List<Aktie>> SucheAktienNachBrancheAsync(AktienBranche branche)
        {
            Debug.WriteLine($"SucheAktienNachBrancheAsync aufgerufen für Branche: {branche}");

            try
            {
                // Symbole für die angegebene Branche holen (könnte später durch API-Aufruf ersetzt werden)
                List<string> branchenSymbole;

                switch (branche)
                {
                    case AktienBranche.TECHNOLOGIE:
                        branchenSymbole = new List<string> { "AAPL", "MSFT", "GOOGL", "AMZN", "FB", "NVDA", "AMD", "INTC", "CSCO", "ADBE" };
                        break;
                    case AktienBranche.FINANZEN:
                        branchenSymbole = new List<string> { "JPM", "BAC", "C", "WFC", "GS", "MS", "BLK", "AXP", "V", "MA" };
                        break;
                    case AktienBranche.GESUNDHEIT:
                        branchenSymbole = new List<string> { "JNJ", "PFE", "MRK", "ABT", "ABBV", "TMO", "LLY", "UNH", "BMY", "AMGN" };
                        break;
                    case AktienBranche.KONSUMGÜTER:
                        branchenSymbole = new List<string> { "PG", "KO", "PEP", "WMT", "COST", "NKE", "MCD", "SBUX", "HD", "LOW" };
                        break;
                    case AktienBranche.ENERGIE:
                        branchenSymbole = new List<string> { "XOM", "CVX", "COP", "BP", "SHEL", "TOT", "ENB", "KMI", "PSX", "EOG" };
                        break;
                    case AktienBranche.INDUSTRIE:
                        branchenSymbole = new List<string> { "GE", "BA", "MMM", "HON", "LMT", "CAT", "DE", "UPS", "FDX", "UNP" };
                        break;
                    case AktienBranche.TELEKOMMUNIKATION:
                        branchenSymbole = new List<string> { "T", "VZ", "TMUS", "CMCSA", "CHTR", "VOD", "NFLX", "DIS", "ATVI", "EA" };
                        break;
                    case AktienBranche.GRUNDSTOFFE:
                        branchenSymbole = new List<string> { "BHP", "RIO", "VALE", "NEM", "FCX", "SHW", "APD", "LIN", "DD", "DOW" };
                        break;
                    case AktienBranche.VERSORGUNG:
                        branchenSymbole = new List<string> { "NEE", "DUK", "SO", "D", "AEP", "EXC", "PCG", "ED", "XEL", "ES" };
                        break;
                    case AktienBranche.IMMOBILIEN:
                        branchenSymbole = new List<string> { "AMT", "CCI", "PLD", "PSA", "EQIX", "DLR", "SPG", "AVB", "EQR", "O" };
                        break;
                    default:
                        branchenSymbole = new List<string>();
                        break;
                }

                if (!branchenSymbole.Any())
                {
                    Debug.WriteLine("Keine Symbole für die angegebene Branche gefunden");
                    return new List<Aktie>();
                }

                // Kurse über TwelveDataService laden
                var aktien = await _twelveDataService.HoleAktienKurse(branchenSymbole);

                // Branchenzuordnung setzen (kann später als Eigenschaft in Aktie-Klasse hinzugefügt werden)
                Debug.WriteLine($"SucheAktienNachBrancheAsync liefert {aktien.Count} Ergebnisse zurück");
                return aktien;
            }
            catch (Exception ex)
            {
                LastErrorMessage = $"Fehler bei der Branchensuche: {ex.Message}";
                Debug.WriteLine(LastErrorMessage);
                return new List<Aktie>();
            }
        }

        /// <summary>
        /// Sucht nach Finanzinstrumenten (Aktien, ETFs, etc.) basierend auf dem Anlagetyp
        /// </summary>
        /// <param name="anlageTyp">Der zu suchende Anlagetyp</param>
        /// <param name="query">Optionaler Suchbegriff</param>
        /// <returns>Liste der gefundenen Finanzinstrumente</returns>
        public async Task<List<Aktie>> SucheNachAnlageTypAsync(AnlageTyp anlageTyp, string query = null)
        {
            Debug.WriteLine($"SucheNachAnlageTypAsync aufgerufen für Typ: {anlageTyp}, Query: {query}");

            try
            {
                List<Aktie> ergebnisse = new List<Aktie>();

                switch (anlageTyp)
                {
                    case AnlageTyp.EINZELAKTIE:
                        // Bekannte Aktien-Symbole
                        var bekannteAktien = new List<string> {
                            "AAPL", "MSFT", "AMZN", "GOOGL", "META", "TSLA", "BRK.B",
                            "V", "JPM", "JNJ", "WMT", "PG", "MA", "UNH", "HD", "BAC"
                        };

                        // Falls eine Suche eingegeben wurde, versuche passende Aktien zu finden
                        if (!string.IsNullOrWhiteSpace(query))
                        {
                            string suchbegriff = query.ToLower().Trim();
                            bekannteAktien = bekannteAktien
                                .Where(s => s.ToLower().Contains(suchbegriff))
                                .ToList();

                            // Hier könnte man später auch eine erweiterte Suche über eine API implementieren
                        }

                        // Kurse laden
                        ergebnisse = await _twelveDataService.HoleAktienKurse(bekannteAktien);
                        break;

                    case AnlageTyp.ETF:
                        // ETFs suchen
                        var etfs = await SucheETFsAsync(query);
                        ergebnisse.AddRange(etfs);
                        break;

                    case AnlageTyp.FOND:
                    case AnlageTyp.ANLEIHE:
                    case AnlageTyp.ZERTIFIKAT:
                        // Diese Typen sind noch nicht implementiert
                        Debug.WriteLine($"Anlagetyp {anlageTyp} ist noch nicht implementiert");
                        break;
                }

                Debug.WriteLine($"SucheNachAnlageTypAsync liefert {ergebnisse.Count} Ergebnisse zurück");
                return ergebnisse;
            }
            catch (Exception ex)
            {
                LastErrorMessage = $"Fehler bei der Anlagetyp-Suche: {ex.Message}";
                Debug.WriteLine(LastErrorMessage);
                return new List<Aktie>();
            }
        }
    }
}