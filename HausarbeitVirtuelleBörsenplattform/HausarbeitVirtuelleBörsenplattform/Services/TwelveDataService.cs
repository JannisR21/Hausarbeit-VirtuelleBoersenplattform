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
            _rateLimiter = new RateLimiter(_maxCallsPerMinute, TimeSpan.FromMinutes(1));

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
        /// Prüft, ob die US-Börse aktuell geöffnet ist (vereinfachte Version)
        /// </summary>
        /// <returns>True, wenn die Börse geöffnet ist, sonst False</returns>
        public bool IstBoerseGeoeffnet()
        {
            try
            {
                // Aktuelle Zeit in der Zeitzone der Anwendung
                DateTime jetztLokal = DateTime.Now;

                // Zeitzone für US Eastern Time (ET) - New York
                TimeZoneInfo usEasternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

                // Aktuelle Zeit in US Eastern Time
                DateTime jetztET = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, usEasternZone);

                // Wochentag prüfen (1 = Montag, ..., 7 = Sonntag)
                int wochentag = (int)jetztET.DayOfWeek;

                // Prüfen, ob es ein Wochenende ist (Samstag = 6, Sonntag = 0)
                if (wochentag == 6 || wochentag == 0)
                {
                    Debug.WriteLine($"Börse geschlossen: Wochenende (Wochentag: {jetztET.DayOfWeek})");
                    return false;
                }

                // Uhrzeit prüfen (9:30 - 16:00 ET sind die regulären Handelszeiten)
                TimeSpan startzeit = new TimeSpan(9, 30, 0); // 9:30 ET
                TimeSpan endzeit = new TimeSpan(16, 0, 0);   // 16:00 ET
                TimeSpan aktuelleZeit = jetztET.TimeOfDay;

                bool istGeoeffnet = aktuelleZeit >= startzeit && aktuelleZeit <= endzeit;

                if (!istGeoeffnet)
                {
                    Debug.WriteLine($"Börse geschlossen: Außerhalb der Handelszeiten (Aktuelle Zeit ET: {jetztET.ToString("HH:mm")})");
                }
                else
                {
                    Debug.WriteLine($"Börse geöffnet (Aktuelle Zeit ET: {jetztET.ToString("HH:mm")})");
                }

                return istGeoeffnet;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler bei der Überprüfung der Börsenöffnungszeiten: {ex.Message}");

                // Im Fehlerfall nehmen wir sicherheitshalber an, dass die Börse geschlossen ist
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