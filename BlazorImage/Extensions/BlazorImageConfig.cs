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
        public int DefaultQuality { get; set; } = Constants.DefaultQuality;

        /// <summary>
        /// Default file format for processed images (e.g., "webp", "jpeg"). Default is "webp".
        /// </summary>
        public FileFormat DefaultFileFormat { get; set; } = Constants.DefaultFormat;

        /// <summary>
        /// Path for storing processed images. Default is "_optimized".
        /// </summary>
        public string OutputDir { get; set; } = Constants.DefaultOutputDir;

        /// <summary>
        /// Array of sizes for image configuration.
        /// </summary>
        public int[] Sizes = Data.Sizes.ConfigSizes;

        /// <summary>
        /// Aspect ratio width for images.
        /// </summary>
        public double AspectWidth = Constants.aspectWidth;

        /// <summary>
        /// Aspect ratio height for images.
        /// </summary>
        public double AspectHeigth = Constants.aspectHeigth;

        /// <summary>
        /// Absolute expiration time relative to now for cached images.
        /// Default is 720 hours (30 days).
        /// </summary>
        public TimeSpan AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(720);

        /// <summary>
        /// Sliding expiration time for cached images.
        /// </summary>
        public TimeSpan? SlidingExpiration;
    }
}
