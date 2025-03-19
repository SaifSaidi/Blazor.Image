namespace BlazorImage.Helpers
{
    internal class HelpersMethods
    {
        public static int ToAspectRatio(int imageWidth, double aspectWidth, double aspectHeigth)
        {
            if (aspectWidth == 0 || aspectHeigth == 0)
                return 0;

            // Calculate the aspect ratio as a decimal (width / height)
            double aspectRatio = aspectWidth / aspectHeigth;

            int imageHeight = (int)(imageWidth / aspectRatio);

            return imageHeight;
        }


        public static double CalculateReductionPercentage(double originalSize, double newSize)
        {
            return Math.Round((1 - newSize / originalSize) * 100, 2);
        }
    }
}
