using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class RestoreProgramNewsUpdateCardApr4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "HomeContentCards",
                keyColumn: "Id",
                keyValue: new Guid("23a7de73-304f-4fd9-a87a-db8e9d1cba04"));

            migrationBuilder.InsertData(
                table: "HomeContentCards",
                columns: new[]
                {
                    "Id",
                    "ActionType",
                    "ActionValue",
                    "BadgeText",
                    "BadgeVariant",
                    "ButtonText",
                    "CreatedAtUtc",
                    "DateText",
                    "DescriptionHtml",
                    "DetailContentHtml",
                    "DetailTitle",
                    "DisplayOrder",
                    "IconKey",
                    "ImageUrl",
                    "IsPublished",
                    "LayoutJson",
                    "PublishEndUtc",
                    "PublishStartUtc",
                    "Section",
                    "Title",
                    "UpdatedAtUtc"
                },
                values: new object[]
                {
                    new Guid("23a7de73-304f-4fd9-a87a-db8e9d1cba04"),
                    "modal",
                    "update",
                    "Update",
                    "update",
                    "Read More",
                    new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                    "February 5, 2026",
                    "<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>",
                    "<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>",
                    "New incentive policy for 2026",
                    1,
                    "",
                    "",
                    true,
                    null,
                    null,
                    null,
                    "program_news",
                    "New incentive policy for 2026",
                    new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "HomeContentCards",
                keyColumn: "Id",
                keyValue: new Guid("23a7de73-304f-4fd9-a87a-db8e9d1cba04"));
        }
    }
}
