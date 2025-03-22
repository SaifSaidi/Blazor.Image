namespace BlazorImage.Models.Interfaces;

internal interface IImageElementService
{
    string GenerateImageFallbackSrc(string src, FileFormat format = FileFormat.jpeg);
    string GenerateImageName(string src, int width, int? quality, FileFormat? format);
    string GenerateImagePlaceholder(string src, FileFormat format);
     (string source, string fallback, string placeholder) GetStaticPictureSourceWithMetadataInfo(string sanitizedName, int quality, FileFormat format, int? Width);
 }
