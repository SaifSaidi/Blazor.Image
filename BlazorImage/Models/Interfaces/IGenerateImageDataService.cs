namespace BlazorImage.Models.Interfaces;

internal interface IGenerateImageDataService
{

    string GenerateImageFallbackSrc(string src, FileFormat format = FileFormat.jpeg);
    string GenerateImageName(string src, int width, int? quality, FileFormat? format);
    string GenerateImagePlaceholder(string src, FileFormat format);
}
