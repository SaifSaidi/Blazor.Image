

namespace BlazorImage.Extensions
{

   
    /// <summary>
    /// Enum representing different image sizes.
    /// </summary>
    internal enum ImageSize
    {
        _480, // Small Mobile (common mobile width)
        _768, // Tablet Portrait (common tablet breakpoint)
        _1024,// Tablet Landscape / Small Desktop 
        _1280,// Standard Desktop
        _1600,// Large Desktop / High-Resolution Laptops
        _1920,// Full HD Desktop (common maximum width for many websites)
        _2560, // QHD/2K Desktop (for higher resolution displays) - Optional, for future-proofing and very high-res screens


    }


    /// <summary>
    /// Extension methods for the ImageSize enum.
    /// </summary>
    internal static class ImageSizeExtensions
    {
        /// <summary>
        /// Converts the ImageSize enum value to standrand size.
        /// </summary>
        /// <param name="size">The ImageSize enum value.</param>
        /// <returns>The int representation of the ImageSize value.</returns>
        internal static int Width(this ImageSize size)
        {
            return size switch
            {
                ImageSize._480 => 480,
                ImageSize._768 => 768,
                ImageSize._1024 => 1024,
                ImageSize._1280 => 1280,
                ImageSize._1600 => 1600,
                ImageSize._1920 => 1920,
                ImageSize._2560 => 2560,
                _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
            };
        }

        /// <summary>
        /// Converts the ImageSize enum value to standrand size.
        /// </summary>
        /// <param name="size">The ImageSize enum value.</param>
        /// <returns>The int representation of the ImageSize value.</returns>
        internal static int Height(this ImageSize size)
        {
            return size switch
            {
                ImageSize._480 => HelpersMethods.ToAspectRatio(480, 4.0, 3.0),
                ImageSize._768 => HelpersMethods.ToAspectRatio(768, 4.0, 3.0),
                ImageSize._1024 => HelpersMethods.ToAspectRatio(1024, 4.0, 3.0),
                ImageSize._1280 => HelpersMethods.ToAspectRatio(1280, 4.0, 3.0),
                ImageSize._1600 => HelpersMethods.ToAspectRatio(1600, 4.0, 3.0),
                ImageSize._1920 => HelpersMethods.ToAspectRatio(1920, 4.0, 3.0),
                ImageSize._2560 => HelpersMethods.ToAspectRatio(2560, 4.0, 3.0),
                _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
            };
        }
    }
}
