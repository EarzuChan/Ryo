using Me.EarzuChan.Ryo.Core.Formations.HelperFormations;
using Me.EarzuChan.Ryo.Utils;

namespace Me.EarzuChan.Ryo.Core.Utils
{
    public static class TextureUtils
    {
        public const int MaxWidthOrHeight = 512;

        public static FragmentalImage FastCreateImage(Stream fileStream) =>
        ControlFlowUtils.TryCatchingThenThrow("Failed to create a fragmental image", () =>
        {
            var image = Image.Load(fileStream) ?? throw new NullReferenceException("Cannot load image texture file");

            return image.ToFragmentalImage(512);
        });

        // 每次缩小一倍
        public static Image[] Mipmap(this Image image)
        {
            List<Image> images = new();
            int newHeight = image.Height;
            int newWidth = image.Width;

            while (newHeight > 0 || newWidth > 0)
            {
                if (newHeight == 0) newHeight = 1;
                else if (newWidth == 0) newWidth = 1;

                // LogUtils.INSTANCE.PrintInfo($"宽：{image.Width} 高：{image.Height}");
                images.Add(image);  // 添加到Mipmap序列中

                newHeight /= 2;
                newWidth /= 2;
                if (newHeight == newWidth && newHeight == 0) break;
                image = image.Clone(size => size.Resize(newWidth, newHeight));  // 克隆原始图像
            }

            return images.ToArray();
        }

        // 每片裁切成最大ClipSize的方片
        public static Image[] Clip(this Image sourceImage, int clipSize)
        {
            // 计算大图可以切割出多少个小图
            int clipCountX = CalculateClipCount(clipSize, sourceImage.Width);
            int clipCountY = CalculateClipCount(clipSize, sourceImage.Height);

            List<Image> result = new();

            // 保护画质
            if (sourceImage.Height <= clipSize && sourceImage.Width <= clipSize) return new Image[] { sourceImage };

            // 循环遍历大图中的所有小图
            for (int y = 0; y < clipCountY; y++)
            {
                for (int x = 0; x < clipCountX; x++)
                {
                    // 裁剪出一个小图
                    int startX = x * clipSize;
                    int startY = y * clipSize;
                    int endX = Math.Min(startX + clipSize, sourceImage.Width);
                    int endY = Math.Min(startY + clipSize, sourceImage.Height);
                    int width = endX - startX;
                    int height = endY - startY;
                    Rectangle cropRectangle = new(startX, startY, width, height);

                    Image clippedImage = sourceImage.Clone(ctx => ctx.Crop(cropRectangle));

                    result.Add(clippedImage);
                }
            }

            return result.ToArray();
        }

        public static FragmentalImage ToFragmentalImage(this Image image, int clipSize)
        {
            List<int> levelWidths = new();
            List<int> levelHeights = new();

            Image[] mipmapped = image.Mipmap();
            List<RyoPixmap[]> clipped = new();
            foreach (var item in mipmapped)
            {
                levelWidths.Add(item.Width);
                levelHeights.Add(item.Height);
                clipped.Add(item.Clip(clipSize).Select(a => new RyoPixmap(a)).ToArray());
            }

            return new FragmentalImage(clipSize, levelWidths.ToArray(), levelHeights.ToArray(), clipped.ToArray());
        }

        // 计算分块数，按长度分割总数一共要多少块（不足的也给完整一块）
        public static int CalculateClipCount(int fullLength, int blockLength)
        {
            int quotient = Math.DivRem(blockLength, fullLength, out int remainder);
            return (remainder != 0 ? 1 : 0) + quotient;
        }

        public static Image ToImage(this FragmentalImage fragmentalImage)
        {
            int clipSize = fragmentalImage.ClipSize;
            int levelWidth = fragmentalImage.LevelWidths.First();
            int levelHeight = fragmentalImage.LevelHeights.First();
            RyoPixmap[] pixmaps = fragmentalImage.RyoPixmaps.First(); // 不要不高清的
            RyoPixmap.FORMAT format = pixmaps.First().Format;

            // 计算画布大小和切片数
            int levelWidthClipCount = CalculateClipCount(clipSize, levelWidth);
            int levelHeightClipCount = CalculateClipCount(clipSize, levelHeight);

            // 创建一个新画布
            Image outputImage = format == RyoPixmap.FORMAT.RGBA8888 ? new Image<Rgba32>(levelWidth, levelHeight) : format == RyoPixmap.FORMAT.RGB888 ? new Image<Rgb24>(levelWidth, levelHeight) : throw new NotSupportedException("暂不支持该格式：" + format);

            // 在画布上绘制图像
            for (int y = 0; y < levelHeightClipCount; y++)
            {
                for (int x = 0; x < levelWidthClipCount; x++)
                {
                    int index = y * levelWidthClipCount + x;
                    Image img = pixmaps[index].ToImage();
                    outputImage.Mutate(ctx => ctx.DrawImage(img, new Point(x * clipSize, y * clipSize), 1F));
                }
            }

            return outputImage;
        }
    }
}