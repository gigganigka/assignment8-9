using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public AudioClip shootSound;
    float nextFireTime;
    SpriteRenderer sr;
    Health health;

    void Awake() { sr = GetComponent<SpriteRenderer>(); health = GetComponent<Health>(); }
    public void OnFire(InputValue value) { if (value.isPressed) TryFire(); }
    public void TryFire()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || Time.time < nextFireTime ||
            (health != null && health.CurrentHp <= 0) || bulletPrefab == null || firePoint == null) return;
        nextFireTime = Time.time + Mathf.Max(0.1f, GameSettings.fireCooldown);
        bool left = sr != null && sr.flipX;
        Vector3 offset = firePoint.position - transform.position;
        if (left) offset.x = -offset.x;
        GameObject bullet = Instantiate(bulletPrefab, transform.position + offset, Quaternion.identity);
        Projectile projectile = bullet.GetComponent<Projectile>();
        if (projectile != null) projectile.Launch(left ? Vector2.left : Vector2.right);
        Sfx.Play(shootSound);
    }
}
