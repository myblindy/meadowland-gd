using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

internal static class AssetLoadingHelpers
{
    public static IEnumerable<string> EnumerateAssets(string path, params string[] extensions)
    {
        foreach (var _file in DirAccess.GetFilesAt(path))
        {
            var file = _file.Replace(".remap", "");

            if (extensions.Length == 0)
                yield return file;
            else if (extensions.Length == 1 && file.EndsWith(extensions[0]))
                yield return file;
            else if (extensions.Length > 1)
                foreach (var ext in extensions)
                    if (file.EndsWith(ext))
                    {
                        yield return file;
                        break;
                    }
        }
    }

    public static void PackTileImagesIntoAtlasImage(IList<Image> tileImages, in Vector2I tileSize,
        Vector2I? atlasImageSize, in Image.Format atlasImageFormat, out Image atlasImage,
        Action<Image, Image, int, int, int> processAddedTileImage, Vector2I? separation = default)
    {
        const int maxAtlasWidth = 1024;
        separation ??= new(1, 1);
        int x = 0, y = 0, tileX = 0, tileY = 0;

        if (atlasImageSize is null)
        {
            var tileCount = 0;
            foreach (var fullTileImage in tileImages)
            {
                var aspectRatio = (double)fullTileImage.GetWidth() / fullTileImage.GetHeight();
                Debug.Assert(aspectRatio % 1.0 == 0 || 1 / aspectRatio % 1.0 == 0, "Tile aspect ratio is not an integer");
                tileCount += Mathf.CeilToInt(aspectRatio > 1 ? aspectRatio : 1 / aspectRatio);
            }

            atlasImageSize = new(
                Math.Min(maxAtlasWidth, tileCount * tileSize.X),
                (int)Math.Ceiling((double)tileCount / (maxAtlasWidth / tileSize.X)) * tileSize.Y
            );

            // add the 1px padding
            atlasImageSize = atlasImageSize.Value with
            {
                X = atlasImageSize.Value.X / tileSize.X * (tileSize.X + separation.Value.X),
                Y = atlasImageSize.Value.Y / tileSize.Y * (tileSize.Y + separation.Value.Y),
            };
        }
        atlasImage = Image.CreateEmpty(atlasImageSize.Value.X, atlasImageSize.Value.Y, true, atlasImageFormat);

        using var tmpImage = Image.CreateEmpty(tileSize.X, tileSize.Y, false, atlasImageFormat);
        foreach (var fullTileImage in tileImages)
        {
            var aspectRatio = (double)fullTileImage.GetWidth() / fullTileImage.GetHeight();
            if (aspectRatio == 1)
                processTileImage(fullTileImage, fullTileImage, 0, tileSize, atlasImage);
            else if (aspectRatio > 1)
            {
                for (int index = 0; index < aspectRatio; ++index)
                {
                    tmpImage.BlitRect(fullTileImage,
                        new(fullTileImage.GetWidth() - fullTileImage.GetHeight() * (index + 1), 0, fullTileImage.GetHeight(), fullTileImage.GetHeight()),
                        new Rect2I(0, 0, tileSize.X, tileSize.Y));
                    processTileImage(fullTileImage, tmpImage, index, tileSize, atlasImage);
                }
            }
            else
            {
                for (int index = 0; index < 1 / aspectRatio; ++index)
                {
                    tmpImage.BlitRect(fullTileImage,
                        new(0, fullTileImage.GetHeight() - fullTileImage.GetWidth() * (index + 1), fullTileImage.GetWidth(), fullTileImage.GetWidth()),
                        new Rect2I(0, 0, tileSize.X, tileSize.Y));
                    processTileImage(fullTileImage, tmpImage, index, tileSize, atlasImage);
                }
            }

            void processTileImage(Image sourceImage, Image tileImage, int index, in Vector2I tileSize, in Image atlasImage)
            {
                atlasImage.BlitRect(tileImage,
                    new Rect2I(0, 0, tileImage.GetWidth(), tileImage.GetHeight()),
                    new Rect2I(x, y, tileSize.X, tileSize.Y));
                processAddedTileImage(sourceImage, tileImage, index, tileX, tileY);

                // advance
                if (x + tileSize.X + separation.Value.X + tileSize.X >= atlasImage.GetWidth())
                {
                    x = 0; tileX = 0;
                    y += tileSize.Y + separation.Value.X; ++tileY;
                }
                else
                {
                    x += tileSize.X + separation.Value.X; ++tileX;
                }
            }
        }
    }

    public static void BlitRect(this Image destinationImage, Image sourceImage, Rect2I sourceRect, Rect2I destinationRect)
    {
        if (sourceImage.GetFormat() != destinationImage.GetFormat())
        {
            // convert the image to the destination format
            sourceImage.Convert(destinationImage.GetFormat());
        }

        if (sourceRect.Size != destinationRect.Size)
        {
            // crop?
            if (sourceRect.Position != default || sourceRect.Size != sourceImage.GetSize())
            {
                sourceImage.BlitRect(sourceImage, new(0, 0, sourceImage.GetWidth(), sourceImage.GetHeight()), default);
                sourceImage.Crop(sourceImage.GetWidth(), sourceImage.GetHeight());
            }

            // resize the image
            sourceImage.Resize(destinationRect.Size.X, destinationRect.Size.Y);
        }

        destinationImage.BlitRect(sourceImage, sourceRect, destinationRect.Position);
    }
}
