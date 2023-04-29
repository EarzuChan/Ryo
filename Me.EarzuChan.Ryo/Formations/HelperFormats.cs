using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Formations
{
    public class RyoPixmap : IDisposable
    {
        public enum FORMAT
        {
            Alpha,
            Intensity,
            LuminanceAlpha,
            RGB565,
            RGBA4444,
            RGB888,
            RGBA8888
        }

        public int Width;
        public int Height;
        public byte[] Pixels { get; set; }
        public FORMAT Format = FORMAT.RGB888;
        public bool IsJPG;

        public RyoPixmap(int width, int height, FORMAT format)
        {
            Width = width;
            Height = height;
            Format = format;
            Pixels = new byte[width * height * GetPixelSize(format)];
        }

        public RyoPixmap(byte[] buffer)
        {
            Pixels = buffer;
            IsJPG = true;
        }

        public int GetPixel(int x, int y)
        {
            if (IsJPG) throw new NotSupportedException("JPG不支持该操作");

            int pixelSize = GetPixelSize(Format);
            int index = (x + y * Width) * pixelSize;
            int pixel = 0;

            switch (Format)
            {
                case FORMAT.RGBA8888:
                    pixel |= (Pixels[index++] & 0xff) << 24;
                    pixel |= (Pixels[index++] & 0xff) << 16;
                    pixel |= (Pixels[index++] & 0xff) << 8;
                    pixel |= (Pixels[index] & 0xff);
                    break;

                case FORMAT.RGB888:
                    pixel |= (Pixels[index++] & 0xff) << 16;
                    pixel |= (Pixels[index++] & 0xff) << 8;
                    pixel |= (Pixels[index] & 0xff);
                    break;

                case FORMAT.RGB565:
                    int r = (Pixels[index++] & 0xff);
                    int g = (Pixels[index] & 0xff);
                    pixel |= ((r & 0xf8) << 8);
                    pixel |= ((g & 0xfc) << 3);
                    break;

                case FORMAT.Alpha:
                    pixel = Pixels[index] & 0xff;
                    break;
            }

            return pixel;
        }

        public void SetPixel(int x, int y, int pixel)
        {
            if (IsJPG) throw new NotSupportedException("JPG不支持该操作");

            int pixelSize = GetPixelSize(Format);
            int index = (x + y * Width) * pixelSize;

            switch (Format)
            {
                case FORMAT.RGBA8888:
                    Pixels[index++] = (byte)((pixel >> 24) & 0xff);
                    Pixels[index++] = (byte)((pixel >> 16) & 0xff);
                    Pixels[index++] = (byte)((pixel >> 8) & 0xff);
                    Pixels[index] = (byte)(pixel & 0xff);
                    break;

                case FORMAT.RGB888:
                    Pixels[index++] = (byte)((pixel >> 16) & 0xff);
                    Pixels[index++] = (byte)((pixel >> 8) & 0xff);
                    Pixels[index] = (byte)(pixel & 0xff);
                    break;

                case FORMAT.RGB565:
                    Pixels[index++] = (byte)(((pixel >> 8) & 0xf8) | ((pixel >> 13) & 0x7));
                    Pixels[index] = (byte)(((pixel >> 3) & 0xfc) | ((pixel >> 9) & 0x3));
                    break;

                case FORMAT.Alpha:
                    Pixels[index] = (byte)(pixel & 0xff);
                    break;
            }
        }

        public int GetPixelsCount()
        {
            if (IsJPG) return 1919810;//throw new NotSupportedException("JPG不支持该操作");
            else return Pixels.Length / GetPixelSize(Format);
        }

        public static int GetPixelSize(FORMAT format)
        {
            switch (format)
            {
                case FORMAT.RGBA8888:
                    return 4;

                case FORMAT.RGB888:
                    return 3;

                case FORMAT.RGB565:
                case FORMAT.Alpha:
                    return 2;
            }

            return 0;
        }

        public void Dispose() => Pixels = Array.Empty<byte>();

        public static explicit operator Bitmap(RyoPixmap v)
        {
            Bitmap bmp;
            if (v.IsJPG)
            {
                bmp = new(new MemoryStream(v.Pixels));
            }
            else
            {
                var fm = v.Format switch
                {
                    FORMAT.RGB565 => PixelFormat.Format16bppRgb565,
                    FORMAT.RGBA8888 => PixelFormat.Format32bppArgb,
                    _ => PixelFormat.Format24bppRgb,
                };
                bmp = new(v.Width, v.Height, fm);
                BitmapData bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, v.Width, v.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);

                IntPtr ptr = bmpData.Scan0;
                int bytes = Math.Abs(bmpData.Stride) * v.Height;

                var pxs = v.Pixels;

                // 修正位数
                if (fm == PixelFormat.Format32bppArgb)
                {
                    if (pxs.Length % 4 != 0) throw new Exception("RGBA编码有问题呀");

                    var npxs = new byte[pxs.Length];
                    for (int i = 0; i < pxs.Length; i += 4)
                    {
                        npxs[i] = pxs[i + 3];
                        npxs[i + 1] = pxs[i];
                        npxs[i + 2] = pxs[i + 1];
                        npxs[i + 3] = pxs[i + 2];
                    }
                    pxs = npxs;
                }

                Marshal.Copy(v.Pixels, 0, ptr, bytes);
                bmp.UnlockBits(bmpData);
            }
            return bmp;
        }
    }

    public class FragmentalImage
    {
        public int ClipCount;
        public int[] SliceWidths;
        public int[] SliceHeights;
        public RyoPixmap[][] RyoPixmaps;

        public FragmentalImage(int clipCount, int[] sliceWidths, int[] sliceHeights, RyoPixmap[][] pixmaps)
        {
            ClipCount = clipCount;
            SliceWidths = sliceWidths;
            SliceHeights = sliceHeights;
            RyoPixmaps = pixmaps;
        }

        public static explicit operator object[](FragmentalImage image) => new object[] { image.ClipCount, image.SliceWidths, image.SliceHeights, image.RyoPixmaps };

        public Bitmap[][] DumpItems()
        {
            Bitmap[][] bitmaps = new Bitmap[RyoPixmaps.Length][];
            for (int i = 0; i < RyoPixmaps.Length; i++)
            {
                RyoPixmap[] items = RyoPixmaps[i];
                var bmpg = new Bitmap[items.Length];
                for (int j = 0; j < items.Length; j++)
                {
                    bmpg[j] = (Bitmap)items[j];
                }
            }
            return bitmaps;
        }
    }
}
