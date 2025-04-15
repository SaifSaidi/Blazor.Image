using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using BlazorImage.Models;
using Microsoft.AspNetCore.Hosting;
using System.Runtime.CompilerServices;


[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace BlazorImage.Services;

internal sealed class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly SemaphoreSlim _dbSemaphore = new(1, 1);
    private readonly DictionaryCacheDataService _dictionaryCacheData;
    private readonly ILiteDatabase _db;
    private readonly TimeSpan AbsoluteExpirationRelativeToNowValue;
    private readonly TimeSpan? SlidingExpirationValue;

    private const string ErrorRetrievingMessage = "Error retrieving from cache or database";
    private const string ErrorSavingMessage = "Error saving to cache or database";
    private const string ErrorRemoveMessage = "Error removing from cache or database";
    private readonly string _Dir;

    public CacheService(
        IMemoryCache cache,
        IOptions<BlazorImageConfig> options,
        ILogger<CacheService> logger,
        IWebHostEnvironment env,
        DictionaryCacheDataService dictionaryCacheData,
        ILiteDatabase db)
    {
        _cache = cache;
        _logger = logger;
        _dictionaryCacheData = dictionaryCacheData;
        _db = db;

        string dirName = options.Value.OutputDir.TrimStart('/');
        _Dir = Path.Combine(env.WebRootPath, dirName);

        AbsoluteExpirationRelativeToNowValue = options.Value.AbsoluteExpirationRelativeToNow;
        SlidingExpirationValue = options.Value.SlidingExpiration;
    }

    private MemoryCacheEntryOptions GetCacheEntryOptions()
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AbsoluteExpirationRelativeToNowValue,
            SlidingExpiration = SlidingExpirationValue
        };

    }

    public async ValueTask<ImageInfo?> GetFromCacheAsync(string cacheKey)
    {
        if (_cache.TryGetValue(cacheKey, out ImageInfo? cachedInfo))
        {
            return cachedInfo;
        }

        await _dbSemaphore.WaitAsync();

        try
        {
            var cachedFromDb = GetImageInfoFromDatabase(cacheKey);

            if (cachedFromDb != null)
            {
                _cache.Set(cacheKey, cachedFromDb, GetCacheEntryOptions());
            }

            return cachedFromDb;
        }
        catch (Exception ex)
        {

            _logger.LogError(ex, ErrorRetrievingMessage);
            throw new Exception(ErrorRetrievingMessage, ex);
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    private ImageInfo? GetImageInfoFromDatabase(string cacheKey)
    {

        var collection = _db.GetCollection<ImageInfo>(Constants.LiteDbCollection);
        var cachedFromDb = collection.FindById(cacheKey);

        return cachedFromDb;
    }

    public async ValueTask<ImageInfo?> SaveToCacheAsync(string cacheKey, ImageInfo imageInfo)
    {
        if (string.IsNullOrEmpty(cacheKey))
        {
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(cacheKey));
        }

        if (imageInfo == null)
        {
            throw new ArgumentNullException(nameof(imageInfo), "ImageInfo cannot be null.");
        }

        _cache.Set(cacheKey, new ImageInfo(imageInfo.SanitizedName, imageInfo.Width,
            imageInfo.Height, imageInfo.Format, imageInfo.Quality)
        {
            Key = cacheKey,
        }, GetCacheEntryOptions());

        _logger.LogDebug("Image info saved to memory cache for key: {CacheKey}", cacheKey); // Added logging for memory cache save


        await _dbSemaphore.WaitAsync();

        try
        {
            var savedImage = SaveImageInfoToDatabase(cacheKey, imageInfo);
            return savedImage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorSavingMessage);
            throw new Exception(ErrorSavingMessage, ex);
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    private ImageInfo? SaveImageInfoToDatabase(string cacheKey, ImageInfo imageInfo)
    {

        imageInfo.Key = cacheKey;
        imageInfo.ProcessedTime = DateTime.UtcNow;
        var collection = _db.GetCollection<ImageInfo>(Constants.LiteDbCollection);

        collection.EnsureIndex(x => x.Key, true);

        collection.Upsert(imageInfo);

        _logger.LogDebug("Image info saved to database for key: {CacheKey}", cacheKey); // Added logging for DB save
        return imageInfo;
    }

    public async Task DeleteFromCacheAsync(string cacheKey)
    {
        if (string.IsNullOrEmpty(cacheKey))
        {
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(cacheKey));
        }

        _cache.Remove(cacheKey);
        _logger.LogDebug("Removed key: {CacheKey} from memory cache.", cacheKey); // Added logging for memory cache removal

        await _dbSemaphore.WaitAsync();
        try
        {
            DeleteImageInfoFromDatabase(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ErrorRemoveMessage); // Logging exception
            throw new Exception(ErrorRemoveMessage, ex);
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    public void DeleteImageInfoFromDatabase(string cacheKey)
    {
        var collection = _db.GetCollection<ImageInfo>(Constants.LiteDbCollection);

        collection.Delete(cacheKey);
        _logger.LogDebug("Removed key: {CacheKey} from database.", cacheKey); // Added logging for DB removal
    }

    public void HardResetAllFromCache()
    {
        _logger.LogInformation("Starting Hard Reset of Cache and Image Files."); // Added logging for Hard Reset start
        try
        {
            CompactMemoryCache();
            DeleteImageDirectoriesAndRecreated();
            ResetDatabaseAndDictionaryCache();
            _dictionaryCacheData.ClearData();
        }
        finally
        {
            _logger.LogInformation("Hard Reset completed."); // Added logging for Hard Reset completion
        }
    }

    private void ResetDatabaseAndDictionaryCache()
    {

        var collection = _db.GetCollection<ImageInfo>(Constants.LiteDbCollection);

        // Recreate the LiteDB file
        collection.DeleteAll();
        collection.EnsureIndex(x => x.Key, true);
        _dictionaryCacheData.ClearData();
        _logger.LogInformation("Recreated LiteDB file and collection at path: {LiteDbPath}", collection.Name);
    }

    private void DeleteImageDirectoriesAndRecreated()
    {
        var directoryPath = _Dir;
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
            _logger.LogDebug("Deleted image directories at path: {DirectoryPath}", directoryPath); // Added logging for directory deletion
        }
        else
        {
            _logger.LogWarning("Image directories not found at path: {DirectoryPath}, skipping deletion.", directoryPath); // Added logging if directory not found
        }

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogInformation("Creating directory: {Directory}", directoryPath);
            Directory.CreateDirectory(directoryPath);
        }
        _dictionaryCacheData.ClearData();
        _logger.LogDebug("Ensured directories exist at path: {DirectoryPath}", directoryPath); // Added logging for directory re-creation
    }

    private void CompactMemoryCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
            _logger.LogDebug("Memory cache compacted."); // Added logging for memory cache compaction
        }
        else
        {
            _logger.LogWarning("Cache is not MemoryCache, compaction skipped."); // Added logging if cache is not MemoryCache
        }
    }


    public async Task ResetAllFromCacheAsync()
    {
        _logger.LogInformation("Starting Reset of Cache (Memory and Database)."); // Added logging for Reset start
        try
        {
            CompactMemoryCache();

            await _dbSemaphore.WaitAsync();
            ResetDatabaseAndDictionaryCache();
        }
        finally
        {
            _dbSemaphore.Release();
            _logger.LogInformation("Reset completed."); // Added logging for Reset completion
        }
    }

    public void DeleteAllImageInfoFromDatabase()
    {
        var collection = _db.GetCollection<ImageInfo>(Constants.LiteDbCollection);


        if (collection != null)
        {
            collection.DeleteAll();

            _logger.LogDebug("All ImageInfo deleted from database."); // Added logging for DB DeleteAll
        }
        else
        {
            _logger.LogWarning("LiteDB path is empty, DeleteAll from database skipped."); // Log if DeleteAll skipped due to empty path
        }
    }


}