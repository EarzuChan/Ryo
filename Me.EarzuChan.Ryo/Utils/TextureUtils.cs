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
    public class TextureUtils
    {
        public static TextureUtils INSTANCE { get { instance ??= new(); return instance; } }
        private static TextureUtils? instance;

        public FragmentalImage FastCreateImg(Stream fileStream)
        {
            //if (!File.Exists(path)) throw new FileNotFoundException("没图说个几把");

            //using FileStream fileStream = new(path, FileMode.Open);
            if (!fileStream.CanRead) throw new UnauthorizedAccessException("Acceed Denied");

            var sourceImage = Image.Load(fileStream);

            int maxWidthOrHeight = 1024;
            int width = sourceImage.Width;
            int height = sourceImage.Height;

            if (width > maxWidthOrHeight || height > maxWidthOrHeight)
            {
                int newWidth, newHeight;
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
            }

            var targetImage = new FragmentalImage(1, new int[] { 1024 }, new int[] { 1024 }, new RyoPixmap[1][]);

            targetImage.RyoPixmaps[0] = new RyoPixmap[] { new RyoPixmap(sourceImage) };

            return targetImage;
        }
    }
}
