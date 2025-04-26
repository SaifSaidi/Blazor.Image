namespace BlazorImage.Data;
internal static class Constants
{
    public const int DefaultQuality = 75;
    public const int MinQuality = 15;
    public const int MaxQuality = 100;

    public const int PlaceholderWidth = 80;
    public const int PlaceholderQuality = 80;

    public const FileFormat DefaultFormat = FileFormat.webp;

    public const string LiteDbName = "images_info.db";
    public const string LiteDbCollection = "images";
    public const string DefaultOutputDir = "_optimized";
}
