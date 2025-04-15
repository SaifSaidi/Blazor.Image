using BlazorImage.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace BlazorImage.Services;
internal sealed class BlazorImageService : IBlazorImageService
{
    private readonly ICacheService _cacheService;
    private readonly IImageProcessingService _imageProcessingService;
     private readonly IFileService _fileService;
    private readonly IGenerateImageDataService _generateImageDataService;
    private readonly BlazorImageConfig _config;
    private readonly int _DefaultQuality;
    private readonly int LastWidth;
    private readonly FileFormat? _DefaultFileFormat;

    private readonly ILogger<BlazorImageService> _logger;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private static readonly SemaphoreSlim _globalProcessingThrottle =
        new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

    public BlazorImageService(
        ICacheService cacheService,
        IImageProcessingService imageProcessingService,
        IOptions<BlazorImageConfig> config,
        ILogger<BlazorImageService> logger,
        IFileService fileService,
        IGenerateImageDataService generateImageDataService)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));

        if (_config.Sizes == null || _config.Sizes.Length == 0)
        {
            throw new InvalidOperationException("BlazorImageConfig.ConfigSizes must be configured with at least one size.");
        }
        if (string.IsNullOrWhiteSpace(_config.OutputDir))
        {
            throw new InvalidOperationException("BlazorImageConfig.OutputDir must be configured.");
        }
        _cacheService = cacheService;
        _imageProcessingService = imageProcessingService;
        _logger = logger;
         _fileService = fileService;
        _generateImageDataService = generateImageDataService;

        _DefaultQuality = _config.DefaultQuality;
        _DefaultFileFormat = _config.DefaultFileFormat;
        LastWidth = _config.Sizes[_config.Sizes.Length / 2];
    }

    public async ValueTask<Result<ImageInfo>?> GetImageInfoAsync(string src, int? quality, FileFormat? format)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            return Result<ImageInfo>.Failure("Source path cannot be empty.");
        }

        quality ??= _DefaultQuality;
        format ??= _DefaultFileFormat;

        if (quality < Constants.MinQuality || quality > Constants.MaxQuality)
        {
            return Result<ImageInfo>.Failure($"Image Quality must be between {Constants.MinQuality} and {Constants.MaxQuality}.");
        }

        var cacheKey = GenerateCacheKey(src, quality.Value, format!.Value);
        var cachedInfo = await _cacheService.GetFromCacheAsync(cacheKey).ConfigureAwait(false);

        if (cachedInfo != null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return Result<ImageInfo>.Success(cachedInfo);
        }

        var originalPath = _fileService.GetRootPath(src);
        if (!_fileService.FileExistsInRootPath(originalPath))
        {
            _logger.LogWarning("Original file not found during GetImageInfoAsync check: {OriginalPath}", originalPath);
            return Result<ImageInfo>.Failure("Source file does not exist.");
        }

        _logger.LogDebug("Cache miss for {CacheKey}. Processing required.", cacheKey);
        return Result<ImageInfo>.Empty();
    }

    public ValueTask ProcessImageInBackgroundAsync(string src, int? quality, FileFormat? format, ChannelWriter<ProgressUpdate> writer)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            var errorMsg = $"Image Quality must be between {Constants.MinQuality} and {Constants.MaxQuality}.";
            _logger.LogWarning("Invalid quality parameter for {Src}: {Quality}", src, quality);

            _ = writer.WriteAsync(new ProgressUpdate(100, errorMsg, IsFinal: true, Success: false, Error: errorMsg), CancellationToken.None)
                      .AsTask()
                      .ContinueWith(_ => writer.TryComplete(), TaskScheduler.Default);
            return ValueTask.CompletedTask;
        }

        quality ??= _DefaultQuality;
        format ??= _DefaultFileFormat;

        if (quality < Constants.MinQuality || quality > Constants.MaxQuality)
        {
            var errorMsg = $"Image Quality must be between {Constants.MinQuality} and {Constants.MaxQuality}.";
            _logger.LogWarning("Invalid quality parameter for {Src}: {Quality}", src, quality);

            _ = writer.WriteAsync(new ProgressUpdate(100, errorMsg, IsFinal: true, Success: false, Error: errorMsg), CancellationToken.None)
                      .AsTask()
                      .ContinueWith(_ => writer.TryComplete(), TaskScheduler.Default);

            return ValueTask.CompletedTask;
        }

        var cacheKey = GenerateCacheKey(src, quality.Value, format!.Value);

        _ = Task.Run(async () =>
        {
            await OptimizeAndCacheImage(src, quality.Value, format.Value, cacheKey, writer).ConfigureAwait(false);
        });

        return ValueTask.CompletedTask;
    }


    private async ValueTask OptimizeAndCacheImage(string src, int quality, FileFormat format, string cacheKey, ChannelWriter<ProgressUpdate> writer)
    {
        await _globalProcessingThrottle.WaitAsync().ConfigureAwait(false);
        bool globalThrottleAcquired = true;

        SemaphoreSlim? processingLock = null;
        bool lockAcquired = false;

        try
        {
            processingLock = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

            await writer.WriteAsync(new ProgressUpdate(0, "Waiting for processing slot..."), CancellationToken.None).ConfigureAwait(false);

            await processingLock.WaitAsync().ConfigureAwait(false);
            lockAcquired = true;

            var cachedInfo = await _cacheService.GetFromCacheAsync(cacheKey).ConfigureAwait(false);
            if (cachedInfo != null)
            {
                _logger.LogInformation("Image {CacheKey} processed by another thread while waiting. Cache hit.", cacheKey);
                await writer.WriteAsync(new ProgressUpdate(100, "Image already processed.", IsFinal: true, Success: true), CancellationToken.None).ConfigureAwait(false);
                return;
            }

            var originalPath = _fileService.GetRootPath(src);
            if (!_fileService.FileExistsInRootPath(originalPath))
            {
                var errorMsg = $"Original file not found: {originalPath}";
                _logger.LogWarning(errorMsg);
                await writer.WriteAsync(new ProgressUpdate(100, errorMsg, IsFinal: true, Success: false, Error: errorMsg), CancellationToken.None).ConfigureAwait(false);
                return;
            }

            await writer.WriteAsync(new ProgressUpdate(10, "Starting image processing..."), CancellationToken.None).ConfigureAwait(false);

            bool success = await ProcessAllImageVariations(originalPath, src, quality, format, writer).ConfigureAwait(false);

            if (!success)
            {
                writer.TryComplete(new("Processing failed within ProcessAllImageVariations."));
                return;
            }

            await writer.WriteAsync(new ProgressUpdate(95, "Creating and caching metadata..."), CancellationToken.None).ConfigureAwait(false);
            var processedImageInfo = CreateImageInfo(src, quality, format, cacheKey);
            await _cacheService.SaveToCacheAsync(cacheKey, processedImageInfo).ConfigureAwait(false);

            await writer.WriteAsync(new ProgressUpdate(100, "Optimization completed successfully.", IsFinal: true, Success: true), CancellationToken.None).ConfigureAwait(false);

            _logger.LogInformation("Successfully processed and cached image {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Unexpected error processing image {src} ({cacheKey})";
            _logger.LogError(ex, errorMsg);
            try
            {
                await writer.WriteAsync(new ProgressUpdate(100, "Processing failed due to an unexpected error.", IsFinal: true, Success: false, Error: ex.Message), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception writeEx) when (writeEx is ChannelClosedException || writeEx is ObjectDisposedException)
            {
                _logger.LogWarning("Channel was closed before final error could be written for {CacheKey}", cacheKey);
            }
            catch (Exception writeEx)
            {
                _logger.LogError(writeEx, "Failed to write final error to channel for {CacheKey}", cacheKey);
            }
        }
        finally
        {
            if (lockAcquired && processingLock != null)
            {
                processingLock.Release();
            }

            if (processingLock != null && processingLock.CurrentCount == 1)
            {
                _locks.TryRemove(cacheKey, out _);
            }

            if (globalThrottleAcquired)
            {
                _globalProcessingThrottle.Release();
            }

            writer.TryComplete();
        }
    }

    private async ValueTask<bool> ProcessAllImageVariations(string originalPath, string src, int quality, FileFormat format, ChannelWriter<ProgressUpdate> writer)
    {
         try
        {
            var sizes = _config.Sizes;
            var sizeTasks = new List<Task>(sizes.Length);
            int totalSteps = sizes.Length + 2;
            int completedSteps = 0;

            foreach (var size in sizes)
            {
                var width = size;
                var height = HelpersMethods.ToAspectRatio(width, _config.AspectWidth, _config.AspectHeigth);
                var imageName = _generateImageDataService.GenerateImageName(src, width, quality, format);
                var outputFilePath = GetImageProccessOutputDir(imageName);

                if (!_fileService.FileExistsInRootPath(outputFilePath))
                {
                    int currentSize = size;
                    int currentWidth = width;
                    int currentHeight = height;
                    string currentOutput = outputFilePath;
                    int currentStepIndex = completedSteps;

                    sizeTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            _logger.LogDebug("Processing size {Width}w for {Src} -> {Output}", currentWidth, src, currentOutput);
                            _fileService.CreateDirectoryForFile(currentOutput);
                            await _imageProcessingService.ProcessAndSaveImageAsync(
                                originalPath,
                                currentOutput,
                                currentWidth, currentHeight, quality, format).ConfigureAwait(false);

                            int progress = CalculateProgress(Interlocked.Increment(ref completedSteps), totalSteps);
                            await writer.WriteAsync(new ProgressUpdate(progress, $"Generated {currentSize}w image"), CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed processing size {Width}w for {Src}", currentWidth, src);
                            throw new AggregateException($"Failed processing size {currentWidth}w", ex);
                        }
                    }));
                }
                else
                {
                    _logger.LogDebug("Skipping existing file: {OutputFilePath}", outputFilePath);
                    int progress = CalculateProgress(Interlocked.Increment(ref completedSteps), totalSteps);
                    await writer.WriteAsync(new ProgressUpdate(progress, $"Skipped existing {size}w image"), CancellationToken.None).ConfigureAwait(false);
                }
            }
            await Task.WhenAll(sizeTasks).ConfigureAwait(false);

            _logger.LogDebug("Starting placeholder and fallback generation for {Src}", src);

            await GeneratePlaceholderImage(originalPath, src, format);
            await GenerateFallbackImages(originalPath, src, quality, format);


            completedSteps += 2;
            int finalProgress = CalculateProgress(completedSteps, totalSteps);
            await writer.WriteAsync(new ProgressUpdate(finalProgress, "Placeholder and fallbacks generated."), CancellationToken.None).ConfigureAwait(false);
             
             return true;
        }
        catch (Exception ex)
        {

            var errorMsg = $"Failed during image variation generation for {src}.";
            if (ex is AggregateException aggEx)
            {
                foreach (var innerEx in aggEx.Flatten().InnerExceptions)
                {
                    _logger.LogError(innerEx, errorMsg + " Inner Exception.");
                }
            }
            else
            {
                _logger.LogError(ex, errorMsg);
            }

            try
            {
                await writer.WriteAsync(new ProgressUpdate(100, "Processing failed during image generation.", IsFinal: true, Success: false, Error: ex.Message), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception writeEx) when (writeEx is ChannelClosedException || writeEx is ObjectDisposedException)
            {
                _logger.LogWarning("Channel was closed before error could be written from ProcessAllImageVariations for {Src}", src);
            }
            catch (Exception writeEx)
            {
                _logger.LogError(writeEx, "Failed to write error to channel from ProcessAllImageVariations for {Src}", src);
            }
            return false;
        }
    }

    private async ValueTask GeneratePlaceholderImage(string originalPath, string src, FileFormat format)
    {
        var placeholderWidth = Constants.PlaceholderWidth;
        var placeholderImageName = _generateImageDataService.GenerateImagePlaceholder(src, format);
        var placeholderOutputFile = GetImageProccessOutputDir(placeholderImageName);

        if (_fileService.FileExistsInRootPath(placeholderOutputFile))
        {
            _logger.LogDebug("Skipping existing placeholder: {PlaceholderOutputFile}", placeholderOutputFile);
            return;
        }
        _logger.LogDebug("Generating placeholder for {Src} -> {PlaceholderOutputFile}", src, placeholderOutputFile);

        var placeholderHeight = HelpersMethods.ToAspectRatio(placeholderWidth, _config.AspectWidth, _config.AspectHeigth);
        _fileService.CreateDirectoryForFile(placeholderOutputFile);
        await _imageProcessingService.ProcessAndSaveImageAsync(originalPath, placeholderOutputFile, placeholderWidth, placeholderHeight, Constants.PlaceholderQuality, format).ConfigureAwait(false);
    }

    private async ValueTask GenerateFallbackImages(string originalPath, string src, int quality, FileFormat format)
    {
        var fallbackWidth = LastWidth;
        var fallbackHeight = HelpersMethods.ToAspectRatio(fallbackWidth, _config.AspectWidth, _config.AspectHeigth);

        var jpegFallbackName = _generateImageDataService.GenerateImageFallbackSrc(src, FileFormat.jpeg);
        var formatFallbackName = _generateImageDataService.GenerateImageFallbackSrc(src, format);
        var jpegFallbackOutputFile = GetImageProccessOutputDir(jpegFallbackName);
        var formatFallbackOutputFile = GetImageProccessOutputDir(formatFallbackName);

        var tasks = new List<Task>();

        if (!_fileService.FileExistsInRootPath(jpegFallbackOutputFile))
        {
            _logger.LogDebug("Generating JPEG fallback for {Src} -> {JpegFallbackOutputFile}", src, jpegFallbackOutputFile);
            tasks.Add(Task.Run(async () =>
            {
                _fileService.CreateDirectoryForFile(jpegFallbackOutputFile);
                await _imageProcessingService.ProcessAndSaveImageAsync(originalPath, jpegFallbackOutputFile, fallbackWidth, fallbackHeight, quality, FileFormat.jpeg).ConfigureAwait(false);
            }));
        }
        else
        {
            _logger.LogDebug("Skipping existing JPEG fallback: {JpegFallbackOutputFile}", jpegFallbackOutputFile);
        }

        if (format != FileFormat.jpeg)
        {
            if (!_fileService.FileExistsInRootPath(formatFallbackOutputFile))
            {
                _logger.LogDebug("Generating {Format} fallback for {Src} -> {FormatFallbackOutputFile}", format, src, formatFallbackOutputFile);
                tasks.Add(Task.Run(async () =>
                {
                    _fileService.CreateDirectoryForFile(formatFallbackOutputFile);
                    await _imageProcessingService.ProcessAndSaveImageAsync(originalPath, formatFallbackOutputFile, fallbackWidth, fallbackHeight, quality, format).ConfigureAwait(false);
                }));
            }
            else
            {
                _logger.LogDebug("Skipping existing {Format} fallback: {FormatFallbackOutputFile}", format, formatFallbackOutputFile);
            }
        }

        if (tasks.Any())
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    private static int CalculateProgress(int completedSteps, int totalSteps)
    {
        if (totalSteps <= 0) return 10;
        const int startPercentage = 10;
        const int endPercentage = 90;
        double fractionComplete = (double)completedSteps / totalSteps;
        int progress = startPercentage + (int)(fractionComplete * (endPercentage - startPercentage));
        return Math.Clamp(progress, startPercentage, endPercentage);
    }

    private string GetImageProccessOutputDir(string fileName)
    {
        var relativePath = Path.Combine(_config.OutputDir, fileName);
        var path = _fileService.GetRootPath(relativePath);
        return path;
    }

    private ImageInfo CreateImageInfo(string src, int quality, FileFormat format, string cacheKey)
    {
        var width = LastWidth;
        var height = HelpersMethods.ToAspectRatio(width, _config.AspectWidth, _config.AspectHeigth);
        var sanitizedName = _fileService.SanitizeFileName(Path.GetFileNameWithoutExtension(src));
         return new ImageInfo(sanitizedName, width, height, format, quality)
        {
            Key = cacheKey
        };
    }

    private static string GenerateCacheKey(string src, int quality, FileFormat format)
    {
        // Assuming src is already validated as non-null/empty by the caller.

        const char Separator = '-';

        // --- Pre-calculate lengths accurately ---

        // 1. Source length
        int srcLength = src.Length;

        // 2. Format string and length (Optimized: Use lookup, avoid ToString())
        // Assumes FileFormatExtensions.FormatStrings maps enum values correctly & efficiently.
        // Add bounds checking here if 'format' could potentially be an invalid enum value.
        string formatStr = FileFormatExtensions.FormatStrings[(int)format];
        int formatLength = formatStr.Length;

        // 3. Quality string length (Optimized: Calculate exact digits)
        int qualityLength = quality < 100 ? 2 : 3;

        // 4. Calculate total exact length for the final string
        int totalLength = srcLength + 1 + formatLength + 1 + qualityLength;

        // --- Use string.Create for optimal allocation ---
        // Pass necessary values efficiently via a state tuple.
        // Pass the looked-up formatStr to avoid recalculating it inside the lambda.
        var state = (src, quality, formatStr);
        return string.Create(totalLength, state, (buffer, stateTuple) =>
        {
            var (s, q, fmtStr) = stateTuple; // Deconstruct state
            int pos = 0;

            // 1. Copy Source
            s.AsSpan().CopyTo(buffer); // Equivalent to buffer.Slice(0, s.Length)
            pos += s.Length;

            // 2. Add Separator 1
            buffer[pos++] = Separator;

            // 3. Copy Format String (using the efficient looked-up string)
            fmtStr.AsSpan().CopyTo(buffer.Slice(pos));
            pos += fmtStr.Length;

            // 4. Add Separator 2
            buffer[pos++] = Separator;

            // 5. Format Quality Int directly into the buffer
            // TryFormat is highly efficient.
            bool success = q.TryFormat(buffer.Slice(pos), out int written);

            // Assertions help catch errors during development if length calculation is wrong.
            Debug.Assert(success, $"Quality formatting failed unexpectedly for value {q}.");
            if (!success) throw new FormatException($"Failed to format quality '{q}'."); // Handle release failure

            pos += written;

            // Final check: Because we calculated the exact length, 'pos' should now equal 'buffer.Length'.
            Debug.Assert(pos == buffer.Length, "Final position does not match calculated total length.");
        }); 
    }
     
  }