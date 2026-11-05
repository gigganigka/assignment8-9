using UnityEngine;

// Враг-патруль: ходит туда-сюда, бьёт игрока (наследует Hazard), его можно убить пулей.
public class PatrolEnemy : Hazard, IDamageable
{
    public float speed = 2f;
    public float patrolDistance = 3f;   // на сколько уходит от стартовой точки
    public int hp = 2;
    public AudioClip hitSound;

    Vector2 startPos;
    int dir = 1;                        // 1 вправо, -1 влево
    SpriteRenderer sr;

    protected override void Awake()
    {
        respectHazardToggle = false;                 // враг слушает переключатель Enemies, не Fire
        if (!GameSettings.enemiesEnabled)
        {
            gameObject.SetActive(false);
            return;
        }
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
        startPos = transform.position;
    }

    void Update()
    {
        transform.Translate(Vector2.right * dir * speed * Time.deltaTime);
        float offset = transform.position.x - startPos.x;
        if (offset > patrolDistance) dir = -1;
        else if (offset < -patrolDistance) dir = 1;
        if (sr != null) sr.flipX = dir < 0;
    }

    public void TakeHit(int damage)
    {
        hp -= damage;
        Sfx.Play(hitSound);
        if (hp <= 0) Destroy(gameObject);
    }
}
