using UnityEngine;

// Сердце / аптечка: лечит героя. Collider2D (Is Trigger) + этот скрипт.
public class HealthPickup : MonoBehaviour
{
    public int healAmount = 1;
    public AudioClip pickupSound;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Health h = other.GetComponent<Health>();
        if (h != null) h.Heal(healAmount);
        Sfx.Play(pickupSound);
        Destroy(gameObject);
    }
}
