// Положить в Assets/Editor.
// Меню Tools: сам открывает нужную сцену и строит в ней уровень (земля, платформы, монетки,
// шипы, враги, портал, HUD, камера, герой). Это инструмент редактора, в игру не попадает.
// Всё, что он создаёт, потом можно двигать/удалять руками.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

public static class LevelBuilder
{
    const float T = 0.6f;            // размер одной плитки в мире (как у твоей земли)
    const float PlayerScale = 2.5f;  // во сколько раз увеличиваем героя (он был крошечный)
    static readonly Color Sky = new Color(0.70f, 0.88f, 0.92f);

    class LevelDef
    {
        public string scene;
        public string nextScene;
        public int[][] ground;        // { от, до } в плитках: куски земли, между ними ямы
        public float[][] platforms;   // { левая плитка, ширина, высота над землёй } в плитках
        public float[][] movers;      // { левая плитка, ширина, высота, на сколько плиток ездит }
        public float[][] coins;       // { x, y } в плитках
        public int[] spikes;          // x в плитках
        public int[] enemies;         // x в плитках
        public int[] hearts;          // x в плитках
        public int portalX;
        public int playerX = 2;
        public bool boss;
        public int bossX;
    }

    // =====================  КНОПКИ В МЕНЮ TOOLS  =====================

