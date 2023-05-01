using Me.EarzuChan.Ryo.Formations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class TextureUtils
    {
        public const int MaxWidthOrHeight = 512;

        public static FragmentalImage FastCreateImg(Stream fileStream)
        {

            if (!fileStream.CanRead) throw new UnauthorizedAccessException("Acceed Denied");

            try
            {
                var image = Image.Load(fileStream) ?? throw new NullReferenceException("加载图片失败");

                int width = image.Width;
                int height = image.Height;
                int newWidth = width, newHeight = height;

                if (width > MaxWidthOrHeight || height > MaxWidthOrHeight)
                {
                    if (width > height)
                    {
                        newWidth = MaxWidthOrHeight;
                        newHeight = (int)(height * ((float)MaxWidthOrHeight / width));
                    }
                    else
                    {
                        newHeight = MaxWidthOrHeight;
                        newWidth = (int)(width * ((float)MaxWidthOrHeight / height));
                    }

                    image.Mutate(x => x.Resize(newWidth, newHeight));
                }

                List<int> widths = new();
                List<int> heights = new();
                List<RyoPixmap[]> pixmaps = new();
                while (newHeight > 1 && newWidth > 1)
                {
                    widths.Add(newWidth);
                    heights.Add(newHeight);
                    pixmaps.Add(new RyoPixmap[] { new RyoPixmap(image) });

                    newHeight /= 2;
                    newWidth /= 2;
                    image.Mutate(size => size.Resize(newWidth, newHeight));
                }

                return new FragmentalImage(MaxWidthOrHeight, widths.ToArray(), heights.ToArray(), pixmaps.ToArray());
            }
            catch (Exception ex)
            {
                throw new Exception("快速创建预制图片对象时出错，因为" + ex.Message, ex);
            }
        }
    }
}
