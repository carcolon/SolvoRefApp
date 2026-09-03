namespace Core.Models.Content
{
    public class HomeContentCard
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Section { get; set; } = string.Empty;
        public string BadgeText { get; set; } = string.Empty;
        public string BadgeVariant { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DescriptionHtml { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public string ButtonText { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ActionValue { get; set; } = string.Empty;
        public string IconKey { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? LayoutJson { get; set; }
        public string DetailTitle { get; set; } = string.Empty;
        public string DetailContentHtml { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsPublished { get; set; } = true;
        public DateTime? PublishStartUtc { get; set; }
        public DateTime? PublishEndUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
