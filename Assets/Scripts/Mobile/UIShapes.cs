using UnityEngine;

// Small procedural nine-slice for interface geometry; no external UI pack needed.
public static class UIShapes
{
    static Sprite panel;
    public static Sprite Panel
    {
        get
        {
            if (panel != null) return panel;
            const int size = 32, radius = 8;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "Coinbound UI panel";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x, radius, size - radius - 1);
                    float cy = Mathf.Clamp(y, radius, size - radius - 1);
                    float alpha = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)));
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            texture.Apply();
            panel = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                                  100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return panel;
        }
    }
}
