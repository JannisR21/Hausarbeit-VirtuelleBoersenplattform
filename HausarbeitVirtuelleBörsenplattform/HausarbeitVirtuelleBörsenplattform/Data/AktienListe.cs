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
                
                // Europäische Aktien – Deutschland (über ADRs)
                new Aktie { AktienID = 26, AktienSymbol = "SAP", AktienName = "SAP SE" }, // NYSE
                new Aktie { AktienID = 27, AktienSymbol = "SIEGY", AktienName = "Siemens AG" }, // OTC
                new Aktie { AktienID = 28, AktienSymbol = "ALIZY", AktienName = "Allianz SE" }, // OTC
                new Aktie { AktienID = 29, AktienSymbol = "BAYRY", AktienName = "Bayer AG" }, // OTC
                new Aktie { AktienID = 30, AktienSymbol = "BASFY", AktienName = "BASF SE" }, // OTC
                new Aktie { AktienID = 31, AktienSymbol = "DTEGY", AktienName = "Deutsche Telekom AG" }, // OTC
                new Aktie { AktienID = 32, AktienSymbol = "BMWYY", AktienName = "Bayerische Motoren Werke AG" }, // OTC
                new Aktie { AktienID = 33, AktienSymbol = "MBGYY", AktienName = "Mercedes-Benz Group AG" }, // OTC
                new Aktie { AktienID = 34, AktienSymbol = "VWAGY", AktienName = "Volkswagen AG" }, // OTC
                new Aktie { AktienID = 35, AktienSymbol = "ADDYY", AktienName = "Adidas AG" }, // OTC

                
                // Europäische Aktien - Frankreich
                new Aktie { AktienID = 36, AktienSymbol = "MC.PA", AktienName = "LVMH Moët Hennessy Louis Vuitton SE" },
                new Aktie { AktienID = 37, AktienSymbol = "OR.PA", AktienName = "L'Oréal S.A." },
                new Aktie { AktienID = 38, AktienSymbol = "SAN.PA", AktienName = "Sanofi S.A." },
                new Aktie { AktienID = 39, AktienSymbol = "BNP.PA", AktienName = "BNP Paribas S.A." },
                new Aktie { AktienID = 40, AktienSymbol = "AI.PA", AktienName = "Air Liquide S.A." },
                
                // Europäische Aktien - Schweiz
                new Aktie { AktienID = 41, AktienSymbol = "NESN.SW", AktienName = "Nestlé S.A." },
                new Aktie { AktienID = 42, AktienSymbol = "ROG.SW", AktienName = "Roche Holding AG" },
                new Aktie { AktienID = 43, AktienSymbol = "NOVN.SW", AktienName = "Novartis AG" },
                
                // Europäische Aktien - Großbritannien
                new Aktie { AktienID = 44, AktienSymbol = "HSBA.L", AktienName = "HSBC Holdings plc" },
                new Aktie { AktienID = 45, AktienSymbol = "BP.L", AktienName = "BP plc" },
                new Aktie { AktienID = 46, AktienSymbol = "GSK.L", AktienName = "GSK plc" },
                new Aktie { AktienID = 47, AktienSymbol = "AZN.L", AktienName = "AstraZeneca plc" },
                
                // Europäische Aktien - Niederlande
                new Aktie { AktienID = 48, AktienSymbol = "ASML.AS", AktienName = "ASML Holding N.V." },
                new Aktie { AktienID = 49, AktienSymbol = "UNA.AS", AktienName = "Unilever plc" },
                
                // Europäische Aktien - Spanien
                new Aktie { AktienID = 50, AktienSymbol = "SAN.MC", AktienName = "Banco Santander, S.A." },
                new Aktie { AktienID = 51, AktienSymbol = "IBE.MC", AktienName = "Iberdrola, S.A." },
                
                // Europäische Aktien - Italien
                new Aktie { AktienID = 52, AktienSymbol = "ENI.MI", AktienName = "Eni S.p.A." },
                new Aktie { AktienID = 53, AktienSymbol = "ISP.MI", AktienName = "Intesa Sanpaolo S.p.A." },
                
                // Europäische Aktien - Schweden
                new Aktie { AktienID = 54, AktienSymbol = "ATCO-A.ST", AktienName = "Atlas Copco AB" },
                
                // US-Aktien - Weitere wichtige
                new Aktie { AktienID = 55, AktienSymbol = "ORCL", AktienName = "Oracle Corporation" },
                new Aktie { AktienID = 56, AktienSymbol = "PYPL", AktienName = "PayPal Holdings, Inc." },
                new Aktie { AktienID = 57, AktienSymbol = "CMCSA", AktienName = "Comcast Corporation" },
                new Aktie { AktienID = 58, AktienSymbol = "PEP", AktienName = "PepsiCo, Inc." },
                new Aktie { AktienID = 59, AktienSymbol = "NKE", AktienName = "Nike, Inc." },
                new Aktie { AktienID = 60, AktienSymbol = "TMO", AktienName = "Thermo Fisher Scientific Inc." },
                
                // Rohstoffe und Energie
                new Aktie { AktienID = 61, AktienSymbol = "XOM", AktienName = "Exxon Mobil Corporation" },
                new Aktie { AktienID = 62, AktienSymbol = "CVX", AktienName = "Chevron Corporation" },
                new Aktie { AktienID = 63, AktienSymbol = "RIO", AktienName = "Rio Tinto Group" },
                new Aktie { AktienID = 64, AktienSymbol = "BHP", AktienName = "BHP Group Limited" },
                
                // Finanzen und Banken
                new Aktie { AktienID = 65, AktienSymbol = "GS", AktienName = "Goldman Sachs Group Inc." },
                new Aktie { AktienID = 66, AktienSymbol = "MS", AktienName = "Morgan Stanley" },
                new Aktie { AktienID = 67, AktienSymbol = "C", AktienName = "Citigroup Inc." },
                new Aktie { AktienID = 68, AktienSymbol = "WFC", AktienName = "Wells Fargo & Company" },
                
                // Pharma und Gesundheitswesen
                new Aktie { AktienID = 69, AktienSymbol = "MRK", AktienName = "Merck & Co., Inc." },
                new Aktie { AktienID = 70, AktienSymbol = "ABBV", AktienName = "AbbVie Inc." },
                
                // Automobil
                new Aktie { AktienID = 71, AktienSymbol = "F", AktienName = "Ford Motor Company" },
                new Aktie { AktienID = 72, AktienSymbol = "GM", AktienName = "General Motors Company" },
                
                // Telekommunikation
                new Aktie { AktienID = 73, AktienSymbol = "T", AktienName = "AT&T Inc." },
                new Aktie { AktienID = 74, AktienSymbol = "VZ", AktienName = "Verizon Communications Inc." },
                new Aktie { AktienID = 75, AktienSymbol = "TMUS", AktienName = "T-Mobile US, Inc." }
            };
        }
    }
}