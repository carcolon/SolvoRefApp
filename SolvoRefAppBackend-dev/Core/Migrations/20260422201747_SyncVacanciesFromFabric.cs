using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class SyncVacanciesFromFabric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [Vacancies];");

            migrationBuilder.DropColumn(
                name: "ClosingDate",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Openings",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Vacancies");

            migrationBuilder.AddColumn<string>(
                name: "ExternalVacancyId",
                table: "Vacancies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalVacancyId",
                table: "Vacancies");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Vacancies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosingDate",
                table: "Vacancies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Openings",
                table: "Vacancies",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
