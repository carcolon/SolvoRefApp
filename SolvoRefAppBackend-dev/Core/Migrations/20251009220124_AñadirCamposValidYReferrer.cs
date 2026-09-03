using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AñadirCamposValidYReferrer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "Referral",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferrerID",
                table: "Referral",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Referral_ReferrerID",
                table: "Referral",
                column: "ReferrerID");

            migrationBuilder.AddForeignKey(
                name: "FK_Referral_AspNetUsers_ReferrerID",
                table: "Referral",
                column: "ReferrerID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Referral_AspNetUsers_ReferrerID",
                table: "Referral");

            migrationBuilder.DropIndex(
                name: "IX_Referral_ReferrerID",
                table: "Referral");

            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "Referral");

            migrationBuilder.DropColumn(
                name: "ReferrerID",
                table: "Referral");
        }
    }
}
