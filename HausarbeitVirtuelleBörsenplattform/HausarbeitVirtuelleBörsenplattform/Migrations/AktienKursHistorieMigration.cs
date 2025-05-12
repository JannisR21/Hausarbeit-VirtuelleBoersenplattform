using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace HausarbeitVirtuelleBörsenplattform.Migrations
{
    /// <summary>
    /// Migration, die die AktienKursHistorie-Tabelle hinzufügt
    /// </summary>
    public partial class AktienKursHistorieMigration : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <summary>
        /// Anwenden der Migration (Tabelle erstellen)
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AktienKursHistorie",
                columns: table => new
                {
                    HistorieID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AktienID = table.Column<int>(nullable: false),
                    AktienSymbol = table.Column<string>(maxLength: 10, nullable: false),
                    Datum = table.Column<DateTime>(nullable: false),
                    Eroeffnungskurs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Hoechstkurs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tiefstkurs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Schlusskurs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Volumen = table.Column<long>(nullable: false),
                    ÄnderungProzent = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AktienKursHistorie", x => x.HistorieID);
                    table.ForeignKey(
                        name: "FK_AktienKursHistorie_Aktien_AktienID",
                        column: x => x.AktienID,
                        principalTable: "Aktien",
                        principalColumn: "AktienID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AktienKursHistorie_AktienID",
                table: "AktienKursHistorie",
                column: "AktienID");

            migrationBuilder.CreateIndex(
                name: "IX_AktienKursHistorie_AktienSymbol_Datum",
                table: "AktienKursHistorie",
                columns: new[] { "AktienSymbol", "Datum" });
        }

        /// <summary>
        /// Rückgängig machen der Migration (Tabelle löschen)
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AktienKursHistorie");
        }
    }
}