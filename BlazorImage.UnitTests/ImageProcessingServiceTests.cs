namespace BlazorImage.UnitTests
{
    using System;
    using System.IO;
    using BlazorImage.Services;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;
    using System.Threading.Tasks;
    using ImageMagick;
    using BlazorImage.Extensions;
    using BlazorImage.Models.Interfaces;

    public class ImageProcessingServiceTests
    {
        private readonly Mock<IFileService> _mockFileService;
        private readonly Mock<ILogger<ImageProcessingService>> _mockLogger;
        private readonly ImageProcessingService _service;

        public ImageProcessingServiceTests()
        {
            _mockFileService = new Mock<IFileService>();
            _mockLogger = new Mock<ILogger<ImageProcessingService>>();
            _service = new ImageProcessingService(_mockFileService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ProcessAndSaveImageAsync_FileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            _mockFileService.Setup(fs => fs.FileExistsInRootPath(It.IsAny<string>())).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                _service.ProcessAndSaveImageAsync("input.jpg", "output.jpg", 100, 100, 90, FileFormat.jpeg));
        }

        [Theory]
        [InlineData(14)]
        [InlineData(101)]
        public async Task ProcessAndSaveImageAsync_InvalidQuality_ThrowsArgumentException(int quality)
        {
            // Arrange
            _mockFileService.Setup(fs => fs.FileExistsInRootPath(It.IsAny<string>())).Returns(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ProcessAndSaveImageAsync("input.jpg", "output.jpg", 100, 100, quality, FileFormat.jpeg));
            Assert.Equal("Image Quality must be greater than 15, and less than 100.", ex.Message);
        }

        [Fact]
        public async Task ProcessAndSaveImageAsync_UnsupportedFormat_ThrowsArgumentException()
        {
            // Arrange
            _mockFileService.Setup(fs => fs.FileExistsInRootPath(It.IsAny<string>())).Returns(true);
            var invalidFormat = (FileFormat)999; // An undefined format value

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ProcessAndSaveImageAsync("input.jpg", "output.jpg", 100, 100, 90, invalidFormat));
        }

        [Fact]
        public async Task ProcessAndSaveImageAsync_ValidInput_CreatesOutputFile()
        {
            // Arrange
            var inputPath = "valid-input.jpg";
            var outputPath = "valid-output.webp";
            _mockFileService.Setup(fs => fs.FileExistsInRootPath(inputPath)).Returns(true);
            _mockFileService.Setup(fs => fs.CreateDirectoryForFile(outputPath))
                            .Callback<string>(path => Directory.CreateDirectory(Path.GetDirectoryName(path)));

            // Create a test input image
            using (var image = new MagickImage(MagickColor.FromRgb(255, 255, 255), 800, 600))
            {
                await image.WriteAsync(inputPath);
            }

            // Act
            await _service.ProcessAndSaveImageAsync(inputPath, outputPath, 400, 300, 85, FileFormat.webp);

            // Assert
            Assert.True(File.Exists(outputPath));
            var outputInfo = new FileInfo(outputPath);
            Assert.True(outputInfo.Length > 0);

            // Cleanup
            File.Delete(inputPath);
            File.Delete(outputPath);
        }
    }
}
