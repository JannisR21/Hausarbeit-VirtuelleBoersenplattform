using System;
using System.Collections.Generic;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Definiert die verschiedenen Branchen, in denen Aktien kategorisiert werden können
    /// </summary>
    public enum AktienBranche
    {
        TECHNOLOGIE = 0,
        FINANZEN = 1,
        GESUNDHEIT = 2,
        KONSUMGÜTER = 3,
        ENERGIE = 4,
        INDUSTRIE = 5,
        TELEKOMMUNIKATION = 6,
        GRUNDSTOFFE = 7,
        VERSORGUNG = 8,
        IMMOBILIEN = 9
    }

    /// <summary>
    /// Statische Klasse mit Methoden und Eigenschaften für Aktienbranchen
    /// </summary>
    public static class AktienBrancheHelper
    {
        /// <summary>
        /// Liefert alle verfügbaren Branchen als Liste zurück
        /// </summary>
        public static List<AktienBranche> AlleBranchen => new List<AktienBranche>
        {
            AktienBranche.TECHNOLOGIE,
            AktienBranche.FINANZEN,
            AktienBranche.GESUNDHEIT,
            AktienBranche.KONSUMGÜTER,
            AktienBranche.ENERGIE,
            AktienBranche.INDUSTRIE,
            AktienBranche.TELEKOMMUNIKATION,
            AktienBranche.GRUNDSTOFFE,
            AktienBranche.VERSORGUNG,
            AktienBranche.IMMOBILIEN
        };

        /// <summary>
        /// Gibt den anzeigbaren Namen für eine Branche zurück
        /// </summary>
        public static string GetBrancheName(AktienBranche branche)
        {
            return branche switch
            {
                AktienBranche.TECHNOLOGIE => "Technologie",
                AktienBranche.FINANZEN => "Finanzen",
                AktienBranche.GESUNDHEIT => "Gesundheit",
                AktienBranche.KONSUMGÜTER => "Konsumgüter",
                AktienBranche.ENERGIE => "Energie",
                AktienBranche.INDUSTRIE => "Industrie",
                AktienBranche.TELEKOMMUNIKATION => "Telekommunikation",
                AktienBranche.GRUNDSTOFFE => "Grundstoffe",
                AktienBranche.VERSORGUNG => "Versorgung",
                AktienBranche.IMMOBILIEN => "Immobilien",
                _ => "Unbekannt"
            };
        }

        /// <summary>
        /// Gibt die verfügbaren Branchen als Dictionary für ComboBox-Anbindung zurück
        /// </summary>
        public static Dictionary<AktienBranche, string> BranchenAlsDictionary()
        {
            var dict = new Dictionary<AktienBranche, string>();
            foreach (var branche in AlleBranchen)
            {
                dict.Add(branche, GetBrancheName(branche));
            }
            return dict;
        }

        /// <summary>
        /// Ordnet ein Aktiensymbol einer Branche zu (Beispielimplementierung)
        /// </summary>
        public static AktienBranche GetBrancheForSymbol(string symbol)
        {
            // Hier eine simple Zuordnung basierend auf bekannten Beispielen
            symbol = symbol.ToUpper();

            if (symbol.Contains("AAPL") || symbol.Contains("MSFT") || symbol.Contains("GOOGL") ||
                symbol.Contains("META") || symbol.Contains("NVDA") || symbol.Contains("AMD"))
                return AktienBranche.TECHNOLOGIE;

            if (symbol.Contains("JPM") || symbol.Contains("BAC") || symbol.Contains("GS") ||
                symbol.Contains("MS") || symbol.Contains("V") || symbol.Contains("MA"))
                return AktienBranche.FINANZEN;

            if (symbol.Contains("JNJ") || symbol.Contains("PFE") || symbol.Contains("MRK") ||
                symbol.Contains("ABBV") || symbol.Contains("LLY") || symbol.Contains("ABT"))
                return AktienBranche.GESUNDHEIT;

            if (symbol.Contains("PG") || symbol.Contains("KO") || symbol.Contains("PEP") ||
                symbol.Contains("WMT") || symbol.Contains("MCD") || symbol.Contains("NKE"))
                return AktienBranche.KONSUMGÜTER;

            if (symbol.Contains("XOM") || symbol.Contains("CVX") || symbol.Contains("COP") ||
                symbol.Contains("BP") || symbol.Contains("SHEL"))
                return AktienBranche.ENERGIE;

            // Fallback für unbekannte Symbole
            return AktienBranche.TECHNOLOGIE;
        }
    }
}