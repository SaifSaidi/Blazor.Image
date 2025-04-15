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

        public override string ToString()
        {
            return $"ImageInfo: [SanitizedName={SanitizedName}, Width={Width}, Height={Height}, Format={Format}, Quality={Quality}, ProcessedTime={ProcessedTime}, Key={Key}]";
        }
    }
}
