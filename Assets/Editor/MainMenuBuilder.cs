// ВАЖНО: положить в папку Assets/Editor (создай её, если нет).
// Это инструмент редактора: он один раз создаёт объекты меню в открытой сцене.
// В саму игру (на телефон) он не попадает.
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

public static class MainMenuBuilder
{
    const string BgFolder = "Assets/Sprites/Backgrounds/";
    const float TileSize = 192f; // плитка 24 px увеличена в 8 раз

    [MenuItem("Tools/Build Main Menu")]
    static void Build()
    {
        // Удаляем прошлую версию, чтобы можно было запускать повторно
        DestroyIfExists("MenuCanvas");
        DestroyIfExists("MenuManager");

        SetupEventSystem();

        // Стандартные картинки Unity для кнопок, слайдеров, галочек
        var res = new TMP_DefaultControls.Resources();
        res.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        res.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        res.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        res.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

        // Для слайдера и галочки в этой версии Unity берём стандартные (не TMP) заготовки
        var dres = new DefaultControls.Resources();
        dres.standard = res.standard;
        dres.background = res.background;
        dres.knob = res.knob;
        dres.checkmark = res.checkmark;

        // ---------- Canvas ----------
        var canvasGO = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas),
                                      typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Build Main Menu");
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGO.transform;

        // ---------- Фон (создаётся ПЕРВЫМ, значит рисуется ПОЗАДИ всего) ----------
        var sky = MakeImage("BG_Sky", root, LoadSprite("tile_0000"), new Color(0.75f, 0.9f, 0.9f));
        Stretch(sky.rectTransform);
        // Ряд холмов и ряд земли внизу из плиток Kenney
        TileRow(root, new[] { "tile_0008", "tile_0009", "tile_0010", "tile_0011" }, TileSize, 12);
        TileRow(root, new[] { "tile_0016", "tile_0017", "tile_0018", "tile_0019" }, 0f, 12);

        // Затемнённая панель под элементы меню, чтобы текст читался
        var panel = MakeImage("Panel", root, null, new Color(0f, 0f, 0f, 0.5f));
        Place(panel.gameObject, root, new Vector2(0, -10), new Vector2(800, 960));

        // ---------- Название (ПОМЕНЯЙ на название своей игры) ----------
        MakeText(root, "MOBILE PLATFORMER", 90, new Vector2(0, 390), new Vector2(760, 130), res);

        // ---------- Кнопки ----------
        var playBtn = MakeButton(root, "PLAY", new Vector2(0, 250), res);
        var quitBtn = MakeButton(root, "QUIT", new Vector2(0, 130), res);

        // ---------- Настройки ----------
        var hpText = MakeText(root, "HP: 3", 40, new Vector2(0, 30), new Vector2(600, 56), res);
        var hpSlider = MakeSlider(root, new Vector2(0, -20), 1, 5, true, 3, dres);

        var cdText = MakeText(root, "Cooldown: 0.50 s", 40, new Vector2(0, -90), new Vector2(600, 56), res);
        var cdSlider = MakeSlider(root, new Vector2(0, -140), 0.1f, 1.5f, false, 0.5f, dres);

        var fireToggle = MakeToggle(root, "Fire", new Vector2(-170, -230), dres);
        var enemyToggle = MakeToggle(root, "Enemies", new Vector2(170, -230), dres);

        // ---------- Менеджер меню и подключение ----------
        var mgrGO = new GameObject("MenuManager");
        Undo.RegisterCreatedObjectUndo(mgrGO, "Build Main Menu");
        var menu = mgrGO.AddComponent<MainMenu>();
        menu.hpSlider = hpSlider;
        menu.cooldownSlider = cdSlider;
        menu.hazardsToggle = fireToggle;
        menu.enemiesToggle = enemyToggle;
        menu.hpText = hpText;
        menu.cooldownText = cdText;
        menu.firstLevelName = "level1";

