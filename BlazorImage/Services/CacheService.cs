using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
 
using System.Text;
using Microsoft.Extensions.Logging;
using BlazorImage.Models;
using Microsoft.AspNetCore.Hosting;

using System.Runtime.CompilerServices;


[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace BlazorImage.Services;

internal class CacheService : ICacheService
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
    private readonly string _DirName; 
     
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
        
        string dirName = options.Value.Dir.TrimStart('/');
         _DirName = dirName; 
         _Dir = Path.Combine(env.WebRootPath,dirName);

        AbsoluteExpirationRelativeToNowValue = options.Value.AbsoluteExpirationRelativeToNow;
        SlidingExpirationValue = options.Value.SlidingExpiration;
    }

    private  MemoryCacheEntryOptions GetCacheEntryOptions()
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AbsoluteExpirationRelativeToNowValue,
            SlidingExpiration = SlidingExpirationValue,
            Size = 1  
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
            _logger.LogError(ex, ErrorSavingMessage); // Logging exception
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

    public string ReadData(string route)
    {

        var collection = _db.GetCollection<ImageInfo>(Constants.LiteDbCollection);

        var imageInfos = collection.FindAll();
        StringBuilder sb = BlazorImageHtml(route, imageInfos);

        return sb.ToString();
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


    private StringBuilder BlazorImageHtml(string route, IEnumerable<ImageInfo> imageInfos)
    {
        if (string.IsNullOrEmpty(route))
        {
            throw new ArgumentException("Route cannot be null or empty.", nameof(route));
        }
        if (imageInfos == null)
        {
            throw new ArgumentNullException(nameof(imageInfos), "ImageInfos cannot be null.");
        }


        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='en'>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset='UTF-8'>");
        sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        sb.AppendLine("    <title>BlazorImage Dashboard</title>");

        sb.AppendLine("    <script src='https://cdn.tailwindcss.com'></script>");
        sb.AppendLine("    <link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.0.0/css/all.min.css' rel='stylesheet' />");

        sb.AppendLine("    <script>");
        sb.AppendLine("        document.addEventListener('DOMContentLoaded', function() {");
        sb.AppendLine("            const getCellValue = (tr, idx) => tr.children[idx].innerText || tr.children[idx].textContent;");
        sb.AppendLine("            const comparer = (idx, asc) => (a, b) => ((v1, v2) => ");
        sb.AppendLine("                v1 !== '' && v2 !== '' && !isNaN(v1) && !isNaN(v2) ? v1 - v2 : v1.toString().localeCompare(v2)");
        sb.AppendLine("            )(getCellValue(asc ? a : b, idx), getCellValue(asc ? b : a, idx));");
        sb.AppendLine("            document.querySelectorAll('th').forEach(th => th.addEventListener('click', (() => {");
        sb.AppendLine("                const table = th.closest('table');");
        sb.AppendLine("                Array.from(table.querySelectorAll('tbody tr'))");
        sb.AppendLine("                    .sort(comparer(Array.from(th.parentNode.children).indexOf(th), this.asc = !this.asc))");
        sb.AppendLine("                    .forEach(tr => table.querySelector('tbody').appendChild(tr) );");
        sb.AppendLine("            })) );");
        sb.AppendLine("        });");
        sb.AppendLine("    </script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body class='bg-gray-50 font-sans antialiased text-gray-900'>");
        sb.AppendLine("    <div class='container mx-auto px-4 md:px-8 py-10'>");
        sb.AppendLine("        <header class='mb-10 flex justify-between items-center'>");
        sb.AppendLine("            <div>");
        sb.AppendLine("                <h1 class='text-3xl font-bold text-indigo-700'><i class='fa fa-image text-indigo-500 mr-2'></i> BlazorImage Dashboard</h1>");
        sb.AppendLine("                <p class='text-gray-600 mt-2'>Monitor and manage your BlazorImage cache services with ease.</p>");
        sb.AppendLine("            </div>");

        sb.AppendLine("            <div class='relative inline-block text-left dropdown'>"); // Dropdown container
        sb.AppendLine("                <div>");
        sb.AppendLine("                    <button type='button' class='inline-flex justify-center w-full px-4 py-2 font-semibold text-gray-700 bg-white border border-gray-300 rounded-md shadow-sm hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-opacity-50 dark:bg-gray-700 dark:text-white dark:border-gray-600 dark:hover:bg-gray-600 dark:hover:border-gray-500 dropdown-toggle' id='dropdownActionButton' aria-haspopup='true' aria-expanded='true'>");
        sb.AppendLine("                        <i class='fa fa-wrench mr-2'></i> Actions"); // Icon for actions button
        sb.AppendLine("                        <svg class='w-5 h-5 ml-2 -mr-1' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20' fill='currentColor' aria-hidden='true'>");
        sb.AppendLine("                            <path fill-rule='evenodd' d='M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z' clip-rule='evenodd' />");
        sb.AppendLine("                        </svg>");
        sb.AppendLine("                    </button>");
        sb.AppendLine("                </div>");

        sb.AppendLine("                <div class='absolute right-0 mt-2 w-56 rounded-md shadow-lg bg-white ring-1 ring-black ring-opacity-5 focus:outline-none dropdown-menu opacity-0 pointer-events-none transform transition-all translate-y-2 ' role='menu' aria-orientation='vertical' aria-labelledby='dropdownActionButton'>");
        sb.AppendLine("                    <div class='py-1'>");
        sb.AppendLine($"                        <a href='{route}/refresh-all' class='block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100 hover:text-gray-900 dark:text-gray-100 dark:hover:text-white dark:hover:bg-gray-600' role='menuitem'><i class='fa fa-sync-alt mr-2'></i> Refresh Cache</a>");
        sb.AppendLine($"                        <a href='{route}/reset-all' class='block px-4 py-2 text-sm text-red-600 hover:bg-gray-100 hover:text-red-700 dark:text-red-300 dark:hover:text-red-500 dark:hover:bg-gray-600' role='menuitem'><i class='fa fa-trash mr-2'></i> Reset Cache</a>");
        sb.AppendLine($"                        <a href='{route}/hard-reset-all' class='block px-4 py-2 text-sm text-red-700 hover:bg-gray-100 hover:text-red-800 dark:text-red-400 dark:hover:text-red-600 dark:hover:bg-gray-600' role='menuitem'><i class='fa fa-skull-crossbones mr-2'></i> Hard Reset (delete optimized images)</a>");
        sb.AppendLine("                    </div>");
        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");

        sb.AppendLine("        </header>");

        sb.AppendLine("        <section class='mb-8 p-6 bg-white shadow rounded-lg border border-gray-200'>");
        sb.AppendLine("            <div class='flex justify-between items-center mb-4'>");
        sb.AppendLine("                <h2 class='text-xl font-semibold text-gray-800'><i class='fa fa-memory text-gray-500 mr-2'></i> Cache Status</h2>");
        sb.AppendLine("                <button id='toggleButton' class='bg-gray-200 hover:bg-gray-300 text-gray-700 font-semibold py-1 px-3 rounded-full focus:outline-none focus:ring-2 focus:ring-gray-400 focus:ring-opacity-50 text-sm'><span id='toggleText'></span></button>");
        sb.AppendLine("            </div>");
        sb.AppendLine($"            <p class='text-gray-700 mb-3'><span class='font-semibold'>Tracked Items:</span> <span class='text-blue-700 font-medium'>({_dictionaryCacheData.SourceInfoCache.Count}) entries</span></p>");

        sb.AppendLine("            <ul id='cacheList' class='list-disc pl-5 text-sm text-gray-600 mt-2' style='display: none;'>");
        foreach (var item in _dictionaryCacheData.SourceInfoCache)
        {
            sb.AppendLine($"                <li class=' '>{item.Key}</li>");
        }
        sb.AppendLine("            </ul>");
        sb.AppendLine("        </section>");

        sb.AppendLine("        <script>");
        sb.AppendLine("            document.addEventListener('DOMContentLoaded', function() {");
        sb.AppendLine("                var toggleButton = document.getElementById('toggleButton');");
        sb.AppendLine("                var cacheList = document.getElementById('cacheList');");
        sb.AppendLine("                var toggleText = document.getElementById('toggleText');");
        sb.AppendLine("                toggleText.textContent = '(Show Cache Keys)';");

        sb.AppendLine("                toggleButton.addEventListener('click', function() {");
        sb.AppendLine("                    if (cacheList.style.display === 'none') {");
        sb.AppendLine("                        cacheList.style.display = 'block';");
        sb.AppendLine("                        toggleText.textContent = '(Hide Cache Keys)';");
        sb.AppendLine("                    } else {");
        sb.AppendLine("                        cacheList.style.display = 'none';");
        sb.AppendLine("                        toggleText.textContent = '(Show Cache Keys)';");
        sb.AppendLine("                    }");
        sb.AppendLine("                });");

        // Dropdown Toggle Script
        sb.AppendLine("                const dropdownButton = document.querySelector('.dropdown-toggle');");
        sb.AppendLine("                const dropdownMenu = document.querySelector('.dropdown-menu');");

        sb.AppendLine("                dropdownButton.addEventListener('click', function(event) {");
        sb.AppendLine("                    event.stopPropagation();"); // Prevent document click from firing immediately
        sb.AppendLine("                    dropdownMenu.classList.toggle('opacity-0');");
        sb.AppendLine("                    dropdownMenu.classList.toggle('pointer-events-none');");
        sb.AppendLine("                    dropdownMenu.classList.toggle('translate-y-2');");
        sb.AppendLine("                    dropdownMenu.classList.toggle('opacity-100');");
        sb.AppendLine("                    dropdownMenu.classList.toggle('translate-y-0');");
        sb.AppendLine("                });");

        sb.AppendLine("                document.addEventListener('click', function(event) {");
        sb.AppendLine("                    if (!dropdownMenu.closest('.dropdown').contains(event.target)) {");
        sb.AppendLine("                        dropdownMenu.classList.add('opacity-0');");
        sb.AppendLine("                        dropdownMenu.classList.add('pointer-events-none');");
        sb.AppendLine("                        dropdownMenu.classList.add('translate-y-2');");
        sb.AppendLine("                        dropdownMenu.classList.remove('opacity-100');");
        sb.AppendLine("                        dropdownMenu.classList.remove('translate-y-0');");
        sb.AppendLine("                    }");
        sb.AppendLine("                });");
        sb.AppendLine("            });");
        sb.AppendLine("        </script>");

        sb.AppendLine("        <section class='bg-white shadow-lg rounded-lg overflow-hidden'>");
        sb.AppendLine("            <div class='px-6 py-5 bg-gray-100 border-b border-gray-200'>");
        sb.AppendLine("                <h2 class='text-xl font-semibold text-gray-800'><i class='fa fa-table mr-2'></i> Image Information</h2>");
        sb.AppendLine("            </div>");
        sb.AppendLine("            <div class='overflow-x-auto'>");
        sb.AppendLine("                <table class='min-w-full divide-y divide-gray-300'>");
        sb.AppendLine("                    <thead class='bg-gray-50'>");
        sb.AppendLine("                        <tr>");
        sb.AppendLine("                            <th class='px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors'><i class='fa fa-cog mr-1'></i> Action</th>");
        sb.AppendLine("                            <th class='px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors'><i class='fa fa-image mr-1'></i> Image</th>");
        sb.AppendLine("                            <th class='px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors'><i class='fa fa-file-signature mr-1'></i> Sanitized Name</th>");
        sb.AppendLine("                            <th class='px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors'><i class='fa fa-file-alt mr-1'></i> Format</th>");
        sb.AppendLine("                            <th class='px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors'><i class='fa fa-compress mr-1'></i> Quality</th>");
        sb.AppendLine("                            <th class='px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-gray-100 transition-colors'><i class='fa fa-clock mr-1'></i> Processed Time (UTC)</th>");
        sb.AppendLine("                        </tr>");
        sb.AppendLine("                    </thead>");

        sb.AppendLine("                    <tbody class='bg-white divide-y divide-gray-200'>");

        int i = 1;
        foreach (var imageInfo in imageInfos)
        {

            var formatExt = imageInfo.Format.GetValueOrDefault().ToFileExtension();
            var imagPath = $"/{_DirName}/{imageInfo.SanitizedName}/{imageInfo.SanitizedName}.{formatExt}";

            sb.AppendLine($"                        <tr class='{(i % 2 == 0 ? "bg-gray-50" : "bg-white")} hover:bg-gray-100 transition-colors'>");
            sb.AppendLine($"                            <td class='px-6 py-4 whitespace-nowrap text-sm font-medium'><a href='{route}/delete?cache={imageInfo.Key}' class='text-red-600 hover:text-red-900 transition-colors'><i class='fa fa-trash-alt mr-1'></i> Remove</a></td>");
            sb.AppendLine($"                            <td class='px-6 py-4 whitespace-nowrap'><img class='object-cover rounded-md w-12 h-12' src='{imagPath}' alt='Image preview' /></td>");
            sb.AppendLine($"                            <td class='px-6 py-4 whitespace-nowrap text-gray-700'>{imageInfo.SanitizedName}</td>");
            sb.AppendLine($"                            <td class='px-6 py-4 whitespace-nowrap text-gray-700'>{imageInfo.Format.GetValueOrDefault().ToMimeType()}</td>");
            sb.AppendLine($"                            <td class='px-6 py-4 whitespace-nowrap text-gray-700'>{imageInfo.Quality}</td>");
            sb.AppendLine($"                            <td class='px-6 py-4 whitespace-nowrap text-gray-700'>{imageInfo?.ProcessedTime.GetValueOrDefault().ToString("yyyy-MM-dd HH:mm:ss UTC")}</td>");
            sb.AppendLine("                        </tr>");
            i++;
        }

        if (i == 1)
        {
            sb.AppendLine("                        <tr><td colspan='10' class='px-6 py-4 whitespace-nowrap text-sm text-gray-500 text-center'>No data found in the database</td></tr>");
        }

        sb.AppendLine("                    </tbody>");
        sb.AppendLine("                </table>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </section>");

        sb.AppendLine("        <footer class='mt-12 text-center text-gray-500 text-sm'>");
        sb.AppendLine("            <p>&copy; 2025 Blazor.Image. All rights reserved.</p>");
        sb.AppendLine("        </footer>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb;
    }
}