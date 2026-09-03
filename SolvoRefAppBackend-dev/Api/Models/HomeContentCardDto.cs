namespace Api.Models
{
    public sealed class HomeContentCardDto
    {
        public Guid Id { get; set; }
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
        public string LayoutJson { get; set; } = string.Empty;
        public string DetailTitle { get; set; } = string.Empty;
        public string DetailContentHtml { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishStartUtc { get; set; }
        public DateTime? PublishEndUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
