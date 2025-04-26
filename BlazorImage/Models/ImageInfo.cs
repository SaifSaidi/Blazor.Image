namespace BlazorImage.Models
{
    internal sealed record ImageInfo(
        string SanitizedName,
        int? Width,
        int? Height,
        FileFormat? Format,
        int? Quality)
    {
        [BsonId]
        public string Key { get; set; } = default!;

        public DateTime? ProcessedTime { get; set; }
    }
}
