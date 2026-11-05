using UnityEngine;

// Пуля героя: бьёт всё, что реализует IDamageable (враги, босс).
public class PlayerBullet : Projectile
{
    public int damage = 1;

    protected override void OnHit(Collider2D other)
    {
        if (other.CompareTag("Player")) return;               // свою же пулю не ловим
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null) target.TakeHit(damage);
        Destroy(gameObject);                                  // пропадает при любом попадании
    }
}
