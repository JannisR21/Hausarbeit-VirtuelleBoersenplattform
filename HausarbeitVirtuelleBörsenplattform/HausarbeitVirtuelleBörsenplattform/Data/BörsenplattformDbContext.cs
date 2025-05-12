using Microsoft.EntityFrameworkCore;
using HausarbeitVirtuelleBörsenplattform.Models;

namespace HausarbeitVirtuelleBörsenplattform.Data
{
    /// <summary>
    /// Entity Framework Core Datenbankkontext für die virtuelle Börsenplattform
    /// </summary>
    public class BörsenplattformDbContext : DbContext
    {
        public BörsenplattformDbContext(DbContextOptions<BörsenplattformDbContext> options)
            : base(options)
        {
            // Erhöht die Robustheit der Datenbankverbindung
            this.Database.SetCommandTimeout(60); // Erhöhtes Timeout für SQL-Befehle
        }

        /// <summary>
        /// Benutzer-Tabelle
        /// </summary>
        public DbSet<Benutzer> Benutzer { get; set; }

        /// <summary>
        /// Aktien-Tabelle
        /// </summary>
        public DbSet<Aktie> Aktien { get; set; }

        /// <summary>
        /// Portfolio-Einträge-Tabelle
        /// </summary>
        public DbSet<PortfolioEintrag> PortfolioEintraege { get; set; }

        /// <summary>
        /// Watchlist-Einträge-Tabelle
        /// </summary>
        public DbSet<WatchlistEintrag> WatchlistEintraege { get; set; }

        /// <summary>
        /// Historische Aktienkurse-Tabelle
        /// </summary>
        public DbSet<AktienKursHistorie> AktienKursHistorie { get; set; }

        /// <summary>
        /// Erweiterte historische Aktiendaten-Tabelle
        /// </summary>
        public DbSet<HistorischeDatenErweitert> HistorischeDatenErweitert { get; set; }

        /// <summary>
        /// Konfigurationen für die Tabellen und Beziehungen
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SQL Server-Kompatibilitätseinstellungen, um einen größeren Versionsbereich zu unterstützen
            modelBuilder.HasAnnotation("SqlServer:CompatibilityMode", "120"); // SQL Server 2014 Kompatibilitätsmodus für bessere Abwärtskompatibilität