        // Кнопки вызывают методы MainMenu (то же самое, что On Click в Inspector)
        UnityEventTools.AddPersistentListener(playBtn.onClick, menu.Play);
        UnityEventTools.AddPersistentListener(quitBtn.onClick, menu.Quit);

        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGO;
        Debug.Log("Main Menu построено. Нажми Ctrl+S, чтобы сохранить сцену.");
    }

    // ---------------- вспомогательные методы ----------------

    static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static void SetupEventSystem()
    {
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(go, "Build Main Menu");
            es = go.GetComponent<EventSystem>();
        }
        // Со старым модулем ввода будет ошибка (у нас новая Input System)
        var old = es.GetComponent<StandaloneInputModule>();
        if (old != null) Object.DestroyImmediate(old);
        if (es.GetComponent<InputSystemUIInputModule>() == null)
            es.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    static Sprite LoadSprite(string file)
    {
        return AssetDatabase.LoadAllAssetsAtPath(BgFolder + file + ".png").OfType<Sprite>().FirstOrDefault();
    }

    static Image MakeImage(string name, Transform parent, Sprite sprite, Color fallbackColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = sprite != null ? Color.white : fallbackColor;
        if (name == "BG_Sky" && sprite != null) img.color = Color.white;
        img.raycastTarget = false; // фон не должен перехватывать нажатия
        return img;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void TileRow(Transform parent, string[] names, float y, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var sprite = LoadSprite(names[i % names.Length]);
            var img = MakeImage("BG_Tile_" + y + "_" + i, parent, sprite, new Color(0.3f, 0.7f, 0.4f));
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); // низ по центру
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(TileSize, TileSize);
            rt.anchoredPosition = new Vector2((i - (count - 1) / 2f) * TileSize, y);
        }
    }

    static RectTransform Place(GameObject go, Transform parent, Vector2 pos, Vector2 size)
    {
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static TMP_Text MakeText(Transform parent, string text, float size, Vector2 pos, Vector2 box,
                             TMP_DefaultControls.Resources res)
    {
        var go = TMP_DefaultControls.CreateText(res);
        Place(go, parent, pos, box);
        var t = go.GetComponent<TMP_Text>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static Button MakeButton(Transform parent, string label, Vector2 pos, TMP_DefaultControls.Resources res)
    {
        var go = TMP_DefaultControls.CreateButton(res);
        Place(go, parent, pos, new Vector2(420, 100));
        var t = go.GetComponentInChildren<TMP_Text>();
        t.text = label;
        t.fontSize = 48;
        return go.GetComponent<Button>();
    }

    static Slider MakeSlider(Transform parent, Vector2 pos, float min, float max, bool whole, float value,
                             DefaultControls.Resources res)
    {
        var go = DefaultControls.CreateSlider(res);
        Place(go, parent, pos, new Vector2(520, 36));
        var s = go.GetComponent<Slider>();
        s.minValue = min;
        s.maxValue = max;
        s.wholeNumbers = whole;
        s.value = value;
        var handle = go.transform.Find("Handle Slide Area/Handle") as RectTransform;
        if (handle != null) handle.sizeDelta = new Vector2(36, 0);
        return s;
    }

    static Toggle MakeToggle(Transform parent, string label, Vector2 pos, DefaultControls.Resources res)
    {
        var go = DefaultControls.CreateToggle(res);
        Place(go, parent, pos, new Vector2(300, 50));
        var t = go.GetComponentInChildren<Text>(); // у галочки обычный (не TMP) текст
        t.text = label;
        t.fontSize = 34;
        t.color = Color.white;

        var bg = go.transform.Find("Background") as RectTransform;
        if (bg != null)
        {
            bg.anchoredPosition = new Vector2(25, -25);
            bg.sizeDelta = new Vector2(44, 44);
            var check = bg.Find("Checkmark") as RectTransform;
            if (check != null) check.sizeDelta = new Vector2(34, 34);
        }
        var lbl = go.transform.Find("Label") as RectTransform;
        if (lbl != null)
        {
            lbl.offsetMin = new Vector2(60, 0);
            lbl.offsetMax = Vector2.zero;
        }
        return go.GetComponent<Toggle>();
    }
}
