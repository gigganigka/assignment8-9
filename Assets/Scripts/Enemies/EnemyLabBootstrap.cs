using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// This component lives only in enemy_lab, a copy of the original level scene.
// It preserves the real player, camera, UI and audio and replaces the world with
// a flat encounter. The other campaign scenes are unaffected.
[DefaultExecutionOrder(-1000)]
public sealed class EnemyLabBootstrap : MonoBehaviour
{
    [SerializeField] Vector2 playerStart = new Vector2(-11, 1.2f);
    [SerializeField] Vector2 floorCenter = new Vector2(8, -0.5f);
    [SerializeField] Vector2 floorSize = new Vector2(48, 1);
    [SerializeField] Vector2[] enemyPositions = { new Vector2(-2, 0.7f), new Vector2(10, 0.7f), new Vector2(21, 0.7f) };
    [SerializeField] float patrolRadius = 2f;
    [SerializeField] Vector2[] covers = { new Vector2(5, 0.6f), new Vector2(15, 0.6f) };
    [SerializeField] Vector2 coverSize = new Vector2(0.8f, 1.2f);
    [SerializeField] float exitX = 29f;
    [SerializeField] float fallY = -5f;
    [SerializeField] bool debugStateLabels = true;
    public EnemyBrain2D[] Enemies { get; private set; }
    public float ExitX => exitX;
    PlayerController hero;
    Health health;
    LevelManager manager;
    TMP_Text finishLabel;
    Sprite groundSprite;

    void Awake()
    {
        hero = FindFirstObjectByType<PlayerController>();
        if (hero == null) { Debug.LogError("Enemy Lab requires the original Player."); enabled = false; return; }
        health = hero.GetComponent<Health>();
        GameSettings.enemiesEnabled = true; // this assessment level always contains all three enemies
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            if (root == gameObject || Keep(root)) continue;
            root.SetActive(false); Destroy(root);
        }
        hero.transform.position = playerStart;
        Rigidbody2D playerBody = hero.GetComponent<Rigidbody2D>();
        playerBody.linearVelocity = Vector2.zero;
        Camera camera = Camera.main;
        if (camera != null) camera.transform.position = new Vector3(playerStart.x, 2, -10);
        groundSprite = MobileSprites.Load("EnemyLab/Ground");
        MakeBlock("Lab floor", floorCenter, floorSize, new Color(0.16f, 0.3f, 0.32f));
        foreach (Vector2 pos in covers) MakeBlock("Sight-blocking cover", pos, coverSize, new Color(0.35f, 0.52f, 0.55f));
        Enemies = new EnemyBrain2D[3];
        Enemies[0] = Create<PatrollerBrain>("PATROLLER", enemyPositions[0], "EnemyLab/Patroller");
        Enemies[1] = Create<ShooterBrain>("SHOOTER", enemyPositions[1], "EnemyLab/Shooter");
        Enemies[2] = Create<ChargerBrain>("CHARGER", enemyPositions[2], "EnemyLab/Charger");
        manager = FindFirstObjectByType<LevelManager>();
        if (manager != null) manager.coinsRequired = 0;
        finishLabel = WorldLabel("Exit sign", new Vector2(exitX, 2), "DEFEAT ALL THREE\nTHEN REACH THE FLAG");
        MakeBlock("Exit flag", new Vector2(exitX, 0.8f), new Vector2(0.2f, 1.6f), Color.green);
        WorldLabel("Instructions", new Vector2(-9, 3), "ENEMY LAB\nRed = attack warning. Jump / shoot / use cover.");
    }
    bool Keep(GameObject root)
    {
        return root.GetComponent<PlayerController>() != null || root.GetComponent<Camera>() != null ||
               root.GetComponent<Canvas>() != null || root.GetComponent<EventSystem>() != null ||
               root.GetComponent<Music>() != null || root.GetComponent<MobileGameUI>() != null ||
               root.GetComponent<DeathScreen>() != null || root.GetComponent<LevelManager>() != null;
    }
    void Update()
    {
        if (hero == null || health == null || health.CurrentHp <= 0 || Time.timeScale <= 0) return;
        if (hero.transform.position.y < fallY) { health.Die(); return; }
        int remaining = 0;
        foreach (EnemyBrain2D enemy in Enemies) if (enemy != null) remaining++;
        bool cleared = remaining == 0;
        if (manager != null && manager.coinText != null) manager.coinText.text = "ENEMIES  " + remaining + " / 3";
        if (finishLabel != null) finishLabel.text = cleared ? "CLEAR!\nREACH THE FLAG" : "DEFEAT ALL THREE\nTHEN REACH THE FLAG";
        if (cleared && hero.transform.position.x >= exitX) GameScenes.Load(SceneNames.Level1);
    }
    T Create<T>(string title, Vector2 position, string spritePath) where T : EnemyBrain2D
    {
        GameObject go = new GameObject(title);
        go.SetActive(false);
        go.transform.SetParent(transform);
        go.transform.position = position;
        go.layer = 7;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = MobileSprites.Load(spritePath);
        renderer.sortingOrder = 5;
        // Keep artwork bright, sharing the existing world material if available.
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader != null)
        {
            Material material = new Material(shader);
            renderer.sharedMaterial = material;
            go.AddComponent<EnemyLabMaterial>().owned = material;
        }
        if (renderer.sprite != null)
            go.transform.localScale = Vector3.one * (1f / Mathf.Max(renderer.sprite.bounds.size.y, 0.01f));
        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        Vector3 scale = go.transform.localScale;
        box.size = new Vector2(0.65f / scale.x, 0.9f / scale.y);
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 3;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        T brain = go.AddComponent<T>();
        Transform[] points = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            points[i] = new GameObject(title + " waypoint " + i).transform;
            points[i].SetParent(transform);
            points[i].position = position + Vector2.right * patrolRadius * (i == 0 ? -1 : 1);
        }
        brain.SetWaypoints(points);
        brain.tuning = Resources.Load<EnemyTuning>(spritePath + "Settings");
        if (debugStateLabels)
        {
            TMP_Text label = WorldLabel(title + " state", position + Vector2.up * 1.2f, title);
            label.transform.SetParent(go.transform, true);
        }
        go.SetActive(true);
        return brain;
    }
    void MakeBlock(string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform); go.transform.position = position;
        go.layer = 6;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = groundSprite; renderer.color = color;
        if (groundSprite != null)
            go.transform.localScale = new Vector3(size.x / groundSprite.bounds.size.x, size.y / groundSprite.bounds.size.y, 1);
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(size.x / go.transform.localScale.x, size.y / go.transform.localScale.y);
    }
    TMP_Text WorldLabel(string name, Vector2 position, string text)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform); go.transform.position = position;
        TextMeshPro label = go.AddComponent<TextMeshPro>();
        label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        label.text = text; label.fontSize = 2.5f; label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(7, 1.2f);
        label.sortingOrder = 10;
        return label;
    }
}
