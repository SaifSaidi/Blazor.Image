using System.Linq;
using ImageMagick;
using Microsoft.Extensions.Logging;

namespace BlazorImage.Services;

internal class ImageProcessingService : IImageProcessingService
{
    private readonly IFileService _fileService;
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingService(
        IFileService fileService,
        ILogger<ImageProcessingService> logger)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ProcessAndSaveImageAsync(
        string inputPath,
        string outputPath,
        int width,
        int height,
        int quality,
        FileFormat format)
    {
        try
        {
            ValidateInputParameters(inputPath, (uint)quality);
            var magickFormat = MapToFormat(format);

            using var image = new MagickImage();
            await image.ReadAsync(inputPath);

            image.Resize(new MagickGeometry
            {
                Width = (uint)width,
                Height = (uint)height,
                IgnoreAspectRatio = true,
                Greater = true
            });

            image.FilterType = FilterType.Lanczos;
            image.AutoOrient();
            image.Strip();
            image.ColorSpace = ColorSpace.sRGB;
            image.Quality = (uint)quality;

            ApplyFormatSettings(image, magickFormat, quality);

            await SaveImageAsync(image, outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image: Input={InputPath}, Output={OutputPath}", inputPath, outputPath);
            throw;
        }
    }

    private static MagickFormat MapToFormat(FileFormat format) => format switch
    {
        FileFormat.webp => MagickFormat.WebP,
        FileFormat.jpeg => MagickFormat.Jpeg,
        FileFormat.png => MagickFormat.Png,
        FileFormat.avif => MagickFormat.Avif,
        _ => throw new ArgumentException($"Unsupported file format: {format}")
    };

    private void ApplyFormatSettings(MagickImage image, MagickFormat format, int quality)
    {
        try
        {
            switch (format)
            {
                case MagickFormat.WebP:
                    image.Settings.SetDefine("webp:method", "6");
                    image.Settings.SetDefine("webp:near-lossless", "true");
                    image.Settings.SetDefine("webp:smart-subsample", "true");
                    break;

                case MagickFormat.Jpeg:
                    image.Settings.SetDefine("jpeg:dct-method", "float");
                    image.Settings.SetDefine("jpeg:optimize-coding", "true");
                    image.Settings.SetDefine("jpeg:progressive", "true");
                    image.Settings.SetDefine("jpeg:sampling-factor", "4:2:0");
                    image.Settings.ColorType = ColorType.TrueColor;
                    break;

                //case MagickFormat.Png:
                //    image.Settings.SetDefine("png:compression-level", "9");
                //    image.Settings.SetDefine("png:filter", "adaptive");
                //    image.SetCompression(CompressionMethod.Zip);
                //    image.Settings.SetDefine("png:bit-depth", "auto");
                //    image.Settings.SetDefine("png:color-type", "6");
                //    break;

                case MagickFormat.Png:
                     
                    image.Settings.SetDefine("png:compression-level", "9");
                    image.Settings.SetDefine("png:filter", "adaptive");
                    image.Settings.SetDefine("png:bit-depth", "8");  
                    bool applyQuantization = true; // Make this configurable or conditional if needed

                    // Only quantize if the image isn't already paletted or has many colors
                    if (applyQuantization && image.ColorType != ColorType.Palette && image.TotalColors > 256)
                    {
                        try
                        {
                             var quantizeSettings = new QuantizeSettings
                            {
                                Colors = 256, // Target 256 colors
                                DitherMethod = DitherMethod.FloydSteinberg // Or NoDither, Riemersma
                            };
                             image.Quantize(quantizeSettings);
                         }
                        catch (Exception qEx)
                        {
                            _logger.LogWarning(qEx, "Quantization step failed for PNG, proceeding without it.");
                        }
                    }
                    else if (image.ColorType == ColorType.Palette || image.TotalColors <= 256)
                    {
                        _logger.LogTrace("Skipping quantization for PNG as image is already paletted or has few colors.");
                    }

                    break;

                case MagickFormat.Avif:
                     
                    image.Settings.SetDefine("avif:speed", "0");
                    image.Settings.SetDefine("avif:format", "yuv420");
                    image.Quality = (uint)Math.Clamp(quality * 0.85, 0, 100);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Format-specific settings failed for {Format}. Proceeding with defaults.", format);
        }
    }

    private void ValidateInputParameters(string inputPath, uint quality)
    {
        if (!_fileService.FileExistsInRootPath(inputPath))
            throw new FileNotFoundException("Input file not found", inputPath);

        if (quality < Constants.MinQuality || quality > Constants.MaxQuality)
            throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 15 and 100.");
    }

    private async Task SaveImageAsync(MagickImage image, string outputPath)
    {
        _fileService.CreateDirectoryForFile(outputPath);
        await image.WriteAsync(outputPath);
        _logger.LogInformation("Image saved: Output={OutputPath}", outputPath);
    }
}
