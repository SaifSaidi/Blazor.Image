//using BenchmarkDotNet.Attributes;
//using BlazorImage.Models;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Moq;

//namespace BlazorImage.Benchmarks
//{
//    //[MemoryDiagnoser]

//    //[ShortRunJob]
//    //public class CacheServiceBenchmarks
//    //{
//    //    private CacheService _cacheService;
//    //    private Mock<IMemoryCache> _mockCache;
//    //    private Mock<ILogger<CacheService>> _mockLogger;
//    //    private Mock<IWebHostEnvironment> _mockEnv;
//    //    private Mock<DictionaryCacheDataService2> _mockDictionaryCacheData;
//    //    private const string CacheKey = "testKey";
//    //    private const string LiteDbPath = "test.db";

//    //    [GlobalSetup]
//    //    public void Setup()
//    //    {
//    //        // Mock dependencies
//    //        _mockCache = new Mock<IMemoryCache>();
//    //        _mockLogger = new Mock<ILogger<CacheService>>();
//    //        _mockEnv = new Mock<IWebHostEnvironment>();
//    //        _mockDictionaryCacheData = new Mock<DictionaryCacheDataService2>();

//    //        // Set up WebRootPath
//    //        _mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());



//    //        // Initialize CacheService
//    //        var options = Options.Create(new BlazorImageConfig { Dir = "test" });

//    //        _cacheService = new CacheService(
//    //            _mockCache.Object,
//    //            options,
//    //            _mockLogger.Object,
//    //            _mockEnv.Object,
//    //            _mockDictionaryCacheData.Object
//    //        );

//    //        // Create a LiteDB file for testing
//    //        File.WriteAllText(LiteDbPath, ""); // Create an empty file
//    //    }

//    //    [GlobalCleanup]
//    //    public void Cleanup()
//    //    {
//    //        // Delete the LiteDB file after benchmarks
//    //        if (File.Exists(LiteDbPath))
//    //        {
//    //            File.Delete(LiteDbPath);
//    //        }
//    //    }

//    //    [Benchmark]
//    //    public async Task GetFromCacheAsync_Benchmark()
//    //    {
//    //        // Mock cache hit
//    //        //var imageInfo = new ImageInfo(CacheKey, "testName", 100, 100, FileFormat.jpeg, 75, DateTime.Now);
//    //        object cacheValue;
//    //        _mockCache.Setup(c => c.TryGetValue(CacheKey, out cacheValue)).Returns(true);

//    //        await _cacheService.GetFromCacheAsync(CacheKey);
//    //    }

//    //    [Benchmark]
//    //    public async Task SaveToCacheAsync_Benchmark()
//    //    {
//    //        var imageInfo = new ImageInfo("testKey", "testName", 100, 100, FileFormat.jpeg, 75, DateTime.Now);
//    //        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

//    //        await _cacheService.SaveToCacheAsync(CacheKey, imageInfo);
//    //    }

//    //[Benchmark]
//    //public async Task DeleteFromCacheAsync_Benchmark()
//    //{
//    //    await _cacheService.DeleteFromCacheAsync(CacheKey);
//    //}

//    //[Benchmark]
//    //public ImageInfo? GetImageInfoFromDatabase_Benchmark()
//    //{
//    //    return _cacheService.GetImageInfoFromDatabase(CacheKey);
//    //}

//    //[Benchmark]
//    //public ImageInfo? SaveImageInfoToDatabase_Benchmark()
//    //{
//    //    var imageInfo = new ImageInfo("testKey", "testName", 100, 100, FileFormat.jpeg, 75, DateTime.Now);
//    //    return _cacheService.SaveImageInfoToDatabase(CacheKey, imageInfo);
//    //}

//    //[Benchmark]
//    //public void DeleteImageInfoFromDatabase_Benchmark()
//    //{
//    //    _cacheService.DeleteImageInfoFromDatabase(CacheKey);
//    //}

//    //[Benchmark]
//    //public void CompactMemoryCache_Benchmark()
//    //{
//    //    _cacheService.CompactMemoryCache();
//    //}
//}
//}
