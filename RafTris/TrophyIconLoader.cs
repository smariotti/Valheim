using System.Collections.Generic;
using UnityEngine;

namespace RafTris
{
    /// <summary>
    /// Resolves trophy prefab names to their item icon Sprites.
    /// Falls back to a generated placeholder icon if the trophy isn't found
    /// (e.g., Deep North placeholders, or biomes where content hasn't loaded yet).
    /// </summary>
    public static class TrophyIconLoader
    {
        // Cache so we don't re-query ObjectDB every frame
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        // Fallback placeholder sprites keyed by a colour hash
        private static readonly Dictionary<Color, Texture2D> _placeholderTextures = new Dictionary<Color, Texture2D>();

        public static void ClearCache() => _cache.Clear();

        /// <summary>
        /// Returns the Sprite for the named trophy item, or a coloured placeholder.
        /// Only caches a successful hit or a known-missing prefab name — never caches
        /// a failure caused by ObjectDB not being ready yet, so the lookup retries
        /// automatically on the next call until the world has finished loading.
        /// </summary>
        public static Sprite GetTrophySprite(string trophyPrefabName, Color fallbackColor)
        {
            if (string.IsNullOrEmpty(trophyPrefabName))
                return GetPlaceholderSprite(fallbackColor);

            if (_cache.TryGetValue(trophyPrefabName, out var cached))
                return cached;

            // ObjectDB isn't populated until the world loads — don't cache the miss yet.
            if (ObjectDB.instance == null)
                return GetPlaceholderSprite(fallbackColor);

            var prefab = ObjectDB.instance.GetItemPrefab(trophyPrefabName);
            if (prefab != null)
            {
                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop != null && itemDrop.m_itemData?.m_shared?.m_icons?.Length > 0)
                {
                    var sprite = itemDrop.m_itemData.m_shared.m_icons[0];
                    _cache[trophyPrefabName] = sprite;
                    return sprite;
                }
            }

            // Prefab genuinely doesn't exist (e.g. Deep North placeholder) — cache the
            // miss so we stop searching for it on every frame.
            RafTrisPlugin.Log.LogWarning($"[RafTris] Trophy prefab not found: {trophyPrefabName}");
            var ph = GetPlaceholderSprite(fallbackColor);
            _cache[trophyPrefabName] = ph;
            return ph;
        }

        /// <summary>
        /// Generates (or retrieves from cache) a simple coloured placeholder icon.
        /// Uses a skull-like symbol drawn with pixels.
        /// </summary>
        private static Sprite GetPlaceholderSprite(Color color)
        {
            if (_placeholderTextures.TryGetValue(color, out var existing))
            {
                return Sprite.Create(existing,
                    new Rect(0, 0, existing.width, existing.height),
                    new Vector2(0.5f, 0.5f));
            }

            const int Size = 32;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[Size * Size];

            // Draw a simple question-mark / "?" shape as placeholder
            var dark      = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 1f);
            var light     = new Color(color.r, color.g, color.b, 1f);
            var border    = new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, 1f);
            var clearPx   = new Color(0, 0, 0, 0);

            // Fill background
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = dark;

            // Border
            for (int x = 0; x < Size; x++)
            {
                pixels[0 * Size + x]        = border;
                pixels[(Size-1) * Size + x]  = border;
                pixels[x * Size + 0]         = border;
                pixels[x * Size + (Size-1)]  = border;
            }

            // Draw "?" pixels (very simple 8x16 glyph centred)
            // The glyph is defined as a bitmask
            int[] glyphRows = {
                0b0111110,
                0b1100011,
                0b1100011,
                0b0000011,
                0b0000110,
                0b0001100,
                0b0001100,
                0b0000000,
                0b0001100,
                0b0001100,
            };
            int gw = 7, gh = 10;
            int offX = (Size - gw) / 2;
            int offY = (Size - gh) / 2;
            for (int gy = 0; gy < gh; gy++)
            {
                int row = glyphRows[gh - 1 - gy];
                for (int gx = 0; gx < gw; gx++)
                {
                    if ((row & (1 << (gw - 1 - gx))) != 0)
                    {
                        int px = offX + gx;
                        int py = offY + gy;
                        if (px >= 0 && px < Size && py >= 0 && py < Size)
                            pixels[py * Size + px] = light;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            _placeholderTextures[color] = tex;

            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f));
        }
    }
}
