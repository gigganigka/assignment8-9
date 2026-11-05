using UnityEngine;
using UnityEngine.SceneManagement;

// Выход уровня. Collider2D (Is Trigger). Открывается, когда собраны все монетки.
public class Portal : MonoBehaviour
{
    public string nextScene = SceneNames.Level2;   // для Level1 = level2, для Level2 = boss
    public AudioClip winSound;
    public SpriteRenderer visual;                  // (необязательно) картинка портала: тусклая, пока закрыт

    void Update()
    {
        if (visual != null && LevelManager.Instance != null)
            visual.color = LevelManager.Instance.PortalOpen ? Color.white : new Color(1f, 1f, 1f, 0.4f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (LevelManager.Instance != null && !LevelManager.Instance.PortalOpen) return; // ещё закрыт
        Sfx.Play(winSound);
        GameScenes.Load(nextScene);
    }
}
