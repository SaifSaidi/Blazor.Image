namespace BlazorImage.Models.Interfaces;

internal interface IImageElementService
{
    (string source, string fallback, string placeholder) GetStaticPictureSourceWithMetadataInfo(string sanitizedName, int quality, FileFormat format, int? Width);
 
}
