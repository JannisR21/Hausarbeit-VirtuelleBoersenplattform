using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HausarbeitVirtuelleBörsenplattform.Migrations
{
    /// <summary>
    /// Migration zur Erstellung der HistorischeDatenErweitert-Tabelle
    /// </summary>
    public partial class HistorischeDatenErweitertMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorischeDatenErweitert",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AktieId = table.Column<int>(type: "int", nullable: false),
                    Datum = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Eröffnungskurs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Höchstkurs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tiefstkurs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Schlusskurs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ÄnderungProzent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Volumen = table.Column<long>(type: "bigint", nullable: true),
                    Intervall = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErstelltAm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AktualisiertAm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorischeDatenErweitert", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorischeDatenErweitert_Aktien_AktieId",
                        column: x => x.AktieId,
                        principalTable: "Aktien",
                        principalColumn: "AktienID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistorischeDatenErweitert_AktieId",
                table: "HistorischeDatenErweitert",
                column: "AktieId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorischeDatenErweitert_AktieId_Datum",
                table: "HistorischeDatenErweitert",
                columns: new[] { "AktieId", "Datum" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorischeDatenErweitert_Datum",
                table: "HistorischeDatenErweitert",
                column: "Datum");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorischeDatenErweitert");
        }
    }
}