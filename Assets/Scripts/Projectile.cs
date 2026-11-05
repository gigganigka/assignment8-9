using UnityEngine;

// БАЗОВЫЙ класс для снарядов: летит по прямой и исчезает через lifeTime секунд.
// Что делать при попадании - решает наследник (PlayerBullet, Fireball).
// Префаб снаряда: SpriteRenderer + Collider2D (Is Trigger) + Rigidbody2D (Kinematic) + скрипт.
public abstract class Projectile : MonoBehaviour
{
    public float speed = 8f;
    public float lifeTime = 3f;
    protected Vector2 direction = Vector2.right;

    public void Launch(Vector2 dir)
    {
        direction = dir.normalized;
    }

    protected virtual void Start()
    {
        Destroy(gameObject, lifeTime); // самоуничтожение, чтобы не копились
    }

    protected virtual void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger) return;  // игнорируем монетки, зоны и т.п.
        OnHit(other);
    }

    protected abstract void OnHit(Collider2D other);
}
