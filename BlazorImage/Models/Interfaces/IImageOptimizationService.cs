using System.Threading.Channels;

namespace BlazorImage.Models.Interfaces
{
    internal interface IImageOptimizationService
    {
        ImageInfo CreateImageInfo(string src, int quality, FileFormat format);
        void DecrementRequestCount(string cacheKey);
        Task GenerateFallbackImages(string originalPath, string src, int quality, FileFormat format);
        Task GeneratePlaceholderImage(string originalPath, string src, FileFormat format);
        ValueTask<Result<ImageInfo>?> GetImageInfoAsync(string src, int? quality, FileFormat? format);
        bool IsImageBeingProcessed(string cacheKey);
        void MarkImageAsProcessing(string cacheKey);
        Task<Result<ImageInfo>> OptimizeAndCacheImage(string src, int quality, FileFormat format, string cacheKey, ChannelWriter<string> writer);
        ValueTask<Result<ImageInfo>?> ProcessAndGetImageInfoAsync(string src, int? quality, FileFormat? format, ChannelWriter<string> writer);
        IEnumerable<Task> ProcessImages(string src, int quality, FileFormat format, ChannelWriter<string> writer);
    }
}