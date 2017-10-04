using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.AccessControl;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;

namespace WpfHelpers.WpfDataManipulation
{
    /// <summary>
    /// Contains methods for data conversion
    /// </summary>
    public static class Convertion
    {
        #region Saves UI Element to file

        public static void SaveScreenToPng(FrameworkElement frameworkElement, Size size, string fileName)
        {
            using (FileStream stream = new FileStream(string.Format("{0}.png", fileName), FileMode.Create))
            {
                SaveScreenToPng(frameworkElement, size, stream);
            }
        }

        public static void SaveScreenToPng(FrameworkElement frameworkElement, Size size, Stream stream)
        {
            Transform transform = frameworkElement.LayoutTransform;
            frameworkElement.LayoutTransform = null;
            Thickness margin = frameworkElement.Margin;
            frameworkElement.Margin = new Thickness(0, 0, margin.Right - margin.Left, margin.Bottom - margin.Top);
            frameworkElement.Measure(size);
            frameworkElement.Arrange(new Rect(size));
            RenderTargetBitmap bmp = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(frameworkElement);
            frameworkElement.LayoutTransform = transform;
            frameworkElement.Margin = margin;
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Interlace = PngInterlaceOption.On;
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            encoder.Save(stream);
        }

        #endregion

        /// <summary>
        /// Converts BitmapSource to byte array
        /// </summary>
        /// <param name="image">Image Source</param>
        /// <returns></returns>
        public static byte[] BitmapSourceToByteArray(BitmapSource image)
        {
            if (image == null)
                return null;
            byte[] imageBuffer;

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image, null, null, null));

            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                imageBuffer = stream.GetBuffer();
            }
            return imageBuffer;
        }
        public static Bitmap ByteArrayToBitmap(byte[] byteArrayIn)
        {
            var ms = new MemoryStream(byteArrayIn);
            var returnImage = (Bitmap)Image.FromStream(ms);
            return returnImage;
        }

        public static BitmapImage ToImagesSourceFormat(Image img)
        {
            if (img == null) return null;
            var bitmapImage = new BitmapImage();

            using (var memory = new MemoryStream())
            {
                img.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }

        public static BitmapImage ToImagesSourceFormat(byte[] arrayBytes)
        {
            return ToImagesSourceFormat(ByteArrayToBitmap(arrayBytes));
        }

        /// <summary>
        /// Extension, converts <see cref="Bitmap"/> to another format
        /// </summary>
        /// <param name="bmpSource"></param>
        /// <param name="format">Format we are looking for</param>
        /// <returns></returns>
        public static Bitmap ConvertTo(this Bitmap bmpSource, System.Drawing.Imaging.PixelFormat format)
        {
            var bmpRet = new Bitmap(bmpSource.Width, bmpSource.Height, format);

            using (Graphics g = Graphics.FromImage(bmpRet))
            {
                g.DrawImage(bmpSource, 0, 0, new System.Drawing.Rectangle(0, 0, bmpSource.Width, bmpSource.Height), GraphicsUnit.Pixel);
                g.Save();
            }

            return bmpRet;
        }

    }
}
