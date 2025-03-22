
using System.Collections.Concurrent;
using System.Runtime.CompilerServices; 
using Microsoft.Extensions.Options;
namespace BlazorImage.Services;


internal class ImageElementService : IImageElementService
{
    private readonly BlazorImageConfig config;
    private readonly string configDir;
    private readonly IFileService _fileService; 
    private readonly DictionaryCacheDataService _dictionaryCacheData2;

    public ImageElementService(IOptions<BlazorImageConfig> _config, 
        IFileService fileService,
        DictionaryCacheDataService dictionaryCacheData)
    {
        config = _config.Value;
        configDir = config.Dir;
        _fileService = fileService; 
        _dictionaryCacheData2 = dictionaryCacheData;
    }


    public (string source, string fallback, string placeholder) GetStaticPictureSourceWithMetadataInfo(
        string sanitizedName, int quality, FileFormat format, int? width)
    {
        var key = new DictionaryCacheDataService.CacheKey
        {
            SanitizedName = sanitizedName,
            Quality = quality,
            Format = format,
            WidthFlag = width.HasValue ? width.Value : -1
        };
        return _dictionaryCacheData2.SourceInfoCache.GetOrAdd(key, static (k, ctx) =>
        {
            var (configDir, formatStrings) = ctx;

            // Preallocate buffers for common case
            Span<char> fallbackBuffer = stackalloc char[256];
            Span<char> placeholderBuffer = stackalloc char[256];

            // Build fallback path
            var fallback = BuildFallbackPath(configDir, k.SanitizedName, formatStrings, fallbackBuffer);

            // Build placeholder path
            var placeholder = BuildPlaceholderPath(configDir, k.SanitizedName, k.Format, formatStrings, placeholderBuffer);

            // Build source
            var source = k.WidthFlag == -1
                ? GetSourceAsString(configDir, k.SanitizedName, k.Quality, k.Format)
                : GetStaticSourceAsString(configDir, k.SanitizedName, k.Quality, k.Format, k.WidthFlag);

            return (source, fallback, placeholder);
        }, (configDir, _formatStrings));
    }



    // Helper method to sanitize file names
    private string SanitizeFileName(string src)
    {
        return _fileService.SanitizeFileName(src);
    }

  

     
    public string GenerateImageName(string src, int width, int? quality, FileFormat? format)
    {
        string sanitizedName = SanitizeFileName(src);
        FileFormat defaultFormat = config.DefaultFileFormat;
        int defaultQuality = config.DefaultQuality;
        string formatExt = (format ?? defaultFormat).ToFileExtension();
        int qualityValue = quality ?? defaultQuality;

        return $"{sanitizedName}/{formatExt}/{sanitizedName}-{width}w-q{qualityValue}.{formatExt}";
    }
    
    public string GenerateImagePlaceholder(string src, FileFormat format)
    {
        string sanitizedName = SanitizeFileName(src);
        string formatExt = format.ToFileExtension(); 

        return $"{sanitizedName}/{formatExt}/{sanitizedName}-placeholder.{formatExt}";
    }



    public string GenerateImageFallbackSrc(string src, FileFormat format = FileFormat.jpeg)
    {
        string sanitizedName = SanitizeFileName(src);
        string formatExt = format.ToFileExtension(); // Cache extension

        var fallbackSrcBuilder = $"{sanitizedName}/{sanitizedName}.{formatExt}";

        return fallbackSrcBuilder;
    }
    private static string GetStaticSourceAsString(string dir, string sanitizedName,
    int quality, FileFormat format, int width)
    {
        ReadOnlySpan<int> imageSizes = Constants.ConfigSizes;
        int index = Math.Min(Constants.GetClosestSize(width), imageSizes.Length - 1);
        int size = imageSizes[index];

        // Pre-calculate lengths
        int dirLen = dir.Length;
        int nameLen = sanitizedName.Length;
        int formatLen = format.ToString().Length;

        // Format numbers
        Span<char> sizeBuffer = stackalloc char[4];
        Span<char> qualityBuffer = stackalloc char[4];
        size.TryFormat(sizeBuffer, out int sizeLen);
        quality.TryFormat(qualityBuffer, out int qualityLen);

        // Calculate total length
        int totalLength = dirLen + 1 + nameLen + 1 + formatLen + 1 + nameLen + 1 + sizeLen + 3
                        + qualityLen + 1 + formatLen + 1 + sizeLen + 1;

        // Allocate buffer
        Span<char> buffer = totalLength <= 256 ? stackalloc char[totalLength] : new char[totalLength];
        int pos = 0;

        // Build string components
        dir.AsSpan().CopyTo(buffer.Slice(pos)); pos += dirLen;
        buffer[pos++] = '/';
        sanitizedName.AsSpan().CopyTo(buffer.Slice(pos)); pos += nameLen;
        buffer[pos++] = '/';
        format.ToString().AsSpan().CopyTo(buffer.Slice(pos)); pos += formatLen;
        buffer[pos++] = '/';
        sanitizedName.AsSpan().CopyTo(buffer.Slice(pos)); pos += nameLen;
        buffer[pos++] = '-';
        sizeBuffer.Slice(0, sizeLen).CopyTo(buffer.Slice(pos)); pos += sizeLen;
        "w-q".AsSpan().CopyTo(buffer.Slice(pos)); pos += 3;
        qualityBuffer.Slice(0, qualityLen).CopyTo(buffer.Slice(pos)); pos += qualityLen;
        buffer[pos++] = '.';
        format.ToString().AsSpan().CopyTo(buffer.Slice(pos)); pos += formatLen;
        buffer[pos++] = ' ';
        sizeBuffer.Slice(0, sizeLen).CopyTo(buffer.Slice(pos)); pos += sizeLen;
        buffer[pos++] = 'w';

        return new string(buffer);
    }

