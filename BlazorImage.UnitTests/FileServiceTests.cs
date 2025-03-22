namespace BlazorImage.UnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using BlazorImage.Data;
    using BlazorImage.Services;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;
    public class FileServiceTests
    {

        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<ILogger<FileService>> _mockLogger;
        private readonly FileService _fileService;

        public FileServiceTests()
        {
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockLogger = new Mock<ILogger<FileService>>();

            // Set up the WebRootPath to a temporary directory
            _mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

            _fileService = new FileService(_mockEnv.Object, _mockLogger.Object);
        }

        [Fact]
        public void GetRootPath_ShouldReturnCorrectPath()
        {
            // Arrange
            var relativePath = "test/file.txt";

            // Act
            var result = _fileService.GetRootPath(relativePath);

            // Assert
            var expectedPath = Path.Combine(_mockEnv.Object.WebRootPath, relativePath.TrimStart('/'));
            Assert.Equal(expectedPath, result);
        }

        [Fact]
        public void EnsureDirectoriesExist_ShouldCreateDirectoryIfNotExists()
        {
            // Arrange
            var relativePath = "test/directory";
            var fullPath = Path.Combine(_mockEnv.Object.WebRootPath, relativePath);

            // Act
            _fileService.EnsureDirectoriesExist(relativePath);

            // Assert
            Assert.True(Directory.Exists(fullPath));

            // Cleanup
            Directory.Delete(fullPath, recursive: true);
        }

        [Fact]
        public void EnsureDirectoriesExist_ShouldLogErrorIfCreationFails()
        {
            // Arrange
            var relativePath = "invalid/path/<>"; // Invalid characters to force an exception
            var fullPath = Path.Combine(_mockEnv.Object.WebRootPath, relativePath);

            // Act & Assert
            Assert.Throws<IOException>(() => _fileService.EnsureDirectoriesExist(relativePath));

            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error creating directory")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public void FileExistsInRootPath_ShouldReturnTrueIfFileExists()
        {
            // Arrange
            var fileName = "testfile.txt";
            var fullPath = Path.Combine(_mockEnv.Object.WebRootPath, fileName);
            File.WriteAllText(fullPath, "Test content");

            // Act
            var result = _fileService.FileExistsInRootPath(fileName);

            // Assert
            Assert.True(result);

            // Cleanup
            File.Delete(fullPath);
        }

        [Fact]
        public void FileExistsInRootPath_ShouldReturnFalseIfFileDoesNotExist()
        {
            // Arrange
            var fileName = "nonexistentfile.txt";

            // Act
            var result = _fileService.FileExistsInRootPath(fileName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CreateDirectoryForFile_ShouldCreateDirectory()
        {
            // Arrange
            var fileName = "test/subdir/file.txt";
            var fullPath = Path.Combine(_mockEnv.Object.WebRootPath, fileName);
            var directory = Path.GetDirectoryName(fullPath);

            // Act
            _fileService.CreateDirectoryForFile(fileName);

            // Assert
            Assert.True(Directory.Exists(directory));

            // Cleanup
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public void GetFileSizeInKb_ShouldReturnCorrectSize()
        {
            // Arrange
            var fileName = "testfile.txt";
            var fullPath = Path.Combine(_mockEnv.Object.WebRootPath, fileName);
            File.WriteAllText(fullPath, "This is a test file.");

            // Act
            var result = _fileService.GetFileSizeInKb(fileName);

            // Assert
            var fileInfo = new FileInfo(fullPath);
            var expectedSize = Math.Round(fileInfo.Length / 1024.0, 2);
            Assert.Equal(expectedSize, result);

            // Cleanup
            File.Delete(fullPath);
        }

        [Fact]
        public void GetFileSizeInKb_ShouldReturnZeroIfFileNotFound()
        {
            // Arrange
            var fileName = "nonexistentfile.txt";

            // Act
            var result = _fileService.GetFileSizeInKb(fileName);

            // Assert
            Assert.Equal(0.0, result);
        }

        [Theory]
        [InlineData("invalid/file:name.txt", "file_name")]
        [InlineData("validfilename.txt", "validfilename")]
        [InlineData("", "")]
        public void SanitizeFileName_ShouldSanitizeCorrectly(string input, string expectedOutput)
        {
            // Act
            var result = _fileService.SanitizeFileName(input);

            // Assert
            Assert.Equal(expectedOutput, result);
        } 
    }
}
