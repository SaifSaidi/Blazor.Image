using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using System.Text;

internal sealed class ImageElementService : IImageElementService
{
    // --- Constants ---
    private const char PathSeparator = '/';
    private const string PlaceholderSuffix = "-placeholder.";
    private const string WidthQualitySuffix = "w-q";
    private const string DefaultFallbackExtension = ".jpeg"; // Use constant for "jpeg"
    private const string WidthDescriptorSuffix = "w";
    private const string SourceSetSeparator = ", ";
 
    // --- Configuration & Dependencies ---
    private readonly BlazorImageConfig _config;
    private readonly string _configDir;
    private readonly int[] _configSizes;

    private readonly DictionaryCacheDataService _dictionaryCacheData;
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;

    public ImageElementService(
        IOptions<BlazorImageConfig> configOptions, 
        DictionaryCacheDataService dictionaryCacheData,
        ObjectPool<StringBuilder> stringBuilderPool)
    {
        ArgumentNullException.ThrowIfNull(configOptions);
        ArgumentNullException.ThrowIfNull(dictionaryCacheData);
        ArgumentNullException.ThrowIfNull(stringBuilderPool);

        _config = configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions), "Configuration value cannot be null.");
        _configDir = _config.OutputDir ?? string.Empty;

        // Validate ConfigSizes - make it an instance field
        _configSizes = _config.Sizes ?? [];
        if (_configSizes.Length == 0)
        {
            Console.WriteLine("Warning: BlazorImageConfig.ConfigSizes is null or empty.");
        }
        
        _dictionaryCacheData = dictionaryCacheData;
        _stringBuilderPool = stringBuilderPool;
    }

    // --- Public Methods ---

    public (string source, string fallback, string placeholder)
        GetStaticPictureSourceWithMetadataInfo(
            string sanitizedName, int quality, FileFormat format, int? width)
    {
        var key = new DictionaryCacheDataService.CacheKey
        {
            SanitizedName = sanitizedName,
            Quality = quality,
            Format = format,
            Width = width ?? -1 // Use -1 or another sentinel for "all sizes"
        };

        // Use GetOrAdd with instance context
        return _dictionaryCacheData.SourceInfoCache.GetOrAdd(key, static (k, instance) =>
        {
            var fallback = instance.BuildFallbackPath(k.SanitizedName);
            var placeholder = instance.BuildPlaceholderPath(k.SanitizedName, k.Format);

            var source = k.Width == -1
                ? instance.GetSourceSetString(k.SanitizedName, k.Quality, k.Format)  
                : instance.GetSingleSourceString(k.SanitizedName, k.Quality, k.Format, k.Width); 

            return (source, fallback, placeholder);

        }, this); 
    }

    public double GetAspectRatio()
    {
        // Avoid division by zero
        if (_config.AspectHeigth <= 0)
        { 
            return 1.0; // Default aspect ratio
        }
        return _config.AspectWidth / _config.AspectHeigth;
    }

    // --- Private Helper Methods ---

    private string GetSingleSourceString(string sanitizedName, int quality, FileFormat format, int requestedWidth)
    {

        int sizeIndex = Sizes.GetClosestSize((int)(requestedWidth * 1.5), _configSizes)  ;

        int actualSize = _configSizes[sizeIndex];
        string formatStr = format.ToString();
 
        var sb = _stringBuilderPool.Get();
        try
        {
            // Build URL: {dir}/{name}/{format}/{name}-{size}w-q{quality}.{format}
            AppendPathBase(sb, sanitizedName, formatStr);
            sb.Append(sanitizedName);
            sb.Append('-');
            sb.Append(actualSize); 
            sb.Append(WidthQualitySuffix);
            sb.Append(quality); 
            sb.Append('.');
            sb.Append(formatStr);

            // Append width descriptor: URL {size}w
            sb.Append(' ');
            sb.Append(actualSize);
            sb.Append(WidthDescriptorSuffix);

            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }

    private string GetSourceSetString(string sanitizedName, int quality, FileFormat format)
    { 

        string formatStr = format.ToString();
        var sb = _stringBuilderPool.Get();
        try
        {
            // Estimate capacity roughly, StringBuilder handles resizing if needed.
            // (dir + / + name + / + format + / + name + - + size + w-q + quality + . + format + space + size + w + , + space) * count
            int estimatedCapacity = (_configDir.Length + sanitizedName.Length * 2 + formatStr.Length * 2 + WidthQualitySuffix.Length + 15) * _configSizes.Length;
            sb.EnsureCapacity(estimatedCapacity);

            for (int i = 0; i < _configSizes.Length; i++)
            {
                int size = _configSizes[i];

                // Append URL: {dir}/{name}/{format}/{name}-{size}w-q{quality}.{format}
                AppendPathBase(sb, sanitizedName, formatStr);
                sb.Append(sanitizedName);
                sb.Append('-');
                sb.Append(size); // Use efficient int append
                sb.Append(WidthQualitySuffix);
                sb.Append(quality); // Use efficient int append
                sb.Append('.');
                sb.Append(formatStr);

                // Append width descriptor: URL {size}w
                sb.Append(' ');
                sb.Append(size); // Use efficient int append
                sb.Append(WidthDescriptorSuffix);

                // Append separator
                if (i < _configSizes.Length - 1)
                {
                    sb.Append(SourceSetSeparator);
                }
            }
            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }

    // Helper to build the common part of the URL path
    private void AppendPathBase(StringBuilder sb, string sanitizedName, string formatStr)
    {
        // Format: {configDir}/{sanitizedName}/{format}/
        sb.Append(_configDir);
        sb.Append(PathSeparator);
        sb.Append(sanitizedName);
        sb.Append(PathSeparator);
        sb.Append(formatStr);
        sb.Append(PathSeparator);
    }

    private string BuildFallbackPath(string sanitizedName)
    {
        // Format: "{configDir}/{sanitizedName}/{sanitizedName}.jpeg"
        var sb = _stringBuilderPool.Get();
 
        sb.Append(_configDir);
            sb.Append(PathSeparator);
            sb.Append(sanitizedName);
            sb.Append(PathSeparator);
            sb.Append(sanitizedName);
            sb.Append(DefaultFallbackExtension);
            return sb.ToString();
        
    }

    private string BuildPlaceholderPath(string sanitizedName, FileFormat format)
    {
        // Format: "{configDir}/{sanitizedName}/{format}/{sanitizedName}-placeholder.{format}"
        string formatStr = format.ToString();
        var sb = _stringBuilderPool.Get();
        AppendPathBase(sb, sanitizedName, formatStr);
        sb.Append(sanitizedName);
        sb.Append(PlaceholderSuffix);
        sb.Append(formatStr);
        return sb.ToString();
    }
}