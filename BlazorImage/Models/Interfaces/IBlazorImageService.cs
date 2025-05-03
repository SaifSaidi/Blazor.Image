using System.Threading.Channels;
namespace BlazorImage.Models.Interfaces
{
    internal interface IBlazorImageService
    {
        ValueTask<Result<ImageInfo>?> GetImageInfoAsync(string src, int? quality, FileFormat? format);
        ValueTask ProcessImageInBackgroundAsync(string src, int? quality, FileFormat? format, ChannelWriter<ProgressUpdate> writer);
    }
}