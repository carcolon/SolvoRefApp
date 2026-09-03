using Core.Models.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.DBContext.Configuration.Content
{
    public class HomeContentCardConfiguration : IEntityTypeConfiguration<HomeContentCard>
    {
        public static IReadOnlyList<HomeContentCard> GetDefaultCards()
        {
            var spotlight1 = Guid.Parse("23a7de73-304f-4fd9-a87a-db8e9d1cba01");
            var spotlight2 = Guid.Parse("23a7de73-304f-4fd9-a87a-db8e9d1cba02");
            var spotlight3 = Guid.Parse("23a7de73-304f-4fd9-a87a-db8e9d1cba03");
            var news1 = Guid.Parse("23a7de73-304f-4fd9-a87a-db8e9d1cba04");
            var news2 = Guid.Parse("23a7de73-304f-4fd9-a87a-db8e9d1cba05");
            var news3 = Guid.Parse("23a7de73-304f-4fd9-a87a-db8e9d1cba06");
            var createdAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            return
            [
                new HomeContentCard
                {
                    Id = spotlight1,
                    Section = "spotlight",
                    BadgeText = "",
                    BadgeVariant = "mint",
                    Title = "New referral incentive",
                    DescriptionHtml = "<p>Get up to $500 in rewards for each talent referred in January.</p>",
                    DateText = "",
                    ButtonText = "More information",
                    ActionType = "modal",
                    ActionValue = "incentive",
                    IconKey = "incentive-star",
                    ImageUrl = "",
                    DetailTitle = "New referral incentive",
                    DetailContentHtml = "<p>Get up to $500 in rewards for each talent referred in January.</p>",
                    DisplayOrder = 1,
                    IsPublished = true,
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt
                },
                new HomeContentCard
                {
                    Id = spotlight2,
                    Section = "spotlight",
                    BadgeText = "",
                    BadgeVariant = "teal",
                    Title = "Check out this week's open positions",
                    DescriptionHtml = "<p>Discover the new open positions and refer.</p>",
                    DateText = "",
                    ButtonText = "View Positions",
                    ActionType = "route",
                    ActionValue = "/ActivePosition",
                    IconKey = "positions-star",
                    ImageUrl = "",
                    DetailTitle = "Check out this week's open positions",
                    DetailContentHtml = "<p>Discover the new open positions and refer.</p>",
                    DisplayOrder = 2,
                    IsPublished = true,
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt
                },
                new HomeContentCard
                {
                    Id = spotlight3,
                    Section = "spotlight",
                    BadgeText = "",
                    BadgeVariant = "orange",
                    Title = "Stories of Soulvers who have already won",
                    DescriptionHtml = "<p>Discover the testimonials of colleagues who have already received their reward.</p>",
                    DateText = "",
                    ButtonText = "Read Stories",
                    ActionType = "modal",
                    ActionValue = "success",
                    IconKey = "success-card-icon",
                    ImageUrl = "",
                    DetailTitle = "Stories of Soulvers who have already won",
                    DetailContentHtml = "<p>Discover the testimonials of colleagues who have already received their reward.</p>",
                    DisplayOrder = 3,
                    IsPublished = true,
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt
                },
                new HomeContentCard
                {
                    Id = news1,
                    Section = "program_news",
                    BadgeText = "Update",
                    BadgeVariant = "update",
                    Title = "New incentive policy for 2026",
                    DescriptionHtml = "<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>",
                    DateText = "February 5, 2026",
                    ButtonText = "Read More",
                    ActionType = "modal",
                    ActionValue = "update",
                    IconKey = "",
                    ImageUrl = "",
                    DetailTitle = "New incentive policy for 2026",
                    DetailContentHtml = "<p>Review the latest changes to the referral incentive policy and eligibility rules.</p>",
                    DisplayOrder = 1,
                    IsPublished = true,
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt
                },
                new HomeContentCard
                {
                    Id = news2,
                    Section = "program_news",
                    BadgeText = "Community",
                    BadgeVariant = "testimony",
                    Title = "Community Story",
                    DescriptionHtml = "<p>Discover experiences and insights shared by members of our community.</p>",
                    DateText = "February 5, 2026",
                    ButtonText = "Read More",
                    ActionType = "modal",
                    ActionValue = "community",
                    IconKey = "",
                    ImageUrl = "",
                    DetailTitle = "Community Story",
                    DetailContentHtml = "<p>Discover experiences and insights shared by members of our community.</p>",
                    DisplayOrder = 2,
                    IsPublished = true,
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt
                },
                new HomeContentCard
                {
                    Id = news3,
                    Section = "program_news",
                    BadgeText = "Campaign",
                    BadgeVariant = "campaign",
                    Title = "Special Campaign",
                    DescriptionHtml = "<p>Discover the most urgent openings by country.</p>",
                    DateText = "February 5, 2026",
                    ButtonText = "Read More",
                    ActionType = "modal",
                    ActionValue = "campaign",
                    IconKey = "",
                    ImageUrl = "",
                    DetailTitle = "Special Campaign",
                    DetailContentHtml = "<p>Discover the most urgent openings by country.</p>",
                    DisplayOrder = 3,
                    IsPublished = true,
                    CreatedAtUtc = createdAt,
                    UpdatedAtUtc = createdAt
                }
            ];
        }

        public void Configure(EntityTypeBuilder<HomeContentCard> builder)
        {
            builder.ToTable("HomeContentCards");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Section).HasMaxLength(50).IsRequired();
            builder.Property(x => x.BadgeText).HasMaxLength(80);
            builder.Property(x => x.BadgeVariant).HasMaxLength(50);
            builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
            builder.Property(x => x.DateText).HasMaxLength(100);
            builder.Property(x => x.ButtonText).HasMaxLength(80);
            builder.Property(x => x.ActionType).HasMaxLength(30).IsRequired();
            builder.Property(x => x.ActionValue).HasMaxLength(2000);
            builder.Property(x => x.IconKey).HasMaxLength(120);
            builder.Property(x => x.ImageUrl).HasMaxLength(2000);
            builder.Property(x => x.LayoutJson).HasColumnType("nvarchar(max)");
            builder.Property(x => x.DetailTitle).HasMaxLength(200);
            builder.Property(x => x.DescriptionHtml).HasColumnType("nvarchar(max)");
            builder.Property(x => x.DetailContentHtml).HasColumnType("nvarchar(max)");

            builder.HasData(GetDefaultCards());
        }
    }
}