    [MenuItem("Tools/0. Add Scenes To Build")]
    public static void AddScenesToBuild()
    {
        string[] names = { SceneNames.MainMenu, SceneNames.Level1, SceneNames.Level2, SceneNames.Boss, SceneNames.Win };
        var list = new List<EditorBuildSettingsScene>();
        string missing = "";
        foreach (string n in names)
        {
            string p = FindScenePath(n);
            if (p == null) { missing += n + " "; continue; }
            list.Add(new EditorBuildSettingsScene(p, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("Сцены добавлены в сборку по порядку: " + string.Join(", ", names) +
                  (missing != "" ? "  НЕ НАЙДЕНЫ: " + missing : ""));
    }

    [MenuItem("Tools/1. Build Level 1")]
    public static void BuildLevel1()
    {
        Build(new LevelDef
        {
            scene = SceneNames.Level1,
            nextScene = SceneNames.Level2,
            ground = new[] { new[] { 0, 16 }, new[] { 19, 32 }, new[] { 35, 50 }, new[] { 53, 72 } },
            platforms = new[] { new[] { 22f, 3f, 2f }, new[] { 27f, 3f, 3.5f } },
            movers = new float[0][],
            coins = new[] { new[] { 10f, 1.3f }, new[] { 23.5f, 3.4f }, new[] { 28.5f, 4.9f },
                            new[] { 42.5f, 1.9f }, new[] { 58f, 1.3f } },
            spikes = new[] { 42, 60 },
            enemies = new int[0],
            hearts = new[] { 47 },
            portalX = 70,
            playerX = 2
        });
    }

    [MenuItem("Tools/2. Build Level 2")]
    public static void BuildLevel2()
    {
        Build(new LevelDef
        {
            scene = SceneNames.Level2,
            nextScene = SceneNames.Boss,
            ground = new[] { new[] { 0, 12 }, new[] { 15, 26 }, new[] { 29, 36 }, new[] { 44, 58 }, new[] { 61, 80 } },
            platforms = new[] { new[] { 18f, 3f, 2f }, new[] { 31f, 3f, 2.5f }, new[] { 48f, 3f, 2f }, new[] { 52f, 3f, 3.5f } },
            movers = new[] { new[] { 36.5f, 3f, 0f, 4f } },
            coins = new[] { new[] { 6f, 1.3f }, new[] { 19.5f, 3.4f }, new[] { 32.5f, 3.9f },
                            new[] { 40f, 2.5f }, new[] { 49.5f, 3.4f }, new[] { 53.5f, 5.0f } },
            spikes = new[] { 22, 54, 66 },
            enemies = new[] { 20, 47, 70 },
            hearts = new[] { 46 },
            portalX = 78,
            playerX = 2
        });
    }

    [MenuItem("Tools/3. Build Boss Level")]
    public static void BuildBossLevel()
    {
        Build(new LevelDef
        {
            scene = SceneNames.Boss,
            nextScene = SceneNames.Win,
            ground = new[] { new[] { 0, 40 } },
            platforms = new[] { new[] { 8f, 4f, 2.5f }, new[] { 26f, 4f, 2.5f } },
            movers = new float[0][],
            coins = new float[0][],
            spikes = new int[0],
            enemies = new int[0],
            hearts = new[] { 18 },
            portalX = -1,
            playerX = 3,
            boss = true,
            bossX = 34
        });
    }

    [MenuItem("Tools/4. Build Win Screen")]
    public static void BuildWinScreen()
    {
        if (!OpenScene(SceneNames.Win)) return;
        foreach (string n in new[] { "WinCanvas", "WinManager", "EventSystem" }) DestroyIfExists(n);
        SetupCameraColor(new Color(0.10f, 0.35f, 0.25f));

        var res = new TMP_DefaultControls.Resources();
        res.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var canvasGO = new GameObject("WinCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = canvasGO.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;

        var title = TMP_DefaultControls.CreateText(res);
        title.name = "Title";
        title.transform.SetParent(canvasGO.transform, false);
        PlaceCenter(title, new Vector2(0, 150), new Vector2(1200, 220));
        var tt = title.GetComponent<TMP_Text>();
        tt.text = "YOU WIN!"; tt.fontSize = 160; tt.alignment = TextAlignmentOptions.Center; tt.color = Color.white;

        var btnGO = TMP_DefaultControls.CreateButton(res);
        btnGO.name = "BackButton";
        btnGO.transform.SetParent(canvasGO.transform, false);
        PlaceCenter(btnGO, new Vector2(0, -80), new Vector2(560, 120));
        var bt = btnGO.GetComponentInChildren<TMP_Text>();
        bt.text = "BACK TO MENU"; bt.fontSize = 46;

        var mgr = new GameObject("WinManager");
        var wm = mgr.AddComponent<WinMenu>();
        UnityEventTools.AddPersistentListener(btnGO.GetComponent<Button>().onClick, wm.BackToMenu);

        SetupEventSystem();
        SaveScene("Win");
    }

    // =====================  ПОСТРОЕНИЕ УРОВНЯ  =====================

    static void Build(LevelDef d)
    {
        if (!OpenScene(d.scene)) return;

        int gl = LayerMask.NameToLayer("Ground");
        if (gl < 0)
        {
            Debug.LogError("В проекте нет слоя Ground. Edit -> Project Settings -> Tags and Layers -> User Layer 6 = Ground, и запусти снова.");
            return;
        }

        foreach (string n in new[] { "LevelRoot", "HUD", "LevelManager", "Ground", "KillZone" }) DestroyIfExists(n);
        Transform root = new GameObject("LevelRoot").transform;

        Sprite sGrass = S("Tiles", 2), sDirt = S("Tiles", 4), sPlat = S("Tiles", 42);
        Sprite sCoin = S("Tiles", 151), sSpike = S("Tiles", 68), sHeart = S("Tiles", 44), sFlag = S("Tiles", 111);
        Sprite sEnemy = S("Characters", 16), sBoss = S("Characters", 22);

        // ---- земля: куски с ямами между ними. Один коллайдер на кусок ----
        int levelEnd = 0;
        foreach (int[] seg in d.ground)
        {
            int a = seg[0], b = seg[1];
            levelEnd = Mathf.Max(levelEnd, b);
            float width = (b - a) * T;
            var g = new GameObject("GroundSegment");
            g.transform.SetParent(root, false);
            g.transform.position = new Vector3(a * T + width / 2f, -T * 1.5f, 0f);
            g.layer = gl;
            var col = g.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, T * 3f);
            for (int i = a; i < b; i++)
                for (int row = 0; row < 3; row++)
                    Tile(g.transform, row == 0 ? sGrass : sDirt, new Vector3(i * T + T / 2f, -T / 2f - row * T, 0f), T, 0);
        }

        // ---- платформы ----
        foreach (float[] p in d.platforms)
            MakePlatform(root, sPlat, p[0], p[1], p[2], gl, "Platform");

        // ---- движущиеся платформы ----
        foreach (float[] m in d.movers)
        {
            GameObject mp = MakePlatform(root, sPlat, m[0], m[1], m[2], gl, "MovingPlatform");
            var rb = mp.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var script = mp.AddComponent<MovingPlatform>();
            script.moveOffset = new Vector2(m[3] * T, 0f);
            script.speed = 0.4f;
        }

        // ---- монетки ----
        foreach (float[] c in d.coins)
        {
            GameObject go = Tile(root, sCoin, new Vector3(c[0] * T, c[1] * T, 0f), 0.35f, 1);
            go.name = "Coin";
            var cc = go.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = sCoin.bounds.extents.x;
            go.AddComponent<Collectible>();
        }

        // ---- шипы (Hazard) ----
        foreach (int x in d.spikes)
        {
            GameObject go = Tile(root, sSpike, new Vector3(x * T + T / 2f, 0f, 0f), T, 1);
            go.name = "Spikes";
            float h = sSpike.bounds.size.y * go.transform.localScale.y;
            go.transform.position = new Vector3(x * T + T / 2f, h / 2f, 0f);
            var bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(sSpike.bounds.size.x * 0.8f, sSpike.bounds.size.y * 0.6f);
            bc.offset = new Vector2(0f, -sSpike.bounds.size.y * 0.2f);
            go.AddComponent<Hazard>();
        }

        // ---- враги-патрули (наследуют Hazard) ----
        foreach (int x in d.enemies)
        {
            GameObject go = Tile(root, sEnemy, new Vector3(x * T + T / 2f, 0f, 0f), 0.55f, 2);
            go.name = "Enemy";
            float h = sEnemy.bounds.size.y * go.transform.localScale.y;
            go.transform.position = new Vector3(x * T + T / 2f, h / 2f, 0f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = sEnemy.bounds.size;
            var pe = go.AddComponent<PatrolEnemy>();
            pe.speed = 1.5f;
            pe.patrolDistance = 1.5f;
            pe.hp = 2;
        }

        // ---- сердца ----
        foreach (int x in d.hearts)
        {
            GameObject go = Tile(root, sHeart, new Vector3(x * T + T / 2f, 0.45f, 0f), 0.4f, 1);
            go.name = "Heart";
            var cc = go.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = sHeart.bounds.extents.x;
            go.AddComponent<HealthPickup>();
        }

        // ---- портал-выход ----
        if (d.portalX >= 0)
        {
            GameObject go = Tile(root, sFlag, new Vector3(d.portalX * T, 0f, 0f), 0.8f, 1);
            go.name = "Portal";
            float h = sFlag.bounds.size.y * go.transform.localScale.y;
            go.transform.position = new Vector3(d.portalX * T, h / 2f, 0f);
            var bc = go.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(sFlag.bounds.size.x * 2f, sFlag.bounds.size.y);
            var portal = go.AddComponent<Portal>();
            portal.nextScene = d.nextScene;
            portal.visual = go.GetComponent<SpriteRenderer>();
        }

        // ---- зона смерти под уровнем ----
        var kz = new GameObject("KillZone");
        kz.transform.position = new Vector3(levelEnd * T / 2f, -8f, 0f);
        var kc = kz.AddComponent<BoxCollider2D>();
        kc.isTrigger = true;
        kc.size = new Vector2(levelEnd * T + 40f, 2f);
        kz.AddComponent<KillZone>();

        // ---- префабы пули и огненного шара ----
        GameObject bulletPrefab = MakeProjectilePrefab("PlayerBullet", S("Tiles", 8), 0.22f, new Color(1f, 0.9f, 0.3f), false);
        GameObject fireballPrefab = MakeProjectilePrefab("Fireball", S("Characters", 8), 0.45f, new Color(1f, 0.45f, 0.15f), true);

        // ---- герой, камера, HUD, менеджер ----
        Health health;
        GameObject player = SetupPlayer(new Vector3(d.playerX * T, 1.0f, 0f), gl, bulletPrefab, out health);
        SetupCamera(player.transform);

        int coinCount = d.coins.Length;
        TMP_Text hpText, coinText;
        MakeHud(out hpText, out coinText, coinCount > 0);

        var lmGO = new GameObject("LevelManager");
        var lm = lmGO.AddComponent<LevelManager>();
        lm.coinsRequired = coinCount;
        lm.player = health;
        lm.hpText = hpText;
        lm.coinText = coinText;

        // ---- босс ----
        if (d.boss)
        {
            GameObject bossGO = Tile(root, sBoss, new Vector3(d.bossX * T, 0f, 0f), 2.4f, 2);
            bossGO.name = "Boss";
            float h = sBoss.bounds.size.y * bossGO.transform.localScale.y;
            bossGO.transform.position = new Vector3(d.bossX * T, h / 2f, 0f);
            var bc = bossGO.AddComponent<BoxCollider2D>();
            bc.size = sBoss.bounds.size;
            var fp = new GameObject("FirePoint");
            fp.transform.SetParent(bossGO.transform, false);
            fp.transform.localPosition = new Vector3(-0.09f, 0.02f, 0f); // в единицах спрайта: у левого края босса (босс увеличен в ~10 раз!)
            var boss = bossGO.AddComponent<Boss>();
            boss.fireballPrefab = fireballPrefab;
            boss.firePoint = fp.transform;
            boss.player = player.transform;
            boss.maxHp = 10;
            boss.hpBar = MakeBossBar();
        }

        SaveScene(d.scene);
    }

    // =====================  ГЕРОЙ, КАМЕРА, HUD  =====================

    static GameObject SetupPlayer(Vector3 pos, int gl, GameObject bulletPrefab, out Health health)
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            player.AddComponent<SpriteRenderer>().sprite = S("Characters", 7);
        }
        player.tag = "Player";
        player.transform.position = pos;
        player.transform.localScale = Vector3.one * PlayerScale;

        var sr = GetOrAdd<SpriteRenderer>(player);
        sr.sortingOrder = 5;
        Vector2 sb = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : new Vector2(0.24f, 0.24f);

        var rb = GetOrAdd<Rigidbody2D>(player);
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        foreach (Collider2D c in player.GetComponents<Collider2D>()) Object.DestroyImmediate(c);
        var cap = player.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(sb.x * 0.7f, sb.y * 0.95f);
        cap.sharedMaterial = GetNoFriction();

        Transform gc = player.transform.Find("GroundCheck");
        if (gc == null)
        {
            gc = new GameObject("GroundCheck").transform;
            gc.SetParent(player.transform, false);
        }
        gc.localPosition = new Vector3(0f, -sb.y * 0.5f - 0.01f, 0f);

        Transform fp = player.transform.Find("FirePoint");
        if (fp == null)
        {
            fp = new GameObject("FirePoint").transform;
            fp.SetParent(player.transform, false);
        }
        fp.localPosition = new Vector3(sb.x * 0.6f, 0f, 0f);

        var pc = GetOrAdd<PlayerController>(player);
        pc.groundCheck = gc;
        pc.groundLayer = (LayerMask)(1 << gl);
        pc.groundRadius = 0.1f;
        pc.moveSpeed = 4f;
        pc.jumpForce = 9f;

        health = GetOrAdd<Health>(player);

        var shooter = GetOrAdd<PlayerShooter>(player);
        shooter.bulletPrefab = bulletPrefab;
        shooter.firePoint = fp;

        if (player.GetComponent<PlayerInput>() == null)
        {
            var pi = player.AddComponent<PlayerInput>();
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Input/PlayerControls.inputactions");
            if (asset == null) Debug.LogError("Не найден Assets/Input/PlayerControls.inputactions");
            pi.actions = asset;
            pi.defaultActionMap = "Player";
            pi.notificationBehavior = PlayerNotifications.SendMessages;
        }
        return player;
    }

    static void SetupCamera(Transform target)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var cg = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cg.tag = "MainCamera";
            cam = cg.GetComponent<Camera>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Sky;
        var cf = GetOrAdd<CameraFollow>(cam.gameObject);
        cf.target = target;
        cf.offset = new Vector3(0f, 1.5f, -10f);
        cf.smoothSpeed = 5f;
        cam.transform.position = target.position + cf.offset;
    }

    static void SetupCameraColor(Color c)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = c;
    }

