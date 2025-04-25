using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using HausarbeitVirtuelleBörsenplattform.Helpers;
using HausarbeitVirtuelleBörsenplattform.Models;

namespace HausarbeitVirtuelleBörsenplattform.Services
{
    /// <summary>
    /// Service für die Kommunikation mit der Twelve Data API mit optimiertem Rate-Limiting
    /// </summary>
    public class TwelveDataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://api.twelvedata.com";
        private readonly Dictionary<string, (Aktie Aktie, DateTime Zeitstempel)> _aktienCache = new();
        private readonly TimeSpan _cacheGültigkeit = TimeSpan.FromMinutes(15);
        private readonly object _cacheLock = new object();
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Verhindert parallele API-Anfragen
        private DateTime _lastApiCallTime = DateTime.MinValue;
        private int _apiCallCount = 0;
        private readonly int _maxCallsPerMinute = 8; // Basic 8 Plan

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
                    // API-Limit-Prüfung und Anpassung
                    await CheckAndWaitForApiRateLimit();

                    // Für jedes abzufragende Symbol eine einzelne Anfrage durchführen (max. 2 pro Aufruf)
                    var maxSymbolsToProcess = Math.Min(2, abzufragendeSymbole.Count);
                    Debug.WriteLine($"Verarbeite {maxSymbolsToProcess} Symbole in diesem Aufruf");

                    for (int i = 0; i < maxSymbolsToProcess; i++)
                    {
                        var symbol = abzufragendeSymbole[i];

                        // API-Rate-Limit prüfen
                        await CheckAndWaitForApiRateLimit();

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

                                // Zusätzliche Debug-Ausgabe, um den genauen Inhalt zu sehen
                                Debug.WriteLine($"API-Antwort-Inhalt für {symbol}: {responseContent}");

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
                                    }
                                }
                                catch (JsonException jsonEx)
                                {
                                    Debug.WriteLine($"JSON-Fehler: {jsonEx.Message}");
                                    LastErrorMessage = $"JSON-Fehler: {jsonEx.Message}";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Fehler bei der Verarbeitung von {symbol}: {ex.Message}");
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
            return ergebnisse;
        }

        /// <summary>
        /// Prüft das API-Rate-Limit und wartet gegebenenfalls, bis neue Anfragen erlaubt sind
        /// </summary>
        private async Task CheckAndWaitForApiRateLimit()
        {
            var now = DateTime.Now;
            var timeSinceLastCall = now - _lastApiCallTime;

            // Wenn die letzte Anfrage in einer anderen Minute war, setzen wir den Zähler zurück
            if (timeSinceLastCall > TimeSpan.FromMinutes(1))
            {
                Debug.WriteLine("Neue Minute begonnen, API-Anfragenzähler zurückgesetzt");
                _apiCallCount = 0;
                return;
            }

            // Wenn wir das Limit erreicht haben, warten wir
            if (_apiCallCount >= _maxCallsPerMinute)
            {
                // Berechnen, wie lange wir warten müssen, bis die nächste Minute beginnt
                var nextMinute = _lastApiCallTime.AddMinutes(1);
                var waitTime = nextMinute - now;

                if (waitTime > TimeSpan.Zero)
                {
                    Debug.WriteLine($"API-Limit erreicht ({_apiCallCount}/{_maxCallsPerMinute}), warte {waitTime.TotalSeconds:F1} Sekunden");
                    await Task.Delay(waitTime);

                    // Zähler zurücksetzen
                    _apiCallCount = 0;
                }
            }
        }

        /// <summary>
        /// Verarbeitet die JSON-Antwort von Twelve Data
        /// </summary>
        private Aktie ParseTwelveDataResponse(string json, string symbol)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return null;

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

                        // Deutsche Aktien - nur Namen, keine Kurse
                        else if (symbol == "SAP.DE") name = "SAP SE";
                        else if (symbol == "SIE.DE") name = "Siemens AG";
                        else if (symbol == "ALV.DE") name = "Allianz SE";
                        else if (symbol == "BAYN.DE") name = "Bayer AG";
                        else if (symbol == "BAS.DE") name = "BASF SE";
                        else if (symbol.EndsWith(".DE", StringComparison.OrdinalIgnoreCase))
                            name = symbol.Replace(".DE", " AG");
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
                return null;
            }
        }
    }
}