            // Benutzer-Konfiguration
            modelBuilder.Entity<Benutzer>(entity =>
            {
                entity.HasKey(e => e.BenutzerID);
                entity.Property(e => e.Benutzername).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswortHash).IsRequired();
                entity.Property(e => e.Kontostand).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Vorname).HasMaxLength(50);
                entity.Property(e => e.Nachname).HasMaxLength(50);
                entity.Property(e => e.VollName).HasMaxLength(100);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Benutzername).IsUnique();
            });

            // Aktie-Konfiguration
            modelBuilder.Entity<Aktie>(entity =>
            {
                entity.HasKey(e => e.AktienID);
                entity.Property(e => e.AktienSymbol).IsRequired().HasMaxLength(10);
                entity.Property(e => e.AktienName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AktuellerPreis).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Änderung).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ÄnderungProzent).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => e.AktienSymbol).IsUnique();
            });

            // PortfolioEintrag-Konfiguration
            modelBuilder.Entity<PortfolioEintrag>(entity =>
            {
                entity.HasKey(e => new { e.BenutzerID, e.AktienID }); // Zusammengesetzter Primärschlüssel
                entity.Property(e => e.AktienSymbol).IsRequired().HasMaxLength(10);
                entity.Property(e => e.AktienName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AktuellerKurs).HasColumnType("decimal(18,2)");
                entity.Property(e => e.EinstandsPreis).HasColumnType("decimal(18,2)");

                // Beziehungen
                entity.HasOne<Benutzer>()
                    .WithMany()
                    .HasForeignKey(e => e.BenutzerID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Aktie>()
                    .WithMany()
                    .HasForeignKey(e => e.AktienID)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            // WatchlistEintrag-Konfiguration
            modelBuilder.Entity<WatchlistEintrag>(entity =>
            {
                entity.HasKey(e => new { e.BenutzerID, e.AktienID }); // Zusammengesetzter Primärschlüssel
                entity.Property(e => e.AktienSymbol).IsRequired().HasMaxLength(10);
                entity.Property(e => e.AktienName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.KursBeimHinzufuegen).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AktuellerKurs).HasColumnType("decimal(18,2)");

                // Beziehungen
                entity.HasOne<Benutzer>()
                    .WithMany()
                    .HasForeignKey(e => e.BenutzerID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Aktie>()
                    .WithMany()
                    .HasForeignKey(e => e.AktienID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // AktienKursHistorie-Konfiguration
            modelBuilder.Entity<AktienKursHistorie>(entity =>
            {
                entity.HasKey(e => e.HistorieID); // Primärschlüssel
                entity.Property(e => e.AktienSymbol).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Datum).IsRequired();
                entity.Property(e => e.Eroeffnungskurs).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Hoechstkurs).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Tiefstkurs).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Schlusskurs).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ÄnderungProzent).HasColumnType("decimal(18,2)");

                // Erstelle einen zusammengesetzten Index für schnelle Abfragen nach Symbol und Datum
                entity.HasIndex(e => new { e.AktienSymbol, e.Datum });

                // Erstelle einen Index für die AktienID zur schnelleren Suche
                entity.HasIndex(e => e.AktienID);

                // Beziehung zur Aktien-Tabelle
                entity.HasOne<Aktie>()
                    .WithMany()
                    .HasForeignKey(e => e.AktienID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // HistorischeDatenErweitert-Konfiguration
            modelBuilder.Entity<HistorischeDatenErweitert>(entity =>
            {
                entity.HasKey(e => e.Id); // Primärschlüssel
                entity.Property(e => e.Datum).IsRequired();
                entity.Property(e => e.Eröffnungskurs).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Höchstkurs).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Tiefstkurs).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Schlusskurs).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ÄnderungProzent).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Intervall).IsRequired().HasMaxLength(20);

                // Erstelle einen zusammengesetzten Index für schnelle Abfragen nach AktieId und Datum
                entity.HasIndex(e => new { e.AktieId, e.Datum });

                // Erstelle einen Index für die AktieId zur schnelleren Suche
                entity.HasIndex(e => e.AktieId);

                // Erstelle einen Index für das Datum zur schnelleren Abfrage von Zeiträumen
                entity.HasIndex(e => e.Datum);

                // Beziehung zur Aktien-Tabelle
                entity.HasOne<Aktie>(e => e.Aktie)
                    .WithMany()
                    .HasForeignKey(e => e.AktieId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Basis-Benutzer für die erste Anmeldung, aber keine Dummy-Aktien mehr
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Initialisiert die Datenbank mit Basis-Benutzerdaten
        /// </summary>
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Basis-Benutzer für die Administration
            modelBuilder.Entity<Benutzer>().HasData(
                new Benutzer
                {
                    BenutzerID = 1,
                    Benutzername = "admin",
                    Email = "admin@example.com",
                    // Hash für 'admin'
                    PasswortHash = "$2a$12$eTxedgRvWVqcV9gOJ5ZOz.zqbTLwc7E0gIOZTSLVMPzb0OFaZqNQK",
                    Kontostand = 10000.00m,
                    Erstellungsdatum = System.DateTime.Now.AddDays(-30),
                    Vorname = "Admin",
                    Nachname = "User",
                    VollName = "Admin User"
                },
                new Benutzer
                {
                    BenutzerID = 2,
                    Benutzername = "demo",
                    Email = "demo@example.com",
                    // Hash für 'demo'
                    PasswortHash = "$2a$12$T30V4QZDsHRbGHqLPBPwleF0K27z0CFkFRgYLBVT8G3V36Ou.wJbu",
                    Kontostand = 10000.00m,
                    Erstellungsdatum = System.DateTime.Now.AddDays(-15),
                    Vorname = "Demo",
                    Nachname = "User",
                    VollName = "Demo User"
                }
            );
        }
    }
}