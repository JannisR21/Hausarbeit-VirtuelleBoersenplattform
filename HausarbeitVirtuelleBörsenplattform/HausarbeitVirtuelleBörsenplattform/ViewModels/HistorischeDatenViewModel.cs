using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HausarbeitVirtuelleBörsenplattform.Data;
using HausarbeitVirtuelleBörsenplattform.Models;
using HausarbeitVirtuelleBörsenplattform.Services;
using Microsoft.EntityFrameworkCore;

namespace HausarbeitVirtuelleBörsenplattform.ViewModels
{
    /// <summary>
    /// ViewModel für die Anzeige und Verwaltung historischer Aktiendaten
    /// </summary>
    public partial class HistorischeDatenViewModel : ObservableObject
    {
        private readonly BörsenplattformDbContext _context;
        private readonly TwelveDataService _twelveDataService;
        private readonly HttpClient _httpClient;

        [ObservableProperty]
        private ObservableCollection<Aktie> _verfügbareAktien = new ObservableCollection<Aktie>();

        [ObservableProperty]
        private Aktie _ausgewählteAktie;

        [ObservableProperty]
        private DateTime? _vonDatum = DateTime.Now.AddMonths(-1);

        [ObservableProperty]
        private DateTime? _bisDatum = DateTime.Now;

        [ObservableProperty]
        private string _intervall = "Täglich";

        [ObservableProperty]
        private ObservableCollection<HistorischeDatenErweitert> _historischeDaten = new ObservableCollection<HistorischeDatenErweitert>();

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusText;

        [ObservableProperty]
        private string _fehlerText;

        [ObservableProperty]
        private int _anzahlDatenpunkte;

        [ObservableProperty]
        private decimal _höchstkurs;

        [ObservableProperty]
        private DateTime? _höchstkursDatum;

        [ObservableProperty]
        private decimal _tiefstkurs;

        [ObservableProperty]
        private DateTime? _tiefstkursDatum;

        [ObservableProperty]
        private decimal _durchschnittskurs;

        // Gesamtänderung und Volatilität entfernt auf Wunsch

        /// <summary>
        /// Initialisiert eine neue Instanz von HistorischeDatenViewModel
        /// </summary>
        public HistorischeDatenViewModel(BörsenplattformDbContext context, TwelveDataService twelveDataService)
        {
            _context = context;
            _twelveDataService = twelveDataService;
            _httpClient = new HttpClient();

            // Timeout auf 30 Sekunden erhöhen für langsame Verbindungen
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Stellen sicher, dass der Cache-Control Header gesetzt wird, um Caching zu verhindern
            _httpClient.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MaxAge = TimeSpan.Zero
            };

            // Standardwerte für ObservableCollection initialisieren, damit sie nie null sind
            VerfügbareAktien = new ObservableCollection<Aktie>();
            HistorischeDaten = new ObservableCollection<HistorischeDatenErweitert>();

            // Standard-Zeitraum setzen auf 5 Tage
            VonDatum = DateTime.Now.AddDays(-5);
            BisDatum = DateTime.Now;

            StatusText = "Bereit";
            Debug.WriteLine("HistorischeDatenViewModel initialisiert");

            // Anmerkung: Für die Sicherstellung der Tabellen nutzen wir die
            // externe Methoden im DatabaseService anstelle des direkten Aufrufs
            // hier, da EnsureHistoricalDataTableExists async ist
        }

