using System.ComponentModel.DataAnnotations;

namespace BlazorImage.Extensions
{
    /// <summary>
    /// Configuration options for the Blazor Image Optimization service.
    /// </summary>
    public sealed class BlazorImageConfig
    {
        /// <summary>
        /// Default quality for processed images (15-100). Default Value = 75
        /// </summary> 
        [Range(15, 100, ErrorMessage = "Default Quality must be between 15 and 100.")]
        public int DefaultQuality { get; set; } = Constants.Quality;

        /// <summary>
        /// Default file format for processed images (e.g., "webp", "jpeg"). Default is "webp".
        /// </summary>
        public FileFormat DefaultFileFormat { get; set; } = Constants.Format;

        /// <summary>
        /// Path for storing processed images. Default is "_optimized".
        /// </summary>
        public string Dir { get; set; } = Constants.DefaultDir;
    }
}
