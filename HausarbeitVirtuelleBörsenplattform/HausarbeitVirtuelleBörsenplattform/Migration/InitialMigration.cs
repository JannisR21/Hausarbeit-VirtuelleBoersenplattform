using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HausarbeitVirtuelleBörsenplattform.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Aktien",
                columns: table => new
                {
                    AktienID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AktienSymbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AktienName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AktuellerPreis = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Änderung = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ÄnderungProzent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LetzteAktualisierung = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aktien", x => x.AktienID);
                });

            migrationBuilder.CreateTable(
                name: "Benutzer",
                columns: table => new
                {
                    BenutzerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Benutzername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswortHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Erstellungsdatum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kontostand = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benutzer", x => x.BenutzerID);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioEintraege",
                columns: table => new
                {
                    BenutzerID = table.Column<int>(type: "int", nullable: false),
                    AktienID = table.Column<int>(type: "int", nullable: false),
                    AktienSymbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AktienName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Anzahl = table.Column<int>(type: "int", nullable: false),
                    AktuellerKurs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EinstandsPreis = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LetzteAktualisierung = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioEintraege", x => new { x.BenutzerID, x.AktienID });
                    table.ForeignKey(
                        name: "FK_PortfolioEintraege_Aktien_AktienID",
                        column: x => x.AktienID,
                        principalTable: "Aktien",
                        principalColumn: "AktienID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PortfolioEintraege_Benutzer_BenutzerID",
                        column: x => x.BenutzerID,
                        principalTable: "Benutzer",
                        principalColumn: "BenutzerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aktien_AktienSymbol",
                table: "Aktien",
                column: "AktienSymbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Benutzer_Benutzername",
                table: "Benutzer",
                column: "Benutzername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Benutzer_Email",
                table: "Benutzer",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioEintraege_AktienID",
                table: "PortfolioEintraege",
                column: "AktienID");

            // Beispieldaten einfügen
            InsertSeedData(migrationBuilder);
        }

        private void InsertSeedData(MigrationBuilder migrationBuilder)
        {
            // Beispielbenutzer einfügen
            migrationBuilder.InsertData(
                table: "Benutzer",
                columns: new[] { "BenutzerID", "Benutzername", "Email", "PasswortHash", "Erstellungsdatum", "Kontostand" },
                values: new object[,]
                {
                    {
                        1,
                        "admin",
                        "admin@example.com",
                        "$2a$12$eTxedgRvWVqcV9gOJ5ZOz.zqbTLwc7E0gIOZTSLVMPzb0OFaZqNQK", // Hash für "admin"
                        DateTime.Now.AddDays(-30),
                        10000.00m
                    },
                    {
                        2,
                        "demo",
                        "demo@example.com",
                        "$2a$12$T30V4QZDsHRbGHqLPBPwleF0K27z0CFkFRgYLBVT8G3V36Ou.wJbu", // Hash für "demo"
                        DateTime.Now.AddDays(-15),
                        10000.00m
                    }
                });

            // Beispielaktien einfügen
            migrationBuilder.InsertData(
                table: "Aktien",
                columns: new[] { "AktienID", "AktienSymbol", "AktienName", "AktuellerPreis", "Änderung", "ÄnderungProzent", "LetzteAktualisierung" },
                values: new object[,]
                {
                    { 1, "AAPL", "Apple Inc.", 150.00m, 1.25m, 0.84m, DateTime.Now },
                    { 2, "TSLA", "Tesla Inc.", 200.20m, -0.70m, -0.35m, DateTime.Now },
                    { 3, "AMZN", "Amazon.com Inc.", 95.10m, 0.72m, 0.76m, DateTime.Now },
                    { 4, "MSFT", "Microsoft Corp.", 320.45m, 4.75m, 1.50m, DateTime.Now },
                    { 5, "GOOGL", "Alphabet Inc.", 128.75m, -0.28m, -0.22m, DateTime.Now }
                });

            // Beispiel-Portfolio-Einträge
            migrationBuilder.InsertData(
                table: "PortfolioEintraege",
                columns: new[] { "BenutzerID", "AktienID", "AktienSymbol", "AktienName", "Anzahl", "AktuellerKurs", "EinstandsPreis", "LetzteAktualisierung" },
                values: new object[,]
                {
                    { 1, 1, "AAPL", "Apple Inc.", 10, 150.00m, 145.00m, DateTime.Now },
                    { 1, 2, "TSLA", "Tesla Inc.", 5, 200.20m, 210.00m, DateTime.Now },
                    { 2, 3, "AMZN", "Amazon.com Inc.", 8, 95.10m, 90.00m, DateTime.Now },
                    { 2, 4, "MSFT", "Microsoft Corp.", 12, 320.45m, 305.80m, DateTime.Now }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioEintraege");

            migrationBuilder.DropTable(
                name: "Aktien");

            migrationBuilder.DropTable(
                name: "Benutzer");
        }
    }
}