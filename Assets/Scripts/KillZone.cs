using UnityEngine;

// Зона смерти под уровнем: упал в яму = смерть = перезапуск уровня.
// Длинный BoxCollider2D (Is Trigger) ниже всех платформ.
public class KillZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Health h = other.GetComponent<Health>();
        if (h != null) h.Die();
    }
}
