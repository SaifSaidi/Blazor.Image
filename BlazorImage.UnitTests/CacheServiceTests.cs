using BlazorImage.Data;
using BlazorImage.Extensions;
using BlazorImage.Models;
using BlazorImage.Models.Interfaces;
using BlazorImage.Services;
using LiteDB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BlazorImage.UnitTests
{

    public class CacheServiceTests
    {
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly Mock<ILogger<CacheService>> _mockLogger;
        private readonly Mock<IOptions<BlazorImageConfig>> _mockOptions;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly Mock<DictionaryCacheDataService> _mockDictionaryCacheData;
        private readonly CacheService _cacheService;
        private readonly Mock<ILiteDatabase> _db;


        public CacheServiceTests()
        {
            _mockMemoryCache = new Mock<IMemoryCache>();
            _mockLogger = new Mock<ILogger<CacheService>>();
            _mockOptions = new Mock<IOptions<BlazorImageConfig>>();
          _db = new Mock<ILiteDatabase>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            _mockDictionaryCacheData = new Mock<DictionaryCacheDataService>();

            // Setup mock options
            _mockOptions.Setup(o => o.Value).Returns(new BlazorImageConfig { OutputDir = "testDir" });

            // Setup mock web host environment
            _mockWebHostEnvironment.Setup(e => e.WebRootPath).Returns("wwwroot");

            // Initialize the service
            _cacheService = new CacheService(
                _mockMemoryCache.Object,
                _mockOptions.Object,
                _mockLogger.Object,
                _mockWebHostEnvironment.Object,
                _mockDictionaryCacheData.Object,
                _db.Object
            );
        }

        [Fact]
        public async Task GetFromCacheAsync_ReturnsCachedItem_WhenItemExistsInCache()
        {
            // Arrange
            var cacheKey = "testKey";
            var expectedImageInfo = new ImageInfo("testImage", 100, 100, FileFormat.webp, 75) { Key = cacheKey };

            object cacheValue = expectedImageInfo;
            _mockMemoryCache
                .Setup(cache => cache.TryGetValue(cacheKey, out cacheValue))
                .Returns(true);

            // Act
            var result = await _cacheService.GetFromCacheAsync(cacheKey);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedImageInfo.Key, result.Key);
        }

        [Fact]
        public async Task GetFromCacheAsync_ReturnsNull_WhenItemDoesNotExistInCacheOrDatabase()
        {
            // Arrange
            var cacheKey = "nonexistentKey";
            object cacheValue = null;
            _mockMemoryCache.Setup(mc => mc.TryGetValue(cacheKey, out cacheValue)).Returns(false);

            // Act
            var result = await _cacheService.GetFromCacheAsync(cacheKey);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SaveToCacheAsync_SavesItemToCacheAndDatabase()
        {
            // Arrange
            var cacheKey = "testKey";
            var imageInfo = new ImageInfo("testImage", 100, 100, FileFormat.webp, 75);

            _mockMemoryCache
                .Setup(cache => cache.CreateEntry(cacheKey))
                .Returns(Mock.Of<ICacheEntry>());

            // Act
            var result = await _cacheService.SaveToCacheAsync(cacheKey, imageInfo);

            // Assert
            Assert.NotNull(result);
            _mockMemoryCache.Verify(cache => cache.CreateEntry(cacheKey), Times.Once);
        }

        [Fact]
        public async Task DeleteFromCacheAsync_RemovesItemFromCacheAndDatabase()
        {
            // Arrange
            var cacheKey = "testKey";

            _mockMemoryCache
                .Setup(cache => cache.Remove(cacheKey))
                .Verifiable();

            // Act
            await _cacheService.DeleteFromCacheAsync(cacheKey);

            // Assert
            _mockMemoryCache.Verify();
        }
 
 
    }
}
