using System.Collections.Generic;
using HausarbeitVirtuelleBörsenplattform.Models;
namespace HausarbeitVirtuelleBörsenplattform.Data
{
    /// <summary>
    /// Stellt eine Liste bekannter Aktien aus dem FTSE All-World und anderen wichtigen Indizes bereit
    /// </summary>
    public static class AktienListe
    {
        /// <summary>
        /// Erstellt eine Liste von bekannten Aktien (ohne aktuelle Kursdaten)
        /// </summary>
        /// <returns>Liste von Aktien-Basis-Informationen</returns>
        public static List<Aktie> GetBekannteBörsenAktien()
        {
            return new List<Aktie>
            {
                // US-Aktien - Big Tech
                new Aktie { AktienID = 1, AktienSymbol = "AAPL", AktienName = "Apple Inc." },
                new Aktie { AktienID = 2, AktienSymbol = "MSFT", AktienName = "Microsoft Corporation" },
                new Aktie { AktienID = 3, AktienSymbol = "GOOGL", AktienName = "Alphabet Inc. Class A" },
                new Aktie { AktienID = 4, AktienSymbol = "GOOG", AktienName = "Alphabet Inc. Class C" },
                new Aktie { AktienID = 5, AktienSymbol = "AMZN", AktienName = "Amazon.com Inc." },
                new Aktie { AktienID = 6, AktienSymbol = "META", AktienName = "Meta Platforms Inc." },
                new Aktie { AktienID = 7, AktienSymbol = "TSLA", AktienName = "Tesla Inc." },
                new Aktie { AktienID = 8, AktienSymbol = "NVDA", AktienName = "NVIDIA Corporation" },
                
                // US-Aktien - Weitere bekannte
                new Aktie { AktienID = 9, AktienSymbol = "JPM", AktienName = "JPMorgan Chase & Co." },
                new Aktie { AktienID = 10, AktienSymbol = "V", AktienName = "Visa Inc." },
                new Aktie { AktienID = 11, AktienSymbol = "MA", AktienName = "Mastercard Inc." },
                new Aktie { AktienID = 12, AktienSymbol = "PG", AktienName = "Procter & Gamble Co." },
                new Aktie { AktienID = 13, AktienSymbol = "JNJ", AktienName = "Johnson & Johnson" },
                new Aktie { AktienID = 14, AktienSymbol = "WMT", AktienName = "Walmart Inc." },
                new Aktie { AktienID = 15, AktienSymbol = "HD", AktienName = "Home Depot Inc." },
                new Aktie { AktienID = 16, AktienSymbol = "BAC", AktienName = "Bank of America Corp." },
                new Aktie { AktienID = 17, AktienSymbol = "PFE", AktienName = "Pfizer Inc." },
                new Aktie { AktienID = 18, AktienSymbol = "KO", AktienName = "Coca-Cola Company" },
                new Aktie { AktienID = 19, AktienSymbol = "DIS", AktienName = "Walt Disney Co." },
                new Aktie { AktienID = 20, AktienSymbol = "NFLX", AktienName = "Netflix Inc." },
                new Aktie { AktienID = 21, AktienSymbol = "CSCO", AktienName = "Cisco Systems Inc." },
                new Aktie { AktienID = 22, AktienSymbol = "ADBE", AktienName = "Adobe Inc." },
                new Aktie { AktienID = 23, AktienSymbol = "INTC", AktienName = "Intel Corporation" },
                new Aktie { AktienID = 24, AktienSymbol = "CRM", AktienName = "Salesforce Inc." },
                new Aktie { AktienID = 25, AktienSymbol = "AMD", AktienName = "Advanced Micro Devices, Inc." },
                
                // Weitere US-Aktien
                new Aktie { AktienID = 26, AktienSymbol = "ORCL", AktienName = "Oracle Corporation" },
                new Aktie { AktienID = 27, AktienSymbol = "PYPL", AktienName = "PayPal Holdings, Inc." },
                new Aktie { AktienID = 28, AktienSymbol = "CMCSA", AktienName = "Comcast Corporation" },
                new Aktie { AktienID = 29, AktienSymbol = "NKE", AktienName = "Nike, Inc." },
                new Aktie { AktienID = 30, AktienSymbol = "UNH", AktienName = "UnitedHealth Group Incorporated" },
                new Aktie { AktienID = 31, AktienSymbol = "ABT", AktienName = "Abbott Laboratories" },
                new Aktie { AktienID = 32, AktienSymbol = "PEP", AktienName = "PepsiCo, Inc." },
                new Aktie { AktienID = 33, AktienSymbol = "XOM", AktienName = "Exxon Mobil Corporation" },
                new Aktie { AktienID = 34, AktienSymbol = "CVX", AktienName = "Chevron Corporation" },
                new Aktie { AktienID = 35, AktienSymbol = "MRK", AktienName = "Merck & Co., Inc." },
                new Aktie { AktienID = 36, AktienSymbol = "COST", AktienName = "Costco Wholesale Corporation" },
                new Aktie { AktienID = 37, AktienSymbol = "AVGO", AktienName = "Broadcom Inc." },
                new Aktie { AktienID = 38, AktienSymbol = "ABBV", AktienName = "AbbVie Inc." },
                new Aktie { AktienID = 39, AktienSymbol = "TMO", AktienName = "Thermo Fisher Scientific Inc." },
                new Aktie { AktienID = 40, AktienSymbol = "ACN", AktienName = "Accenture plc" },
                new Aktie { AktienID = 41, AktienSymbol = "DHR", AktienName = "Danaher Corporation" },
                new Aktie { AktienID = 42, AktienSymbol = "MCD", AktienName = "McDonald's Corporation" },
                new Aktie { AktienID = 43, AktienSymbol = "QCOM", AktienName = "QUALCOMM Incorporated" },
                new Aktie { AktienID = 44, AktienSymbol = "LLY", AktienName = "Eli Lilly and Company" },
                new Aktie { AktienID = 45, AktienSymbol = "TXN", AktienName = "Texas Instruments Incorporated" },
                new Aktie { AktienID = 46, AktienSymbol = "PM", AktienName = "Philip Morris International Inc." },
                new Aktie { AktienID = 47, AktienSymbol = "UPS", AktienName = "United Parcel Service, Inc." },
                new Aktie { AktienID = 48, AktienSymbol = "SBUX", AktienName = "Starbucks Corporation" },
                new Aktie { AktienID = 49, AktienSymbol = "BMY", AktienName = "Bristol-Myers Squibb Company" },
                new Aktie { AktienID = 50, AktienSymbol = "AMGN", AktienName = "Amgen Inc." },
                
                // Technologie- und Wachstumsaktien
                new Aktie { AktienID = 51, AktienSymbol = "ZM", AktienName = "Zoom Video Communications, Inc." },
                new Aktie { AktienID = 52, AktienSymbol = "UBER", AktienName = "Uber Technologies, Inc." },
                new Aktie { AktienID = 53, AktienSymbol = "LYFT", AktienName = "Lyft, Inc." },
                new Aktie { AktienID = 54, AktienSymbol = "SNOW", AktienName = "Snowflake Inc." },
                new Aktie { AktienID = 55, AktienSymbol = "PLTR", AktienName = "Palantir Technologies Inc." },
                new Aktie { AktienID = 56, AktienSymbol = "PINS", AktienName = "Pinterest, Inc." },
                new Aktie { AktienID = 57, AktienSymbol = "SNAP", AktienName = "Snap Inc." },
                new Aktie { AktienID = 58, AktienSymbol = "TWTR", AktienName = "Twitter, Inc." },
                new Aktie { AktienID = 59, AktienSymbol = "SPOT", AktienName = "Spotify Technology S.A." },
                new Aktie { AktienID = 60, AktienSymbol = "SQ", AktienName = "Block, Inc." },
                
                // Finanz- und Bankaktien
                new Aktie { AktienID = 61, AktienSymbol = "GS", AktienName = "The Goldman Sachs Group, Inc." },
                new Aktie { AktienID = 62, AktienSymbol = "MS", AktienName = "Morgan Stanley" },
                new Aktie { AktienID = 63, AktienSymbol = "C", AktienName = "Citigroup Inc." },
                new Aktie { AktienID = 64, AktienSymbol = "WFC", AktienName = "Wells Fargo & Company" },
                new Aktie { AktienID = 65, AktienSymbol = "AXP", AktienName = "American Express Company" },
                
                // Deutsche Aktien für heimisches Flair
                new Aktie { AktienID = 66, AktienSymbol = "ALV", AktienName = "Allianz SE" },
                new Aktie { AktienID = 67, AktienSymbol = "SAP", AktienName = "SAP SE" },
                new Aktie { AktienID = 68, AktienSymbol = "SIE", AktienName = "Siemens AG" },
                new Aktie { AktienID = 69, AktienSymbol = "DAI", AktienName = "Mercedes-Benz Group AG" },
                new Aktie { AktienID = 70, AktienSymbol = "BMW", AktienName = "Bayerische Motoren Werke AG" },
                
                // ETFs - US-Index ETFs
                new Aktie { AktienID = 71, AktienSymbol = "SPY", AktienName = "SPDR S&P 500 ETF Trust" },
                new Aktie { AktienID = 72, AktienSymbol = "VOO", AktienName = "Vanguard S&P 500 ETF" },
                new Aktie { AktienID = 73, AktienSymbol = "QQQ", AktienName = "Invesco QQQ Trust" },
                new Aktie { AktienID = 74, AktienSymbol = "IWM", AktienName = "iShares Russell 2000 ETF" },
                new Aktie { AktienID = 75, AktienSymbol = "DIA", AktienName = "SPDR Dow Jones Industrial Average ETF" },
                
                // ETFs - International
                new Aktie { AktienID = 76, AktienSymbol = "VEA", AktienName = "Vanguard FTSE Developed Markets ETF" },
                new Aktie { AktienID = 77, AktienSymbol = "VWO", AktienName = "Vanguard FTSE Emerging Markets ETF" },
                new Aktie { AktienID = 78, AktienSymbol = "EFA", AktienName = "iShares MSCI EAFE ETF" },
                new Aktie { AktienID = 79, AktienSymbol = "IEMG", AktienName = "iShares Core MSCI Emerging Markets ETF" },
                
                // ETFs - Sektoren
                new Aktie { AktienID = 80, AktienSymbol = "XLF", AktienName = "Financial Select Sector SPDR Fund" },
                new Aktie { AktienID = 81, AktienSymbol = "XLK", AktienName = "Technology Select Sector SPDR Fund" },
                new Aktie { AktienID = 82, AktienSymbol = "XLE", AktienName = "Energy Select Sector SPDR Fund" },
                new Aktie { AktienID = 83, AktienSymbol = "XLV", AktienName = "Health Care Select Sector SPDR Fund" },
                new Aktie { AktienID = 84, AktienSymbol = "XLC", AktienName = "Communication Services Select Sector SPDR Fund" },
                
                // ETFs - Anleihen
                new Aktie { AktienID = 85, AktienSymbol = "AGG", AktienName = "iShares Core U.S. Aggregate Bond ETF" },
                new Aktie { AktienID = 86, AktienSymbol = "BND", AktienName = "Vanguard Total Bond Market ETF" },
                new Aktie { AktienID = 87, AktienSymbol = "LQD", AktienName = "iShares iBoxx $ Investment Grade Corporate Bond ETF" },
                new Aktie { AktienID = 88, AktienSymbol = "TLT", AktienName = "iShares 20+ Year Treasury Bond ETF" },
                
                // ETFs - Dividenden
                new Aktie { AktienID = 89, AktienSymbol = "VYM", AktienName = "Vanguard High Dividend Yield ETF" },
                new Aktie { AktienID = 90, AktienSymbol = "SCHD", AktienName = "Schwab U.S. Dividend Equity ETF" },
                new Aktie { AktienID = 91, AktienSymbol = "HDV", AktienName = "iShares Core High Dividend ETF" },
                
                // ETFs - Spezielle Anlagethemen
                new Aktie { AktienID = 92, AktienSymbol = "ARKK", AktienName = "ARK Innovation ETF" },
                new Aktie { AktienID = 93, AktienSymbol = "ICLN", AktienName = "iShares Global Clean Energy ETF" },
                new Aktie { AktienID = 94, AktienSymbol = "ESGU", AktienName = "iShares ESG Aware MSCI USA ETF" },
                new Aktie { AktienID = 95, AktienSymbol = "ROBO", AktienName = "ROBO Global Robotics and Automation Index ETF" },
                
                // Europäische ETFs (auch auf US-Börsen handelbar)
                new Aktie { AktienID = 96, AktienSymbol = "VGK", AktienName = "Vanguard FTSE Europe ETF" },
                new Aktie { AktienID = 97, AktienSymbol = "EWG", AktienName = "iShares MSCI Germany ETF" },
                new Aktie { AktienID = 98, AktienSymbol = "EWU", AktienName = "iShares MSCI United Kingdom ETF" },
                new Aktie { AktienID = 99, AktienSymbol = "EWQ", AktienName = "iShares MSCI France ETF" },
                new Aktie { AktienID = 100, AktienSymbol = "EWI", AktienName = "iShares MSCI Italy ETF" }
            };
        }
    }
}