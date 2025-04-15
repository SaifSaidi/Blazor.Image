
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
 
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace BlazorImage.Services;
internal sealed class FileService : IFileService
{
    private readonly string _webRootPath;
    private readonly ILogger<FileService> _logger;
     private static readonly HashSet<char> InvalidFileNameCharsSet = [.. Path.GetInvalidFileNameChars()];

    public FileService(IWebHostEnvironment env, ILogger<FileService> logger)
    {
        _webRootPath = env.WebRootPath;
        _logger = logger;
    }

    public string GetRootPath(string webRootRelativePath) => Path.Combine(_webRootPath, webRootRelativePath.TrimStart('/'));

    public void EnsureDirectoriesExist(params string[] relativePaths)
    {
        foreach (var relativePath in relativePaths)
        {
            var fullPath = GetRootPath(relativePath);
            try
            {
                if (!Directory.Exists(fullPath))
                {
                    _logger.LogInformation("Creating directory: {Directory}", fullPath);
                    Directory.CreateDirectory(fullPath);
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error creating directory {Directory}", fullPath);
                throw;
            }
        }
    }

    public bool FileExistsInRootPath(string fileName)
    {
        var fullPath = GetRootPath(fileName);
         return File.Exists(fullPath);
    }

    public void CreateDirectoryForFile(string fileName)
    {
        var fullPath = GetRootPath(fileName);
        var directory = Path.GetDirectoryName(fullPath);


        if (string.IsNullOrWhiteSpace(directory))
            return;

        if (Directory.Exists(directory))
            return;
            
        Directory.CreateDirectory(directory); 
    }

    public double GetFileSizeInKb(string relativePath)
    {
        var fullPath = GetRootPath(relativePath);
        try
        {
            var fileInfo = new FileInfo(fullPath);
            return Math.Round(fileInfo.Length / 1024.0, 2);
        }
        catch (FileNotFoundException)
        {
            return 0.0;
        }
    }
    public string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "";

       
        var sanitized = new StringBuilder(fileName.Length);

        foreach (var ch in Path.GetFileNameWithoutExtension(fileName))
            sanitized.Append(InvalidFileNameCharsSet.Contains(ch) ? '_' : ch);
        

        return sanitized.ToString();
    }
}

