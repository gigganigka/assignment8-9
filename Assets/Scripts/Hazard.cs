using UnityEngine;

// БАЗОВЫЙ класс опасности: бьёт игрока при касании. Шипы/огонь = просто Hazard.
// От него наследуются PatrolEnemy и Boss (это и есть "наследование" из задания).
public class Hazard : MonoBehaviour
{
    public int damage = 1;
    public bool respectHazardToggle = true; // выключаться, если в меню снят переключатель Fire

    protected virtual void Awake()
    {
        if (respectHazardToggle && !GameSettings.hazardsEnabled)
            gameObject.SetActive(false);
    }

    // Наследники могут поменять, что происходит при ударе
    protected virtual void OnHitPlayer(Health player)
    {
        player.TakeDamage(damage);
    }

    void OnCollisionEnter2D(Collision2D c) { TryHit(c.collider); }
    void OnCollisionStay2D(Collision2D c)  { TryHit(c.collider); }
    void OnTriggerEnter2D(Collider2D c)    { TryHit(c); }
    void OnTriggerStay2D(Collider2D c)     { TryHit(c); }

    void TryHit(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;       // у героя должен быть тег Player
        Health h = other.GetComponent<Health>();       // GetComponent: ищем Health на герое
        if (h != null) OnHitPlayer(h);
    }
}
