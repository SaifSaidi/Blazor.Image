namespace BlazorImage.Models.Interfaces;

internal interface IFileService
{
    void EnsureDirectoriesExist(params string[] relativePaths);
    bool FileExistsInRootPath(string fileName);
    void CreateDirectoryForFile(string fileName);
    string SanitizeFileName(string fileName);
    double GetFileSizeInKb(string relativePath);
    string GetRootPath(string webRootRelativePath); 
}
