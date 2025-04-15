namespace BlazorImage.Models.Interfaces;

internal interface ICacheService
{
    ValueTask<ImageInfo?> GetFromCacheAsync(string cacheKey);
    ValueTask<ImageInfo?> SaveToCacheAsync(string cacheKey, ImageInfo imageInfo);
    void DeleteAllImageInfoFromDatabase();
    Task DeleteFromCacheAsync(string cacheKey);
    void DeleteImageInfoFromDatabase(string cacheKey);
    void HardResetAllFromCache(); 
    Task ResetAllFromCacheAsync();
}
