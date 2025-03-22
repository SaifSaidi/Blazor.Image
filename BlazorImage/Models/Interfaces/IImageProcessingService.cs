using ImageMagick;

namespace BlazorImage.Models.Interfaces
{
    internal interface IImageProcessingService
    {
        Task ProcessAndSaveImageAsync(string inputPath, string outputPath, int width, int height, int quality, FileFormat format);
        
    }
}