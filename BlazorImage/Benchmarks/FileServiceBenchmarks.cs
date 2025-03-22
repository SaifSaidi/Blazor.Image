//using System;
//using System.Collections.Generic;
//using System.IO.Abstractions.TestingHelpers;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using BenchmarkDotNet.Attributes;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.Extensions.Logging;
//using Moq;

//namespace BlazorImage.Benchmarks
//{ 
     
//     */


//    [ShortRunJob]

//    public class FileServiceBenchmarks
//    {
//        private IFileService _fileService;
//        private string _testFilePath;
//        private string _testDirectoryPath;

//        [GlobalSetup]
//        public void Setup()
//        {
//            // Mock IWebHostEnvironment
//            var mockEnv = new Mock<IWebHostEnvironment>();
//            mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath()); // Use temp directory for testing

//            // Mock ILogger
//            var mockLogger = new Mock<ILogger<FileService>>();

//            // Initialize FileService
//            _fileService = new FileService(mockEnv.Object, mockLogger.Object);


//            var mockFileSystem = new MockFileSystem();
//            mockFileSystem.AddFile("testfile.txt", new MockFileData("This is a test file."));


//            var fileService = new FileService(mockEnv.Object, mockLogger.Object);

//            // Create a test file and directory
//            _testFilePath = Path.Combine(mockEnv.Object.WebRootPath, "testfile.txt");
//            _testDirectoryPath = Path.Combine(mockEnv.Object.WebRootPath, "testdir");

//            if (!File.Exists(_testFilePath))
//            {
//                File.WriteAllText(_testFilePath, "This is a test file.");
//            }

//            if (!Directory.Exists(_testDirectoryPath))
//            {
//                Directory.CreateDirectory(_testDirectoryPath);
//            }
//        }

//        [GlobalCleanup]
//        public void Cleanup()
//        {
//            // Clean up test file and directory
//            if (File.Exists(_testFilePath))
//            {
//                File.Delete(_testFilePath);
//            }

//            if (Directory.Exists(_testDirectoryPath))
//            {
//                Directory.Delete(_testDirectoryPath);
//            }
//        }

//        //[Benchmark]
//        //public string Benchmark_GetRootPath()
//        //{
//        //    return _fileService.GetRootPath("testfile.txt");
//        //}

//        //[Benchmark]
//        //public void Benchmark_EnsureDirectoriesExist()
//        //{
//        //    _fileService.EnsureDirectoriesExist("testdir1", "testdir2");
//        //}

//        //[Benchmark]
//        //public bool Benchmark_FileExistsInRootPath()
//        //{
//        //    return _fileService.FileExistsInRootPath("testfile.txt");
//        //}

//        //[Benchmark]
//        //public void Benchmark_CreateDirectoryForFile()
//        //{
//        //    _fileService.CreateDirectoryForFile("testdir2/testfile.txt");
//        //}

//        //[Benchmark]
//        //public string Benchmark_SanitizeFileName()
//        //{
//        //    return _fileService.SanitizeFileName("invalid/file:name.txt");
//        //}

//        //[Benchmark]
//        //public double Benchmark_GetFileSizeInKb()
//        //{
//        //    return _fileService.GetFileSizeInKb("testfile.txt");
//        //}

//    }

//}
