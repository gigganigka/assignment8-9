using UnityEngine;

// Монетка / кристалл / морковка. Collider2D с галочкой Is Trigger + этот скрипт.
public class Collectible : MonoBehaviour
{
    public AudioClip pickupSound;
    bool collected;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.CompareTag("Player")) return;
        collected = true;
        if (LevelManager.Instance != null) LevelManager.Instance.AddCoin();
        Sfx.Play(pickupSound != null ? pickupSound : GameAudio.Clip("coin"), 0.75f);
        Destroy(gameObject);
    }
}
