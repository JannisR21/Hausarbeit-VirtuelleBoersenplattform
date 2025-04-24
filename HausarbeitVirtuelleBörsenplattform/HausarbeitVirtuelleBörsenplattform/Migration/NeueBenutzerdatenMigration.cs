using Microsoft.EntityFrameworkCore.Migrations;

namespace HausarbeitVirtuelleBörsenplattform.Migrations
{
    public partial class NeueBenutzerdaten : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nachname",
                table: "Benutzer",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VollName",
                table: "Benutzer",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Vorname",
                table: "Benutzer",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Aktualisiere bestehende Demo-Benutzer mit Standardwerten
            migrationBuilder.UpdateData(
                table: "Benutzer",
                keyColumn: "BenutzerID",
                keyValue: 1,
                columns: new[] { "Vorname", "Nachname", "VollName" },
                values: new object[] { "Admin", "User", "Admin User" });

            migrationBuilder.UpdateData(
                table: "Benutzer",
                keyColumn: "BenutzerID",
                keyValue: 2,
                columns: new[] { "Vorname", "Nachname", "VollName" },
                values: new object[] { "Demo", "User", "Demo User" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nachname",
                table: "Benutzer");

            migrationBuilder.DropColumn(
                name: "VollName",
                table: "Benutzer");

            migrationBuilder.DropColumn(
                name: "Vorname",
                table: "Benutzer");
        }
    }
}