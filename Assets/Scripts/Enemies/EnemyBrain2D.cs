using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public abstract class EnemyBrain2D : MonoBehaviour, IDamageable
{
    [Header("Health and movement")]
    [SerializeField, Min(1)] protected int hitPoints = 2;
    [SerializeField, Min(0)] protected float moveSpeed = 1.6f;
    [SerializeField] protected Transform[] waypoints;
    [SerializeField, Min(0.01f)] protected float waypointTolerance = 0.2f;
    [SerializeField, Min(0.01f)] protected float ledgeProbeForward = 0.5f;
    [SerializeField, Min(0.01f)] protected float ledgeProbeDepth = 0.7f;
    [Header("Vision and attack")]
    [SerializeField, Min(0.1f)] protected float visionRange = 6f;
    [SerializeField, Range(1, 360)] protected float visionAngle = 150f;
    [SerializeField] protected Vector2 eyeOffset = new Vector2(0, 0.15f);
    [SerializeField] protected LayerMask walls = 1 << 6;
    [SerializeField, Range(0.3f, 0.8f)] protected float windUp = 0.6f;
    [SerializeField, Min(0.1f)] protected float cooldown = 1.3f;
    [SerializeField, Min(1)] protected int damage = 1;
    [SerializeField, Min(0)] protected float attackRange = 1.1f;
    [SerializeField, Range(0, 0.2f)] float screenMargin = 0.02f;
    [SerializeField, Min(0)] float hitFlashTime = 0.12f;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color warningColor = new Color(1, 0.22f, 0.1f);
    [SerializeField] string currentState;

    protected Rigidbody2D body;
    protected BoxCollider2D shape;
    protected SpriteRenderer visual;
    protected Health target;
    Collider2D targetShape;
    public EnemyTuning tuning;
    protected Camera gameCamera;
    protected float enteredAt;
    protected float moveIntent;
    protected int facing = -1;
    int waypointIndex;
    float flashUntil;
    TMP_Text stateLabel;
    public abstract string StateName { get; }
    protected abstract bool Telegraphing { get; }
    protected abstract void TickState();
    protected float StateAge => Time.time - enteredAt;
    protected Vector2 TargetPosition => targetShape.bounds.center;
    protected float DistanceToTarget => target == null ? float.PositiveInfinity : Vector2.Distance(transform.position, TargetPosition);
    public bool HasLivingTarget => target != null && targetShape != null && target.CurrentHp > 0;

    protected virtual void Awake()
    {
        if (tuning != null)
        {
            hitPoints = tuning.hitPoints; moveSpeed = tuning.moveSpeed;
            visionRange = tuning.visionRange; visionAngle = tuning.visionAngle;
            windUp = Mathf.Clamp(tuning.windUp, 0.3f, 0.8f); cooldown = Mathf.Max(0.1f, tuning.cooldown);
            damage = tuning.damage; attackRange = tuning.attackRange;
        }
        body = GetComponent<Rigidbody2D>();
        shape = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        gameCamera = Camera.main;
        PlayerController hero = FindFirstObjectByType<PlayerController>();
        if (hero != null)
        {
            target = hero.GetComponent<Health>();
            targetShape = hero.GetComponent<Collider2D>();
            foreach (Collider2D c in hero.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(shape, c);
        }
        stateLabel = GetComponentInChildren<TMP_Text>();
        enteredAt = Time.time;
        if (!GameSettings.enemiesEnabled) gameObject.SetActive(false);
    }
    void Update()
    {
        if (Time.timeScale <= 0 || !HasLivingTarget) { moveIntent = 0; return; }
        TickState();
        currentState = StateName;
        visual.color = Telegraphing ? warningColor : (Time.time < flashUntil ? Color.white : normalColor);
        visual.flipX = facing < 0;
        if (stateLabel != null)
        {
            stateLabel.text = name + "\n" + StateName + (Telegraphing ? " !" : "");
            stateLabel.color = Telegraphing ? warningColor : Color.white;
        }
    }
    void FixedUpdate()
    {
        body.linearVelocity = new Vector2(moveIntent, body.linearVelocity.y);
    }
    protected void FaceTarget()
    {
        if (target != null) facing = TargetPosition.x < transform.position.x ? -1 : 1;
    }
    protected void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) { moveIntent = 0; return; }
        Transform point = waypoints[waypointIndex];
        if (point == null) { moveIntent = 0; return; }
        float dx = point.position.x - transform.position.x;
        if (Mathf.Abs(dx) <= waypointTolerance)
        {
            waypointIndex = (waypointIndex + 1) % waypoints.Length;
            moveIntent = 0;
            return;
        }
        MoveGrounded(Mathf.Sign(dx) * moveSpeed);
    }
    protected bool MoveGrounded(float speed)
    {
        if (Mathf.Abs(speed) < 0.001f) { moveIntent = 0; return true; }
        facing = speed < 0 ? -1 : 1;
        Vector2 foot = new Vector2(shape.bounds.center.x + facing * ledgeProbeForward, shape.bounds.min.y + 0.1f);
        bool ground = Physics2D.Raycast(foot, Vector2.down, ledgeProbeDepth, walls);
        moveIntent = ground ? speed : 0;
        return ground;
    }
    public bool IsOnScreen(Vector3 point)
    {
        if (gameCamera == null) return false;
        Vector3 v = gameCamera.WorldToViewportPoint(point);
        return v.z > 0 && v.x >= screenMargin && v.x <= 1 - screenMargin &&
               v.y >= screenMargin && v.y <= 1 - screenMargin;
    }
    public bool AttackVisible => HasLivingTarget && IsOnScreen(transform.position) && IsOnScreen(TargetPosition);
    protected bool CanSeeTarget()
    {
        if (!HasLivingTarget) return false;
        Vector2 eye = (Vector2)transform.position + eyeOffset;
        Vector2 delta = TargetPosition - eye;
        if (delta.magnitude > visionRange) return false;
        if (Vector2.Angle(Vector2.right * facing, delta) > visionAngle * 0.5f) return false;
        // Only solid world layers block vision. Coin/trigger colliders are ignored.
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(walls); filter.useTriggers = false;
        return Physics2D.Raycast(eye, delta.normalized, filter, visionHits, delta.magnitude) == 0;
    }
    readonly RaycastHit2D[] visionHits = new RaycastHit2D[1];
    protected bool MayStartAttack => AttackVisible && CanSeeTarget();
    protected void Strike()
    {
        if (MayStartAttack && DistanceToTarget <= attackRange) target.TakeDamage(damage);
    }
    public void TakeHit(int amount)
    {
        if (amount <= 0 || hitPoints <= 0) return;
        hitPoints -= amount;
        flashUntil = Time.time + hitFlashTime;
        Sfx.Play(GameAudio.Clip("hurt"), 0.4f);
        if (hitPoints <= 0) Destroy(gameObject);
    }
    public void SetWaypoints(Transform[] points) { waypoints = points; }
    protected virtual void OnValidate()
    {
        windUp = Mathf.Clamp(windUp, 0.3f, 0.8f);
        cooldown = Mathf.Max(0.1f, cooldown);
        hitPoints = Mathf.Max(1, hitPoints);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, visionRange);
        if (target != null) Gizmos.DrawLine((Vector2)transform.position + eyeOffset, TargetPosition);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
