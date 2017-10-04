using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WpfHelpers.Drawing
{
    /// <summary>
    /// Contains functions for drawing primitives
    /// </summary>
    public class WriteableBitmap
    {
        /// <summary>
        /// Draws simple rect. Note! First lock wbitmap, and unlock after using this f-tion
        /// </summary>
        /// <param name="writeableBitmap">source wbitmap</param>
        /// <param name="left">X</param>
        /// <param name="top">Y</param>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <param name="color">Color of rect</param>
        public static void DrawRectangle(System.Windows.Media.Imaging.WriteableBitmap writeableBitmap, int left, int top, int width, int height, Color color)
        {
            // Compute the pixel's color
            int colorData = color.R << 16; // R
            colorData |= color.G << 8; // G
            colorData |= color.B << 0; // B
            int bpp = writeableBitmap.Format.BitsPerPixel / 8;

            unsafe
            {
                for (int y = 0; y < height; y++)
                {
                    // Get a pointer to the back buffer
                    var pBackBuffer = (int)writeableBitmap.BackBuffer;

                    // Find the address of the pixel to draw
                    pBackBuffer += (top + y) * writeableBitmap.BackBufferStride;
                    pBackBuffer += left * bpp;
                    int step;

                    if (y == 0 || y == height - 1)
                        step = 1;
                    else
                        step = width - 1;

                    for (int x = 0; x < width; x += step)
                    {
                        // Assign the color data to the pixel
                        *((int*)pBackBuffer) = colorData;

                        // Increment the address of the pixel to draw
                        pBackBuffer += bpp * step;
                    }
                }
            }

            try
            {
                writeableBitmap.AddDirtyRect(new Int32Rect(left, top, width, height));
            }
            catch (Exception e)
            {
            }
        }


        /// <summary>
        /// Draws simple rect with checking bounds. Note! First lock wbitmap, and unlock after using this f-tion
        /// </summary>
        /// <param name="wBitmap">source wbitmap</param>
        /// <param name="frameWidth">Width of image</param>
        /// <param name="frameHeight">Height of image</param>
        /// <param name="x">center of object</param>
        /// <param name="y">center of object</param>
        /// <param name="width">Width of object in px</param>
        /// <param name="height">Height of object in px</param>
        /// <param name="color">Color of rect</param>
        /// <param name="thickness">thickness of bounds</param>
        public static void SafeDrawRectangle(System.Windows.Media.Imaging.WriteableBitmap wBitmap, int frameWidth, int frameHeight, int x, int y, int width, int height, Color color, int thickness = 1)
        {
            if (thickness < 1)
                throw new ArgumentOutOfRangeException("thickness", @"Thickness must be positive and non zero");
            else if (thickness != 1)
            {
                SafeDrawRectangle(wBitmap, frameWidth, frameHeight, x, y, width - 2, height - 2, color, --thickness);
            }

            if (height <= 0 || width <= 0)
            {
                return;
            }

            double ix = x - width / 2;
            double iy = y - height / 2;
            double iwidth = width;
            double iheight = height;

            if (ix >= frameWidth || iy >= frameHeight)
            {
                return;
            }

            if (ix <= 0)
            {
                iwidth = iwidth + ix;
                ix = 0;
            }

            if (ix + iwidth >= frameWidth - 1)
            {
                iwidth = frameWidth - ix;
            }

            if (iy <= 0)
            {
                iheight = iheight + iy;
                iy = 0;
            }

            if (iy + iheight >= frameHeight - 1)
            {
                iheight = frameHeight - iy;

            }

            if (iwidth <= 0)
                return;

            if (iheight <= 0)
                return;

            DrawRectangle(wBitmap, (int)ix, (int)iy, (int)(iwidth), (int)(iheight), color);
        }
    }
}
