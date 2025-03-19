using System.Collections.Concurrent;

namespace BlazorImage.Data;

internal class DictionaryCacheDataService2
{
    public ConcurrentDictionary<CacheKey,
          (string source, string fallback, string placeholder)> SourceInfoCache
  = [];

    public void ClearData()
    {
        SourceInfoCache.Clear();
    }

    public struct CacheKey : IEquatable<CacheKey>
    {
        public string SanitizedName;
        public int Quality;
        public FileFormat Format;
        public int WidthFlag; // -1 for null, actual value otherwise

        public bool Equals(CacheKey other) =>
            SanitizedName == other.SanitizedName &&
            Quality == other.Quality &&
            Format == other.Format &&
            WidthFlag == other.WidthFlag;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = SanitizedName.GetHashCode();
                hash = (hash * 397) ^ Quality;
                hash = (hash * 397) ^ (int)Format;
                hash = (hash * 397) ^ WidthFlag;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"SanitizedName: {SanitizedName}, Quality: {Quality}, Format: {Format}, WidthFlag: {WidthFlag}";
        }
    }



}