        /// <summary>
        /// Stellt sicher, dass die Tabelle für historische Daten existiert
        /// </summary>
        public async Task EnsureHistoricalDataTableExists()
        {
            try
            {
                Debug.WriteLine("Prüfe historische Daten Tabelle...");

                // Prüfe, ob die Tabelle bereits existiert
                var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = @"
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'HistorischeDatenErweitert')
                        SELECT 1
                    ELSE
                        SELECT 0";

                // Verbindung öffnen, falls noch nicht offen
                if (command.Connection.State != System.Data.ConnectionState.Open)
                    await command.Connection.OpenAsync();

                var result = await command.ExecuteScalarAsync();
                bool tableExists = Convert.ToBoolean(result);

                Debug.WriteLine($"Historische Daten-Tabelle existiert: {tableExists}");

                // Falls die Tabelle nicht existiert, erstelle sie mit einfachster SQL-Syntax für maximale Kompatibilität
                if (!tableExists)
                {
                    Debug.WriteLine("Erstelle Tabelle für historische Daten mit einfacher SQL-Syntax...");

                    // Verwende einfache SQL-Syntax für maximale Kompatibilität
                    var createTableCmd = _context.Database.GetDbConnection().CreateCommand();
                    createTableCmd.CommandText = @"
                        CREATE TABLE HistorischeDatenErweitert (
                            Id int IDENTITY(1,1) PRIMARY KEY,
                            AktieId int NOT NULL,
                            Datum datetime NOT NULL,
                            Eröffnungskurs decimal(18,2) NOT NULL,
                            Höchstkurs decimal(18,2) NOT NULL,
                            Tiefstkurs decimal(18,2) NOT NULL,
                            Schlusskurs decimal(18,2) NOT NULL,
                            ÄnderungProzent decimal(18,2) NOT NULL,
                            Volumen bigint NULL,
                            Intervall nvarchar(20) NOT NULL,
                            ErstelltAm datetime DEFAULT GETDATE(),
                            AktualisiertAm datetime DEFAULT GETDATE()
                        )";

                    await createTableCmd.ExecuteNonQueryAsync();
                    Debug.WriteLine("Tabelle erfolgreich erstellt");

                    // Einfache Indizes erstellen
                    var indexCmd = _context.Database.GetDbConnection().CreateCommand();
                    indexCmd.CommandText = @"
                        CREATE INDEX IX_HistorischeDatenErweitert_AktieId ON HistorischeDatenErweitert(AktieId);
                        CREATE INDEX IX_HistorischeDatenErweitert_Datum ON HistorischeDatenErweitert(Datum);";

                    await indexCmd.ExecuteNonQueryAsync();
                    Debug.WriteLine("Indizes erfolgreich erstellt");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Prüfen/Erstellen der Tabelle: {ex.Message}");
                // Wir fangen den Fehler ab, damit die Anwendung weiter funktioniert
            }
        }

