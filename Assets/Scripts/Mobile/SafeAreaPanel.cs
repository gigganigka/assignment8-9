using UnityEngine;

// Only foreground UI lives here. Backgrounds remain full screen.
[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaPanel : MonoBehaviour
{
    Rect lastArea;
    Vector2Int lastSize;
    void OnEnable() { Apply(); }
    void Update()
    {
        if (lastArea != Screen.safeArea || lastSize.x != Screen.width || lastSize.y != Screen.height)
            Apply();
    }
    public void Apply()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Rect area = Screen.safeArea;
        // Some preview/editor environments report an empty safe rectangle.
        if (area.width <= 0 || area.height <= 0) area = new Rect(0, 0, Screen.width, Screen.height);
        RectTransform rt = (RectTransform)transform;
        rt.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        rt.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        lastArea = Screen.safeArea;
        lastSize = new Vector2Int(Screen.width, Screen.height);
    }
}
