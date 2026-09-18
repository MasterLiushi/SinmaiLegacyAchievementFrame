using UnityEngine;
using Object = UnityEngine.Object;

namespace SinmaiLegacyAchievementFrame;

internal static class LegacyAchievementAssets
{
    private const string ResourcePrefix = "SinmaiLegacyAchievementFrame.Assets.";
    private const float PixelsPerUnit = 100f;

    private static readonly Dictionary<string, Vector4> Borders = new(StringComparer.Ordinal)
    {
        {
            "UI_UPE_Box_02", new Vector4(22f, 0f, 22f, 0f)
        }
    };

    private static readonly Dictionary<string, Sprite> Loaded = new(StringComparer.Ordinal);
    private static readonly HashSet<Sprite> Created = [];
    private static readonly HashSet<string> Unavailable = new(StringComparer.Ordinal);

    private static readonly Dictionary<Texture, Texture2D> MipFreeCopies = new();
    private static readonly Dictionary<int, Sprite[]> Sheets = new();

    internal static bool TryRebuildSheet(Sprite[] source, bool integer, int state, out Sprite[] sheet)
    {
        sheet = null;
        if (source == null || source.Length == 0 || state < 0 || state > 2)
        {
            return false;
        }

        var key = (integer ? 0 : 3) + state;
        if (Sheets.TryGetValue(key, out sheet))
        {
            return sheet != null;
        }

        var atlas = GetMipFreeCopy(source[0] != null ? source[0].texture : null);
        if (atlas == null)
        {
            Sheets[key] = null;
            return false;
        }

        var sprites = new Sprite[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            var origin = source[i];
            if (origin == null)
            {
                Sheets[key] = null;
                return false;
            }

            var sprite = Sprite.Create(
                atlas,
                origin.rect,
                origin.pivot,
                origin.pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = origin.name + "_NoMip";
            sprites[i] = sprite;
        }

        Sheets[key] = sprites;
        sheet = sprites;
        return true;
    }

    private static Texture2D GetMipFreeCopy(Texture source)
    {
        if (source == null)
        {
            return null;
        }

        if (MipFreeCopies.TryGetValue(source, out var cached))
        {
            return cached;
        }

        Texture2D copy = null;
        try
        {
            copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
            {
                name = source.name + "_NoMip",
                filterMode = source.filterMode,
                wrapMode = source.wrapMode
            };
            Graphics.CopyTexture(source, 0, 0, copy, 0, 0);

            var raw = copy.GetRawTextureData();
            var hasData = false;
            for (var i = 0; i < raw.Length; i += 61)
            {
                if (raw[i] != 0)
                {
                    hasData = true;
                    break;
                }
            }

            if (!hasData)
            {
                Object.Destroy(copy);
                copy = null;
            }
        }
        catch (Exception)
        {
            if (copy != null)
            {
                Object.Destroy(copy);
                copy = null;
            }
        }

        if (copy == null)
        {
            source.mipMapBias = -1f;
        }

        MipFreeCopies[source] = copy;
        return copy;
    }

    internal static bool IsLegacySprite(Sprite sprite)
    {
        return sprite != null && Created.Contains(sprite);
    }

    internal static bool TryGetSprite(string spriteName, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrEmpty(spriteName) || Unavailable.Contains(spriteName))
        {
            return false;
        }

        if (Loaded.TryGetValue(spriteName, out sprite))
        {
            return sprite != null;
        }

        var assembly = typeof(LegacyAchievementAssets).Assembly;
        var stream = assembly.GetManifestResourceStream(ResourcePrefix + spriteName + ".png");

        if (stream == null)
        {
            Unavailable.Add(spriteName);
            return false;
        }

        using (stream)
        {
            return CreateSprite(spriteName, stream, out sprite);
        }
    }

    private static bool CreateSprite(string spriteName, Stream stream, out Sprite sprite)
    {
        sprite = null;

        var buffer = new byte[stream.Length];
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = stream.Read(buffer, offset, buffer.Length - offset);
            if (read <= 0)
            {
                break;
            }

            offset += read;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(buffer))
        {
            Object.Destroy(texture);
            Unavailable.Add(spriteName);
            return false;
        }

        texture.name = spriteName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var border = Borders.TryGetValue(spriteName, out var value) ? value : Vector4.zero;
        sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit,
            0,
            SpriteMeshType.FullRect,
            border);
        sprite.name = spriteName + "_Legacy";

        Loaded[spriteName] = sprite;
        Created.Add(sprite);
        return true;
    }
}
