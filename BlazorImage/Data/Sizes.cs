using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorImage.Data
{
    internal static class Sizes
    {

        internal static readonly int[] ConfigSizes = [ 
            480, // xs
            640, // sm
            768, // md
            1024, // lg
            1280, // xl
            1536 //2xl
            ];



        internal static int GetClosestSize(int width, ReadOnlySpan<int> ConfigSizes)
        {


            if (width <= ConfigSizes[0])
            {
                return 0;
            }

             if (width >= ConfigSizes[^1])
            {
                return ConfigSizes.Length - 1;
            }


            // Find the index of the first size that is greater than or equal to the provided width
            for (int i = 0; i < ConfigSizes.Length; i++)
            {
                if (ConfigSizes[i] >= width)
                {
                    return i;
                }
            }

            // Fallback in case of an unexpected input (should not happen with the above checks)
            return ConfigSizes.Length - 1;
        }
    }
}
