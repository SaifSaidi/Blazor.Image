using System.Runtime.CompilerServices;

namespace BlazorImage.Extensions
{

    /// <summary>
    /// Represents the different image file formats supported by the image processing service.
    /// </summary>
    public enum FileFormat
    {
        /// <summary>
        /// WebP format, offering both lossy and lossless compression with high quality and small file sizes.
        /// Recommended for web images due to its superior compression and quality compared to JPEG and PNG in many cases.
        /// </summary>
        webp,

        /// <summary>
        /// JPEG/JPG format, widely used for lossy compression with a good balance of quality and file size.
        /// Best suited for photographs and images where slight quality loss is acceptable for smaller file sizes.
        /// </summary>
        jpeg,

        /// <summary>
        /// PNG format, supports lossless compression and is commonly used for images requiring transparency.
        /// Ideal for images with sharp lines, text, logos, and when preserving image detail without loss is important.
        /// </summary>
        png,

        /// <summary>
        /// AVIF format, a modern image format that offers high compression efficiency and quality, supporting both lossy and lossless compression.
        ///  A good alternative to WebP, offering potentially even better compression, and supports features like animation and HDR.
        /// </summary>
        avif


    }
    /// <summary>
    /// Extension methods for the FileFormat enum.
    /// </summary>
    internal static class FileFormatExtensions
    {
        /// <summary>
        /// Converts the ImageSize enum value to its corresponding string representation.
        /// </summary>
        /// <param name="format">The FileFormat enum value.</param>
        /// <returns>The string representation of the FileFormat enum value.</returns>
        public static string ToMimeType(this FileFormat format)
        {
            return format switch
            {
                FileFormat.webp => "image/webp",
                FileFormat.jpeg => "image/jpeg",
                FileFormat.png => "image/png",
                FileFormat.avif => "image/avif",
                _ => "image/webp"
            };
        }
       
        public static readonly string[] FormatStrings = ["webp", "jpeg", "png", "avif",];


        public static string ToFileExtension(this FileFormat format) => FormatStrings[(int)format];

    }
    
}
