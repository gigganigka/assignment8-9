using UnityEngine;

// Swept ray each physics step avoids tunnelling. Other enemies and triggers are ignored.
public sealed class EnemyShot2D : MonoBehaviour
{
    EnemyBrain2D owner;
    Vector2 direction;
    float speed, expires;
    int damage;
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    public static void Spawn(EnemyBrain2D source, Vector2 origin, Vector2 direction,
                             float speed, float lifetime, int damage, Sprite sprite)
    {
        GameObject go = new GameObject("Enemy shot");
        go.transform.position = origin;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : MobileSprites.Load("EnemyLab/Shot");
        renderer.sharedMaterial = source.GetComponent<SpriteRenderer>().sharedMaterial;
        renderer.sortingOrder = 6;
        renderer.color = new Color(1, 0.4f, 0.12f);
        go.transform.localScale = Vector3.one * 1.5f;
        EnemyShot2D shot = go.AddComponent<EnemyShot2D>();
        shot.owner = source; shot.direction = direction.normalized;
        shot.speed = speed; shot.damage = damage; shot.expires = Time.time + lifetime;
    }
    void FixedUpdate()
    {
        if (owner == null || !owner.AttackVisible || Time.time >= expires) { Destroy(gameObject); return; }
        Vector2 start = transform.position;
        float distance = speed * Time.fixedDeltaTime;
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        int count = Physics2D.Raycast(start, direction, filter, hits, distance);
        Collider2D nearest = null; float best = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider2D c = hits[i].collider;
            if (c.GetComponentInParent<EnemyBrain2D>() != null || hits[i].distance >= best) continue;
            nearest = c; best = hits[i].distance;
        }
        if (nearest != null)
        {
            Health health = nearest.GetComponentInParent<Health>();
            if (health != null && nearest.CompareTag("Player")) health.TakeDamage(damage);
            Destroy(gameObject); return;
        }
        transform.position = start + direction * distance;
    }
}
