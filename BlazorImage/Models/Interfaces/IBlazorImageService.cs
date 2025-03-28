using System.Threading.Channels;

namespace BlazorImage.Models.Interfaces
{
    internal interface IBlazorImageService
    { 
        ValueTask<Result<ImageInfo>?> GetImageInfoAsync(string src, int? quality, FileFormat? format);
        
         ValueTask<Result<ImageInfo>?> ProcessAndGetImageInfoAsync(string src, int? quality, FileFormat? format, ChannelWriter<string> writer);
     }
}