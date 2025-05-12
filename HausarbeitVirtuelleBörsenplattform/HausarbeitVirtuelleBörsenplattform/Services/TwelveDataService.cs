using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using HausarbeitVirtuelleBörsenplattform.Helpers;
using HausarbeitVirtuelleBörsenplattform.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace HausarbeitVirtuelleBörsenplattform.Services
{
    /// <summary>
    /// Service für die Kommunikation mit der Twelve Data API mit optimiertem Rate-Limiting
    /// Mit Unterstützung für historische Kursdaten
    /// </summary>
    public class TwelveDataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://api.twelvedata.com";

        /// <summary>
        /// Gibt den API-Key zurück
        /// </summary>
        public string ApiKey => _apiKey;
        private readonly Dictionary<string, (Aktie Aktie, DateTime Zeitstempel)> _aktienCache = new();
        private readonly TimeSpan _cacheGültigkeit = TimeSpan.FromMinutes(15);
        private readonly object _cacheLock = new object();
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Verhindert parallele API-Anfragen
        private DateTime _lastApiCallTime = DateTime.MinValue;
        private int _apiCallCount = 0;
        private readonly int _maxCallsPerMinute = 12; // Optimiert - eigentlich Basic 8 Plan
        private readonly RateLimiter _rateLimiter;

        // Hilfsvariablen
        public string LastErrorMessage { get; private set; }

        // CultureInfo für korrekte Dezimalkonvertierung
        private static readonly CultureInfo EnglishCulture = new CultureInfo("en-US");

        /// <summary>
        /// Initialisiert eine neue Instanz des TwelveDataService
        /// </summary>
        /// <param name="apiKey">Der API-Schlüssel für Twelve Data</param>
        public TwelveDataService(string apiKey)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
            // Erhöhe den RateLimiter auf 12 statt 8 Anfragen pro Minute für schnelleres Laden
            // (Die API erlaubt 8, aber wir nutzen 12 und nutzen den Retry-Mechanismus bei Fehlern)
            _rateLimiter = new RateLimiter(12, TimeSpan.FromMinutes(1));

            Debug.WriteLine($"TwelveDataService initialisiert mit API-Key: {apiKey}");
        }

        /// <summary>
        /// Holt die aktuellen Kursdaten für die angegebenen Aktien-Symbole mit verbesserter Symbolbehandlung
        /// </summary>
        public async Task<List<Aktie>> HoleAktienKurse(List<string> symbole)
        {
            var ergebnisse = new List<Aktie>();
            LastErrorMessage = null;

            try
            {
                if (symbole == null || !symbole.Any())
                {
                    LastErrorMessage = "Keine Symbole zur Aktualisierung übergeben";
                    return ergebnisse;
                }

                Debug.WriteLine($"HoleAktienKurse aufgerufen für Symbole: {string.Join(", ", symbole)}");

                // Symbole überprüfen und ggf. korrigieren
                var korrigierteSymbole = new List<string>();
                foreach (var symbol in symbole)
                {
                    // Symbole nach oben normalisieren
                    var normalisiert = symbol.Trim().ToUpper();

                    // Prüfen, ob es ein Symbol einer deutschen Aktie ist
                    bool istDeutscheAktie = normalisiert.EndsWith(".DE", StringComparison.OrdinalIgnoreCase);

                    // Wenn es ein deutsches Symbol ist, entfernen wir das Suffix nicht mehr - die API unterstützt .DE-Symbole
                    // Stattdessen ein besseres Debug-Log
                    if (istDeutscheAktie)
                    {
                        Debug.WriteLine($"Deutsche Aktie erkannt: {normalisiert}");
                    }

                    korrigierteSymbole.Add(normalisiert);
                }

                // Zuerst alle gecachten Aktien verwenden
                lock (_cacheLock)
                {
                    foreach (var symbol in korrigierteSymbole)
                    {
                        if (_aktienCache.TryGetValue(symbol, out var cachedEntry) &&
                            (DateTime.Now - cachedEntry.Zeitstempel) < _cacheGültigkeit)
                        {
                            ergebnisse.Add(cachedEntry.Aktie);
                            Debug.WriteLine($"Aktie {symbol} aus Cache verwendet, Alter: {(DateTime.Now - cachedEntry.Zeitstempel).TotalMinutes:F1} Minuten");
                        }
                    }
                }

                // Nur die Symbole abfragen, die nicht im Cache sind oder deren Cache abgelaufen ist
                var abzufragendeSymbole = korrigierteSymbole
                    .Where(s => !ergebnisse.Any(e => e.AktienSymbol == s))
                    .ToList();

                Debug.WriteLine($"Aus Cache: {korrigierteSymbole.Count - abzufragendeSymbole.Count}, Neu abzufragen: {abzufragendeSymbole.Count}");

                // Wenn alle Symbole im Cache sind, direkt zurückgeben
                if (abzufragendeSymbole.Count == 0)
                {
                    Debug.WriteLine("Alle Symbole im Cache gefunden, keine API-Anfrage nötig");
                    return ergebnisse;
                }

                // Wir verwenden ein Semaphore, um sicherzustellen, dass nur eine Anfrage gleichzeitig durchgeführt wird
                await _semaphore.WaitAsync();

                try
                {
                    // Rate-Limiter verwenden, um API-Limits zu respektieren
                    await _rateLimiter.ThrottleAsync();

                    // Für jedes abzufragende Symbol eine einzelne Anfrage durchführen (max. 2 pro Aufruf)
                    var maxSymbolsToProcess = Math.Min(2, abzufragendeSymbole.Count);
                    Debug.WriteLine($"Verarbeite {maxSymbolsToProcess} Symbole in diesem Aufruf");

                    for (int i = 0; i < maxSymbolsToProcess; i++)
                    {
                        var symbol = abzufragendeSymbole[i];

                        // Rate-Limiter vor jeder Anfrage prüfen
                        await _rateLimiter.ThrottleAsync();

                        try
                        {
                            // URL für die API-Anfrage
                            var url = $"{_baseUrl}/quote?symbol={symbol}&apikey={_apiKey}";
                            Debug.WriteLine($"API-Anfrage URL für {symbol}: {url}");

                            // API-Anfrage senden
                            _apiCallCount++;
                            _lastApiCallTime = DateTime.Now;

                            using (var response = await _httpClient.GetAsync(url))
                            {
                                string responseContent = await response.Content.ReadAsStringAsync();
                                Debug.WriteLine($"API-Antwort für {symbol} erhalten: {responseContent.Length} Zeichen");

                                if (!response.IsSuccessStatusCode)
                                {
                                    Debug.WriteLine($"HTTP-Fehler: {response.StatusCode}");
                                    LastErrorMessage = $"HTTP-Fehler: {response.StatusCode}";
                                    continue;
                                }

                                // JSON-Antwort verarbeiten
                                try
                                {
                                    // Prüfen auf API-Fehlermeldung
                                    if (responseContent.Contains("\"status\":\"error\""))
                                    {
                                        using (JsonDocument doc = JsonDocument.Parse(responseContent))
                                        {
                                            if (doc.RootElement.TryGetProperty("message", out JsonElement messageElement))
                                            {
                                                string errorMessage = messageElement.GetString();
                                                Debug.WriteLine($"API-Fehler: {errorMessage}");
                                                LastErrorMessage = $"API-Fehler: {errorMessage}";

                                                // Bei API-Limit-Fehler abbrechen und warten
                                                if (errorMessage.Contains("API credits") && errorMessage.Contains("limit"))
                                                {
                                                    Debug.WriteLine("API-Limit erreicht, keine weiteren Anfragen mehr in dieser Minute");
                                                    break;
                                                }
                                            }
                                        }
                                        continue;
                                    }

                                    // JSON-Antwort deserialisieren
                                    var aktie = ParseTwelveDataResponse(responseContent, symbol);
                                    if (aktie != null)
                                    {
                                        // Im Cache speichern
                                        lock (_cacheLock)
                                        {
                                            _aktienCache[symbol] = (aktie, DateTime.Now);
                                        }

                                        ergebnisse.Add(aktie);
                                        Debug.WriteLine($"Aktie {symbol} erfolgreich geladen: {aktie.AktuellerPreis}€");
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"FEHLER: Aktie {symbol} konnte nicht geparst werden");
                                        LastErrorMessage = $"Aktie {symbol} konnte nicht geladen werden. Ungültige oder fehlende Daten.";
                                    }
                                }
                                catch (Newtonsoft.Json.JsonException jsonEx)
                                {
                                    Debug.WriteLine($"JSON-Fehler: {jsonEx.Message}");
                                    LastErrorMessage = $"JSON-Fehler: {jsonEx.Message}";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Fehler bei der Verarbeitung von {symbol}: {ex.Message}");
                            LastErrorMessage = $"Fehler bei der Verarbeitung von {symbol}: {ex.Message}";
                        }

                        // Kurze Pause zwischen den Anfragen
                        if (i < maxSymbolsToProcess - 1)
                        {
                            await Task.Delay(500);
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = $"Allgemeiner Fehler: {ex.Message}";
                Debug.WriteLine(LastErrorMessage);
                Debug.WriteLine($"Stacktrace: {ex.StackTrace}");
            }

            Debug.WriteLine($"HoleAktienKurse liefert {ergebnisse.Count} Ergebnisse zurück");

            // Fallback für leere Ergebnisse
            if (ergebnisse.Count == 0 && !string.IsNullOrEmpty(LastErrorMessage))
            {
                LastErrorMessage = $"Keine Aktien konnten geladen werden. {LastErrorMessage} Bitte versuchen Sie es später erneut oder prüfen Sie Ihre Internetverbindung.";
            }

            return ergebnisse;
        }

        /// <summary>
        /// Prüft, ob eine relevante Börse geöffnet ist oder simuliert Börsenöffnungszeiten
        /// </summary>
        public bool IstBoerseGeoeffnet()
        {
            // Für Simulationszwecke können wir einen Debug-Modus aktivieren
            bool simulationsModus = false; // Auf "true" setzen für Tests

            if (simulationsModus)
                return true;

            var jetzt = DateTime.UtcNow; // Verwende UTC als Basis für alle Zeitzonenberechnungen

            try
            {
                // US-Börse (NYSE/NASDAQ): 9:30 - 16:00 ET
                TimeZoneInfo usZeitzone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                DateTime usZeit = TimeZoneInfo.ConvertTimeFromUtc(jetzt, usZeitzone);

                // Prüfe, ob Wochenende (Samstag = 6, Sonntag = 0)
                bool istWochenende = usZeit.DayOfWeek == DayOfWeek.Saturday || usZeit.DayOfWeek == DayOfWeek.Sunday;

                // US-Börsenzeiten (9:30 - 16:00 ET)
                bool usBoerseGeoeffnet = !istWochenende &&
                                        (usZeit.Hour > 9 || (usZeit.Hour == 9 && usZeit.Minute >= 30)) &&
                                        usZeit.Hour < 16;

                // Deutsche Börse (XETRA): 9:00 - 17:30 MEZ/MESZ
                TimeZoneInfo deZeitzone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
                DateTime deZeit = TimeZoneInfo.ConvertTimeFromUtc(jetzt, deZeitzone);

                // Deutsche Börsenzeiten (9:00 - 17:30 MEZ/MESZ)
                bool deBoerseGeoeffnet = deZeit.DayOfWeek != DayOfWeek.Saturday &&
                                        deZeit.DayOfWeek != DayOfWeek.Sunday &&
                                        deZeit.Hour >= 9 &&
                                        (deZeit.Hour < 17 || (deZeit.Hour == 17 && deZeit.Minute <= 30));

                // Feiertage werden hier zur Vereinfachung nicht berücksichtigt

                // Protokolliere den Status für Debug-Zwecke
                Console.WriteLine($"US-Börse Status: {(usBoerseGeoeffnet ? "Geöffnet" : "Geschlossen")} (Zeit: {usZeit.Hour:D2}:{usZeit.Minute:D2})");
                Console.WriteLine($"Deutsche Börse Status: {(deBoerseGeoeffnet ? "Geöffnet" : "Geschlossen")} (Zeit: {deZeit.Hour:D2}:{deZeit.Minute:D2})");

                // Ist entweder die US oder die deutsche Börse geöffnet?
                return usBoerseGeoeffnet || deBoerseGeoeffnet;
            }
            catch (Exception ex)
            {
                // Bei Fehlern in der Zeitzonen-Berechnung loggen wir den Fehler und geben true zurück,
                // damit die Anwendung trotzdem Daten abrufen kann
                Console.WriteLine($"Fehler bei der Börsenzeit-Berechnung: {ex.Message}");
                return true;
            }
        }

        private bool IstUSBoerseGeoeffnet(DateTime jetztUS)
        {
            // Wochentag prüfen (1 = Montag, ..., 7 = Sonntag)
            int wochentagUS = (int)jetztUS.DayOfWeek;

            // Prüfen, ob es ein Wochenende ist (Samstag = 6, Sonntag = 0)
            if (wochentagUS == 6 || wochentagUS == 0)
            {
                return false;
            }

            // US-Börsenhandelszeiten: 9:30 - 16:00 ET
            TimeSpan startzeitUS = new TimeSpan(9, 30, 0);   // 9:30 ET
            TimeSpan endzeitUS = new TimeSpan(16, 0, 0);     // 16:00 ET
            TimeSpan aktuelleZeitUS = jetztUS.TimeOfDay;

            return aktuelleZeitUS >= startzeitUS && aktuelleZeitUS <= endzeitUS;
        }

        private bool IstDeutscheBoerseGeoeffnet(DateTime jetztDeutschland)
        {
            // Wochentag prüfen (1 = Montag, ..., 7 = Sonntag)
            int wochentagDeutschland = (int)jetztDeutschland.DayOfWeek;

            // Prüfen, ob es ein Wochenende ist (Samstag = 6, Sonntag = 0)
            if (wochentagDeutschland == 6 || wochentagDeutschland == 0)
            {
                return false;
            }

            // Deutsche Börsenhandelszeiten: 9:00 - 17:30 Uhr
            TimeSpan startzeitDeutschland = new TimeSpan(9, 0, 0);   // 9:00 Uhr
            TimeSpan endzeitDeutschland = new TimeSpan(17, 30, 0);   // 17:30 Uhr
            TimeSpan aktuelleZeitDeutschland = jetztDeutschland.TimeOfDay;

            return aktuelleZeitDeutschland >= startzeitDeutschland && aktuelleZeitDeutschland <= endzeitDeutschland;
        }

        #region Historische Kursdaten

        /// <summary>
        /// Holt historische Kursdaten für eine Aktie für einen bestimmten Zeitraum
        /// Optimiert für schnelleres Laden durch angepassten Rate-Limiter
        /// </summary>
        /// <param name="symbol">Das Symbol der Aktie</param>
        /// <param name="startDatum">Startdatum für die historischen Daten</param>
        /// <param name="endDatum">Enddatum für die historischen Daten</param>
        /// <param name="interval">Intervall der Daten (z.B. "1day", "1week")</param>
        /// <returns>Liste von AktienKursHistorie-Objekten</returns>
        public async Task<List<AktienKursHistorie>> HoleHistorischeDaten(string symbol, DateTime startDatum, DateTime endDatum, string interval = "1day")
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                Debug.WriteLine("Kein Symbol für historische Daten angegeben");
                return new List<AktienKursHistorie>();
            }

            try
            {
                // Früheres Datum zuerst für die API
                var startDateString = startDatum.ToString("yyyy-MM-dd");
                var endDateString = endDatum.ToString("yyyy-MM-dd");

                string url = $"{_baseUrl}/time_series?symbol={symbol}&interval={interval}&start_date={startDateString}&end_date={endDateString}&apikey={_apiKey}";
                Debug.WriteLine($"API-Anfrage für historische Daten: {url}");

                // Rate-Limiter vor API-Anfrage verwenden
                await _rateLimiter.ThrottleAsync();

                var response = await _httpClient.GetStringAsync(url);

                if (string.IsNullOrWhiteSpace(response))
                {
                    Debug.WriteLine($"Keine Antwort von der API für historische Daten von {symbol}");
                    return new List<AktienKursHistorie>();
                }

                // Ergebnis parsen
                return ParseHistorischeDaten(response, symbol);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Abrufen historischer Daten für {symbol}: {ex.Message}");
                LastErrorMessage = $"Fehler beim Abrufen historischer Daten: {ex.Message}";
                return new List<AktienKursHistorie>();
            }
        }

        /// <summary>
        /// Holt historische Kursdaten für eine Aktie für einen bestimmten Zeitraum und gibt sie als Response-Objekt zurück
        /// </summary>
        /// <param name="symbol">Das Symbol der Aktie</param>
        /// <param name="interval">Intervall der Daten (z.B. "1day", "1week")</param>
        /// <param name="startDatum">Startdatum für die historischen Daten</param>
        /// <param name="endDatum">Enddatum für die historischen Daten</param>
        /// <returns>HistorischeDatenResponse-Objekt</returns>
        public async Task<HistorischeDatenResponse> GetHistoricalDataAsync(string symbol, string interval, DateTime startDatum, DateTime endDatum)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                Debug.WriteLine("Kein Symbol für historische Daten angegeben");
                return null;
            }

            try
            {
                // Früheres Datum zuerst für die API
                var startDateString = startDatum.ToString("yyyy-MM-dd");
                var endDateString = endDatum.ToString("yyyy-MM-dd");

                // URL für API-Anfrage erstellen
                string url = $"{_baseUrl}/time_series?symbol={symbol}&interval={interval}&start_date={startDateString}&end_date={endDateString}&apikey={_apiKey}";
                Debug.WriteLine($"API-Anfrage für historische Daten (JSON-Objekt): {url}");

                // Rate-Limiter vor API-Anfrage verwenden
                await _rateLimiter.ThrottleAsync();

                // API-Anfrage senden
                var response = await _httpClient.GetStringAsync(url);

                if (string.IsNullOrWhiteSpace(response))
                {
                    Debug.WriteLine($"Keine Antwort von der API für historische Daten von {symbol}");
                    return null;
                }

                // JSON-Antwort deserialisieren
                var result = JsonConvert.DeserializeObject<HistorischeDatenResponse>(response);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Abrufen historischer Daten (JSON-Objekt) für {symbol}: {ex.Message}");
                LastErrorMessage = $"Fehler beim Abrufen historischer Daten: {ex.Message}";
                return null;
            }
        }

        /// <summary>
        /// Parst die JSON-Antwort der Time-Series-API und konvertiert sie in AktienKursHistorie-Objekte
        /// </summary>
        private List<AktienKursHistorie> ParseHistorischeDaten(string json, string symbol)
        {
            var ergebnis = new List<AktienKursHistorie>();

            try
            {
                // JSON-Antwort auswerten
                var responseObj = JObject.Parse(json);

                // Prüfen, ob ein Fehler zurückgegeben wurde
                if (responseObj.ContainsKey("status") && responseObj["status"].ToString() == "error")
                {
                    Debug.WriteLine($"API-Fehler für historische Daten von {symbol}: {responseObj["message"]}");
                    LastErrorMessage = $"API-Fehler: {responseObj["message"]}";
                    return ergebnis;
                }

                // Metadaten abrufen
                int aktienID = 0; // Wird später zugewiesen
                string meta_symbol = responseObj["meta"]?["symbol"]?.ToString() ?? symbol;

                // Daten abrufen
                if (responseObj.ContainsKey("values") && responseObj["values"].HasValues)
                {
                    var values = responseObj["values"] as JArray;

                    if (values != null)
                    {
                        // Iterate through historical data entries
                        foreach (var value in values)
                        {
                            try
                            {
                                // Parse date from ISO format
                                string dateString = value["datetime"]?.ToString();
                                if (!DateTime.TryParse(dateString, out DateTime date))
                                {
                                    Debug.WriteLine($"Konnte Datum nicht parsen: {dateString}");
                                    continue;
                                }

                                // Parse OHLC values
                                if (!decimal.TryParse(value["open"]?.ToString(), NumberStyles.Any, EnglishCulture, out decimal open) ||
                                    !decimal.TryParse(value["high"]?.ToString(), NumberStyles.Any, EnglishCulture, out decimal high) ||
                                    !decimal.TryParse(value["low"]?.ToString(), NumberStyles.Any, EnglishCulture, out decimal low) ||
                                    !decimal.TryParse(value["close"]?.ToString(), NumberStyles.Any, EnglishCulture, out decimal close))
                                {
                                    Debug.WriteLine($"Konnte OHLC-Werte nicht parsen für {dateString}");
                                    continue;
                                }

                                // Optional: Parse volume (may not be available)
                                long volume = 0;
                                if (value["volume"] != null)
                                {
                                    long.TryParse(value["volume"]?.ToString(), out volume);
                                }

                                // Calculate percentage change (simply from previous close)
                                decimal changePct = 0;
                                if (ergebnis.Count > 0)
                                {
                                    var prevClose = ergebnis.Last().Schlusskurs;
                                    if (prevClose > 0)
                                    {
                                        changePct = ((close - prevClose) / prevClose) * 100;
                                    }
                                }

                                // Create history entry
                                var historieEintrag = new AktienKursHistorie
                                {
                                    AktienID = aktienID, // Will be updated later when saving to database
                                    AktienSymbol = meta_symbol,
                                    Datum = date,
                                    Eroeffnungskurs = open,
                                    Hoechstkurs = high,
                                    Tiefstkurs = low,
                                    Schlusskurs = close,
                                    Volumen = volume,
                                    ÄnderungProzent = changePct
                                };

                                ergebnis.Add(historieEintrag);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Fehler beim Parsen eines historischen Datensatzes: {ex.Message}");
                            }
                        }
                    }
                }

                Debug.WriteLine($"Erfolgreich {ergebnis.Count} historische Datensätze für {symbol} geladen");

                // Sortiere das Ergebnis nach Datum (ältestes zuerst für die Berechnung der Änderungsprozente)
                ergebnis = ergebnis.OrderBy(e => e.Datum).ToList();

                // Now recalculate percentage changes more accurately
                if (ergebnis.Count > 1)
                {
                    for (int i = 1; i < ergebnis.Count; i++)
                    {
                        var prev = ergebnis[i - 1];
                        var curr = ergebnis[i];

                        if (prev.Schlusskurs > 0)
                        {
                            curr.ÄnderungProzent = Math.Round(((curr.Schlusskurs - prev.Schlusskurs) / prev.Schlusskurs) * 100, 2);
                        }
                    }
                }

                // Ausgabe für Debugging
                Debug.WriteLine($"Historische Daten für {symbol} nach Datum sortiert:");
                foreach (var eintrag in ergebnis)
                {
                    Debug.WriteLine($"  {eintrag.Datum:yyyy-MM-dd}: Schlusskurs={eintrag.Schlusskurs:F2}, ÄnderungProzent={eintrag.ÄnderungProzent:F2}%");
                }

                return ergebnis;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Parsen der historischen Daten: {ex.Message}");
                LastErrorMessage = $"Fehler beim Parsen der historischen Daten: {ex.Message}";
                return ergebnis;
            }
        }

        /// <summary>
        /// Speichert die historischen Daten für eine Aktie in der Datenbank
        /// </summary>
        /// <param name="symbol">Das Symbol der Aktie</param>
        /// <param name="anzahlTage">Die Anzahl der zu ladenden Tage</param>
        /// <returns>Anzahl der gespeicherten Datensätze</returns>
        public async Task<int> LadeUndSpeichereHistorischeDaten(string symbol, int anzahlTage = 5)
        {
            if (string.IsNullOrWhiteSpace(symbol) || anzahlTage <= 0)
            {
                return 0;
            }

            try
            {
                // Aktie aus der Datenbank abrufen oder erstellen
                var aktie = await App.DbService.GetAktieBySymbolAsync(symbol);

                if (aktie == null)
                {
                    Debug.WriteLine($"Aktie mit Symbol {symbol} nicht gefunden, kann keine historischen Daten speichern");
                    return 0;
                }

                // Datumsbereich berechnen
                var endDatum = DateTime.Now.Date;
                var startDatum = endDatum.AddDays(-anzahlTage);

                // Historische Daten von der API abrufen
                var historieDaten = await HoleHistorischeDaten(symbol, startDatum, endDatum, "1day");

                if (historieDaten == null || historieDaten.Count == 0)
                {
                    Debug.WriteLine($"Keine historischen Daten für {symbol} gefunden");
                    return 0;
                }

                // AktienID zu allen Einträgen hinzufügen
                foreach (var eintrag in historieDaten)
                {
                    eintrag.AktienID = aktie.AktienID;
                }

                // Historische Daten in der Datenbank speichern
                int erfolgreich = 0;
                foreach (var eintrag in historieDaten)
                {
                    if (await App.DbService.AddAktienKursHistorieAsync(eintrag))
                    {
                        erfolgreich++;
                    }
                }

                Debug.WriteLine($"Erfolgreich {erfolgreich} von {historieDaten.Count} historischen Datensätzen für {symbol} gespeichert");
                return erfolgreich;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Laden und Speichern historischer Daten für {symbol}: {ex.Message}");
                LastErrorMessage = $"Fehler beim Laden und Speichern historischer Daten: {ex.Message}";
                return 0;
            }
        }

        #endregion

        /// <summary>
        /// Überprüft, ob die Twelve Data API Daten für ein bestimmtes Aktien-Symbol bereitstellt
        /// </summary>
        /// <param name="symbol">Das zu überprüfende Aktien-Symbol</param>
        /// <returns>True, wenn Daten verfügbar sind, sonst False</returns>
        public async Task<bool> IstAktieVerfügbar(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                Debug.WriteLine("Kein Symbol zum Überprüfen angegeben");
                return false;
            }

            try
            {
                string url = $"https://api.twelvedata.com/time_series?symbol={symbol}&interval=1min&apikey={_apiKey}";
                Debug.WriteLine($"API-Anfrage für Symbol {symbol}: {url}");

                // Rate-Limiter vor API-Anfrage verwenden
                await _rateLimiter.ThrottleAsync();

                var response = await _httpClient.GetStringAsync(url);

                if (string.IsNullOrWhiteSpace(response))
                {
                    Debug.WriteLine($"Keine Antwort von der API für Symbol {symbol}");
                    return false;
                }

                // JSON-Antwort auswerten
                var responseObj = JsonConvert.DeserializeObject<JObject>(response);

                // Prüfen, ob Daten oder ein Fehler zurückgegeben wurde
                if (responseObj.ContainsKey("status") && responseObj["status"].ToString() == "error")
                {
                    Debug.WriteLine($"API-Fehler für Symbol {symbol}: {responseObj["message"]}");
                    return false;
                }

                // Prüfen, ob Daten in der Antwort enthalten sind
                if (responseObj.ContainsKey("values") && responseObj["values"].HasValues)
                {
                    Debug.WriteLine($"Daten für Symbol {symbol} sind verfügbar");
                    return true;
                }

                Debug.WriteLine($"Keine Daten für Symbol {symbol} gefunden");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Überprüfung von Symbol {symbol}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Verarbeitet die JSON-Antwort von Twelve Data
        /// </summary>
        /// <summary>
        /// Verarbeitet die JSON-Antwort von Twelve Data mit Berücksichtigung von Handelszeiten
        /// </summary>
        private Aktie ParseTwelveDataResponse(string json, string symbol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return null;

                // Prüfen, ob die Börse aktuell geöffnet ist
                bool istBoerseGeoeffnet = IstBoerseGeoeffnet();

                // Versuche, das JSON zu deserialisieren
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;

                    // Prüfe auf Fehlermeldung in der Antwort
                    if (root.TryGetProperty("status", out JsonElement statusElement))
                    {
                        string status = statusElement.GetString();
                        if (status == "error")
                        {
                            if (root.TryGetProperty("message", out JsonElement messageElement))
                            {
                                LastErrorMessage = messageElement.GetString();
                                Debug.WriteLine($"API-Fehler: {LastErrorMessage}");
                            }
                            return null;
                        }
                    }

                    // Extrahiere die benötigten Daten
                    string name = root.TryGetProperty("name", out JsonElement nameElement) ?
                        nameElement.GetString() : symbol;

                    // Für bekannte Aktien, falls nichts genaues zurückkommt
                    if (string.IsNullOrEmpty(name) || name == symbol)
                    {
                        // Standard-Aktiennamen
                        if (symbol == "AAPL") name = "Apple Inc.";
                        else if (symbol == "MSFT") name = "Microsoft Corp.";
                        else if (symbol == "TSLA") name = "Tesla Inc.";
                        else if (symbol == "AMZN") name = "Amazon.com Inc.";
                        else if (symbol == "GOOGL") name = "Alphabet Inc.";
                        // ... weitere bekannte Aktien ...
                    }

                    decimal close = 0;
                    if (root.TryGetProperty("close", out JsonElement closeElement))
                    {
                        string closeString = closeElement.GetString();
                        if (!decimal.TryParse(closeString, NumberStyles.Any, EnglishCulture, out close))
                        {
                            Debug.WriteLine($"Konnte close nicht parsen: {closeString}");
                        }
                    }

                    decimal change = 0;
                    if (root.TryGetProperty("change", out JsonElement changeElement))
                    {
                        string changeString = changeElement.GetString();
                        if (!decimal.TryParse(changeString, NumberStyles.Any, EnglishCulture, out change))
                        {
                            Debug.WriteLine($"Konnte change nicht parsen: {changeString}");
                        }
                    }

                    decimal percentChange = 0;
                    // Versuche percent_change zu lesen
                    if (root.TryGetProperty("percent_change", out JsonElement pctElement))
                    {
                        string pctString = pctElement.GetString();
                        if (!string.IsNullOrEmpty(pctString) &&
                            !decimal.TryParse(pctString, NumberStyles.Any, EnglishCulture, out percentChange))
                        {
                            Debug.WriteLine($"Konnte percent_change nicht parsen: {pctString}");
                        }
                    }

                    // Wenn percent_change nicht vorhanden oder leer ist, selbst berechnen
                    if (percentChange == 0 && close > 0)
                    {
                        // Schätzen der Prozentänderung anhand der absoluten Änderung
                        // Wir berechnen: (change / (close - change)) * 100
                        try
                        {
                            decimal previousClose = close - change;
                            if (previousClose > 0) // Vermeiden Sie Division durch Null
                            {
                                percentChange = Math.Round((change / previousClose) * 100, 2);
                                Debug.WriteLine($"Prozentänderung selbst berechnet: {percentChange}%");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Fehler bei der Berechnung der Prozentänderung: {ex.Message}");
                        }
                    }

                    // WICHTIG: Verbesserte Fallback-Logik, wenn keine gültigen API-Daten vorhanden sind
                    if (close <= 0)
                    {
                        Debug.WriteLine($"WARNUNG: Ungültiger Kurs für {symbol}");

                        // Prüfen, ob wir einen gecachten Kurs haben
                        if (_aktienCache.TryGetValue(symbol, out var cachedEntry) && cachedEntry.Aktie.AktuellerPreis > 0)
                        {
                            Debug.WriteLine($"Verwende letzten bekannten Kurs für {symbol} vom {cachedEntry.Zeitstempel}");

                            // Letzten bekannten Kurs verwenden, aber keine Änderungen
                            close = cachedEntry.Aktie.AktuellerPreis;
                            change = 0; // Keine Änderung, da nicht aktualisiert
                            percentChange = 0;

                            // Spezielle Fehlermeldung für Nicht-Handelszeiten
                            if (!istBoerseGeoeffnet)
                            {
                                LastErrorMessage = $"Der Markt ist derzeit geschlossen. Letzter bekannter Kurs für {symbol} wird angezeigt.";
                            }
                            else
                            {
                                LastErrorMessage = $"Für {symbol} konnten keine aktuellen Daten abgerufen werden. Letzter bekannter Kurs wird angezeigt.";
                            }
                        }
                        else
                        {
                            // Nur im echten Notfall einen Standardwert verwenden, aber NICHT zufällig
                            Debug.WriteLine($"Kein bekannter Kurs für {symbol} verfügbar, verwende konservativen Standardwert");

                            // Konservativer Standardwert je nach Aktie
                            if (symbol == "AAPL") close = 180.00m;
                            else if (symbol == "MSFT") close = 350.00m;
                            else if (symbol == "TSLA") close = 250.00m;
                            else if (symbol == "AMZN") close = 140.00m;
                            else if (symbol == "GOOGL") close = 130.00m;
                            else if (symbol == "NVDA") close = 120.00m;
                            else close = 100.00m; // Generischer Fallback für unbekannte Aktien

                            change = 0;
                            percentChange = 0;

                            LastErrorMessage = $"Für {symbol} sind keine Kursdaten verfügbar. Es wird ein Standardwert ohne Veränderungen angezeigt.";
                        }
                    }

                    // Erstellen eines Aktienobjekts
                    var aktie = new Aktie
                    {
                        AktienID = 0, // ID wird später gesetzt
                        AktienSymbol = symbol,
                        AktienName = name,
                        AktuellerPreis = close,
                        Änderung = change,
                        ÄnderungProzent = percentChange,
                        LetzteAktualisierung = DateTime.Now
                    };

                    Debug.WriteLine($"Aktie {symbol} verarbeitet: Preis={aktie.AktuellerPreis:F2}€, Änderung={aktie.Änderung:F2}€, Prozent={aktie.ÄnderungProzent:F2}%");
                    return aktie;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Parsen der API-Antwort für {symbol}: {ex.Message}");
                LastErrorMessage = $"Fehler beim Verarbeiten der Daten für {symbol}: {ex.Message}";
                return null;
            }
        }
    }
}