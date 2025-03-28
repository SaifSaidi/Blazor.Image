using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace BlazorImage.Services;

internal class ImageElementService : IImageElementService
{
    // Pre-compute common string fragments
    private static readonly string _placeholderSuffix = "-placeholder.";
    private static readonly string _wqSuffix = "w-q";

    // Frequently used constants
    private const int MAX_STACK_ALLOC = 512;
    private const int ESTIMATED_SIZE_PER_BREAKPOINT = 25;

    private readonly BlazorImageConfig _config;
    private readonly string _configDir;
    private static int[]? _configSizes;
    private readonly IFileService _fileService;
    private readonly DictionaryCacheDataService _dictionaryCacheData;

    // Cache for commonly used sizes strings to avoid repeated allocations
    private static readonly ConcurrentDictionary<string, string> _sizesCache = new();

    // Improved StringBuilder pool using Microsoft.Extensions.ObjectPool
    private static readonly ObjectPool<StringBuilder> _stringBuilderPool =
        new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy
        {
            InitialCapacity = 256,
            MaximumRetainedCapacity = 4000
        });

    public ImageElementService(
        IOptions<BlazorImageConfig> config,
        IFileService fileService,
        DictionaryCacheDataService dictionaryCacheData)
    {
        _config = config.Value;
        _configDir = _config.Dir;
        _configSizes = _config.ConfigSizes;
        _fileService = fileService;
        _dictionaryCacheData = dictionaryCacheData;
    }

    public (string source, string fallback, string placeholder) GetStaticPictureSourceWithMetadataInfo(
        string sanitizedName, int quality, FileFormat format, int? width)
    {
        // Create cache key
        var key = new DictionaryCacheDataService.CacheKey
        {
            SanitizedName = sanitizedName,
            Quality = quality,
            Format = format,
            WidthFlag = width ?? -1
        };

        // Use GetOrAdd with value factory to avoid duplicate computation
        return _dictionaryCacheData.SourceInfoCache.GetOrAdd(key, static (k, ctx) =>
        {
            var (configDir, formatStrings) = ctx;

            // Use a single buffer for both paths to reduce allocations
            int maxPathLength = configDir.Length + k.SanitizedName.Length * 2 + formatStrings[(int)k.Format].Length * 2 + 30;
            Span<char> pathBuffer = maxPathLength <= MAX_STACK_ALLOC
                ? stackalloc char[maxPathLength]
                : new char[maxPathLength];

            // Build fallback path
            var fallback = BuildFallbackPath(configDir, k.SanitizedName, formatStrings, pathBuffer);

            // Reuse the same buffer for placeholder path
            var placeholder = BuildPlaceholderPath(configDir, k.SanitizedName, k.Format, formatStrings, pathBuffer);

            // Build source
            var source = k.WidthFlag == -1
                ? GetSourceAsString(configDir, k.SanitizedName, k.Quality, k.Format, _configSizes)
                : GetStaticSourceAsString(configDir, k.SanitizedName, k.Quality, k.Format, k.WidthFlag);

            return (source, fallback, placeholder);
        }, (_configDir, FileFormatExtensions.FormatStrings));
    }
     
    // Helper method to sanitize file names
    private string SanitizeFileName(string src) => _fileService.SanitizeFileName(src);

    public string GenerateImageName(string src, int width, int? quality, FileFormat? format)
    {
        string sanitizedName = SanitizeFileName(src);
        FileFormat defaultFormat = _config.DefaultFileFormat;
        int defaultQuality = _config.DefaultQuality;
        string formatExt = (format ?? defaultFormat).ToFileExtension();
        int qualityValue = quality ?? defaultQuality;

        // Use StringBuilder from pool for better performance
        var sb = _stringBuilderPool.Get();
        try
        {
            sb.Clear();
            sb.EnsureCapacity(sanitizedName.Length * 2 + formatExt.Length * 2 + 20);

            sb.Append(sanitizedName)
              .Append('/')
              .Append(formatExt)
              .Append('/')
              .Append(sanitizedName)
              .Append('-')
              .Append(width)
              .Append("w-q")
              .Append(qualityValue)
              .Append('.')
              .Append(formatExt);

            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }

    public string GenerateImagePlaceholder(string src, FileFormat format)
    {
        string sanitizedName = SanitizeFileName(src);
        string formatExt = format.ToFileExtension();

        // Use StringBuilder from pool for better performance
        var sb = _stringBuilderPool.Get();
        try
        {
            sb.Clear();
            sb.EnsureCapacity(sanitizedName.Length * 2 + formatExt.Length * 2 + 20);

            sb.Append(sanitizedName)
              .Append('/')
              .Append(formatExt)
              .Append('/')
              .Append(sanitizedName)
              .Append("-placeholder.")
              .Append(formatExt);

            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }

    public string GenerateImageFallbackSrc(string src, FileFormat format = FileFormat.jpeg)
    {
        string sanitizedName = SanitizeFileName(src);
        string formatExt = format.ToFileExtension();

        // Use StringBuilder from pool for better performance
        var sb = _stringBuilderPool.Get();
        try
        {
            sb.Clear();
            sb.EnsureCapacity(sanitizedName.Length * 2 + formatExt.Length + 10);

            sb.Append(sanitizedName)
              .Append('/')
              .Append(sanitizedName)
              .Append('.')
              .Append(formatExt);

            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetStaticSourceAsString(string dir, string sanitizedName,
        int quality, FileFormat format, int width)
    {
        ReadOnlySpan<int> imageSizes = _configSizes;
        int index = Math.Min(Sizes.GetClosestSize(width, imageSizes), imageSizes.Length - 1);
        int size = imageSizes[index];

        // Format extension
        string formatStr = format.ToString().ToLowerInvariant();

        // Calculate total buffer size
        int totalLength = dir.Length + 1 + sanitizedName.Length * 2 + formatStr.Length * 2 + 20;

        // Use stack allocation for small buffers
        Span<char> buffer = totalLength <= MAX_STACK_ALLOC ? stackalloc char[totalLength] : new char[totalLength];
        int pos = 0;

        // Build path components efficiently
        dir.AsSpan().CopyTo(buffer.Slice(pos)); pos += dir.Length;
        buffer[pos++] = '/';
        sanitizedName.AsSpan().CopyTo(buffer.Slice(pos)); pos += sanitizedName.Length;
        buffer[pos++] = '/';
        formatStr.AsSpan().CopyTo(buffer.Slice(pos)); pos += formatStr.Length;
        buffer[pos++] = '/';
        sanitizedName.AsSpan().CopyTo(buffer.Slice(pos)); pos += sanitizedName.Length;
        buffer[pos++] = '-';

        // Format numbers directly into buffer
        pos += size.TryFormat(buffer.Slice(pos), out int bytesWritten) ? bytesWritten : 0;

        _wqSuffix.AsSpan().CopyTo(buffer.Slice(pos)); pos += _wqSuffix.Length;
        pos += quality.TryFormat(buffer.Slice(pos), out bytesWritten) ? bytesWritten : 0;

        buffer[pos++] = '.';
        formatStr.AsSpan().CopyTo(buffer.Slice(pos)); pos += formatStr.Length;
        buffer[pos++] = ' ';

        // Add width descriptor
        pos += size.TryFormat(buffer.Slice(pos), out bytesWritten) ? bytesWritten : 0;
        buffer[pos++] = 'w';

        return new string(buffer.Slice(0, pos));
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static string GetSourceAsString(string dir, string name, int quality, FileFormat format, ReadOnlySpan<int> sizes)
    {
        if (sizes.IsEmpty)
            return string.Empty;

        const int MaxNumberDigits = 4; // Up to 9999px wide

        // Precompute all static components
        var formatStr = format.ToString().ToLowerInvariant();
        var qualityStr = quality.ToString();

        // Calculate exact buffer size
        int totalSize = CalculateSourceBufferSize(dir, name, formatStr, qualityStr, sizes.Length);

        // Allocate buffer
        var buffer = totalSize <= MAX_STACK_ALLOC ? stackalloc char[totalSize] : new char[totalSize];
        int pos = 0;

        // Format numbers directly into buffer
        Span<char> numBuffer = stackalloc char[MaxNumberDigits];

        for (int i = 0; i < sizes.Length; i++)
        {
            int size = sizes[i];

            // Write dir
            dir.AsSpan().CopyTo(buffer.Slice(pos));
            pos += dir.Length;
            buffer[pos++] = '/';

            // Write name
            name.AsSpan().CopyTo(buffer.Slice(pos));
            pos += name.Length;
            buffer[pos++] = '/';

            // Write format
            formatStr.AsSpan().CopyTo(buffer.Slice(pos));
            pos += formatStr.Length;
            buffer[pos++] = '/';

            // Write name again
            name.AsSpan().CopyTo(buffer.Slice(pos));
            pos += name.Length;
            buffer[pos++] = '-';

            // Write size
            int bytesWritten;
            if (size.TryFormat(buffer.Slice(pos), out bytesWritten))
            {
                pos += bytesWritten;
            }
            else
            {
                var sizeStr = size.ToString();
                sizeStr.AsSpan().CopyTo(buffer.Slice(pos));
                pos += sizeStr.Length;
            }

            // Write w-q
            _wqSuffix.AsSpan().CopyTo(buffer.Slice(pos));
            pos += _wqSuffix.Length;

            // Write quality
            qualityStr.AsSpan().CopyTo(buffer.Slice(pos));
            pos += qualityStr.Length;

            // Write .format
            buffer[pos++] = '.';
            formatStr.AsSpan().CopyTo(buffer.Slice(pos));
            pos += formatStr.Length;
            buffer[pos++] = ' ';

            // Write size again for width descriptor
            if (size.TryFormat(buffer.Slice(pos), out bytesWritten))
            {
                pos += bytesWritten;
            }
            else
            {
                var sizeStr = size.ToString();
                sizeStr.AsSpan().CopyTo(buffer.Slice(pos));
                pos += sizeStr.Length;
            }

            // Write w
            buffer[pos++] = 'w';

            // Add comma if not last
            if (i < sizes.Length - 1)
            {
                buffer[pos++] = ',';
                buffer[pos++] = ' ';
            }
        }

        return new string(buffer.Slice(0, pos));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateSourceBufferSize(
        string dir,
        string name,
        string formatStr,
        string qualityStr,
        int count)
    {
        // Base size per entry
        int baseSize = dir.Length + 1 + // dir/
                      name.Length + 1 + // name/
                      formatStr.Length + 1 + // format/
                      name.Length + 1 + // name-
                      _wqSuffix.Length + // w-q
                      qualityStr.Length + 1 + // quality.
                      formatStr.Length + 1 + // format space
                      1; // w

        // Size for all entries
        int totalSize = baseSize * count;

        // Add size for commas and spaces
        totalSize += (count - 1) * 2; // ", " between entries

        // Add size for numbers (estimate 4 digits per number, 2 numbers per entry)
        totalSize += count * 8;

        // Add 10% padding to be safe
        return (int)(totalSize * 1.1);
    }

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
        string formatStr = formatStrings[(int)format];

        configDir.AsSpan().CopyTo(buffer);
        pos += configDir.Length;
        buffer[pos++] = '/';

        sanitizedName.AsSpan().CopyTo(buffer.Slice(pos));
        pos += sanitizedName.Length;
        buffer[pos++] = '/';

        formatStr.AsSpan().CopyTo(buffer.Slice(pos));
        pos += formatStr.Length;
        buffer[pos++] = '/';

        sanitizedName.AsSpan().CopyTo(buffer.Slice(pos));
        pos += sanitizedName.Length;

        _placeholderSuffix.AsSpan().CopyTo(buffer.Slice(pos));
        pos += _placeholderSuffix.Length;

        formatStr.AsSpan().CopyTo(buffer.Slice(pos));
        pos += formatStr.Length;

        return new string(buffer.Slice(0, pos));
    }
}