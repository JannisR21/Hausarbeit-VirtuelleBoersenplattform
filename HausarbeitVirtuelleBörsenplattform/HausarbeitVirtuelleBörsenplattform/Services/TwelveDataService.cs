using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
        private readonly RateLimiter _rateLimiter = new(8, TimeSpan.FromMinutes(1));

        public TwelveDataService(string apiKey)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
        }

        public async Task<List<AktienDaten>> HoleAktienKurse(List<string> symbole)
        {
            var ergebnisse = new List<AktienDaten>();
            var neu = symbole
                .Where(s => !_aktienCache.TryGetValue(s, out var c) || (DateTime.Now - c.Zeitstempel) >= _cacheGültigkeit)
                .ToList();

            // füge gecachte hinzu
            ergebnisse.AddRange(symbole.Except(neu)
                .Select(s => _aktienCache[s]));

            if (!neu.Any()) return ergebnisse;

            // max. 8 pro API-Call
            var batch = neu.Take(8).ToList();
            var url = $"{_baseUrl}/quote?symbol={string.Join(',', batch)}&apikey={_apiKey}";

            // throttle
            await _rateLimiter.ThrottleAsync();

            try
            {
                var json = await _httpClient.GetStringAsync(url);
                var resp = JsonSerializer.Deserialize<TwelveDataResponse>(json);

                // **Hier** das ExtensionData‐Dictionary in echte Quotes umwandeln:
                if (resp.ExtensionData != null)
                {
                    resp.MultipleQuotes = resp.ExtensionData
                        .Where(kvp => kvp.Key != "status")
                        .Select(kvp => JsonSerializer
                            .Deserialize<TwelveDataQuote>(kvp.Value.GetRawText()))
                        .ToList();
                }

                // Einzel‑Symbol?
                if (batch.Count == 1)
                {
                    var d = KonvertiereZuAktienDaten(resp);
                    if (d != null) ergebnisse.Add(CacheUndReturn(d));
                }
                else
                {
                    foreach (var q in resp.MultipleQuotes)
                        ergebnisse.Add(CacheUndReturn(KonvertiereZuAktienDaten(q)));
                }
            }
            catch
            {
                // bei Fehlern: Fake‑Daten
                foreach (var s in neu)
                    ergebnisse.Add(CacheUndReturn(ErstelleBeispielDaten(s)));
            }

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
            decimal.TryParse(q.Close, out var close);
            decimal.TryParse(q.Change, out var change);
            decimal.TryParse(q.PercentChange, out var pct);
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

        private AktienDaten ErstelleBeispielDaten(string symbol)
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

            // Gib das AktienDaten‑Objekt zurück
            return new AktienDaten
            {
                AktienSymbol = symbol,
                AktienName = $"{symbol} Inc.",
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
