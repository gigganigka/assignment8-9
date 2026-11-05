// Лежит в Assets/Editor (ВАЖНО: не в Scripts, иначе сборка APK падает - UnityEditor нет на телефоне).
// Tools -> 5. Finish Game: в Level1, Level2 и Boss добавляет
//  - анимации (герой Idle/Run/Jump, монетки, сердца, враги, босс, флаг-портал),
//  - звуки и музыку (файлы из Assets/Audio),
//  - мобильное управление (экранный джойстик и кнопки Jump / Fire),
//  - экран смерти (YOU DIED + RESTART + MAIN MENU),
//  - чинит точку выстрела босса.
// В Main Menu и Win добавляет музыку. В конце обновляет список сцен в Build.
// Это инструмент редактора, в игру не попадает. Можно запускать повторно.
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

public static class FinishBuilder
{
    const float PlayerScale = 2.5f;   // как в LevelBuilder: герой увеличен в 2.5 раза
    const float HeartWidth = 0.4f;    // как в LevelBuilder: ширина сердца в мире

    class Anims
    {
        public AnimatorController player, coin, heart, enemy, boss, portal;
    }

    [MenuItem("Tools/5. Finish Game (animations + sounds + mobile + death screen)")]
    public static void FinishAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Anims anims = CreateAnimationAssets();

        // ---- уровни ----
        string[] levels = { SceneNames.Level1, SceneNames.Level2, SceneNames.Boss };
        foreach (string sceneName in levels)
        {
            if (!Open(sceneName)) continue;
            ApplyAnimators(anims);
            AssignSounds();
            AddMusic();
            GameObject mobile = AddMobileControls();
            AddDeathScreen(mobile);
            FixBossFirePoint();
            Save();
        }

        // ---- меню и экран победы: только музыка ----
        foreach (string sceneName in new[] { SceneNames.MainMenu, SceneNames.Win })
        {
            if (!Open(sceneName)) continue;
            AddMusic();
            SetupEventSystem();
            Save();
        }

