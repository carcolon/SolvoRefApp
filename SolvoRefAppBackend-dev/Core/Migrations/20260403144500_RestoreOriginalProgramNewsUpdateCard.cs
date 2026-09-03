using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    public partial class RestoreOriginalProgramNewsUpdateCard : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "HomeContentCards",
                keyColumn: "Id",
                keyValue: new Guid("7d63fd48-b6fd-4f89-8c53-66f875a91d41"));

            migrationBuilder.InsertData(
                table: "HomeContentCards",
                columns: new[]
                {
                    "Id",
                    "Section",
                    "BadgeText",
                    "BadgeVariant",
                    "Title",
                    "DescriptionHtml",
                    "DateText",
                    "ButtonText",
                    "ActionType",
                    "ActionValue",
                    "IconKey",
                    "ImageUrl",
                    "LayoutJson",
                    "DetailTitle",
                    "DetailContentHtml",
                    "DisplayOrder",
                    "IsPublished",
                    "PublishStartUtc",
                    "PublishEndUtc",
                    "CreatedAtUtc",
                    "UpdatedAtUtc"
                },
                values: new object[]
                {
                    new Guid("23a7de73-304f-4fd9-a87a-db8e9d1cba04"),
                    "program_news",
                    "Update",
                    "update",
                    "New incentive policy for 2026",
                    "<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>",
                    "February 5, 2026",
                    "Read More",
                    "modal",
                    "update",
                    "",
                    "",
                    null,
                    "New incentive policy for 2026",
                    "<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>",
                    1,
                    true,
                    null,
                    null,
                    new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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
                    "Section",
                    "BadgeText",
                    "BadgeVariant",
                    "Title",
                    "DescriptionHtml",
                    "DateText",
                    "ButtonText",
                    "ActionType",
                    "ActionValue",
                    "IconKey",
                    "ImageUrl",
                    "LayoutJson",
                    "DetailTitle",
                    "DetailContentHtml",
                    "DisplayOrder",
                    "IsPublished",
                    "PublishStartUtc",
                    "PublishEndUtc",
                    "CreatedAtUtc",
                    "UpdatedAtUtc"
                },
                values: new object[]
                {
                    new Guid("7d63fd48-b6fd-4f89-8c53-66f875a91d41"),
                    "program_news",
                    "Update",
                    "update",
                    "New incentive policy for 2026",
                    "<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>",
                    "February 5, 2026",
                    "Read More",
                    "modal",
                    "update",
                    "",
                    "",
                    null,
                    "New incentive policy for 2026",
                    "<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>",
                    1,
                    true,
                    null,
                    null,
                    new DateTime(2026, 4, 3, 11, 30, 0, DateTimeKind.Utc),
                    new DateTime(2026, 4, 3, 11, 30, 0, DateTimeKind.Utc)
                });
        }
    }
}