    static void MakeHud(out TMP_Text hp, out TMP_Text coins, bool withCoins)
    {
        var canvasGO = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = canvasGO.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        hp = HudText(canvasGO.transform, "HpText", "HP: 3", new Vector2(0f, 1f), new Vector2(40f, -30f), TextAlignmentOptions.TopLeft);
        coins = withCoins
            ? HudText(canvasGO.transform, "CoinText", "Coins: 0", new Vector2(1f, 1f), new Vector2(-40f, -30f), TextAlignmentOptions.TopRight)
            : null;
    }

    static TMP_Text HudText(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, TextAlignmentOptions align)
    {
        var go = TMP_DefaultControls.CreateText(new TMP_DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(500f, 80f);
        var t = go.GetComponent<TMP_Text>();
        t.text = text;
        t.fontSize = 56;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static Slider MakeBossBar()
    {
        var res = new DefaultControls.Resources();
        res.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        res.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        res.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        var canvas = GameObject.Find("HUD");
        var go = DefaultControls.CreateSlider(res);
        go.name = "BossHpBar";
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -120f);
        rt.sizeDelta = new Vector2(800f, 40f);
        var s = go.GetComponent<Slider>();
        s.interactable = false;
        Transform handle = go.transform.Find("Handle Slide Area");
        if (handle != null) handle.gameObject.SetActive(false);
        return s;
    }

    // =====================  ВСПОМОГАТЕЛЬНОЕ  =====================

    static GameObject Tile(Transform parent, Sprite s, Vector3 worldPos, float targetWidth, int order)
    {
        var go = new GameObject(s != null ? s.name : "Tile");
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        if (s != null) go.transform.localScale = Vector3.one * (targetWidth / s.bounds.size.x);
        go.transform.position = worldPos;
        return go;
    }

    static GameObject MakePlatform(Transform root, Sprite s, float leftTile, float wTiles, float hTiles, int gl, string name)
    {
        float cx = (leftTile + wTiles / 2f) * T;
        float cy = hTiles * T - T / 2f; // верхняя грань платформы на высоте hTiles плиток над землёй
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(cx, cy, 0f);
        go.layer = gl;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(wTiles * T, T);
        int count = Mathf.RoundToInt(wTiles);
        for (int i = 0; i < count; i++)
            Tile(go.transform, s, new Vector3(leftTile * T + i * T + T / 2f, cy, 0f), T, 0);
        return go;
    }

    static GameObject MakeProjectilePrefab(string name, Sprite s, float width, Color color, bool fireball)
    {
        Directory.CreateDirectory("Assets/Prefabs");
        string path = "Assets/Prefabs/" + name + ".prefab";
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = color;
        sr.sortingOrder = 4;
        go.transform.localScale = Vector3.one * (width / s.bounds.size.x);
        var cc = go.AddComponent<CircleCollider2D>();
        cc.isTrigger = true;
        cc.radius = s.bounds.extents.x;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (fireball)
        {
            var f = go.AddComponent<Fireball>();
            f.speed = 5f;
            f.lifeTime = 6f;   // арена длинная: за 3 секунды шар не долетал до героя
        }
        else
        {
            var b = go.AddComponent<PlayerBullet>();
            b.speed = 8f;
        }
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    static PhysicsMaterial2D GetNoFriction()
    {
        Directory.CreateDirectory("Assets/Prefabs");
        string path = "Assets/Prefabs/NoFriction.physicsMaterial2D";
        var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (pm == null)
        {
            pm = new PhysicsMaterial2D("NoFriction");
            pm.friction = 0f;
            pm.bounciness = 0f;
            AssetDatabase.CreateAsset(pm, path);
        }
        return pm;
    }

    static Sprite S(string folder, int n)
    {
        string path = "Assets/Sprites/" + folder + "/tile_" + n.ToString("0000") + ".png";
        Sprite s = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (s == null) Debug.LogError("Не найдена картинка: " + path);
        return s;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    static void PlaceCenter(GameObject go, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void SetupEventSystem()
    {
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        var old = es.GetComponent<StandaloneInputModule>();
        if (old != null) Object.DestroyImmediate(old);
        if (es.GetComponent<InputSystemUIInputModule>() == null)
            es.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    static void DestroyIfExists(string name)
    {
        GameObject go;
        while ((go = GameObject.Find(name)) != null) Object.DestroyImmediate(go);
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

    static bool OpenScene(string sceneName)
    {
        string path = FindScenePath(sceneName);
        if (path == null)
        {
            EditorUtility.DisplayDialog("Нет сцены", "Не найдена сцена с именем: " + sceneName +
                "\nПроверь имена в SceneNames.cs", "OK");
            return false;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(path);
        return true;
    }

    static void SaveScene(string label)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Готово: " + label + " построен и сохранён.");
    }
}