    private static string GetSourceAsString(string dir, string sanitizedName, int quality, FileFormat format)
    {
        ReadOnlySpan<int> sizes = Constants.ConfigSizes;
        int count = sizes.Length;
        if (count == 0) return string.Empty;

        // Precompute all static components as spans
        ReadOnlySpan<char> prefix = $"{dir}/{sanitizedName}/{format}/{sanitizedName}-".AsSpan();
        ReadOnlySpan<char> middle = $"-q{quality}.{format} ".AsSpan();
        ReadOnlySpan<char> w = "w".AsSpan();
        ReadOnlySpan<char> comma = ", ".AsSpan();

        // Calculate total buffer size with exact precision
        int totalSize = CalculateTotalBufferSize(sizes, prefix, middle, w, comma, count);

        // Allocate buffer with exact size
        Span<char> buffer = totalSize <= 1024 ? stackalloc char[totalSize] : new char[totalSize];
        int pos = 0;

        // Temporary buffer for number formatting (max 4 digits)
        Span<char> numBuffer = stackalloc char[4];

        for (int i = 0; i < count; i++)
        {
            int size = sizes[i];
            FormatNumber(size, numBuffer, out var numSpan);

            // Write full URL component using batch copies
            CopyAllComponents(ref buffer, ref pos, prefix, numSpan, w, middle, comma, i, count);
        }

        return new string(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateTotalBufferSize(
        ReadOnlySpan<int> sizes,
        ReadOnlySpan<char> prefix,
        ReadOnlySpan<char> middle,
        ReadOnlySpan<char> w,
        ReadOnlySpan<char> comma,
        int count)
    {
        int total = prefix.Length * count
                  + middle.Length * count
                  + w.Length * 2 * count
                  + comma.Length * (count - 1);

        for (int i = 0; i < count; i++)
            total += GetIntLength(sizes[i]) * 2;

        return total;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FormatNumber(int number, Span<char> buffer, out ReadOnlySpan<char> formatted)
    {
        if (!number.TryFormat(buffer, out int charsWritten))
            throw new ArgumentException("Invalid number format");
        formatted = buffer.Slice(0, charsWritten);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyAllComponents(
        ref Span<char> buffer,
        ref int pos,
        ReadOnlySpan<char> prefix,
        ReadOnlySpan<char> numSpan,
        ReadOnlySpan<char> w,
        ReadOnlySpan<char> middle,
        ReadOnlySpan<char> comma,
        int index,
        int count)
    {
        // Component 1: prefix + number + w
        prefix.CopyTo(buffer.Slice(pos));
        pos += prefix.Length;
        numSpan.CopyTo(buffer.Slice(pos));
        pos += numSpan.Length;
        w.CopyTo(buffer.Slice(pos));
        pos += w.Length;

        // Component 2: middle + number + w
        middle.CopyTo(buffer.Slice(pos));
        pos += middle.Length;
        numSpan.CopyTo(buffer.Slice(pos));
        pos += numSpan.Length;
        w.CopyTo(buffer.Slice(pos));
        pos += w.Length;

        // Add comma separator if not last item
        if (index < count - 1)
        {
            comma.CopyTo(buffer.Slice(pos));
            pos += comma.Length;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIntLength(int n)
    {
        // Fast path for common image sizes
        if (n >= 1000) return 4;
        if (n >= 100) return 3;
        if (n >= 10) return 2;
        return 1;
    }
  
  
    private static readonly string[] _formatStrings = Enum.GetValues<FileFormat>()
    .Select(f => f.ToString().ToLowerInvariant())
    .ToArray();



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string BuildFallbackPath(string configDir, string sanitizedName,
        string[] formatStrings, Span<char> buffer)
    {
        // Format: "{configDir}/{sanitizedName}/{sanitizedName}.jpeg"
        int pos = 0;

        configDir.AsSpan().CopyTo(buffer);
        pos += configDir.Length;
        buffer[pos++] = '/';

        sanitizedName.AsSpan().CopyTo(buffer.Slice(pos));
        pos += sanitizedName.Length;
        buffer[pos++] = '/';

        sanitizedName.AsSpan().CopyTo(buffer.Slice(pos));
        pos += sanitizedName.Length;
        buffer[pos++] = '.';

        "jpeg".AsSpan().CopyTo(buffer.Slice(pos));
        pos += 4;

        return new string(buffer.Slice(0, pos));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string BuildPlaceholderPath(string configDir, string sanitizedName,
        FileFormat format, string[] formatStrings, Span<char> buffer)
    {
        // Format: "{configDir}/{sanitizedName}/{format}/{sanitizedName}-placeholder.{format}"
        int pos = 0;

        configDir.AsSpan().CopyTo(buffer);
        pos += configDir.Length;
        buffer[pos++] = '/';

        sanitizedName.AsSpan().CopyTo(buffer.Slice(pos));
        pos += sanitizedName.Length;
        buffer[pos++] = '/';

        formatStrings[(int)format].AsSpan().CopyTo(buffer.Slice(pos));
        pos += formatStrings[(int)format].Length;
        buffer[pos++] = '/';

        sanitizedName.AsSpan().CopyTo(buffer.Slice(pos));
        pos += sanitizedName.Length;
        "-placeholder.".AsSpan().CopyTo(buffer.Slice(pos));
        pos += 13;

        formatStrings[(int)format].AsSpan().CopyTo(buffer.Slice(pos));
        pos += formatStrings[(int)format].Length;

        return new string(buffer.Slice(0, pos));
    }


     
}

