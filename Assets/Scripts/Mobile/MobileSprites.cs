using System.Collections.Generic;
using UnityEngine;

public static class MobileSprites
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    public static Sprite Load(string path)
    {
        if (cache.TryGetValue(path, out Sprite cached) && cached != null) return cached;
        // Uploaded textures use Multiple sprite import mode, so explicitly load sub-assets.
        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        Sprite sprite = sprites.Length > 0 ? sprites[0] : Resources.Load<Sprite>(path);
        if (sprite != null) cache[path] = sprite;
        return sprite;
    }
}