        /// <summary>
        /// Command zum Laden aller verfügbaren Aktien, jetzt mit allen Aktien aus der Marktdaten-Liste
        /// </summary>
        [RelayCommand]
        private async Task LadeVerfügbareAktien()
        {
            try
            {
                IsLoading = true;
                StatusText = "Lade verfügbare Aktien...";

                // Versuchen alle Aktien aus der Datenbank zu laden
                var aktienAusDb = await _context.Aktien
                    .OrderBy(a => a.AktienSymbol)
                    .ToListAsync();

                // Aktien aus der Datenbank in die Liste einfügen
                VerfügbareAktien.Clear();

                // Zuerst die Aktien aus der Datenbank hinzufügen
                foreach (var aktie in aktienAusDb)
                {
                    // Duplikate vermeiden
                    if (!VerfügbareAktien.Any(a => a.AktienSymbol == aktie.AktienSymbol))
                    {
                        VerfügbareAktien.Add(aktie);
                    }
                }

                // Verwende die Liste aus der AktienListe-Klasse
                var alleAktien = AktienListe.GetBekannteBörsenAktien();

                // Füge alle bekannten Aktien aus der AktienListe hinzu
                Debug.WriteLine("Füge Aktien aus der AktienListe hinzu");
                
                foreach (var aktie in alleAktien)
                {
                    // Prüfe, ob die Aktie bereits in der Liste ist
                    if (!VerfügbareAktien.Any(a => a.AktienSymbol == aktie.AktienSymbol))
                    {
                        VerfügbareAktien.Add(new Aktie
                        {
                            AktienID = aktie.AktienID,
                            AktienSymbol = aktie.AktienSymbol,
                            AktienName = aktie.AktienName,
                            AktuellerPreis = 0,
                            Änderung = 0,
                            ÄnderungProzent = 0,
                            LetzteAktualisierung = DateTime.Now
                        });
                    }
                }

                StatusText = $"{VerfügbareAktien.Count} Aktien verfügbar";
                AnzahlDatenpunkte = HistorischeDaten.Count;
                Debug.WriteLine($"Verfügbare Aktien geladen: {VerfügbareAktien.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der verfügbaren Aktien: {ex.Message}");
                FehlerText = $"Fehler: {ex.Message}";
                StatusText = "Fehler beim Laden der Aktien";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Command zum Aktualisieren der historischen Daten für die ausgewählte Aktie
        /// </summary>
        [RelayCommand]
        private async Task Aktualisieren()
        {
            try
            {
                if (AusgewählteAktie == null)
                {
                    StatusText = "Keine Aktie ausgewählt";
                    return;
                }

                IsLoading = true;
                StatusText = $"Lade historische Daten für {AusgewählteAktie.AktienSymbol}...";
                FehlerText = "";
                Debug.WriteLine($"Lade historische Daten für {AusgewählteAktie.AktienSymbol} der letzten 5 Tage");

                // Datum für die letzten 5 Tage setzen
                VonDatum = DateTime.Now.AddDays(-5);
                BisDatum = DateTime.Now;

                try
                {
                    // Direkte HTTP-Anfrage senden
                    string apiUrl = $"https://api.twelvedata.com/time_series" +
                                  $"?symbol={AusgewählteAktie.AktienSymbol}" +
                                  $"&interval=1day" +
                                  $"&start_date={VonDatum.Value:yyyy-MM-dd}" +
                                  $"&end_date={BisDatum.Value:yyyy-MM-dd}" +
                                  $"&apikey={_twelveDataService.ApiKey}";

                    Debug.WriteLine($"API-URL: {apiUrl}");

                    var response = await _httpClient.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine($"API-Antwort erhalten: {jsonString.Length} Zeichen");

                        // Parse JSON und Anzeige aktualisieren
                        var historischeDaten = new List<HistorischeDatenErweitert>();
                        JsonDocument doc = JsonDocument.Parse(jsonString);

                        if (doc.RootElement.TryGetProperty("values", out JsonElement valuesElement) &&
                            valuesElement.ValueKind == JsonValueKind.Array)
                        {
                            decimal? lastClose = null;

                            foreach (JsonElement entry in valuesElement.EnumerateArray())
                            {
                                try
                                {
                                    // Daten extrahieren
                                    string dateString = entry.GetProperty("datetime").GetString();
                                    decimal open = decimal.Parse(entry.GetProperty("open").GetString(), System.Globalization.CultureInfo.InvariantCulture);
                                    decimal high = decimal.Parse(entry.GetProperty("high").GetString(), System.Globalization.CultureInfo.InvariantCulture);
                                    decimal low = decimal.Parse(entry.GetProperty("low").GetString(), System.Globalization.CultureInfo.InvariantCulture);
                                    decimal close = decimal.Parse(entry.GetProperty("close").GetString(), System.Globalization.CultureInfo.InvariantCulture);

                                    // Volumen, falls vorhanden
                                    long? volume = null;
                                    if (entry.TryGetProperty("volume", out JsonElement volumeElement))
                                    {
                                        volume = long.TryParse(volumeElement.GetString(), out long vol) ? vol : null;
                                    }

                                    // Änderungsprozent berechnen
                                    decimal änderungProzent = 0;
                                    if (lastClose.HasValue && lastClose.Value > 0)
                                    {
                                        änderungProzent = (close - lastClose.Value) / lastClose.Value * 100;
                                    }
                                    lastClose = close;

                                    var datenpunkt = new HistorischeDatenErweitert
                                    {
                                        AktieId = AusgewählteAktie.AktienID,
                                        Datum = DateTime.Parse(dateString),
                                        Eröffnungskurs = open,
                                        Höchstkurs = high,
                                        Tiefstkurs = low,
                                        Schlusskurs = close,
                                        ÄnderungProzent = änderungProzent,
                                        Volumen = volume,
                                        Intervall = "Täglich"
                                    };

                                    historischeDaten.Add(datenpunkt);
                                    Debug.WriteLine($"Datenpunkt hinzugefügt: {dateString} - {close}");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Fehler beim Parsen eines Datenpunkts: {ex.Message}");
                                }
                            }

                            // Sortieren nach Datum (absteigend)
                            historischeDaten = historischeDaten.OrderByDescending(d => d.Datum).ToList();

                            if (historischeDaten.Any())
                            {
                                // Daten in Datenbank speichern
                                try
                                {
                                    await SpeichereHistorischeDatenInDb(historischeDaten);
                                    Debug.WriteLine($"{historischeDaten.Count} Datenpunkte in der Datenbank gespeichert");
                                    StatusText = $"{historischeDaten.Count} Datenpunkte geladen und in Datenbank gespeichert";
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Fehler beim Speichern in der Datenbank: {ex.Message}");
                                    // Fehler beim Speichern werden ignoriert, da es für die Anzeige nicht kritisch ist
                                    StatusText = $"{historischeDaten.Count} Datenpunkte geladen (Speichern fehlgeschlagen)";
                                }

                                // UI aktualisieren
                                AktualisiereDatenAnzeige(historischeDaten);
                                StatusText = $"{historischeDaten.Count} historische Datenpunkte geladen";
                            }
                            else
                            {
                                StatusText = "Keine historischen Daten verfügbar";
                                HistorischeDaten.Clear();
                                AnzahlDatenpunkte = 0;
                                Höchstkurs = 0;
                                Tiefstkurs = 0;
                                Durchschnittskurs = 0;
                            }
                        }
                        else
                        {
                            // Prüfen auf API-Fehler
                            if (doc.RootElement.TryGetProperty("status", out JsonElement statusElement) &&
                                statusElement.GetString() == "error")
                            {
                                string fehlerMsg = doc.RootElement.TryGetProperty("message", out JsonElement msgElement)
                                    ? msgElement.GetString()
                                    : "Unbekannter API-Fehler";

                                Debug.WriteLine($"API-Fehler: {fehlerMsg}");
                                FehlerText = $"API-Fehler: {fehlerMsg}";
                                StatusText = "Fehler beim Laden der Daten";
                            }
                            else
                            {
                                StatusText = "Keine historischen Daten verfügbar";
                            }

                            HistorischeDaten.Clear();
                            AnzahlDatenpunkte = 0;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"HTTP-Fehler: {response.StatusCode}");
                        StatusText = $"HTTP-Fehler: {response.StatusCode}";
                        FehlerText = $"Fehler beim Abrufen der Daten: {response.StatusCode}";
                        HistorischeDaten.Clear();
                        AnzahlDatenpunkte = 0;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"API-Anfrage-Fehler: {ex.Message}");
                    StatusText = "Fehler beim Laden der Daten";
                    FehlerText = $"API-Fehler: {ex.Message}";
                    HistorischeDaten.Clear();
                    AnzahlDatenpunkte = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Aktualisieren der historischen Daten: {ex.Message}");
                FehlerText = $"Fehler: {ex.Message}";
                StatusText = "Fehler beim Laden der Daten";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Lädt historische Daten aus der Datenbank
        /// </summary>
        private async Task<List<HistorischeDatenErweitert>> LadeHistorischeDatenAusDb(int aktieId, DateTime vonDatum, DateTime bisDatum, string intervall)
        {
            return await _context.HistorischeDatenErweitert
                .Where(h => h.AktieId == aktieId &&
                           h.Datum >= vonDatum &&
                           h.Datum <= bisDatum &&
                           h.Intervall == intervall)
                .OrderByDescending(h => h.Datum)
                .ToListAsync();
        }

        /// <summary>
        /// Lädt historische Daten von der API - vereinfacht für die letzten 3 Tage
        /// </summary>
        private async Task LadeHistorischeDatenVonApi()
        {
            try
            {
                StatusText = "Lade historische Daten...";
                IsLoading = true;
                FehlerText = "";

                // Immer tägliche Daten für die letzten 3 Tage laden
                string apiIntervall = "1day";

                // Datum für die letzten 5 Tage setzen
                VonDatum = DateTime.Now.AddDays(-5);
                BisDatum = DateTime.Now;

                Debug.WriteLine($"Lade historische Daten für {AusgewählteAktie?.AktienSymbol} - letzte 5 Tage: {VonDatum:yyyy-MM-dd} bis {BisDatum:yyyy-MM-dd}");

                if (AusgewählteAktie == null)
                {
                    Debug.WriteLine("Keine Aktie ausgewählt");
                    StatusText = "Bitte zuerst eine Aktie auswählen";
                    IsLoading = false;
                    return;
                }

                // Direkt einen HTTP-Request an die TwelveData API senden
                string apiUrl = $"https://api.twelvedata.com/time_series" +
                               $"?symbol={AusgewählteAktie.AktienSymbol}" +
                               $"&interval={apiIntervall}" +
                               $"&start_date={VonDatum.Value:yyyy-MM-dd}" +
                               $"&end_date={BisDatum.Value:yyyy-MM-dd}" +
                               $"&apikey={_twelveDataService.ApiKey}";

                Debug.WriteLine($"API-URL: {apiUrl}");

                var response = await _httpClient.GetAsync(apiUrl);
                var jsonString = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"API-Antwort erhalten: {jsonString.Length} Zeichen");

                // Manuelles Parsen der JSON-Antwort
                List<HistorischeDatenErweitert> historischeDaten = new List<HistorischeDatenErweitert>();

                if (response.IsSuccessStatusCode)
                {
                    // JSON parsen
                    JsonDocument doc = JsonDocument.Parse(jsonString);

                    // Prüfen, ob die Antwort Werte enthält
                    if (doc.RootElement.TryGetProperty("values", out JsonElement valuesElement) &&
                        valuesElement.ValueKind == JsonValueKind.Array)
                    {
                        decimal? lastClose = null;

                        // Durch die Werte iterieren
                        foreach (JsonElement dayElement in valuesElement.EnumerateArray())
                        {
                            try
                            {
                                if (dayElement.TryGetProperty("datetime", out JsonElement datetimeElement) &&
                                    dayElement.TryGetProperty("open", out JsonElement openElement) &&
                                    dayElement.TryGetProperty("high", out JsonElement highElement) &&
                                    dayElement.TryGetProperty("low", out JsonElement lowElement) &&
                                    dayElement.TryGetProperty("close", out JsonElement closeElement))
                                {
                                    // Parsen der Werte
                                    DateTime datum = DateTime.Parse(datetimeElement.GetString());
                                    decimal open = decimal.Parse(openElement.GetString(), System.Globalization.CultureInfo.InvariantCulture);
                                    decimal high = decimal.Parse(highElement.GetString(), System.Globalization.CultureInfo.InvariantCulture);
                                    decimal low = decimal.Parse(lowElement.GetString(), System.Globalization.CultureInfo.InvariantCulture);
                                    decimal close = decimal.Parse(closeElement.GetString(), System.Globalization.CultureInfo.InvariantCulture);

                                    // Volumen, falls vorhanden
                                    long? volume = null;
                                    if (dayElement.TryGetProperty("volume", out JsonElement volumeElement))
                                    {
                                        volume = long.TryParse(volumeElement.GetString(), out long vol) ? vol : null;
                                    }

                                    // Änderungsprozent berechnen
                                    decimal änderungProzent = 0;
                                    if (lastClose.HasValue && lastClose.Value > 0)
                                    {
                                        änderungProzent = (close - lastClose.Value) / lastClose.Value * 100;
                                    }
                                    lastClose = close;

                                    var historischerPunkt = new HistorischeDatenErweitert
                                    {
                                        AktieId = AusgewählteAktie.AktienID,
                                        Datum = datum,
                                        Eröffnungskurs = open,
                                        Höchstkurs = high,
                                        Tiefstkurs = low,
                                        Schlusskurs = close,
                                        ÄnderungProzent = änderungProzent,
                                        Volumen = volume,
                                        Intervall = "Täglich"
                                    };

                                    historischeDaten.Add(historischerPunkt);
                                    Debug.WriteLine($"Datenpunkt hinzugefügt: {datum:yyyy-MM-dd} - {close}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Fehler beim Parsen eines Datenpunkts: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Keine 'values' in der JSON-Antwort gefunden");

                        // Auf Fehlermeldung prüfen
                        if (doc.RootElement.TryGetProperty("status", out JsonElement statusElement) &&
                            statusElement.GetString() == "error" &&
                            doc.RootElement.TryGetProperty("message", out JsonElement messageElement))
                        {
                            Debug.WriteLine($"API-Fehlermeldung: {messageElement.GetString()}");
                            FehlerText = $"API-Fehler: {messageElement.GetString()}";
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"API-Fehler: {response.StatusCode}");
                    StatusText = $"Fehler beim Abrufen der Daten: {response.StatusCode}";
                    FehlerText = $"API-Fehler: {response.StatusCode}";
                }

                if (historischeDaten.Any())
                {
                    // Speichern der Daten in der Datenbank
                    await SpeichereHistorischeDatenInDb(historischeDaten);

                    // Anzeige aktualisieren
                    AktualisiereDatenAnzeige(historischeDaten);
                    StatusText = $"{historischeDaten.Count} historische Datenpunkte geladen";
                }
                else
                {
                    Debug.WriteLine("Keine historischen Daten gefunden");
                    StatusText = "Keine historischen Daten verfügbar";
                    HistorischeDaten.Clear();
                    AnzahlDatenpunkte = 0;
                    Höchstkurs = 0;
                    Tiefstkurs = 0;
                    Durchschnittskurs = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden der API-Daten: {ex.Message}");
                StatusText = "Fehler beim Laden der Daten";
                FehlerText = $"Fehler: {ex.Message}";
                HistorischeDaten.Clear();
                AnzahlDatenpunkte = 0;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Lädt historische Daten mit dem TwelveDataService
        /// </summary>
        private async Task<List<HistorischeDatenErweitert>> LadeHistorischeDatenMitTwelveDataService(string apiIntervall)
        {
            try
            {
                Debug.WriteLine($"Verwende TwelveDataService für {AusgewählteAktie.AktienSymbol}");
                
                var response = await _twelveDataService.GetHistoricalDataAsync(
                    AusgewählteAktie.AktienSymbol,
                    apiIntervall,
                    VonDatum.Value,
                    BisDatum.Value);

                if (response != null && response.TimeSeries != null && response.TimeSeries.Any())
                {
                    Debug.WriteLine($"TwelveDataService lieferte {response.TimeSeries.Count} Datenpunkte");
                    
                    // Konvertieren der API-Daten in unser Datenmodell
                    var historischeDaten = new List<HistorischeDatenErweitert>();
                    decimal? lastClose = null;
                    
                    foreach (var punkt in response.TimeSeries)
                    {
                        decimal änderungProzent = 0;
                        if (lastClose.HasValue && lastClose.Value > 0)
                        {
                            änderungProzent = (punkt.Close - lastClose.Value) / lastClose.Value * 100;
                        }
                        lastClose = punkt.Close;
                        
                        var historischerPunkt = new HistorischeDatenErweitert
                        {
                            AktieId = AusgewählteAktie.AktienID,
                            Datum = DateTime.Parse(punkt.DateTime),
                            Eröffnungskurs = punkt.Open,
                            Höchstkurs = punkt.High,
                            Tiefstkurs = punkt.Low,
                            Schlusskurs = punkt.Close,
                            ÄnderungProzent = änderungProzent,
                            Volumen = punkt.Volume.HasValue ? (long)punkt.Volume.Value : null,
                            Intervall = Intervall
                        };
                        
                        historischeDaten.Add(historischerPunkt);
                    }
                    
                    return historischeDaten;
                }
                
                Debug.WriteLine("TwelveDataService lieferte keine Daten");
                return new List<HistorischeDatenErweitert>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei TwelveDataService: {ex.Message}");
                return new List<HistorischeDatenErweitert>();
            }
        }

        /// <summary>
        /// Lädt historische Daten mit direktem HTTP-Aufruf
        /// </summary>
        private async Task<List<HistorischeDatenErweitert>> LadeHistorischeDatenMitDirektemHttp(string apiIntervall)
        {
            try
            {
                Debug.WriteLine($"Direkter HTTP-Aufruf für {AusgewählteAktie.AktienSymbol}");
                
                // API-URL für TwelveData
                string apiUrl = $"https://api.twelvedata.com/time_series" +
                               $"?symbol={AusgewählteAktie.AktienSymbol}" +
                               $"&interval={apiIntervall}" +
                               $"&start_date={VonDatum.Value:yyyy-MM-dd}" +
                               $"&end_date={BisDatum.Value:yyyy-MM-dd}" +
                               $"&apikey={_twelveDataService.ApiKey}";
                
                var response = await _httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    
                    // Manuelles Parsen der JSON-Antwort
                    JsonDocument doc = JsonDocument.Parse(jsonString);
                    
                    if (doc.RootElement.TryGetProperty("values", out JsonElement valuesElement) && 
                        valuesElement.ValueKind == JsonValueKind.Array)
                    {
                        var historischeDaten = new List<HistorischeDatenErweitert>();
                        decimal? lastClose = null;
                        
                        foreach (JsonElement dayElement in valuesElement.EnumerateArray())
                        {
                            if (dayElement.TryGetProperty("datetime", out JsonElement datetimeElement) &&
                                dayElement.TryGetProperty("open", out JsonElement openElement) &&
                                dayElement.TryGetProperty("high", out JsonElement highElement) &&
                                dayElement.TryGetProperty("low", out JsonElement lowElement) &&
                                dayElement.TryGetProperty("close", out JsonElement closeElement))
                            {
                                // Parsen der Werte
                                DateTime datum = DateTime.Parse(datetimeElement.GetString());
                                decimal open = decimal.Parse(openElement.GetString());
                                decimal high = decimal.Parse(highElement.GetString());
                                decimal low = decimal.Parse(lowElement.GetString());
                                decimal close = decimal.Parse(closeElement.GetString());
                                
                                // Volumen, falls vorhanden
                                long? volume = null;
                                if (dayElement.TryGetProperty("volume", out JsonElement volumeElement))
                                {
                                    volume = long.TryParse(volumeElement.GetString(), out long vol) ? vol : null;
                                }
                                
                                // Änderungsprozent berechnen
                                decimal änderungProzent = 0;
                                if (lastClose.HasValue && lastClose.Value > 0)
                                {
                                    änderungProzent = (close - lastClose.Value) / lastClose.Value * 100;
                                }
                                lastClose = close;
                                
                                var historischerPunkt = new HistorischeDatenErweitert
                                {
                                    AktieId = AusgewählteAktie.AktienID,
                                    Datum = datum,
                                    Eröffnungskurs = open,
                                    Höchstkurs = high,
                                    Tiefstkurs = low,
                                    Schlusskurs = close,
                                    ÄnderungProzent = änderungProzent,
                                    Volumen = volume,
                                    Intervall = Intervall
                                };
                                
                                historischeDaten.Add(historischerPunkt);
                            }
                        }
                        
                        Debug.WriteLine($"HTTP-Anfrage lieferte {historischeDaten.Count} Datenpunkte");
                        return historischeDaten;
                    }
                }
                
                Debug.WriteLine("HTTP-Anfrage lieferte keine Daten");
                return new List<HistorischeDatenErweitert>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei HTTP-Anfrage: {ex.Message}");
                return new List<HistorischeDatenErweitert>();
            }
        }

        /// <summary>
        /// Speichert historische Daten in der Datenbank
        /// </summary>
        private async Task SpeichereHistorischeDatenInDb(List<HistorischeDatenErweitert> historischeDaten)
        {
            try
            {
                if (historischeDaten == null || !historischeDaten.Any()) return;
                
                Debug.WriteLine($"Speichere {historischeDaten.Count} Datenpunkte in der Datenbank");
                
                // Für jeder Datenpunkt prüfen, ob bereits vorhanden
                foreach (var daten in historischeDaten)
                {
                    var existierendeDaten = await _context.HistorischeDatenErweitert
                        .FirstOrDefaultAsync(h => h.AktieId == daten.AktieId && 
                                                h.Datum == daten.Datum && 
                                                h.Intervall == daten.Intervall);
                    
                    if (existierendeDaten != null)
                    {
                        // Daten aktualisieren
                        existierendeDaten.Eröffnungskurs = daten.Eröffnungskurs;
                        existierendeDaten.Höchstkurs = daten.Höchstkurs;
                        existierendeDaten.Tiefstkurs = daten.Tiefstkurs;
                        existierendeDaten.Schlusskurs = daten.Schlusskurs;
                        existierendeDaten.ÄnderungProzent = daten.ÄnderungProzent;
                        existierendeDaten.Volumen = daten.Volumen;
                        existierendeDaten.AktualisiertAm = DateTime.Now;
                    }
                    else
                    {
                        // Neue Daten hinzufügen
                        _context.HistorischeDatenErweitert.Add(daten);
                    }
                }
                
                // Änderungen speichern
                await _context.SaveChangesAsync();
                Debug.WriteLine("Historische Daten erfolgreich gespeichert");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Speichern der historischen Daten: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Aktualisiert die Anzeige mit den geladenen historischen Daten
        /// </summary>
        private void AktualisiereDatenAnzeige(List<HistorischeDatenErweitert> historischeDaten)
        {
            if (historischeDaten == null || !historischeDaten.Any())
            {
                HistorischeDaten.Clear();
                AnzahlDatenpunkte = 0;
                Höchstkurs = 0;
                Tiefstkurs = 0;
                Durchschnittskurs = 0;
                StatusText = "Keine Daten verfügbar";
                Debug.WriteLine("Keine Daten für die Anzeige verfügbar");
                return;
            }

            // Sortieren nach Datum (absteigend)
            var sortierteDaten = historischeDaten.OrderByDescending(d => d.Datum).ToList();

            // UI aktualisieren
            Debug.WriteLine($"Aktualisiere UI mit {sortierteDaten.Count} Datenpunkten");
            HistorischeDaten.Clear();
            foreach (var daten in sortierteDaten)
            {
                HistorischeDaten.Add(daten);
                Debug.WriteLine($"Hinzugefügt: {daten.Datum:yyyy-MM-dd} - Schluss: {daten.Schlusskurs:F2}, Änderung: {daten.ÄnderungProzent:F2}%");
            }

            // Statistiken berechnen
            AnzahlDatenpunkte = HistorischeDaten.Count;
            Debug.WriteLine($"AnzahlDatenpunkte gesetzt: {AnzahlDatenpunkte}");

            // Höchstkurs ermitteln
            var höchsterPunkt = sortierteDaten.OrderByDescending(d => d.Höchstkurs).FirstOrDefault();
            if (höchsterPunkt != null)
            {
                Höchstkurs = höchsterPunkt.Höchstkurs;
                HöchstkursDatum = höchsterPunkt.Datum;
                Debug.WriteLine($"Höchstkurs: {Höchstkurs:F2} am {HöchstkursDatum:yyyy-MM-dd}");
            }

            // Tiefstkurs ermitteln
            var tiefsterPunkt = sortierteDaten.OrderBy(d => d.Tiefstkurs).FirstOrDefault();
            if (tiefsterPunkt != null)
            {
                Tiefstkurs = tiefsterPunkt.Tiefstkurs;
                TiefstkursDatum = tiefsterPunkt.Datum;
                Debug.WriteLine($"Tiefstkurs: {Tiefstkurs:F2} am {TiefstkursDatum:yyyy-MM-dd}");
            }

            // Durchschnittskurs berechnen
            Durchschnittskurs = sortierteDaten.Count > 0
                ? sortierteDaten.Average(d => d.Schlusskurs)
                : 0;
            Debug.WriteLine($"Durchschnittskurs: {Durchschnittskurs:F2}");

            // Gesamtänderung und Volatilität werden nicht mehr berechnet, da sie aus der UI entfernt wurden
            Debug.WriteLine("Gesamtänderung und Volatilität werden nicht mehr berechnet");

            StatusText = $"{AnzahlDatenpunkte} Datenpunkte geladen";
            Debug.WriteLine($"StatusText gesetzt: {StatusText}");
        }

        // Export- und Druckfunktionalität auf Wunsch entfernt

        /// <summary>
        /// Speichert aktuelle Kursdaten der ausgewählten Aktie in der Datenbank
        /// Methode zum Speichern der aktuellen Kurse aus dem MainViewModel
        /// </summary>
        [RelayCommand]
        private async Task SpeichereAktuelleKurseAlsHistorie()
        {
            try
            {
                if (AusgewählteAktie == null)
                {
                    StatusText = "Keine Aktie ausgewählt";
                    FehlerText = "Sie müssen eine Aktie auswählen, um aktuelle Kurse zu speichern.";
                    return;
                }

                IsLoading = true;
                StatusText = $"Speichere aktuelle Kurse für {AusgewählteAktie.AktienSymbol}...";

                // Leere Datenstruktur für historische Daten vorbereiten
                var historischeDaten = new List<HistorischeDatenErweitert>();

                // Aktuelle Kursdaten als historische Daten speichern
                var historischerPunkt = new HistorischeDatenErweitert
                {
                    AktieId = AusgewählteAktie.AktienID,
                    Datum = DateTime.Now,
                    Eröffnungskurs = AusgewählteAktie.AktuellerPreis,
                    Höchstkurs = AusgewählteAktie.AktuellerPreis,
                    Tiefstkurs = AusgewählteAktie.AktuellerPreis,
                    Schlusskurs = AusgewählteAktie.AktuellerPreis,
                    ÄnderungProzent = AusgewählteAktie.ÄnderungProzent,
                    Intervall = "Täglich"
                };

                historischeDaten.Add(historischerPunkt);

                // In Datenbank speichern
                await SpeichereHistorischeDatenInDb(historischeDaten);
                Debug.WriteLine($"Aktuelle Kurse für {AusgewählteAktie.AktienSymbol} als historische Daten gespeichert");

                // UI aktualisieren
                await Aktualisieren();
                StatusText = $"Aktuelle Kurse für {AusgewählteAktie.AktienSymbol} gespeichert";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Speichern der aktuellen Kurse: {ex.Message}");
                FehlerText = $"Fehler: {ex.Message}";
                StatusText = "Fehler beim Speichern der aktuellen Kurse";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}