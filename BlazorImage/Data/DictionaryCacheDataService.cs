using System.Collections.Concurrent;

namespace BlazorImage.Data;

internal class DictionaryCacheDataService
{
    public ConcurrentDictionary<CacheKey,
          (string source, string fallback, string placeholder)> SourceInfoCache
  = [];

    public void ClearData()
    {
        SourceInfoCache.Clear();
    }

    public record struct CacheKey 
        : IEquatable<CacheKey>
    {
        public string SanitizedName;
        public int Quality;
        public FileFormat Format;
        public int Width; 

        public readonly bool Equals(CacheKey other) =>
            SanitizedName == other.SanitizedName &&
            Quality == other.Quality &&
            Format == other.Format &&
            Width == other.Width;

        public override readonly int GetHashCode() => HashCode.Combine(SanitizedName, Quality, Format, Width);
 
    }



}
