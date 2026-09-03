using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTbpoReferralAccountLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ReferralAccounts",
                keyColumn: "Id",
                keyValue: 4,
                column: "Description",
                value: "TBPO CR and SALES roles (Cyracom, Uly, TPG - Travel Pass, Propio, Nolan, Netsol, JLR, Truly, Urgently EHI UDA, Spirit, Honk, TTC)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ReferralAccounts",
                keyColumn: "Id",
                keyValue: 4,
                column: "Description",
                value: "TBPO CR and SALES roles (Cyracom, Uly, TPG, Truly, Spirit, Honk, TTC)");
        }
    }
}
