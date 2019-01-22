using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace OCRFunctionApp.Helpers
{
    public static class ImageHelpers
    {

        /// <summary>
        /// Resize an image to fit within the desired boundaries.
        /// Code based on <a href="https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp">How to resize an Image C#</a>
        /// </summary>
        /// <param name="image">Image to resize</param>
        /// <param name="maxWidth">Max width for the image</param>
        /// <param name="maxHeight">Max height for the image</param>
        /// <returns></returns>
        public static Image Fit(this Image image, int maxWidth, int maxHeight)
        {
            // If it already fits within the boundaries, do nothing
            if (image.Width < maxWidth && image.Height < maxHeight)
            {
                return image;
            }

            (int width, int height) = Fit((image.Width, image.Height), (maxWidth, maxHeight));
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        // Scale a rectangle to fit into another
        private static (int newWidth, int newHeight) Fit((int width, int height) original, (int width, int height) boundaries)
        {
            double scaleRatio =
                original.width / (double)original.height > boundaries.width / (double)boundaries.height
                ? boundaries.width / (double)original.width
                : boundaries.height / (double)original.height;
            int newWidth = (int)(original.width * scaleRatio);
            int newHeight = (int)(original.height * scaleRatio);

            return (newWidth, newHeight);
        }

    }
}
