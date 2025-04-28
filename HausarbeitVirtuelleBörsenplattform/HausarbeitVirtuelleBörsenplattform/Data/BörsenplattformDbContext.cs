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
        /// Watchlist-Einträge-Tabelle
        /// </summary>
        public DbSet<WatchlistEintrag> WatchlistEintraege { get; set; }

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