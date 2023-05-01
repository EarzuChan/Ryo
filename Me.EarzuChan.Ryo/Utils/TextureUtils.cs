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
        public static FragmentalImage FastCreateImg(Stream fileStream)
        {
            //if (!File.Exists(path)) throw new FileNotFoundException("没图说个几把");

            //using FileStream fileStream = new(path, FileMode.Open);
            if (!fileStream.CanRead) throw new UnauthorizedAccessException("Acceed Denied");

            try
            {
                var sourceImage = Image.Load(fileStream) ?? throw new NullReferenceException("加载图片失败");

                int maxWidthOrHeight = 512;

                int width = sourceImage.Width;
                int height = sourceImage.Height;
                int newWidth = width, newHeight = height;

                if (width > maxWidthOrHeight || height > maxWidthOrHeight)
                {
                    if (width > height)
                    {
                        newWidth = maxWidthOrHeight;
                        newHeight = (int)(height * ((float)maxWidthOrHeight / width));
                    }
                    else
                    {
                        newHeight = maxWidthOrHeight;
                        newWidth = (int)(width * ((float)maxWidthOrHeight / height));
                    }

                    sourceImage.Mutate(x => x.Resize(newWidth, newHeight));
                    // LogUtil.INSTANCE.PrintInfo("新大小：" + newHeight, newWidth.ToString());
                }

                var targetImage = new FragmentalImage(maxWidthOrHeight, new int[] { newWidth }, new int[] { newHeight }, new RyoPixmap[1][]);

                targetImage.RyoPixmaps[0] = new RyoPixmap[] { new RyoPixmap(sourceImage) };

                return targetImage;
            }
            catch (Exception ex)
            {
                throw new Exception("快速创建预制图片对象时出错，因为" + ex.Message, ex);
            }
        }
    }
}
