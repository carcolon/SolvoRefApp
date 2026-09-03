using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class CreateReferallDataWithSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReferralAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferralApplyAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralApplyAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferralCountries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralCountries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferralEnglishLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralEnglishLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferralExperiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralExperiences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferralFounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralFounds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferralCities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralCities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferralCities_ReferralCountries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "ReferralCountries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ReferralAccounts",
                columns: new[] { "Id", "Active", "Description" },
                values: new object[,]
                {
                    { 1, true, "Vensure" },
                    { 2, true, "Staff" },
                    { 3, true, "Inktel" },
                    { 4, true, "TBPO CR and SALES roles (Cyracom, Uly, TPG, Truly, Spirit, Honk, TTC)" },
                    { 5, true, "Pink Program (isolved)" },
                    { 6, true, "BackOffice" },
                    { 7, true, "I'm not referring to any particular account" },
                    { 8, true, "Other" }
                });

            migrationBuilder.InsertData(
                table: "ReferralApplyAreas",
                columns: new[] { "Id", "Active", "Description" },
                values: new object[,]
                {
                    { 1, true, "Accounting & Financial" },
                    { 2, true, "Administrative" },
                    { 3, true, "Collections" },
                    { 4, true, "Customer service" },
                    { 5, true, "IT" },
                    { 6, true, "Legal" },
                    { 7, true, "Marketing" },
                    { 8, true, "Recruitment" },
                    { 9, true, "Sales" }
                });

            migrationBuilder.InsertData(
                table: "ReferralCountries",
                columns: new[] { "Id", "Active", "Description" },
                values: new object[,]
                {
                    { 1, true, "Colombia" },
                    { 2, true, "Argentina" },
                    { 3, true, "México" },
                    { 4, true, "Guatemala" },
                    { 5, true, "Honduras" },
                    { 6, true, "Perú" },
                    { 7, true, "Kenya" }
                });

            migrationBuilder.InsertData(
                table: "ReferralEnglishLevels",
                columns: new[] { "Id", "Active", "Description" },
                values: new object[,]
                {
                    { 1, true, "B2-Intermediate" },
                    { 2, true, "C1-Advanced" },
                    { 3, true, "C2-Professional" }
                });

            migrationBuilder.InsertData(
                table: "ReferralExperiences",
                columns: new[] { "Id", "Active", "Description" },
                values: new object[,]
                {
                    { 1, true, "Junior (6 months to 1 year)" },
                    { 2, true, "Mid - level (1 to 3 year)" },
                    { 3, true, "Senior (3 years +)" }
                });

            migrationBuilder.InsertData(
                table: "ReferralFounds",
                columns: new[] { "Id", "Active", "Description" },
                values: new object[,]
                {
                    { 1, true, "Email campaign" },
                    { 2, true, "Social media ads" },
                    { 3, true, "Program info banner" },
                    { 4, true, "On-site activation or event" },
                    { 5, true, "Corporate onboarding" },
                    { 6, true, "Other" }
                });

            migrationBuilder.InsertData(
                table: "ReferralCities",
                columns: new[] { "Id", "Active", "CountryId", "Description" },
                values: new object[,]
                {
                    { 1, true, 1, "Medellín" },
                    { 2, true, 1, "Bogotá" },
                    { 3, true, 1, "Barranquilla" },
                    { 4, true, 1, "Cali" },
                    { 5, true, 1, "Bucaramanga" },
                    { 6, true, 2, "Buenos Aires -CABA" },
                    { 7, true, 2, "Mendoza" },
                    { 8, true, 2, "Córdoba" },
                    { 9, true, 3, "Mérida" },
                    { 10, true, 3, "Chihuahua" },
                    { 11, true, 4, "Ciudad de Guatemala" },
                    { 12, true, 5, "Tegucugalpa" },
                    { 13, true, 5, "San Pedro de Sula" },
                    { 14, true, 6, "Lima" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferralCities_CountryId",
                table: "ReferralCities",
                column: "CountryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferralAccounts");

            migrationBuilder.DropTable(
                name: "ReferralApplyAreas");

            migrationBuilder.DropTable(
                name: "ReferralCities");

            migrationBuilder.DropTable(
                name: "ReferralEnglishLevels");

            migrationBuilder.DropTable(
                name: "ReferralExperiences");

            migrationBuilder.DropTable(
                name: "ReferralFounds");

            migrationBuilder.DropTable(
                name: "ReferralCountries");
        }
    }
}
