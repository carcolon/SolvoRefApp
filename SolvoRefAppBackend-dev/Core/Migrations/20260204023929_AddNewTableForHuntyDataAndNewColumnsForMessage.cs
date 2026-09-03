using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTableForHuntyDataAndNewColumnsForMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMessage",
                table: "Referral",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Updatable",
                table: "Referral",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CountriesHuntyInformation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VacancyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Api_key = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountriesHuntyInformation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HolyDatesCountryCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataLakeCountryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NagerCountryCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolyDatesCountryCodes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CountriesHuntyInformation",
                columns: new[] { "Id", "Api_key", "CompanyId", "Country", "VacancyId" },
                values: new object[,]
                {
                    { 1, "ak_REDACTED", "f5a1e34a-c171-4660-aef4-4d84d8e98c3c", "Colombia", "696e894f49c12683fb81a59f" },
                    { 2, "ak_REDACTED", "92ecedb8-d6ab-477a-84e9-03663e2d80b1", "Argentina", "6978dc2c802281ef5ffeb013" },
                    { 3, "ak_REDACTED", "e8d866b1-1743-4504-92f2-8ef0a64d74ec", "Mexico", "6978c8763a5287c0e4671991" },
                    { 4, "ak_REDACTED", "defa286f-c14a-453d-8500-b24ed080ed82", "Guatemala", "6978d49de37d1c5b2a928182" },
                    { 5, "ak_REDACTED", "8ac2eaa9-da7c-4a4b-8ed1-bb43c264c95e", "Honduras", "6978d62da2b1f3bec591fa07" },
                    { 6, "ak_REDACTED", "d3f8fbdc-08fc-462d-bdb7-35e730032fda", "Peru", "6978de8ce37d1c5b2a928b1e" },
                    { 7, "ak_REDACTED", "1a20f16e-08d2-46cc-90b8-00b4104d936a", "Kenya", "6978df21e37d1c5b2a928ba6" }
                });

            migrationBuilder.InsertData(
                table: "HolyDatesCountryCodes",
                columns: new[] { "Id", "DataLakeCountryName", "NagerCountryCode" },
                values: new object[,]
                {
                    { 1, "kenya", "KE  " },
                    { 2, "guatemala", "GT" },
                    { 3, "el salvador", "SV" },
                    { 4, "nicaragua", "NI" },
                    { 5, "honduras", "HN" },
                    { 6, "belize", "BZ" },
                    { 7, "philippines", "PH" },
                    { 8, "jamaica", "JM" },
                    { 9, "argentina", "AR" },
                    { 10, "brasil", "BR" },
                    { 11, "chile", "CL" },
                    { 12, "perú", "PE" },
                    { 13, "españa", "ES" },
                    { 14, "india", "IN" },
                    { 15, "united states", "US" },
                    { 16, "ecuador", "EC" },
                    { 17, "nigeria", "NG" },
                    { 18, "dominican republic", "DO" },
                    { 19, "greece", "GR" },
                    { 20, "costa rica", "CR" },
                    { 21, "paraguay", "PY" },
                    { 22, "canada", "CA" },
                    { 23, "south africa", "ZA" }
                });

            migrationBuilder.UpdateData(
                table: "ReferralCountries",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "Mexico");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CountriesHuntyInformation");

            migrationBuilder.DropTable(
                name: "HolyDatesCountryCodes");

            migrationBuilder.DropColumn(
                name: "PaymentMessage",
                table: "Referral");

            migrationBuilder.DropColumn(
                name: "Updatable",
                table: "Referral");

            migrationBuilder.UpdateData(
                table: "ReferralCountries",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "México");
        }
    }
}
