using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSolvoPartnersProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReferralFromSolvoPartner",
                table: "Referral",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReferrerSolvoPartnerStatus",
                table: "Referral",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SolvoPartnerStatus",
                table: "AspNetUsers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferralFromSolvoPartner",
                table: "Referral");

            migrationBuilder.DropColumn(
                name: "ReferrerSolvoPartnerStatus",
                table: "Referral");

            migrationBuilder.DropColumn(
                name: "SolvoPartnerStatus",
                table: "AspNetUsers");
        }
    }
}
