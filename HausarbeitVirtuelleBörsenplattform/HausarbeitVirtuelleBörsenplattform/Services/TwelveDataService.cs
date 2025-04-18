using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using HausarbeitVirtuelleBörsenplattform.Helpers;
using HausarbeitVirtuelleBörsenplattform.Models;

namespace HausarbeitVirtuelleBörsenplattform.Services
{
    public class TwelveDataService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://api.twelvedata.com";
        private readonly Dictionary<string, AktienDaten> _aktienCache = new();
        private readonly TimeSpan _cacheGültigkeit = TimeSpan.FromMinutes(1);
        private readonly RateLimiter _rateLimiter = new(5, TimeSpan.FromMinutes(1)); // Reduziert auf 5 pro Minute, um unter dem Limit zu bleiben
        public string LastErrorMessage { get; private set; }

        // CultureInfo für korrekte Dezimalkonvertierung
        private static readonly CultureInfo EnglishCulture = new CultureInfo("en-US");
        private static readonly CultureInfo GermanCulture = new CultureInfo("de-DE");

        public TwelveDataService(string apiKey)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
            Debug.WriteLine($"TwelveDataService initialisiert mit API-Key: {apiKey}");
        }

        public async Task<List<AktienDaten>> HoleAktienKurse(List<string> symbole)
        {
            var ergebnisse = new List<AktienDaten>();
            LastErrorMessage = null;

            try
            {
                Debug.WriteLine($"HoleAktienKurse aufgerufen für Symbole: {string.Join(", ", symbole)}");

                var neu = symbole
                    .Where(s => !_aktienCache.TryGetValue(s, out var c) || (DateTime.Now - c.Zeitstempel) >= _cacheGültigkeit)
                    .ToList();

                // füge gecachte hinzu
                ergebnisse.AddRange(symbole.Except(neu)
                    .Select(s => _aktienCache[s]));

                Debug.WriteLine($"Aus Cache: {symbole.Count - neu.Count}, Neu abzufragen: {neu.Count}");

                if (!neu.Any()) return ergebnisse;

                // max. 5 pro API-Call, um unter dem Limit zu bleiben
                var batch = neu.Take(5).ToList();
                var url = $"{_baseUrl}/quote?symbol={string.Join(',', batch)}&apikey={_apiKey}";

                Debug.WriteLine($"API-Anfrage URL: {url.Replace(_apiKey, "API_KEY_HIDDEN")}");

                // throttle
                await _rateLimiter.ThrottleAsync();

                var json = await _httpClient.GetStringAsync(url);
                Debug.WriteLine($"API-Antwort erhalten: {json.Substring(0, Math.Min(json.Length, 100))}...");

                var resp = JsonSerializer.Deserialize<TwelveDataResponse>(json);
                Debug.WriteLine($"Deserialisierung erfolgreich: {(resp != null ? "Ja" : "Nein")}");

                // Prüfen, ob die API einen Fehlerstatus zurückgegeben hat
                if (resp?.ExtensionData != null && resp.ExtensionData.TryGetValue("status", out var statusElement))
                {
                    string status = statusElement.GetString();
                    if (status != "ok")
                    {
                        string message = resp.ExtensionData.TryGetValue("message", out var messageElement)
                            ? messageElement.GetString()
                            : "Unbekannter API-Fehler";

                        LastErrorMessage = $"API-Statusfehler: {status}, Nachricht: {message}";
                        Debug.WriteLine(LastErrorMessage);
                        throw new Exception(LastErrorMessage);
                    }
                }

                // **Hier** das ExtensionData‐Dictionary in echte Quotes umwandeln:
                if (resp.ExtensionData != null)
                {
                    resp.MultipleQuotes = resp.ExtensionData
                        .Where(kvp => kvp.Key != "status")
                        .Select(kvp => JsonSerializer
                            .Deserialize<TwelveDataQuote>(kvp.Value.GetRawText()))
                        .ToList();

                    Debug.WriteLine($"ExtensionData verarbeitet, Anzahl Quotes: {resp.MultipleQuotes?.Count ?? 0}");
                }

                // Einzel‑Symbol?
                if (batch.Count == 1)
                {
                    Debug.WriteLine("Verarbeite Einzelsymbol-Antwort");
                    var d = KonvertiereZuAktienDaten(resp);
                    if (d != null) ergebnisse.Add(CacheUndReturn(d));
                }
                else
                {
                    Debug.WriteLine("Verarbeite Multi-Symbol-Antwort");
                    foreach (var q in resp.MultipleQuotes)
                        ergebnisse.Add(CacheUndReturn(KonvertiereZuAktienDaten(q)));
                }
            }
            catch (HttpRequestException ex)
            {
                LastErrorMessage = $"Netzwerkfehler bei der API-Anfrage: {ex.Message}";
                Debug.WriteLine(LastErrorMessage);
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // bei Fehlern: Fake‑Daten, aber mit Fehlerinformation im Namen
                foreach (var s in symbole.Except(ergebnisse.Select(e => e.AktienSymbol)))
                    ergebnisse.Add(CacheUndReturn(ErstelleBeispielDaten(s, true)));
            }
            catch (JsonException ex)
            {
                LastErrorMessage = $"Fehler beim Deserialisieren der API-Antwort: {ex.Message}";
                Debug.WriteLine(LastErrorMessage);
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // bei Fehlern: Fake‑Daten, aber mit Fehlerinformation im Namen
                foreach (var s in symbole.Except(ergebnisse.Select(e => e.AktienSymbol)))
                    ergebnisse.Add(CacheUndReturn(ErstelleBeispielDaten(s, true)));
            }
            catch (Exception ex)
            {
                LastErrorMessage = $"Allgemeiner Fehler: {ex.Message}";
                Debug.WriteLine(LastErrorMessage);
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // bei Fehlern: Fake‑Daten, aber mit Fehlerinformation im Namen
                foreach (var s in symbole.Except(ergebnisse.Select(e => e.AktienSymbol)))
                    ergebnisse.Add(CacheUndReturn(ErstelleBeispielDaten(s, true)));
            }

            Debug.WriteLine($"HoleAktienKurse liefert {ergebnisse.Count} Ergebnisse zurück");
            return ergebnisse;
        }

        private AktienDaten CacheUndReturn(AktienDaten d)
        {
            _aktienCache[d.AktienSymbol] = d;
            return d;
        }

        private AktienDaten KonvertiereZuAktienDaten(TwelveDataResponse r)
        {
            if (r is null) return null;
            var q = new TwelveDataQuote
            {
                Symbol = r.Symbol,
                Name = r.Name,
                Close = r.Close,
                Change = r.Change,
                PercentChange = r.PercentChange
            };
            return KonvertiereZuAktienDaten(q);
        }

        private AktienDaten KonvertiereZuAktienDaten(TwelveDataQuote q)
        {
            if (q is null) return null;

            // Verwende die englische Kultur für die Konvertierung, da die API englisches Format liefert
            bool closeSuccess = decimal.TryParse(q.Close, NumberStyles.Any, EnglishCulture, out var close);
            bool changeSuccess = decimal.TryParse(q.Change, NumberStyles.Any, EnglishCulture, out var change);
            bool pctSuccess = decimal.TryParse(q.PercentChange, NumberStyles.Any, EnglishCulture, out var pct);

            Debug.WriteLine($"Konvertierung von {q.Symbol}: Close={closeSuccess}({q.Close}->{close}), Change={changeSuccess}({q.Change}->{change}), Pct={pctSuccess}({q.PercentChange}->{pct})");

            if (!closeSuccess)
            {
                Debug.WriteLine($"WARNUNG: Konnte {q.Close} nicht in decimal konvertieren für Symbol {q.Symbol}");
                // Versuche alternative Konvertierungsmethoden
                if (q.Close != null)
                {
                    try
                    {
                        close = Convert.ToDecimal(q.Close, EnglishCulture);
                        Debug.WriteLine($"Alternative Konvertierung erfolgreich: {q.Close} -> {close}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Alternative Konvertierung fehlgeschlagen: {ex.Message}");
                        close = 0;
                    }
                }
            }

            return new AktienDaten
            {
                AktienSymbol = q.Symbol,
                AktienName = q.Name ?? q.Symbol,
                AktuellerPreis = close,
                Änderung = change,
                ÄnderungProzent = pct,
                Zeitstempel = DateTime.Now,
                LetzteAktualisierung = DateTime.Now
            };
        }

        private AktienDaten ErstelleBeispielDaten(string symbol, bool mitFehlerinfo = false)
        {
            var rnd = new Random();
            // Generiere zufälligen Preis zwischen 50 und 500
            var preisDouble = rnd.Next(50, 500) + rnd.NextDouble();
            var preis = Math.Round(preisDouble, 2);

            // Generiere zufällige Änderung zwischen -5 und +5
            var changeDouble = rnd.Next(-5, 5) + rnd.NextDouble();
            var aenderung = Math.Round(changeDouble, 2);

            // Berechne Prozentänderung
            var prozent = Math.Round((aenderung / preis) * 100, 2);

            string name = mitFehlerinfo && !string.IsNullOrEmpty(LastErrorMessage)
                ? $"{symbol} Inc. (DEMO - API-Fehler)"
                : $"{symbol} Inc. (DEMO-Daten)";

            // Gib das AktienDaten‑Objekt zurück
            return new AktienDaten
            {
                AktienSymbol = symbol,
                AktienName = name,
                AktuellerPreis = (decimal)preis,
                Änderung = (decimal)aenderung,
                ÄnderungProzent = (decimal)prozent,
                Zeitstempel = DateTime.Now,
                LetzteAktualisierung = DateTime.Now
            };
        }

        // **** Models ****

        public class TwelveDataResponse
        {
            [JsonPropertyName("symbol")] public string Symbol { get; set; }
            [JsonPropertyName("name")] public string Name { get; set; }
            [JsonPropertyName("close")] public string Close { get; set; }
            [JsonPropertyName("change")] public string Change { get; set; }
            [JsonPropertyName("percent_change")] public string PercentChange { get; set; }

            [JsonIgnore]
            public List<TwelveDataQuote> MultipleQuotes { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }
        }

        public class TwelveDataQuote
        {
            [JsonPropertyName("symbol")] public string Symbol { get; set; }
            [JsonPropertyName("name")] public string Name { get; set; }
            [JsonPropertyName("close")] public string Close { get; set; }
            [JsonPropertyName("change")] public string Change { get; set; }
            [JsonPropertyName("percent_change")] public string PercentChange { get; set; }
        }

        public class AktienDaten : Aktie
        {
            public DateTime Zeitstempel { get; set; }
        }
    }
}