using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTester
{
    public static class ImageTools
    {
        public static byte[] Transform(this byte[] input, int width, int height, int colcount, int rowcount)
        {
            var result = new byte[input.Length];
            var sz = width * height;
            for (int row = 0; row < rowcount; row++)
            {
                int gdestidx = sz * (row * colcount);
                for (int col = 0; col < colcount; col++)
                {

                    int gsrcidx = sz * (col + (row * colcount));

                    for (int srow = 0; srow < height; srow++)
                    {
                        int srcidx = gsrcidx + width * srow;
                        int destidx = gdestidx + width * (col + srow * colcount);
                        Array.Copy(input, srcidx, result, destidx, width);
                    }

                }

            }
            return result;
        }


        public static void ArrayToBitmap(this byte[] input, string filename, int width, int heigtht, PixelFormat pixelformat)
        {
            var bmp = new Bitmap(width, heigtht, pixelformat);
            var rect = new Rectangle(0, 0, width, heigtht);


            if (pixelformat == PixelFormat.Format8bppIndexed)
            {
                var palette = bmp.Palette;
                var entries = palette.Entries;
                for (int i = 0; i < 256; i++)
                {
                    entries[i] = Color.FromArgb(i, i, i);
                }
                bmp.Palette = palette;
            }
            var bitmapdata = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, pixelformat);
            try
            {
                IntPtr ptr = bitmapdata.Scan0;
                var stride = Math.Abs(bitmapdata.Stride);
                var len = stride * bitmapdata.Height;
                var rgbValues = new byte[len];

                for (int line = 0; line < heigtht; line++)
                {
                    Array.Copy(input, line * width, rgbValues, stride * line, width);
                }
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, len);

            }
            finally
            {
                bmp.UnlockBits(bitmapdata);
            }

            bmp.Save(filename, ImageFormat.Bmp);

        }
    }
}