        LevelBuilder.AddScenesToBuild();   // mainmenu, level1, level2, bosslevel, win - по порядку
        AssetDatabase.SaveAssets();
        Debug.Log("Готово: анимации, звуки, музыка, джойстик и экран смерти добавлены. Открой mainmenu и нажми Play.");
    }

    // ===================== Анимации =====================

    static Anims CreateAnimationAssets()
    {
        Directory.CreateDirectory("Assets/Animations");
        AssetDatabase.Refresh();
        var a = new Anims();

        // ---- Герой (жёлтый, Characters 6 и 7): Idle / Run / Jump ----
        // Idle: стоит (кадр 6) и "дышит" - чуть сжимается по высоте
        AnimationClip idle = MakeClip("Assets/Animations/Player_Idle.anim", new[] { S("Characters", 6) }, 1f);
        AddBreathing(idle, PlayerScale, 0.04f, 1.2f);
        AnimationClip run = MakeClip("Assets/Animations/Player_Run.anim", new[] { S("Characters", 6), S("Characters", 7) }, 8f);
        AnimationClip jump = MakeClip("Assets/Animations/Player_Jump.anim", new[] { S("Characters", 7) }, 1f);

        string pPath = "Assets/Animations/Player.controller";
        AssetDatabase.DeleteAsset(pPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(pPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        var sm = ctrl.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sRun = sm.AddState("Run"); sRun.motion = run;
        var sJump = sm.AddState("Jump"); sJump.motion = jump;
        sm.defaultState = sIdle;

        // Idle -> Run: бежим по земле
        var t = Trans(sIdle, sRun);
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        // Run -> Idle: остановились
        t = Trans(sRun, sIdle);
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        // Idle/Run -> Jump: оторвались от земли
        t = Trans(sIdle, sJump);
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
        t = Trans(sRun, sJump);
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
        // Jump -> Idle / Run: приземлились
        t = Trans(sJump, sIdle);
        t.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        t = Trans(sJump, sRun);
        t.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        a.player = ctrl;

        // ---- Монетка крутится (Tiles 151 -> 152) ----
        a.coin = SimpleController("Assets/Animations/Coin.controller",
            MakeClip("Assets/Animations/Coin_Spin.anim", new[] { S("Tiles", 151), S("Tiles", 152) }, 6f));

        // ---- Сердце пульсирует (масштаб) ----
        Sprite heart = S("Tiles", 44);
        AnimationClip heartClip = MakeClip("Assets/Animations/Heart_Pulse.anim", new[] { heart }, 1f);
        float heartScale = heart != null ? HeartWidth / heart.bounds.size.x : 1f;
        AddPulse(heartClip, heartScale, 0.15f, 0.8f);
        a.heart = SimpleController("Assets/Animations/Heart.controller", heartClip);

        // ---- Враг шагает (Characters 15 -> 16) ----
        a.enemy = SimpleController("Assets/Animations/Enemy.controller",
            MakeClip("Assets/Animations/Enemy_Walk.anim", new[] { S("Characters", 15), S("Characters", 16) }, 5f));

        // ---- Босс "дышит" (Characters 21 -> 22 -> 23 -> 22) ----
        a.boss = SimpleController("Assets/Animations/Boss.controller",
            MakeClip("Assets/Animations/Boss_Idle.anim",
                new[] { S("Characters", 21), S("Characters", 22), S("Characters", 23), S("Characters", 22) }, 4f));

        // ---- Флаг-портал развевается (Tiles 111 -> 112) - анимация окружения ----
        a.portal = SimpleController("Assets/Animations/Portal_Flag.controller",
            MakeClip("Assets/Animations/Portal_Flag.anim", new[] { S("Tiles", 111), S("Tiles", 112) }, 4f));

        AssetDatabase.SaveAssets();
        return a;
    }

    // Клип, который меняет картинку SpriteRenderer по кадрам
    static AnimationClip MakeClip(string path, Sprite[] frames, float fps)
    {
        AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip();
        clip.frameRate = fps;

        var binding = new EditorCurveBinding();
        binding.type = typeof(SpriteRenderer);
        binding.path = "";
        binding.propertyName = "m_Sprite";

        // +1 ключ в конце (повтор последнего кадра), иначе последний кадр
        // показывается 0 секунд и анимация из 2 кадров "застывает" на первом
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i <= frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe();
            keys[i].time = i / fps;
            keys[i].value = frames[Mathf.Min(i, frames.Length - 1)];
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // "Дыхание": по Y чуть меньше и обратно (X и Z не трогаем)
    static void AddBreathing(AnimationClip clip, float baseScale, float amount, float duration)
    {
        SetScaleCurve(clip, "m_LocalScale.x", Flat(baseScale, duration));
        SetScaleCurve(clip, "m_LocalScale.y", Wave(baseScale, baseScale * (1f - amount), duration));
        SetScaleCurve(clip, "m_LocalScale.z", Flat(baseScale, duration));
    }

    // Пульс: равномерно больше и обратно
    static void AddPulse(AnimationClip clip, float baseScale, float amount, float duration)
    {
        AnimationCurve c = Wave(baseScale, baseScale * (1f + amount), duration);
        SetScaleCurve(clip, "m_LocalScale.x", c);
        SetScaleCurve(clip, "m_LocalScale.y", c);
        SetScaleCurve(clip, "m_LocalScale.z", Flat(baseScale, duration));
    }

    static AnimationCurve Wave(float from, float to, float duration)
    {
        return new AnimationCurve(new Keyframe(0f, from), new Keyframe(duration / 2f, to), new Keyframe(duration, from));
    }

    static AnimationCurve Flat(float v, float duration)
    {
        return new AnimationCurve(new Keyframe(0f, v), new Keyframe(duration, v));
    }

    static void SetScaleCurve(AnimationClip clip, string prop, AnimationCurve curve)
    {
        var b = EditorCurveBinding.FloatCurve("", typeof(Transform), prop);
        AnimationUtility.SetEditorCurve(clip, b, curve);
    }

    static AnimatorController SimpleController(string path, AnimationClip clip)
    {
        AssetDatabase.DeleteAsset(path);
        var c = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = c.layers[0].stateMachine;
        var st = sm.AddState("Loop");
        st.motion = clip;
        sm.defaultState = st;
        return c;
    }

    static AnimatorStateTransition Trans(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = false;   // переключаемся сразу, не дожидаясь конца анимации
        t.duration = 0f;
        return t;
    }

    static void ApplyAnimators(Anims a)
    {
        GameObject player = GameObject.Find("Player");
        if (player != null) SetAnimator(player, a.player);
        foreach (var c in Object.FindObjectsByType<Collectible>(FindObjectsSortMode.None)) SetAnimator(c.gameObject, a.coin);
        foreach (var h in Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None)) SetAnimator(h.gameObject, a.heart);
        foreach (var e in Object.FindObjectsByType<PatrolEnemy>(FindObjectsSortMode.None)) SetAnimator(e.gameObject, a.enemy);
        foreach (var b in Object.FindObjectsByType<Boss>(FindObjectsSortMode.None)) SetAnimator(b.gameObject, a.boss);
        foreach (var p in Object.FindObjectsByType<Portal>(FindObjectsSortMode.None)) SetAnimator(p.gameObject, a.portal);
    }

    static void SetAnimator(GameObject go, RuntimeAnimatorController ctrl)
    {
        var an = go.GetComponent<Animator>();
        if (an == null) an = go.AddComponent<Animator>();
        an.runtimeAnimatorController = ctrl;
        an.applyRootMotion = false;
    }

    // ===================== Звуки и музыка =====================

    static AudioClip A(string name)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + name + ".wav");
        if (clip == null) Debug.LogError("Нет звука Assets/Audio/" + name + ".wav");
        return clip;
    }

    static void AssignSounds()
    {
        AudioClip jump = A("jump"), coin = A("coin"), hurt = A("hurt"), shoot = A("shoot"),
                  win = A("win"), bossHit = A("boss_hit");

        foreach (var x in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None)) { x.jumpSound = jump; EditorUtility.SetDirty(x); }
        foreach (var x in Object.FindObjectsByType<Health>(FindObjectsSortMode.None)) { x.damageSound = hurt; EditorUtility.SetDirty(x); }
        foreach (var x in Object.FindObjectsByType<PlayerShooter>(FindObjectsSortMode.None)) { x.shootSound = shoot; EditorUtility.SetDirty(x); }
        foreach (var x in Object.FindObjectsByType<Collectible>(FindObjectsSortMode.None)) { x.pickupSound = coin; EditorUtility.SetDirty(x); }
        foreach (var x in Object.FindObjectsByType<HealthPickup>(FindObjectsSortMode.None)) { x.pickupSound = coin; EditorUtility.SetDirty(x); }
        foreach (var x in Object.FindObjectsByType<Portal>(FindObjectsSortMode.None)) { x.winSound = win; EditorUtility.SetDirty(x); }
        foreach (var x in Object.FindObjectsByType<PatrolEnemy>(FindObjectsSortMode.None)) { x.hitSound = bossHit; EditorUtility.SetDirty(x); }
        foreach (var x in Object.FindObjectsByType<Boss>(FindObjectsSortMode.None)) { x.hitSound = bossHit; x.deathSound = win; EditorUtility.SetDirty(x); }
    }

    static void AddMusic()
    {
        GameObject old = GameObject.Find("Music");
        if (old != null) Object.DestroyImmediate(old);
        var go = new GameObject("Music", typeof(AudioSource));
        var src = go.GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        var m = go.AddComponent<Music>();
        m.musicClip = A("music");
        m.volume = 0.25f;   // музыка тише эффектов

        // Без AudioListener звука не будет совсем - проверяем камеру
        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
    }

    // ===================== Мобильное управление =====================

    static GameObject AddMobileControls()
    {
        GameObject old = GameObject.Find("MobileControls");
        if (old != null) Object.DestroyImmediate(old);

        GameObject canvasGO = MakeCanvas("MobileControls", 5);
        Sprite round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // Джойстик (левый нижний угол):
        //  StickBG   - видимое кольцо, касания НЕ ловит
        //  Stick     - невидимая зона размером с кольцо, на ней OnScreenStick:
        //              палец можно ставить в ЛЮБОЕ место кольца, а не только точно на кружок
        //  Knob      - видимый кружок, ребёнок Stick, двигается вместе с ним
        Image bg = MakeCircle("StickBG", canvasGO.transform, round, new Color(1f, 1f, 1f, 0.25f),
                              new Vector2(0f, 0f), new Vector2(300f, 300f), 380f);
        bg.raycastTarget = false;
        Image area = MakeCircle("Stick", bg.transform, round, new Color(1f, 1f, 1f, 0f),
                                new Vector2(0.5f, 0.5f), Vector2.zero, 380f);   // прозрачная, но ловит касания
        Image knob = MakeCircle("Knob", area.transform, round, new Color(1f, 1f, 1f, 0.6f),
                                new Vector2(0.5f, 0.5f), Vector2.zero, 160f);
        knob.raycastTarget = false;
        var stick = area.gameObject.AddComponent<OnScreenStick>();
        stick.controlPath = "<Gamepad>/leftStick";
        stick.movementRange = 110f;

        // Кнопки справа (правый нижний угол)
        Image jump = MakeCircle("JumpButton", canvasGO.transform, round, new Color(1f, 1f, 1f, 0.35f),
                                new Vector2(1f, 0f), new Vector2(-230f, 230f), 260f);
        jump.gameObject.AddComponent<OnScreenButton>().controlPath = "<Gamepad>/buttonSouth";
        Label(jump.transform, "JUMP", 44);

        Image fire = MakeCircle("FireButton", canvasGO.transform, round, new Color(1f, 0.6f, 0.3f, 0.4f),
                                new Vector2(1f, 0f), new Vector2(-530f, 160f), 220f);
        fire.gameObject.AddComponent<OnScreenButton>().controlPath = "<Gamepad>/buttonWest";
        Label(fire.transform, "FIRE", 44);

        SetupEventSystem();
        return canvasGO;
    }

    // ===================== Экран смерти =====================

    static void AddDeathScreen(GameObject mobileControls)
    {
        GameObject old = GameObject.Find("DeathScreen");
        if (old != null) Object.DestroyImmediate(old);

        GameObject canvasGO = MakeCanvas("DeathScreen", 20);   // поверх HUD и джойстика
        var ds = canvasGO.AddComponent<DeathScreen>();

        // Тёмная панель на весь экран
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.08f, 0.02f, 0.05f, 0.82f);

        GameObject title = Label(panel.transform, "YOU DIED", 170);
        var trt = title.GetComponent<RectTransform>();
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(1400f, 240f);
        trt.anchoredPosition = new Vector2(0f, 170f);
        title.GetComponent<TMP_Text>().color = new Color(1f, 0.35f, 0.3f);

        Button restart = MakeButton(panel.transform, "RESTART", new Vector2(-300f, -110f), new Color(1f, 0.85f, 0.35f));
        Button menu = MakeButton(panel.transform, "MAIN MENU", new Vector2(300f, -110f), new Color(0.75f, 0.9f, 0.95f));
        UnityEventTools.AddPersistentListener(restart.onClick, ds.Restart);
        UnityEventTools.AddPersistentListener(menu.onClick, ds.ToMainMenu);

        ds.panel = panel;
        ds.mobileControls = mobileControls;
        panel.SetActive(false);   // скрыта, пока герой жив
        EditorUtility.SetDirty(ds);
    }

    static Button MakeButton(Transform parent, string text, Vector2 pos, Color color)
    {
        var res = new TMP_DefaultControls.Resources();
        res.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        GameObject go = TMP_DefaultControls.CreateButton(res);
        go.name = text + " Button";
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(500f, 130f);
        rt.anchoredPosition = pos;
        go.GetComponent<Image>().color = color;
        var t = go.GetComponentInChildren<TMP_Text>();
        t.text = text;
        t.fontSize = 52;
        t.fontStyle = FontStyles.Bold;
        t.color = new Color(0.15f, 0.1f, 0.1f);
        return go.GetComponent<Button>();
    }

    // ===================== Босс =====================

    static void FixBossFirePoint()
    {
        foreach (var b in Object.FindObjectsByType<Boss>(FindObjectsSortMode.None))
        {
            if (b.firePoint == null) continue;
            // Босс увеличен ~в 10 раз, поэтому смещение маленькое (в единицах спрайта 0.24 x 0.23):
            // точка у левого края, чуть выше центра - шары вылетают прямо из босса
            b.firePoint.localPosition = new Vector3(-0.09f, 0.02f, 0f);
            EditorUtility.SetDirty(b.firePoint);
        }
    }

    // ===================== Вспомогательное =====================

    static GameObject MakeCanvas(string name, int sortingOrder)
    {
        var canvasGO = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var sc = canvasGO.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        return canvasGO;
    }

    static Image MakeCircle(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = true;
        return img;
    }

    static GameObject Label(Transform parent, string text, float size)
    {
        var go = TMP_DefaultControls.CreateText(new TMP_DefaultControls.Resources());
        go.name = "Label";
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TMP_Text>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        return go;
    }

    // Без EventSystem экранные кнопки и джойстик не получают касания
    static void SetupEventSystem()
    {
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        var oldModule = es.GetComponent<StandaloneInputModule>();
        if (oldModule != null) Object.DestroyImmediate(oldModule);
        var module = es.GetComponent<InputSystemUIInputModule>();
        if (module == null) module = es.gameObject.AddComponent<InputSystemUIInputModule>();
        if (module.actionsAsset == null) module.AssignDefaultActions();
    }

    static Sprite S(string folder, int n)
    {
        string path = "Assets/Sprites/" + folder + "/tile_" + n.ToString("0000") + ".png";
        Sprite s = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (s == null) Debug.LogError("Не найдена картинка: " + path);
        return s;
    }

    static string FindScenePath(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.StartsWith("Assets/") && Path.GetFileNameWithoutExtension(p) == name) return p;
        }
        return null;
    }

    static bool Open(string sceneName)
    {
        string path = FindScenePath(sceneName);
        if (path == null) { Debug.LogError("Нет сцены: " + sceneName + " (проверь SceneNames.cs)"); return false; }
        EditorSceneManager.OpenScene(path);
        return true;
    }

    static void Save()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
