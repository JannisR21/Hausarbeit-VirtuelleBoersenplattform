using System;
using System.Collections.Generic;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Modellklasse für ETFs (Exchange Traded Funds)
    /// </summary>
    public class ETF : Aktie
    {
        /// <summary>
        /// ISIN-Nummer des ETFs
        /// </summary>
        public string ISIN { get; set; }

        /// <summary>
        /// Beschreibung des ETF-Fokus/Strategie
        /// </summary>
        public string Beschreibung { get; set; }

        /// <summary>
        /// Jährliche Verwaltungsgebühr (TER - Total Expense Ratio) in Prozent
        /// </summary>
        public decimal Verwaltungsgebühr { get; set; }

        /// <summary>
        /// Gibt an, ob der ETF ausschüttend (true) oder thesaurierend (false) ist
        /// </summary>
        public bool IstAusschüttend { get; set; }

        /// <summary>
        /// Der Index, den der ETF abbildet (z.B. "DAX", "S&P 500")
        /// </summary>
        public string AbgebildeterIndex { get; set; }

        /// <summary>
        /// Die Fondsgesellschaft, die den ETF ausgibt
        /// </summary>
        public string Fondsgesellschaft { get; set; }

        /// <summary>
        /// Der Anlagefokus des ETFs
        /// </summary>
        public ETFTyp ETFTyp { get; set; }

        /// <summary>
        /// Anzahl der enthaltenen Positionen im ETF
        /// </summary>
        public int AnzahlPositionen { get; set; }

        /// <summary>
        /// Konstruktor für ETF mit Mindestparametern
        /// </summary>
        public ETF() : base()
        {
            // Standardwerte setzen
            Verwaltungsgebühr = 0.2m; // 0.2% als Standardwert
            IstAusschüttend = false;  // Thesaurierend als Standard
            ETFTyp = ETFTyp.AKTIEN;   // Aktien-ETF als Standard
            AnzahlPositionen = 0;
        }
    }

    /// <summary>
    /// Definiert die verschiedenen Typen von ETFs nach Anlageklasse
    /// </summary>
    public enum ETFTyp
    {
        AKTIEN = 0,
        ANLEIHEN = 1,
        ROHSTOFFE = 2,
        IMMOBILIEN = 3,
        MULTI_ASSET = 4,
        GELDMARKT = 5
    }

    /// <summary>
    /// Hilfsmethoden für ETF-Typen
    /// </summary>
    public static class ETFTypHelper
    {
        /// <summary>
        /// Liefert alle verfügbaren ETF-Typen als Liste zurück
        /// </summary>
        public static List<ETFTyp> AlleETFTypen => new List<ETFTyp>
        {
            ETFTyp.AKTIEN,
            ETFTyp.ANLEIHEN,
            ETFTyp.ROHSTOFFE,
            ETFTyp.IMMOBILIEN,
            ETFTyp.MULTI_ASSET,
            ETFTyp.GELDMARKT
        };

        /// <summary>
        /// Gibt den anzeigbaren Namen für einen ETF-Typ zurück
        /// </summary>
        public static string GetETFTypName(ETFTyp typ)
        {
            return typ switch
            {
                ETFTyp.AKTIEN => "Aktien-ETF",
                ETFTyp.ANLEIHEN => "Anleihen-ETF",
                ETFTyp.ROHSTOFFE => "Rohstoff-ETF",
                ETFTyp.IMMOBILIEN => "Immobilien-ETF",
                ETFTyp.MULTI_ASSET => "Multi-Asset-ETF",
                ETFTyp.GELDMARKT => "Geldmarkt-ETF",
                _ => "Unbekannt"
            };
        }

        /// <summary>
        /// Gibt die verfügbaren ETF-Typen als Dictionary für ComboBox-Anbindung zurück
        /// </summary>
        public static Dictionary<ETFTyp, string> ETFTypenAlsDictionary()
        {
            var dict = new Dictionary<ETFTyp, string>();
            foreach (var typ in AlleETFTypen)
            {
                dict.Add(typ, GetETFTypName(typ));
            }
            return dict;
        }
    }

    /// <summary>
    /// Statische Klasse mit häufig verwendeten ETFs
    /// </summary>
    public static class BekannteBörsenETFs
    {
        /// <summary>
        /// Liefert eine Liste mit bekannten ETFs zurück
        /// </summary>
        /// <returns>Liste mit ETF-Objekten</returns>
        public static List<ETF> GetBekannteBörsenETFs()
        {
            var etfs = new List<ETF>
            {
                new ETF
                {
                    AktienID = 1001,
                    AktienSymbol = "VWRL.L",
                    AktienName = "Vanguard FTSE All-World UCITS ETF",
                    ISIN = "IE00B3RBWM25",
                    Beschreibung = "Weltweiter ETF, der über 3.000 große und mittelgroße Unternehmen aus Industrie- und Schwellenländern abbildet.",
                    Verwaltungsgebühr = 0.22m,
                    AbgebildeterIndex = "FTSE All-World Index",
                    Fondsgesellschaft = "Vanguard",
                    ETFTyp = ETFTyp.AKTIEN,
                    AnzahlPositionen = 3570
                },
                new ETF
                {
                    AktienID = 1002,
                    AktienSymbol = "EUNL.DE",
                    AktienName = "iShares Core MSCI World UCITS ETF",
                    ISIN = "IE00B4L5Y983",
                    Beschreibung = "ETF, der den MSCI World Index abbildet und in über 1.500 Unternehmen aus 23 Industrieländern investiert.",
                    Verwaltungsgebühr = 0.20m,
                    AbgebildeterIndex = "MSCI World Index",
                    Fondsgesellschaft = "BlackRock",
                    ETFTyp = ETFTyp.AKTIEN,
                    AnzahlPositionen = 1523
                },
                new ETF
                {
                    AktienID = 1003,
                    AktienSymbol = "SPY",
                    AktienName = "SPDR S&P 500 ETF Trust",
                    ISIN = "US78462F1030",
                    Beschreibung = "Der größte und liquideste ETF der Welt, der den S&P 500 Index abbildet.",
                    Verwaltungsgebühr = 0.0945m,
                    AbgebildeterIndex = "S&P 500",
                    Fondsgesellschaft = "State Street Global Advisors",
                    ETFTyp = ETFTyp.AKTIEN,
                    AnzahlPositionen = 500
                },
                new ETF
                {
                    AktienID = 1004,
                    AktienSymbol = "EXH1.DE",
                    AktienName = "iShares Core DAX UCITS ETF",
                    ISIN = "DE0005933931",
                    Beschreibung = "ETF, der den deutschen Leitindex DAX mit 40 großen deutschen Unternehmen abbildet.",
                    Verwaltungsgebühr = 0.16m,
                    AbgebildeterIndex = "DAX",
                    Fondsgesellschaft = "BlackRock",
                    ETFTyp = ETFTyp.AKTIEN,
                    AnzahlPositionen = 40
                },
                new ETF
                {
                    AktienID = 1005,
                    AktienSymbol = "IEMG",
                    AktienName = "iShares Core MSCI Emerging Markets ETF",
                    ISIN = "US46434G1031",
                    Beschreibung = "ETF, der in über 2.600 Aktien aus Schwellenländern investiert.",
                    Verwaltungsgebühr = 0.11m,
                    AbgebildeterIndex = "MSCI Emerging Markets Index",
                    Fondsgesellschaft = "BlackRock",
                    ETFTyp = ETFTyp.AKTIEN,
                    AnzahlPositionen = 2645
                }
            };

            return etfs;
        }
    }
}