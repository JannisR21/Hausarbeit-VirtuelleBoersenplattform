using System;
using System.Collections.Generic;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Definiert die verschiedenen Arten von Anlageprodukten, die auf der Plattform gehandelt werden können
    /// </summary>
    public enum AnlageTyp
    {
        EINZELAKTIE = 0,
        ETF = 1,
        FOND = 2,
        ANLEIHE = 3,
        ZERTIFIKAT = 4
    }

    /// <summary>
    /// Statische Klasse mit Methoden und Eigenschaften für Anlagetypen
    /// </summary>
    public static class AnlageTypHelper
    {
        /// <summary>
        /// Liefert alle verfügbaren Anlagetypen als Liste zurück
        /// </summary>
        public static List<AnlageTyp> AlleAnlageTypen => new List<AnlageTyp>
        {
            AnlageTyp.EINZELAKTIE,
            AnlageTyp.ETF,
            AnlageTyp.FOND,
            AnlageTyp.ANLEIHE,
            AnlageTyp.ZERTIFIKAT
        };

        /// <summary>
        /// Gibt den anzeigbaren Namen für einen Anlagetyp zurück
        /// </summary>
        public static string GetAnlageTypName(AnlageTyp typ)
        {
            return typ switch
            {
                AnlageTyp.EINZELAKTIE => "Einzelaktie",
                AnlageTyp.ETF => "ETF",
                AnlageTyp.FOND => "Investmentfonds",
                AnlageTyp.ANLEIHE => "Anleihe",
                AnlageTyp.ZERTIFIKAT => "Zertifikat",
                _ => "Unbekannt"
            };
        }

        /// <summary>
        /// Gibt die verfügbaren Anlagetypen als Dictionary für ComboBox-Anbindung zurück
        /// </summary>
        public static Dictionary<AnlageTyp, string> AnlageTypenAlsDictionary()
        {
            var dict = new Dictionary<AnlageTyp, string>();
            foreach (var typ in AlleAnlageTypen)
            {
                dict.Add(typ, GetAnlageTypName(typ));
            }
            return dict;
        }
    }
}