using ImageMagick;
using Microsoft.Extensions.Logging;

namespace BlazorImage.Services;

internal class ImageProcessingService : IImageProcessingService
{
    private readonly IFileService _fileService;
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingService(IFileService fileService,
        ILogger<ImageProcessingService> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    public async Task ProcessAndSaveImageAsync(string inputPath,
             string outputPath,
             int width,
             int height,
             int quality,
             FileFormat format)
    {
        _logger.LogInformation("Starting image processing: InputPath={InputPath}, OutputPath={OutputPath}, Width={Width}, Height={Height}, Quality={Quality}, Format={Format}",
            inputPath, outputPath, width, height, quality, format);

        try
        {
            ValidateInputParameters(inputPath, (uint)quality);


            var magickFormat = format switch
            {
                FileFormat.webp => MagickFormat.WebP,
                FileFormat.jpeg => MagickFormat.Jpeg,
                FileFormat.png => MagickFormat.Png,
                FileFormat.avif => MagickFormat.Avif,

                _ => throw new ArgumentException("Unsupported file format")
            };

            using var image = new MagickImage(inputPath);

            var geometry = new MagickGeometry
            {
                Width = (uint)width,
                Height = (uint)height,
                IgnoreAspectRatio = true,
            };

            image.FilterType = FilterType.Lanczos;
            image.Resize(geometry);

            image.AutoOrient();
            image.Strip();
            image.ColorSpace = ColorSpace.sRGB;

            image.Quality = (uint)quality;


            switch (magickFormat)
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

                case MagickFormat.Png:
                    image.Settings.SetDefine("png:compression-level", "9");
                    image.Settings.SetDefine("png:filter", "adaptive");
                    image.Settings.Compression = CompressionMethod.Zip;
                    image.Settings.SetDefine("png:bit-depth", "auto");
                    image.Settings.SetDefine("png:color-type", "6");
                    break;


                case MagickFormat.Avif: // AVIF Settings
                    image.Settings.SetDefine("avif:speed", "0"); // 0=slowest, best quality, configurable
                    image.Settings.SetDefine("avif:format", "yuv420");
                    image.Quality = (uint)(quality > 50 ? quality - 15 : quality); // Quality setting applies to AVIF
                    break;

            }

            await SaveImageAsync(image, outputPath, magickFormat);

            _logger.LogInformation("Image processing completed successfully: OutputPath={OutputPath}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during image processing: InputPath={InputPath}, OutputPath={OutputPath}", inputPath, outputPath);
            _logger.LogError(ex.Message);
            throw;
        }
    }

    private void ValidateInputParameters(string inputPath, uint quality)
    {
        if (!_fileService.FileExistsInRootPath(inputPath))
            throw new FileNotFoundException("Input file not found", inputPath);

        if (quality < 15 || quality > 100)
            throw new ArgumentException("Image Quality must be greater than 15, and less than 100.");
    }


    private async Task SaveImageAsync(MagickImage image, string outputPath, MagickFormat format)
    {
        try
        {
             _fileService.CreateDirectoryForFile(outputPath);


            await image.WriteAsync(outputPath, format);
            if (format == MagickFormat.Png || format == MagickFormat.Jpeg)
            {
                var file = new FileInfo(outputPath);
                var optimizer = new ImageOptimizer
                {
                    OptimalCompression = true
                };
                if(optimizer.Compress(file))
                {

                    file.Refresh();
                }
            } 

            _logger.LogInformation("Image saved: OutputPath={OutputPath}, Format={Format}",
                outputPath, format);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving image: OutputPath={OutputPath}", outputPath);
            throw;
        }
    }
}
