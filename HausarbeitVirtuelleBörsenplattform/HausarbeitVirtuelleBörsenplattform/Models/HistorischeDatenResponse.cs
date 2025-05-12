using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Antwortklasse für die historischen Daten von der TwelveData API
    /// </summary>
    public class HistorischeDatenResponse
    {
        /// <summary>
        /// Meta-Informationen zur Abfrage
        /// </summary>
        [JsonProperty("meta")]
        public MetaInfo Meta { get; set; }

        /// <summary>
        /// Werte der Zeitreihe
        /// </summary>
        [JsonProperty("values")]
        public List<TimeSeriesValue> TimeSeries { get; set; }

        /// <summary>
        /// Status-Code der Antwort
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }
    }

    /// <summary>
    /// Meta-Informationen zur Abfrage
    /// </summary>
    public class MetaInfo
    {
        /// <summary>
        /// Symbol der Aktie
        /// </summary>
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        /// <summary>
        /// Währung
        /// </summary>
        [JsonProperty("currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Exchange-Informationen
        /// </summary>
        [JsonProperty("exchange")]
        public string Exchange { get; set; }

        /// <summary>
        /// Zeitzone
        /// </summary>
        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        /// <summary>
        /// Intervall
        /// </summary>
        [JsonProperty("interval")]
        public string Interval { get; set; }
    }

    /// <summary>
    /// Einzelner Datenpunkt der Zeitreihe
    /// </summary>
    public class TimeSeriesValue
    {
        /// <summary>
        /// Datum und Uhrzeit
        /// </summary>
        [JsonProperty("datetime")]
        public string DateTime { get; set; }

        /// <summary>
        /// Eröffnungskurs
        /// </summary>
        [JsonProperty("open")]
        public decimal Open { get; set; }

        /// <summary>
        /// Höchstkurs
        /// </summary>
        [JsonProperty("high")]
        public decimal High { get; set; }

        /// <summary>
        /// Tiefstkurs
        /// </summary>
        [JsonProperty("low")]
        public decimal Low { get; set; }

        /// <summary>
        /// Schlusskurs
        /// </summary>
        [JsonProperty("close")]
        public decimal Close { get; set; }

        /// <summary>
        /// Handelsvolumen (optional)
        /// </summary>
        [JsonProperty("volume")]
        public long? Volume { get; set; }
    }
}