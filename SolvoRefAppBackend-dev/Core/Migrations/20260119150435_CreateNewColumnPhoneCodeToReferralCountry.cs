using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class CreateNewColumnPhoneCodeToReferralCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneCode",
                table: "ReferralCountries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "ReferralCountries",
                keyColumn: "Id",
                keyValue: 1,
                column: "PhoneCode",
                value: "+57");

            migrationBuilder.UpdateData(
                table: "ReferralCountries",
                keyColumn: "Id",
                keyValue: 2,
                column: "PhoneCode",
                value: "+54");

            migrationBuilder.UpdateData(
                table: "ReferralCountries",
                keyColumn: "Id",
                keyValue: 3,
                column: "PhoneCode",
                value: "+52");

            migrationBuilder.UpdateData(
                table: "ReferralCountries",
                keyColumn: "Id",
                keyValue: 4,
                column: "PhoneCode",
                value: "+502");

            migrationBuilder.UpdateData(
                table: "ReferralCountries",
                keyColumn: "Id",
                keyValue: 5,
                column: "PhoneCode",
                value: "+504");

            migrationBuilder.UpdateData(
                table: "ReferralCountries",
                keyColumn: "Id",
                keyValue: 6,
                column: "PhoneCode",
                value: "+51");

            migrationBuilder.UpdateData(
                table: "ReferralCountries",
                keyColumn: "Id",
                keyValue: 7,
                column: "PhoneCode",
                value: "+254");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneCode",
                table: "ReferralCountries");
        }
    }
}
