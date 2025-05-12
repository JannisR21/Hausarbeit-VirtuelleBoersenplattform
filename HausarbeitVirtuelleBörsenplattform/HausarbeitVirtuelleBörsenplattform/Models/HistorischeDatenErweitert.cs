using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HausarbeitVirtuelleBörsenplattform.Models
{
    /// <summary>
    /// Erweiterte Klasse für historische Aktiendaten mit zusätzlichen Feldern
    /// </summary>
    [Table("HistorischeDatenErweitert")]
    public class HistorischeDatenErweitert
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int AktieId { get; set; }

        [ForeignKey("AktieId")]
        public virtual Aktie Aktie { get; set; }

        [Required]
        public DateTime Datum { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Eröffnungskurs { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Höchstkurs { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Tiefstkurs { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Schlusskurs { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ÄnderungProzent { get; set; }

        public long? Volumen { get; set; }

        [Required]
        public string Intervall { get; set; }

        public DateTime ErstelltAm { get; set; } = DateTime.Now;
        public DateTime AktualisiertAm { get; set; } = DateTime.Now;

        // Berechnete Felder (nicht in der Datenbank gespeichert)
        [NotMapped]
        public decimal ÄnderungAbsolut 
        { 
            get => ÄnderungProzent * Schlusskurs / 100; 
        }

        /// <summary>
        /// Erstellt eine neue Instanz von HistorischeDatenErweitert
        /// </summary>
        public HistorischeDatenErweitert()
        {
        }

        /// <summary>
        /// Erstellt eine neue Instanz von HistorischeDatenErweitert mit den angegebenen Werten
        /// </summary>
        public HistorischeDatenErweitert(
            int aktieId, 
            DateTime datum, 
            decimal eröffnungskurs,
            decimal höchstkurs, 
            decimal tiefstkurs, 
            decimal schlusskurs, 
            decimal änderungProzent, 
            long? volumen = null,
            string intervall = "Täglich")
        {
            AktieId = aktieId;
            Datum = datum;
            Eröffnungskurs = eröffnungskurs;
            Höchstkurs = höchstkurs;
            Tiefstkurs = tiefstkurs;
            Schlusskurs = schlusskurs;
            ÄnderungProzent = änderungProzent;
            Volumen = volumen;
            Intervall = intervall;
        }
    }
}