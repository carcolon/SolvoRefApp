using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class ChangeReferralTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "Referral");

            migrationBuilder.RenameColumn(
                name: "HowHear",
                table: "Referral",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "CVFileName",
                table: "Referral",
                newName: "Account");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationDate",
                table: "Referral",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationDate",
                table: "Referral");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Referral",
                newName: "HowHear");

            migrationBuilder.RenameColumn(
                name: "Account",
                table: "Referral",
                newName: "CVFileName");

            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "Referral",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }
    }
}
