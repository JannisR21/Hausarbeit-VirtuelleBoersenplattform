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
        /// Konfigurationen für die Tabellen und Beziehungen
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            // Hier könnten Seed-Daten eingefügt werden
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Initialisiert die Datenbank mit Beispieldaten
        /// </summary>
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Beispiel-Benutzer
            modelBuilder.Entity<Benutzer>().HasData(
                new Benutzer
                {
                    BenutzerID = 1,
                    Benutzername = "admin",
                    Email = "admin@example.com",
                    PasswortHash = "AQAAAAIAAYagAAAAECsUTzp+asdsa87d9sa9d87as9d879sa9d87a9sd7as97d9as87d=", // "admin" (Beispiel-Hash)
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
                    PasswortHash = "AQAAAAIAAYagAAAAEFGdg5c+dsadsad9sa7d9sa7d9sa7d8a7sd98as7d98as7d98as7d=", // "demo" (Beispiel-Hash)
                    Kontostand = 10000.00m,
                    Erstellungsdatum = System.DateTime.Now.AddDays(-15),
                    Vorname = "Demo",
                    Nachname = "User",
                    VollName = "Demo User"
                }
            );

            // Beispiel-Aktien
            modelBuilder.Entity<Aktie>().HasData(
                new Aktie { AktienID = 1, AktienSymbol = "AAPL", AktienName = "Apple Inc.", AktuellerPreis = 150.00m, Änderung = 1.25m, ÄnderungProzent = 0.84m, LetzteAktualisierung = System.DateTime.Now },
                new Aktie { AktienID = 2, AktienSymbol = "TSLA", AktienName = "Tesla Inc.", AktuellerPreis = 200.20m, Änderung = -0.70m, ÄnderungProzent = -0.35m, LetzteAktualisierung = System.DateTime.Now },
                new Aktie { AktienID = 3, AktienSymbol = "AMZN", AktienName = "Amazon.com Inc.", AktuellerPreis = 95.10m, Änderung = 0.72m, ÄnderungProzent = 0.76m, LetzteAktualisierung = System.DateTime.Now },
                new Aktie { AktienID = 4, AktienSymbol = "MSFT", AktienName = "Microsoft Corp.", AktuellerPreis = 320.45m, Änderung = 4.75m, ÄnderungProzent = 1.50m, LetzteAktualisierung = System.DateTime.Now },
                new Aktie { AktienID = 5, AktienSymbol = "GOOGL", AktienName = "Alphabet Inc.", AktuellerPreis = 128.75m, Änderung = -0.28m, ÄnderungProzent = -0.22m, LetzteAktualisierung = System.DateTime.Now }
            );

            // Beispiel-Portfolio-Einträge
            modelBuilder.Entity<PortfolioEintrag>().HasData(
                new PortfolioEintrag { BenutzerID = 1, AktienID = 1, AktienSymbol = "AAPL", AktienName = "Apple Inc.", Anzahl = 10, AktuellerKurs = 150.00m, EinstandsPreis = 145.00m, LetzteAktualisierung = System.DateTime.Now },
                new PortfolioEintrag { BenutzerID = 1, AktienID = 2, AktienSymbol = "TSLA", AktienName = "Tesla Inc.", Anzahl = 5, AktuellerKurs = 200.20m, EinstandsPreis = 210.00m, LetzteAktualisierung = System.DateTime.Now },
                new PortfolioEintrag { BenutzerID = 2, AktienID = 3, AktienSymbol = "AMZN", AktienName = "Amazon.com Inc.", Anzahl = 8, AktuellerKurs = 95.10m, EinstandsPreis = 90.00m, LetzteAktualisierung = System.DateTime.Now },
                new PortfolioEintrag { BenutzerID = 2, AktienID = 4, AktienSymbol = "MSFT", AktienName = "Microsoft Corp.", Anzahl = 12, AktuellerKurs = 320.45m, EinstandsPreis = 305.80m, LetzteAktualisierung = System.DateTime.Now }
            );
        }
    }
}