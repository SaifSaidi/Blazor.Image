namespace BlazorImage.Data;

internal static class Constants
{
    public const int Quality = 75; 
    public const int PlaceholderWidth = 80;
    public const int PlaceholderQuality = 75;
    public const double aspectWidth = 4.0;
    public const double aspectHeigth = 3.0;
    public const string LiteDbName = "images.db";
    public const string LiteDbCollection = "images";
    public const FileFormat Format = FileFormat.webp;
    public const string DefaultDir = "_optimized";
 

    internal static readonly int[] ConfigSizes = [
            480,  // Small Mobile (common mobile width)
            768,  // Tablet Portrait (common tablet breakpoint)
            1024, // Tablet Landscape / Small Desktop 
            1280, // Standard Desktop
            1600, // Large Desktop / High-Resolution Laptops
            1920, // Full HD Desktop (common maximum width for many websites)
            2560  // QHD/2K Desktop (for higher resolution displays) - Optional, for future-proofing and very high-res screens
    ];
 
    internal static int GetClosestSize(int width)
    {
        // Direct comparisons with the specific values
        if (width >= ConfigSizes[6]) return 7;
        if (width >= ConfigSizes[5]) return 6;
        if (width >= ConfigSizes[4]) return 5;
        if (width >= ConfigSizes[3]) return 4;
        if (width >= ConfigSizes[2]) return 3;
        if (width >= ConfigSizes[1]) return 2;
        if (width >= ConfigSizes[0]) return 1;
        return 0; // If width is less than 480
    }


    //internal static int GetClosestSizeV2(int width)
    //{
    //    // Check if width exceeds the largest size, return the largest size index
    //    if (width >= ConfigSizes[^1])
    //    {
    //        return ConfigSizes.Length - 1;
    //    }

    //    // Find the index of the first size that is greater than or equal to the provided width
    //    for (int i = 0; i < ConfigSizes.Length; i++)
    //    {
    //        if (ConfigSizes[i] >= width)
    //        {
    //            return i;
    //        }
    //    }

    //    // Fallback in case of an unexpected input (should not happen with the above checks)
    //    return ConfigSizes.Length - 1;
    //}


}
