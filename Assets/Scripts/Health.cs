using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Здоровье героя. Висит на Player.
public class Health : MonoBehaviour
{
    public int maxHp = 3;                 // по заданию HP героя = 3
    public bool useMenuSettings = true;   // true: берём HP из настроек меню
    public float invincibleTime = 1.5f;   // сколько секунд неуязвим после удара
    public float blinkInterval = 0.1f;    // как часто мигает
    public AudioClip damageSound;

    public int CurrentHp { get; private set; }
    public bool IsInvincible { get; private set; }

    SpriteRenderer sr;
    bool dead;                            // чтобы смерть не сработала два раза

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (useMenuSettings) maxHp = GameSettings.playerMaxHp;
        CurrentHp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        if (dead || IsInvincible) return;   // пока неуязвим - урон не проходит
        CurrentHp -= amount;
        Sfx.Play(damageSound != null ? damageSound : GameAudio.Clip("hurt"), 0.75f);
        if (CurrentHp <= 0) { Die(); return; }
        StartCoroutine(InvincibilityRoutine());
    }

    public void Heal(int amount)
    {
        if (dead) return;
        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount); // не больше максимума
    }

    // Смерть (HP = 0 или падение в яму)
    public void Die()
    {
        if (dead) return;
        dead = true;
        CurrentHp = 0;

        StopAllCoroutines();              // останавливаем мигание
        sr.enabled = true;

        // Отключаем управление, чтобы мёртвый герой не бегал и не стрелял
        PlayerInput input = GetComponent<PlayerInput>();
        if (input != null) input.enabled = false;

        if (MobileGameUI.Instance != null)
            MobileGameUI.Instance.ShowDeath();
        else if (DeathScreen.Instance != null)
            DeathScreen.Instance.Show();  // показываем экран смерти (Restart / Main Menu)
        else
            GameScenes.Load(SceneManager.GetActiveScene().name); // запасной вариант: перезапуск уровня
    }

    // Корутина: код, который "растянут" во времени (yield return = подождать)
    IEnumerator InvincibilityRoutine()
    {
        IsInvincible = true;
        float t = 0f;
        while (t < invincibleTime)
        {
            sr.enabled = !sr.enabled;                    // мигаем: вкл/выкл картинку
            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }
        sr.enabled = true;
        IsInvincible = false;
    }
}
