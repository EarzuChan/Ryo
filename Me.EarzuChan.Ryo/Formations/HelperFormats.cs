using Me.EarzuChan.Ryo.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
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
        public bool IsJPG = false;

        public RyoPixmap(int width, int height, FORMAT format)
        {
            Width = width;
            Height = height;
            Format = format;
            Pixels = new byte[width * height * GetPixelSize(format)];
        }

        public RyoPixmap(Image image)
        {
            Width = image.Width;
            Height = image.Height;

            // 32->RGBA8888 24->RGB888->JPG
            // LogUtil.INSTANCE.PrintInfo("BPP：" + image.PixelType.BitsPerPixel, "格式：" + image.PixelType);

            // Needly Overdose
            switch (image.PixelType.BitsPerPixel)
            {
                case 24:
                    {
                        IsJPG = true;
                        Format = FORMAT.RGB888;

                        // 因为是JPG，直接压缩即可
                        using MemoryStream memoryStream = new();
                        image.Save(memoryStream, new JpegEncoder());
                        Pixels = memoryStream.ToArray();
                        // File.WriteAllBytes("D:\\D Images\\Play My Genshin.jpg", Pixels);
                        break;
                    }
                case 32:
                    Format = FORMAT.RGBA8888;

                    var imgBuffer = ((ImageFrame<Rgba32>)image.Frames.RootFrame).PixelBuffer;
                    var bytes = new List<byte>();
                    for (int i = 0; i < Height; i++)
                    {
                        var span = imgBuffer.DangerousGetRowSpan(i);
                        foreach (Rgba32 px in span)
                        {
                            bytes.Add(px.R);
                            bytes.Add(px.G);
                            bytes.Add(px.B);
                            bytes.Add(px.A);
                        }
                    }
                    // LogUtil.INSTANCE.PrintInfo($"指望的BytesCount：{Height * Width * 4}，实际：{bytes.Count}");

                    Pixels = bytes.ToArray();
                    break;
                default:
                    throw new NotSupportedException("这这格式不能：" + image.PixelType.BitsPerPixel);
            }
        }

        public RyoPixmap(byte[] buffer)
        {
            Pixels = buffer;
            IsJPG = true;
        }

        /*public int GetPixel(int x, int y)
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
        }*/

        /*public int GetPixelsCount()
        {
            if (IsJPG) return 1919810;//throw new NotSupportedException("JPG不支持该操作");
            else return Pixels.Length / GetPixelSize(Format);
        }*/

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

        /*public static explicit operator Bitmap(RyoPixmap v)
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
        }*/

        public Image ToImage()
        {
            // LogUtil.INSTANCE.PrintInfo("像素大小：" + Pixels.Length);
            if (IsJPG)
            {
                // File.WriteAllBytes("D:\\D Images\\Play Testing Genshin.jpg", Pixels);
                return Image.Load(Pixels);
            }
            else return Format switch
            {
                FORMAT.RGBA8888 => Image.LoadPixelData<Rgba32>(Pixels, Width, Height),
                FORMAT.RGB565 => throw new NotSupportedException("暂不支持RGB565，需要作者颐指气使寻找解决方案"),// return Image.LoadPixelData<Rgb>(Pixels,)
                FORMAT.RGB888 => Image.LoadPixelData<Rgb24>(Pixels, Width, Height),
                _ => throw new NotSupportedException("肥肠爆芡，暂不支持" + Format),
            };
        }
    }

    public class FragmentalImage
    {
        public int MaxClipSize;
        public int[] SliceWidths;
        public int[] SliceHeights;
        public RyoPixmap[][] RyoPixmaps;

        public FragmentalImage(int clipCount, int[] sliceWidths, int[] sliceHeights, RyoPixmap[][] pixmaps)
        {
            MaxClipSize = clipCount;
            SliceWidths = sliceWidths;
            SliceHeights = sliceHeights;
            RyoPixmaps = pixmaps;
        }

        public static explicit operator object[](FragmentalImage image) => new object[] { image.MaxClipSize, image.SliceWidths, image.SliceHeights, image.RyoPixmaps };

        public Image[][] DumpItems()
        {
            Image[][] bitmaps = new Image[RyoPixmaps.Length][];
            for (int i = 0; i < RyoPixmaps.Length; i++)
            {
                RyoPixmap[] items = RyoPixmaps[i];
                var bmpg = new Image[items.Length];
                for (int j = 0; j < items.Length; j++)
                {
                    bmpg[j] = items[j].ToImage();
                }
            }
            return bitmaps;
        }
    }
}
