using System.Collections.Concurrent;

namespace BlazorImage.Data;

internal class DictionaryCacheDataService
{
    public ConcurrentDictionary<(string sanitizedName, int quality, FileFormat format, int? Width),
          (string source, string fallback, string placeholder)> SourceInfoCache
  = [];

    public void ClearData()
    {
        SourceInfoCache.Clear();
    }




}
