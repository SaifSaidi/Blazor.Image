namespace BlazorImage.Models.Interfaces;

internal interface IImageElementService
{ 
    double GetAspectRatio();
    (string source, string fallback, string placeholder) GetStaticPictureSourceWithMetadataInfo(string sanitizedName, int quality, FileFormat format, int? Width);
 
}
