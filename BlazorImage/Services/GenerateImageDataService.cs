using Microsoft.Extensions.Options;
using System.Text;

internal sealed class GenerateImageDataService : IGenerateImageDataService
{
    private readonly BlazorImageConfig _config;
    private readonly IFileService _fileService;

    public GenerateImageDataService(
        IOptions<BlazorImageConfig> config,
        IFileService fileService)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    }

    public string GenerateImageName(string src, int width, int? quality, FileFormat? format)
    {
        string sanitizedName = SanitizeFileName(src);
        FileFormat actualFormat = format ?? _config.DefaultFileFormat;
        int actualQuality = quality ?? _config.DefaultQuality;
        string formatExt = actualFormat.ToFileExtension();

        return BuildString(sb =>
        {
            sb.Append(sanitizedName)
              .Append('/')
              .Append(formatExt)
              .Append('/')
              .Append(sanitizedName)
              .Append('-')
              .Append(width)
              .Append("w-q")
              .Append(actualQuality)
              .Append('.')
              .Append(formatExt);
        }, estimatedCapacity: sanitizedName.Length * 2 + formatExt.Length * 2 + 20);
    }

    public string GenerateImagePlaceholder(string src, FileFormat format)
    {
        string sanitizedName = SanitizeFileName(src);
        string formatExt = format.ToFileExtension();

        return BuildString(sb =>
        {
            sb.Append(sanitizedName)
              .Append('/')
              .Append(formatExt)
              .Append('/')
              .Append(sanitizedName)
              .Append("-placeholder.")
              .Append(formatExt);
        }, estimatedCapacity: sanitizedName.Length * 2 + formatExt.Length * 2 + 20);
    }

    public string GenerateImageFallbackSrc(string src, FileFormat format = FileFormat.jpeg)
    {
        string sanitizedName = SanitizeFileName(src);
        string formatExt = format.ToFileExtension();

        return BuildString(sb =>
        {
            sb.Append(sanitizedName)
              .Append('/')
              .Append(sanitizedName)
              .Append('.')
              .Append(formatExt);
        }, estimatedCapacity: sanitizedName.Length * 2 + formatExt.Length + 10);
    }

    private string SanitizeFileName(string src) => _fileService.SanitizeFileName(src);

    private static string BuildString(Action<StringBuilder> buildAction, int estimatedCapacity)
    {
        var sb = new StringBuilder(estimatedCapacity > 0 ? estimatedCapacity : 64);
        buildAction(sb);
        return sb.ToString();
    }
}
