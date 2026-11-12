using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One consistent interface for all five original scenes. It is built in sceneLoaded,
// before the original Start methods, so menu and HUD references are wired in time.
// Tools > Mobile Game > Bake UI also creates an editable preview in each scene.
public sealed class MobileGameUI : MonoBehaviour
{
    public static MobileGameUI Instance { get; private set; }
    public const string GameTitle = "COINBOUND";
    public bool IsPaused { get; private set; }
    public bool IsDead { get; private set; }
    RectTransform safe;
    GameObject hud, controls, modal;
    TMP_Text modalTitle, modalHint;
    Button resumeButton;
    PlayerController player;
    Health health;
    Image[] hearts;
    TMP_Text healthLabel;
    bool ready;
    GameObject menuCard, settingsCard;
    TMP_Text settingsHealth, settingsRate, settingsHazards, settingsEnemies;

    void Start()
    {
        // Fallback for entering Play Mode from an already baked scene.
        if (!ready && IsGameScene(gameObject.scene.name)) Build(gameObject.scene.name);
    }
    static readonly Color Ink = new Color(0.06f, 0.15f, 0.18f, 0.97f);
    static readonly Color Cream = new Color(0.94f, 0.98f, 0.89f);
    static readonly Color Mint = new Color(0.35f, 0.88f, 0.68f);
    static readonly Color Gold = new Color(1f, 0.77f, 0.34f);
    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || !IsGameScene(scene.name)) return;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Application.targetFrameRate = 60;
        Install(scene);
    }
    public static bool IsGameScene(string name)
    {
        return name == SceneNames.EnemyLab || name == SceneNames.MainMenu || name == SceneNames.Level1 ||
               name == SceneNames.Level2 || name == SceneNames.Boss || name == SceneNames.Win;
    }
    public static MobileGameUI Install(Scene scene)
    {
        MobileGameUI ui = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            ui = root.GetComponent<MobileGameUI>();
            if (ui != null) break;
        }
        if (ui == null)
        {
            GameObject go = new GameObject("MobileGameUI");
            SceneManager.MoveGameObjectToScene(go, scene);
            ui = go.AddComponent<MobileGameUI>();
        }
        ui.Build(scene.name);
        return ui;
    }
    public void Build(string sceneName)
    {
        ready = false;
        Instance = this;
        IsPaused = IsDead = false;
        player = FindFirstObjectByType<PlayerController>();
        health = player != null ? player.GetComponent<Health>() : null;
        if (player != null && player.GetComponent<PlayerAppearance>() == null)
            player.gameObject.AddComponent<PlayerAppearance>();
        // Keep the uploaded interface as inactive reference material. It must not
        // intercept touches or create a second set of virtual-gamepad controls.
        foreach (Canvas old in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!old.transform.IsChildOf(transform) && old.renderMode != RenderMode.WorldSpace)
                old.gameObject.SetActive(false);
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.SetActive(false);
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        ConfigureEventSystem();
        RectTransform canvas = Rect("MobileCanvas", transform);
        Canvas cv = canvas.gameObject.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;
        CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        Stretch(canvas);
        if (sceneName == SceneNames.MainMenu || sceneName == SceneNames.Win) BuildBackground(canvas);
        safe = Rect("SafeArea", canvas);
        Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaPanel>().Apply();
        if (sceneName == SceneNames.MainMenu) BuildMenu();
        else if (sceneName == SceneNames.Win) BuildWin();
        else BuildLevel(sceneName, canvas);
        ready = true;
    }
    static void ConfigureEventSystem()
    {
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        // Default actions include touch*/position and touch*/press and support multitouch.
        foreach (BaseInputModule old in es.GetComponents<BaseInputModule>()) old.enabled = false;
        InputSystemUIInputModule module = es.GetComponent<InputSystemUIInputModule>();
        if (module == null) module = es.gameObject.AddComponent<InputSystemUIInputModule>();
        module.enabled = false;
        module.AssignDefaultActions();
        module.pointerBehavior = UIPointerBehavior.AllPointersAsIs;
        module.enabled = true;
        es.sendNavigationEvents = true;
    }
    void BuildBackground(Transform canvas)
    {
        Image sky = Box("Sky", canvas, new Color(0.76f, 0.90f, 0.87f));
        Stretch(sky.rectTransform);
        sky.sprite = MobileSprites.Load("Mobile/Sky");
        Sprite hills = MobileSprites.Load("Mobile/Hills");
        Sprite ground = MobileSprites.Load("Mobile/Ground");
        // Enough tiles to cover wide 20:9 phones while matching the original pixel art.
        for (int i = 0; i < 18; i++)
        {
            Image hill = Box("Hill", canvas, Color.white);
            hill.sprite = hills;
            Place(hill.rectTransform, new Vector2(0.5f, 0), new Vector2((i - 8.5f) * 192, 192), new Vector2(192, 192));
            Image floor = Box("Ground", canvas, Color.white);
            floor.sprite = ground;
            Place(floor.rectTransform, new Vector2(0.5f, 0), new Vector2((i - 8.5f) * 192, 0), new Vector2(192, 192));
        }
        Image shade = Box("BackgroundShade", canvas, new Color(0.02f, 0.12f, 0.14f, 0.25f));
        Stretch(shade.rectTransform);
    }
    void BuildMenu()
    {
        MainMenu menu = FindFirstObjectByType<MainMenu>();
        if (menu == null) menu = new GameObject("MenuManager").AddComponent<MainMenu>();
        // The dedicated settings page writes GameSettings directly.
        menu.hpSlider = menu.cooldownSlider = null;
        menu.hazardsToggle = menu.enemiesToggle = null;
        menu.hpText = menu.cooldownText = null;
        menu.firstLevelName = SceneNames.EnemyLab;
        Image card = Box("MenuCard", safe, Ink);
        menuCard = card.gameObject;
        Place(card.rectTransform, Center, Vector2.zero, new Vector2(1120, 860));
        Transform root = card.transform;
        Label("Eyebrow", root, "SMALL HERO. BIG ADVENTURE.", 36, new Vector2(0, 340), new Vector2(1000, 60)).color = Mint;
        Label("Title", root, GameTitle, 100, new Vector2(0, 230), new Vector2(1000, 140)).color = Gold;
        Label("Pitch", root, "Collect coins. Find the portal.", 36, new Vector2(0, 130), new Vector2(1000, 65));
        Image portrait = Box("ExplorerPortrait", root, Color.white);
        portrait.sprite = MobileSprites.Load("Mobile/ExplorerIdle");
        portrait.preserveAspect = true;
        Place(portrait.rectTransform, Center, new Vector2(-330, -65), new Vector2(220, 220));
        Label("ExplorerName", root, "NOVA", 42, new Vector2(-330, -220), new Vector2(300, 70)).color = Gold;
        ButtonFor(root, "PLAY", new Vector2(190, 10), new Vector2(540, 140), Mint, menu.Play);
        ButtonFor(root, "SETTINGS", new Vector2(190, -155), new Vector2(540, 120), Cream, () => ShowSettings(true));
        ButtonFor(root, "QUIT", new Vector2(190, -300), new Vector2(540, 120), Gold, menu.Quit);
        Label("MenuHint", safe, Application.isMobilePlatform ? "MOVE + JUMP + FIRE" : "A / D  MOVE     SPACE  JUMP     X  FIRE", 36,
              new Vector2(0, -490), new Vector2(1250, 60));
        BuildSettings();
    }
    void BuildSettings()
    {
        Image card = Box("SettingsCard", safe, Ink);
        settingsCard = card.gameObject;
        Place(card.rectTransform, Center, Vector2.zero, new Vector2(1120, 900));
        Transform root = card.transform;
        Label("Title", root, "SETTINGS", 72, new Vector2(0, 340), new Vector2(1000, 110)).color = Gold;
        settingsHealth = Label("Health", root, "", 40, new Vector2(0, 180), new Vector2(600, 80));
        ButtonFor(root, "HP -", new Vector2(-390, 180), new Vector2(180, 120), Cream, null);
        ButtonFor(root, "HP +", new Vector2(390, 180), new Vector2(180, 120), Mint, null);
        settingsRate = Label("Rate", root, "", 40, new Vector2(0, 30), new Vector2(600, 80));
        ButtonFor(root, "RATE -", new Vector2(-390, 30), new Vector2(180, 120), Cream, null);
        ButtonFor(root, "RATE +", new Vector2(390, 30), new Vector2(180, 120), Mint, null);
        Button hazards = ButtonFor(root, "HAZARDS", new Vector2(-250, -135), new Vector2(460, 120), Cream, null);
        settingsHazards = hazards.GetComponentInChildren<TMP_Text>();
        Button enemies = ButtonFor(root, "ENEMIES", new Vector2(250, -135), new Vector2(460, 120), Cream, null);
        settingsEnemies = enemies.GetComponentInChildren<TMP_Text>();
        ButtonFor(root, "BACK", new Vector2(0, -310), new Vector2(500, 130), Gold, () => ShowSettings(false));
        RefreshSettings();
        settingsCard.SetActive(false);
    }
    public void ShowSettings(bool visible)
    {
        if (settingsCard == null) return;
        RefreshSettings();
        settingsCard.SetActive(visible);
        menuCard.SetActive(!visible);
    }
    public void ChangeSetting(UIActionButton.Command command)
    {
        switch (command)
        {
            case UIActionButton.Command.HealthDown: GameSettings.playerMaxHp = Mathf.Max(1, GameSettings.playerMaxHp - 1); break;
            case UIActionButton.Command.HealthUp: GameSettings.playerMaxHp = Mathf.Min(5, GameSettings.playerMaxHp + 1); break;
            case UIActionButton.Command.RateDown: GameSettings.fireCooldown = Mathf.Max(0.1f, GameSettings.fireCooldown - 0.1f); break;
            case UIActionButton.Command.RateUp: GameSettings.fireCooldown = Mathf.Min(1.5f, GameSettings.fireCooldown + 0.1f); break;
            case UIActionButton.Command.Hazards: GameSettings.hazardsEnabled = !GameSettings.hazardsEnabled; break;
            case UIActionButton.Command.Enemies: GameSettings.enemiesEnabled = !GameSettings.enemiesEnabled; break;
        }
        RefreshSettings();
    }
    void RefreshSettings()
    {
        if (settingsHealth != null) settingsHealth.text = "HEALTH  " + GameSettings.playerMaxHp;
        if (settingsRate != null) settingsRate.text = "SHOT DELAY  " + GameSettings.fireCooldown.ToString("0.0") + "s";
        if (settingsHazards != null) settingsHazards.text = "HAZARDS  " + (GameSettings.hazardsEnabled ? "ON" : "OFF");
        if (settingsEnemies != null) settingsEnemies.text = "ENEMIES  " + (GameSettings.enemiesEnabled ? "ON" : "OFF");
    }
    public void ShowMessage(string text)
    {
        foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
            if (label.name == "MenuHint") label.text = text;
    }
    void BuildWin()
    {
        Image card = Box("WinCard", safe, Ink);
        Place(card.rectTransform, Center, Vector2.zero, new Vector2(1000, 640));
        Label("Title", card.transform, "YOU WIN!", 100, new Vector2(0, 180), new Vector2(900, 140)).color = Gold;
        Label("Hint", card.transform, "All portals cleared. The boss is defeated.", 40,
              new Vector2(0, 40), new Vector2(920, 100));
        ButtonFor(card.transform, "PLAY AGAIN", new Vector2(-225, -155), new Vector2(410, 140), Mint,
                  () => Load(SceneNames.Level1));
        ButtonFor(card.transform, "MAIN MENU", new Vector2(225, -155), new Vector2(410, 140), Gold,
                  () => Load(SceneNames.MainMenu));
    }
    void BuildLevel(string sceneName, Transform canvas)
    {
        hud = Rect("HUD", safe).gameObject;
        Stretch((RectTransform)hud.transform);
        Image hpPlate = Box("HealthPlate", hud.transform, Ink);
        Place(hpPlate.rectTransform, new Vector2(0, 1), new Vector2(225, -85), new Vector2(390, 120));
        int max = health != null ? (health.useMenuSettings ? GameSettings.playerMaxHp : health.maxHp) : 3;
        max = Mathf.Clamp(max, 1, 5);
        hearts = new Image[max];
        for (int i = 0; i < max; i++)
        {
            hearts[i] = Box("Heart" + i, hpPlate.transform, Color.white);
            hearts[i].sprite = MobileSprites.Load("Mobile/Heart");
            hearts[i].preserveAspect = true;
            Place(hearts[i].rectTransform, Center, new Vector2((i - (max - 1) / 2f) * 62, 20), new Vector2(50, 50));
        }
        healthLabel = Label("HealthValue", hpPlate.transform, "HP " + max + " / " + max, 36,
                            new Vector2(0, -40), new Vector2(400, 50));
        Image coinPlate = Box("CoinPlate", hud.transform, Ink);
        Place(coinPlate.rectTransform, new Vector2(1, 1), new Vector2(-400, -85), new Vector2(320, 120));
        TMP_Text coins = Label("CoinText", coinPlate.transform, "Coins: 0", 40, Vector2.zero, new Vector2(300, 90));
        LevelManager manager = FindFirstObjectByType<LevelManager>();
        if (manager != null) { manager.coinText = sceneName == SceneNames.Boss ? null : coins; manager.hpText = null; }
        if (sceneName == SceneNames.Boss) coins.text = "FINAL BOSS";
        Button pause = ButtonFor(hud.transform, "PAUSE", Vector2.zero, new Vector2(170, 120), Gold,
                                () => SetPaused(true));
        Place((RectTransform)pause.transform, new Vector2(1, 1), new Vector2(-125, -85), new Vector2(170, 120));
        controls = Rect("TouchControls", safe).gameObject;
        Stretch((RectTransform)controls.transform);
        Image stick = Box("TouchStick", controls.transform, new Color(0.05f, 0.15f, 0.18f, 0.65f), true);
        stick.sprite = MobileSprites.Load("Mobile/Circle");
        Place(stick.rectTransform, Vector2.zero, new Vector2(210, 220), new Vector2(320, 320));
        Image knob = Box("Knob", stick.transform, Mint);
        knob.sprite = stick.sprite;
        Place(knob.rectTransform, Center, Vector2.zero, new Vector2(130, 130));
        TouchStick touch = stick.gameObject.AddComponent<TouchStick>();
        touch.player = player; touch.knob = knob.rectTransform; touch.radius = 90;
        Label("MoveHint", controls.transform, "MOVE", 36, Vector2.zero, new Vector2(240, 60));
        RectTransform hint = (RectTransform)controls.transform.Find("MoveHint");
        Place(hint, Vector2.zero, new Vector2(210, 30), new Vector2(240, 50));
        TouchButton("JUMP", new Vector2(-160, 200), Mint, TouchActionButton.ActionKind.Jump);
        TouchButton("FIRE", new Vector2(-410, 145), Gold, TouchActionButton.ActionKind.Fire);
        Boss boss = FindFirstObjectByType<Boss>();
        if (boss != null)
        {
            Slider bar = MakeSlider(hud.transform, Vector2.zero, 0, boss.maxHp, true, boss.maxHp);
            Place((RectTransform)bar.transform, new Vector2(0.5f, 1), new Vector2(0, -240), new Vector2(600, 120));
            bar.interactable = false;
            if (bar.handleRect != null) bar.handleRect.gameObject.SetActive(false);
            boss.hpBar = bar;
            Label("BossLabel", bar.transform, "BOSS", 36, Vector2.zero, new Vector2(500, 60));
        }
        BuildModal(canvas);
    }
    void TouchButton(string text, Vector2 position, Color color, TouchActionButton.ActionKind kind)
    {
        Image image = Box(text + "Button", controls.transform, color, true);
        image.sprite = MobileSprites.Load("Mobile/Circle");
        image.type = Image.Type.Simple;
        Place(image.rectTransform, new Vector2(1, 0), position, new Vector2(220, 220));
        Label("Label", image.transform, text, 40, Vector2.zero, new Vector2(210, 80)).color = Ink;
        TouchActionButton button = image.gameObject.AddComponent<TouchActionButton>();
        button.action = kind; button.player = player;
        button.shooter = player != null ? player.GetComponent<PlayerShooter>() : null;
    }
    void BuildModal(Transform canvas)
    {
        // Full-screen shade blocks the game. Its text and buttons use another safe panel.
        Image shade = Box("PauseOverlay", canvas, new Color(0.02f, 0.08f, 0.10f, 0.85f), true);
        Stretch(shade.rectTransform);
        modal = shade.gameObject;
        RectTransform modalSafe = Rect("SafeArea", modal.transform);
        Stretch(modalSafe);
        modalSafe.gameObject.AddComponent<SafeAreaPanel>().Apply();
        Image card = Box("PauseCard", modalSafe, Ink);
        Place(card.rectTransform, Center, Vector2.zero, new Vector2(900, 900));
        modalTitle = Label("Title", card.transform, "PAUSED", 88, new Vector2(0, 320), new Vector2(840, 130));
        modalTitle.color = Gold;
        modalHint = Label("Hint", card.transform, "Take a breath. Your adventure can wait.", 36,
                          new Vector2(0, 210), new Vector2(840, 90));
        resumeButton = ButtonFor(card.transform, "RESUME", new Vector2(0, 65), new Vector2(620, 140), Mint,
                                 () => SetPaused(false));
        ButtonFor(card.transform, "RESTART", new Vector2(0, -105), new Vector2(620, 140), Gold, Restart);
        ButtonFor(card.transform, "MAIN MENU", new Vector2(0, -275), new Vector2(620, 140), Cream,
                  () => Load(SceneNames.MainMenu));
        modal.SetActive(false);
    }
    public void SetPaused(bool paused)
    {
        if (IsDead || modal == null) return;
        IsPaused = paused;
        ResetControls();
        modalTitle.text = "PAUSED";
        modalHint.text = "Take a breath. Your adventure can wait.";
        resumeButton.gameObject.SetActive(true);
        modal.SetActive(paused);
        if (controls != null) controls.SetActive(!paused);
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
        EventSystem.current?.SetSelectedGameObject(null);
    }
    public void ShowDeath()
    {
        if (modal == null) return;
        IsDead = true; IsPaused = false;
        ResetControls();
        modalTitle.text = "TRY AGAIN";
        modalHint.text = "Every run is a new chance.";
        resumeButton.gameObject.SetActive(false);
        controls.SetActive(false);
        modal.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }
    void ResetControls()
    {
        if (player != null) player.ResetInput();
        foreach (TouchStick stick in GetComponentsInChildren<TouchStick>(true)) stick.ResetInput();
        foreach (TouchActionButton button in GetComponentsInChildren<TouchActionButton>(true)) button.ResetInput();
    }
    void Update()
    {
        if (!ready) return;
        if (health != null && hearts != null)
        {
            for (int i = 0; i < hearts.Length; i++)
                hearts[i].color = i < health.CurrentHp ? Color.white : new Color(0.35f, 0.4f, 0.4f, 0.5f);
            if (healthLabel != null) healthLabel.text = "HP " + health.CurrentHp + " / " + health.maxHp;
        }
        if (player != null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetPaused(!IsPaused);
    }
    void OnApplicationPause(bool paused) { if (paused && ready && player != null && !IsDead) SetPaused(true); }
    void OnApplicationFocus(bool focused) { if (!focused && ready && player != null && !IsDead) SetPaused(true); }
    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
    public void ShowQuitNotice()
    {
        foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
            if (label.name == "MenuHint") label.text = "Use Home or close the browser tab to exit.";
    }
    public void Restart() { Load(SceneManager.GetActiveScene().name); }
    static void Load(string scene)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameScenes.Load(scene);
    }
    public static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = Center; rt.anchoredPosition = position; rt.sizeDelta = size;
    }
    static Image Box(string name, Transform parent, Color color, bool raycast = false)
    {
        Image image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = raycast;
        if (name.EndsWith("Card") || name.EndsWith("Plate") || name.EndsWith("Button"))
        { image.sprite = UIShapes.Panel; image.type = Image.Type.Sliced; }
        return image;
    }
    static TMP_Text Label(string name, Transform parent, string text, float size, Vector2 pos, Vector2 box)
    {
        RectTransform rt = Rect(name, parent);
        Place(rt, Center, pos, box);
        TextMeshProUGUI label = rt.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        label.text = text; label.fontSize = size; label.color = Cream;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;
        label.enableAutoSizing = true;
        label.fontSizeMin = 36;
        label.fontSizeMax = size;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        return label;
    }
    static Button ButtonFor(Transform parent, string text, Vector2 pos, Vector2 size, Color color, UnityAction action)
    {
        Image image = Box(text + "Button", parent, color, true);
        Place(image.rectTransform, Center, pos, size);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
        string key = text.Replace(" ", "");
        UIActionButton.Command command;
        bool known = true;
        switch (key)
        {
            case "PLAY": command = UIActionButton.Command.Play; break;
            case "PLAYAGAIN": command = UIActionButton.Command.Replay; break;
            case "QUIT": command = UIActionButton.Command.Quit; break;
            case "SETTINGS": command = UIActionButton.Command.Settings; break;
            case "BACK": command = UIActionButton.Command.Back; break;
            case "PAUSE": command = UIActionButton.Command.Pause; break;
            case "RESUME": command = UIActionButton.Command.Resume; break;
            case "RESTART": command = UIActionButton.Command.Restart; break;
            case "MAINMENU": command = UIActionButton.Command.Menu; break;
            case "HP-": command = UIActionButton.Command.HealthDown; break;
            case "HP+": command = UIActionButton.Command.HealthUp; break;
            case "RATE-": command = UIActionButton.Command.RateDown; break;
            case "RATE+": command = UIActionButton.Command.RateUp; break;
            case "HAZARDS": command = UIActionButton.Command.Hazards; break;
            case "ENEMIES": command = UIActionButton.Command.Enemies; break;
            default: known = false; command = UIActionButton.Command.Play; break;
        }
        if (known)
        {
            UIActionButton handler = image.gameObject.AddComponent<UIActionButton>();
            handler.command = command;
            handler.Bind();
        }
        else if (action != null) button.onClick.AddListener(action);
        Label("Label", image.transform, text, 42, Vector2.zero, size - new Vector2(20, 10)).color = Ink;
        return button;
    }
    static Slider MakeSlider(Transform parent, Vector2 pos, float min, float max, bool whole, float value)
    {
        Image hit = Box("Slider", parent, new Color(0, 0, 0, 0), true);
        Place(hit.rectTransform, Center, pos, new Vector2(400, 120));
        Image track = Box("Track", hit.transform, Ink);
        track.rectTransform.anchorMin = new Vector2(0, 0.5f);
        track.rectTransform.anchorMax = new Vector2(1, 0.5f);
        track.rectTransform.sizeDelta = new Vector2(-40, 24);
        RectTransform fillArea = Rect("FillArea", hit.transform);
        fillArea.anchorMin = new Vector2(0, 0.5f); fillArea.anchorMax = new Vector2(1, 0.5f);
        fillArea.sizeDelta = new Vector2(-40, 24);
        Image fill = Box("Fill", fillArea, Gold);
        Stretch(fill.rectTransform);
        RectTransform handleArea = Rect("HandleArea", hit.transform);
        handleArea.anchorMin = new Vector2(0, 0.5f); handleArea.anchorMax = new Vector2(1, 0.5f);
        handleArea.sizeDelta = new Vector2(-120, 0);
        Image handle = Box("Handle", handleArea, Gold, true);
        handle.rectTransform.sizeDelta = new Vector2(72, 72);
        Slider slider = hit.gameObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = whole; slider.value = value;
        return slider;
    }
    static Toggle MakeToggle(Transform parent, string text, Vector2 pos, bool on)
    {
        Image background = Box(text + "Toggle", parent, new Color(0.17f, 0.28f, 0.29f), true);
        Place(background.rectTransform, Center, pos, new Vector2(420, 120));
        Image check = Box("Check", background.transform, Mint);
        Place(check.rectTransform, new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(72, 72));
        TMP_Text label = Label("Label", background.transform, text, 36, new Vector2(50, 0), new Vector2(300, 100));
        label.color = Cream;
        Toggle toggle = background.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = background; toggle.graphic = check; toggle.isOn = on;
        return toggle;
    }
}
