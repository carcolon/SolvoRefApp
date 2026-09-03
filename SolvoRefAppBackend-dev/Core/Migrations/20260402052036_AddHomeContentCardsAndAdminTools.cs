using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddHomeContentCardsAndAdminTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomeContentCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Section = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BadgeText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    BadgeVariant = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DescriptionHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ButtonText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActionValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DetailTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DetailContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishStartUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomeContentCards", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "HomeContentCards",
                columns: new[] { "Id", "ActionType", "ActionValue", "BadgeText", "BadgeVariant", "ButtonText", "CreatedAtUtc", "DateText", "DescriptionHtml", "DetailContentHtml", "DetailTitle", "DisplayOrder", "IconKey", "ImageUrl", "IsPublished", "PublishEndUtc", "PublishStartUtc", "Section", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("23a7de73-304f-4fd9-a87a-db8e9d1cba01"), "modal", "incentive", "", "mint", "More information", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "", "<p>Get up to $500 in rewards for each talent referred in January.</p>", "<p>Get up to $500 in rewards for each talent referred in January.</p>", "New referral incentive", 1, "incentive-star", "", true, null, null, "spotlight", "New referral incentive", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("23a7de73-304f-4fd9-a87a-db8e9d1cba02"), "route", "/ActivePosition", "", "teal", "View Positions", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "", "<p>Discover the new open positions and refer.</p>", "<p>Discover the new open positions and refer.</p>", "Check out this week's open positions", 2, "positions-star", "", true, null, null, "spotlight", "Check out this week's open positions", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("23a7de73-304f-4fd9-a87a-db8e9d1cba03"), "modal", "success", "", "orange", "Read Stories", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "", "<p>Discover the testimonials of colleagues who have already received their reward.</p>", "<p>Discover the testimonials of colleagues who have already received their reward.</p>", "Stories of Soulvers who have already won", 3, "success-card-icon", "", true, null, null, "spotlight", "Stories of Soulvers who have already won", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("23a7de73-304f-4fd9-a87a-db8e9d1cba04"), "modal", "update", "Update", "update", "Read More", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "February 5, 2026", "<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>", "<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>", "New incentive policy for 2026", 1, "", "", true, null, null, "program_news", "New incentive policy for 2026", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("23a7de73-304f-4fd9-a87a-db8e9d1cba05"), "modal", "community", "Community", "testimony", "Read More", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "February 5, 2026", "<p>Discover experiences and insights shared by members of our community.</p>", "<p>Discover experiences and insights shared by members of our community.</p>", "Community Story", 2, "", "", true, null, null, "program_news", "Community Story", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("23a7de73-304f-4fd9-a87a-db8e9d1cba06"), "modal", "campaign", "Campaign", "campaign", "Read More", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc), "February 5, 2026", "<p>Discover the most urgent openings by country.</p>", "<p>Discover the most urgent openings by country.</p>", "Special Campaign", 3, "", "", true, null, null, "program_news", "Special Campaign", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomeContentCards");
        }
    }
}
