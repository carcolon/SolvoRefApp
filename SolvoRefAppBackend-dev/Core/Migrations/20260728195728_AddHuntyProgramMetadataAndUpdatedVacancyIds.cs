using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddHuntyProgramMetadataAndUpdatedVacancyIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProgramName",
                table: "CountriesHuntyInformation",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProgramType",
                table: "CountriesHuntyInformation",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ProgramName", "ProgramType", "VacancyId" },
                values: new object[] { "Referral Program Colombia", "Referidos", "69fa4ef050598f0910812fbc" });

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ProgramName", "ProgramType", "VacancyId" },
                values: new object[] { "Referall Program Argentina", "Referidos", "69fcf140fd194c2d84863ad4" });

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ProgramName", "ProgramType", "VacancyId" },
                values: new object[] { "Referral Program Mexico", "Referidos", "69fe5f823054dc6c56a46ffe" });

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ProgramName", "ProgramType", "VacancyId" },
                values: new object[] { "Referral Program Guatemala", "Referidos", "69fe6c753054dc6c56a48257" });

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ProgramName", "ProgramType", "VacancyId" },
                values: new object[] { "Referall Program Honduras", "Referidos", "69fe01153054dc6c56a3eee4" });

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ProgramName", "ProgramType", "VacancyId" },
                values: new object[] { "Referral Program Peru", "Referidos", "69fe0639df45ad5a5d249634" });

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ProgramName", "ProgramType", "VacancyId" },
                values: new object[] { "Referall Program Kenya", "Referidos", "69fe687850598f09108203b9" });

            migrationBuilder.InsertData(
                table: "CountriesHuntyInformation",
                columns: new[] { "Id", "Api_key", "CompanyId", "Country", "ProgramName", "ProgramType", "VacancyId" },
                values: new object[,]
                {
                    { 8, "ak_REDACTED", "92ecedb8-d6ab-477a-84e9-03663e2d80b1", "Argentina", "Vacantes TBPO Argentina Referidos", "Referidos", "6a5962062071b02aca17aaa0" },
                    { 9, "ak_REDACTED", "f5a1e34a-c171-4660-aef4-4d84d8e98c3c", "Colombia", "Vacantes TBPO Colombia Referidos", "Referidos", "6a5965ed564d697f4d61f520" },
                    { 10, "ak_REDACTED", "e8d866b1-1743-4504-92f2-8ef0a64d74ec", "Mexico", "Vacantes TBPO Mexico Referidos", "Referidos", "6a5a2e98b3c00994dc176018" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DropColumn(
                name: "ProgramName",
                table: "CountriesHuntyInformation");

            migrationBuilder.DropColumn(
                name: "ProgramType",
                table: "CountriesHuntyInformation");

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 1,
                column: "VacancyId",
                value: "696e894f49c12683fb81a59f");

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 2,
                column: "VacancyId",
                value: "6978dc2c802281ef5ffeb013");

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 3,
                column: "VacancyId",
                value: "6978c8763a5287c0e4671991");

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 4,
                column: "VacancyId",
                value: "6978d49de37d1c5b2a928182");

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 5,
                column: "VacancyId",
                value: "6978d62da2b1f3bec591fa07");

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 6,
                column: "VacancyId",
                value: "6978de8ce37d1c5b2a928b1e");

            migrationBuilder.UpdateData(
                table: "CountriesHuntyInformation",
                keyColumn: "Id",
                keyValue: 7,
                column: "VacancyId",
                value: "6978df21e37d1c5b2a928ba6");
        }
    }
}
