using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Босс: HP, полоска здоровья, две атаки по очереди:
//   1) веер огненных шаров прямо в героя;
//   2) огненный дождь: сначала красная колонна и "!" над героем, потом сверху падают шары.
// Ниже половины HP - вторая фаза: пауза короче, шаров больше, веер шире.
// Наследует Hazard (бьёт при касании) и IDamageable (его бьёт пуля игрока).
public class Boss : Hazard, IDamageable
{
    public int maxHp = 10;                    // по заданию минимум 3 попадания
    public GameObject fireballPrefab;
    public Transform firePoint;               // откуда вылетают шары (у "рта" босса)
    public Transform player;                  // перетащи Player
    public Slider hpBar;                      // UI Slider над боссом или на экране
    public float restTime = 2.5f;             // ПАУЗА между атаками: окно, когда игрок стреляет
    public float telegraphTime = 0.8f;        // предупреждение перед веером (босс краснеет)
    public int fireballsPhase1 = 3;           // сколько шаров в веере (1-я фаза)
    public int fireballsPhase2 = 5;           // во 2-й фазе
    public float spread = 12f;                // угол между шарами в градусах

    [Header("Огненный дождь")]
    public float rainWarningTime = 1f;        // сколько висит красная колонна (успеть отбежать)
    public float rainWidth = 1.6f;            // ширина колонны в мире
    public int rainFireballs = 3;             // сколько шаров падает в колонне
    public float rainSpeed = 7f;

    public AudioClip hitSound;
    public AudioClip deathSound;
    public string winScene = SceneNames.Win;

    int hp;
    SpriteRenderer sr;
    bool dead;
    Sprite squareSprite;                      // белый квадрат для красной колонны

    protected override void Awake()
    {
        respectHazardToggle = false;          // босс не отключается переключателем Fire
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
        hp = maxHp;
        if (hpBar != null) { hpBar.maxValue = maxHp; hpBar.value = hp; }

        // Делаем спрайт-квадрат 1x1 из встроенной белой текстуры (не нужен отдельный файл)
        squareSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
    }

    void Start()
    {
        StartCoroutine(AttackLoop());
    }

    bool Phase2 { get { return hp <= maxHp / 2; } }   // ниже половины HP - вторая фаза

    IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(1.5f);
        bool fanTurn = true;                           // атаки идут по очереди: веер, дождь, веер...
        while (!dead)
        {
            // StartCoroutine внутри корутины: ждём, пока атака закончится
            if (fanTurn) yield return StartCoroutine(FanAttack());
            else yield return StartCoroutine(RainAttack());
            fanTurn = !fanTurn;

            // окно для атаки игрока
            yield return new WaitForSeconds(Phase2 ? restTime * 0.5f : restTime);
        }
    }

    // ---------- Атака 1: веер ----------
    IEnumerator FanAttack()
    {
        sr.color = Color.red;                          // предупреждение: босс краснеет
        yield return new WaitForSeconds(telegraphTime);
        sr.color = Color.white;
        if (dead) yield break;

        int count = Phase2 ? fireballsPhase2 : fireballsPhase1;
        float step = Phase2 ? spread * 1.5f : spread;  // во 2-й фазе веер шире
        FireFan(count, step);
    }

    void FireFan(int count, float angleStep)
    {
        if (player == null || fireballPrefab == null) return;
        Vector3 origin = FireOrigin();

        // Целимся в ЦЕНТР героя (центр его коллайдера), а не в точку у ног
        Vector3 target = player.position;
        Collider2D pc = player.GetComponent<Collider2D>();
        if (pc != null) target = pc.bounds.center;

        Vector2 toPlayer = (target - origin).normalized;
        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            // средний шар летит точно в героя, остальные - симметрично вверх и вниз
            float a = baseAngle + (i - (count - 1) / 2f) * angleStep;
            Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            SpawnFireball(origin, dir, 0f);
        }
    }

    // ---------- Атака 2: огненный дождь ----------
    IEnumerator RainAttack()
    {
        if (player == null || fireballPrefab == null) yield break;

        int columns = Phase2 ? 2 : 1;                  // во 2-й фазе две колонны
        float[] xs = new float[columns];
        xs[0] = player.position.x;                     // колонна там, где герой СЕЙЧАС
        if (columns > 1) xs[1] = player.position.x + (Random.value < 0.5f ? -1f : 1f) * rainWidth * 1.5f;

        // Предупреждение: красная полупрозрачная колонна + "!"
        GameObject[] warnings = new GameObject[columns];
        for (int c = 0; c < columns; c++) warnings[c] = MakeWarning(xs[c]);

        yield return new WaitForSeconds(rainWarningTime);   // время, чтобы отбежать

        for (int c = 0; c < columns; c++) Destroy(warnings[c]);
        if (dead) yield break;

        float topY = Camera.main.transform.position.y + Camera.main.orthographicSize + 0.5f;
        for (int c = 0; c < columns; c++)
        {
            for (int i = 0; i < rainFireballs; i++)
            {
                // шары равномерно по ширине колонны
                float x = xs[c] - rainWidth / 2f + rainWidth * (i + 0.5f) / rainFireballs;
                SpawnFireball(new Vector3(x, topY + i * 0.4f, 0f), Vector2.down, rainSpeed);
            }
        }
    }

    GameObject MakeWarning(float x)
    {
        float camY = Camera.main.transform.position.y;
        float h = Camera.main.orthographicSize * 2f + 2f;   // от верха до низа экрана

        GameObject col = new GameObject("RainWarning");
        col.transform.position = new Vector3(x, camY, 0f);
        col.transform.localScale = new Vector3(rainWidth, h, 1f);
        SpriteRenderer csr = col.AddComponent<SpriteRenderer>();
        csr.sprite = squareSprite;
        csr.color = new Color(1f, 0f, 0f, 0.3f);
        csr.sortingOrder = -1;                               // позади героя и платформ

        GameObject mark = new GameObject("!");
        mark.transform.SetParent(col.transform, false);
        mark.transform.position = new Vector3(x, camY + Camera.main.orthographicSize * 0.5f, 0f);
        mark.transform.localScale = new Vector3(1f / rainWidth, 1f / h, 1f); // отменяем растяжение колонны
        TextMeshPro t = mark.AddComponent<TextMeshPro>();
        t.text = "!";
        t.fontSize = 12;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.red;
        t.sortingOrder = 10;
        return col;
    }

    // ---------- Общее ----------
    Vector3 FireOrigin()
    {
        return firePoint != null ? firePoint.position : sr.bounds.center;
    }

    void SpawnFireball(Vector3 pos, Vector2 dir, float speedOverride)
    {
        GameObject f = Instantiate(fireballPrefab, pos, Quaternion.identity);
        Projectile p = f.GetComponent<Projectile>();
        if (speedOverride > 0f) p.speed = speedOverride;
        p.Launch(dir);
    }

    // Сюда приходит урон от PlayerBullet (через интерфейс IDamageable)
    public void TakeHit(int damage)
    {
        if (dead) return;
        hp -= damage;
        Sfx.Play(hitSound);
        if (hpBar != null) hpBar.value = hp;
        if (hp <= 0) StartCoroutine(Die());
    }

    IEnumerator Die()
    {
        dead = true;
        Sfx.Play(deathSound);
        sr.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        if (hpBar != null) hpBar.gameObject.SetActive(false);
        // убираем шары, которые ещё летят, чтобы герой не умер после победы
        foreach (Fireball f in FindObjectsByType<Fireball>(FindObjectsSortMode.None)) Destroy(f.gameObject);
        yield return new WaitForSeconds(1f);
        GameScenes.Load(winScene);                        // босс побеждён -> экран победы
    }
}
