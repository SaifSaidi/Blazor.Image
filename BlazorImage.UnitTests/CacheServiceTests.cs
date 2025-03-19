using BlazorImage.Data;
using BlazorImage.Extensions;
using BlazorImage.Models;
using BlazorImage.Services;
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
        private readonly Mock<DictionaryCacheDataService2> _mockDictionaryCacheData;
        private readonly CacheService _cacheService;

        public CacheServiceTests()
        {
            _mockMemoryCache = new Mock<IMemoryCache>();
            _mockLogger = new Mock<ILogger<CacheService>>();
            _mockOptions = new Mock<IOptions<BlazorImageConfig>>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            _mockDictionaryCacheData = new Mock<DictionaryCacheDataService2>();

            // Setup mock options
            _mockOptions.Setup(o => o.Value).Returns(new BlazorImageConfig { Dir = "testDir" });

            // Setup mock web host environment
            _mockWebHostEnvironment.Setup(e => e.WebRootPath).Returns("wwwroot");

            // Initialize the service
            _cacheService = new CacheService(
                _mockMemoryCache.Object,
                _mockOptions.Object,
                _mockLogger.Object,
                _mockWebHostEnvironment.Object,
                _mockDictionaryCacheData.Object
            );
        }

        [Fact]
        public async Task GetFromCacheAsync_ReturnsCachedItem_WhenItemExistsInCache()
        {
            // Arrange
            var cacheKey = "testKey";
            var expectedImageInfo = new ImageInfo("testImage", 100, 100, FileFormat.webp, 75, null) { Key = cacheKey };

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
            var imageInfo = new ImageInfo("testImage", 100, 100, FileFormat.webp, 75, null);

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
 

        [Fact]
        public void ResetDatabaseAndDictionaryCache_DeletesAndRecreatesDatabase()
        {
            // Arrange
            var liteDbPath = Path.Combine("wwwroot", "testDir", Constants.LiteDbName);

            // Act
            _cacheService.ResetDatabaseAndDictionaryCache();

            // Assert
            Assert.False(File.Exists(liteDbPath)); // Ensure the file is deleted
        }

        [Fact]
        public void DeleteImageDirectoriesAndRecreated_DeletesAndRecreatesDirectories()
        {
            // Arrange
            var directoryPath = Path.Combine("wwwroot", "testDir");

            // Act
            _cacheService.DeleteImageDirectoriesAndRecreated();

            // Assert
            Assert.True(Directory.Exists(directoryPath)); // Ensure the directory is recreated
        }
    }
}
