using UnityEngine;
using UnityEngine.SceneManagement;

// Экран смерти. Висит на объекте "DeathScreen" (Canvas) в каждом уровне.
// Пока герой жив, панель скрыта. Health.Die() вызывает Show().
// Кнопки: RESTART - заново этот же уровень, MAIN MENU - в главное меню.
public class DeathScreen : MonoBehaviour
{
    public static DeathScreen Instance;   // чтобы Health мог найти экран смерти

    public GameObject panel;              // тёмная панель с "YOU DIED" и кнопками
    public GameObject mobileControls;     // джойстик и кнопки - прячем, когда умерли

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;              // на всякий случай: новая сцена всегда идёт с обычной скоростью
        if (panel != null) panel.SetActive(false);
    }

    public void Show()
    {
        if (panel != null) panel.SetActive(true);
        if (mobileControls != null) mobileControls.SetActive(false);
        Time.timeScale = 0f;              // пауза: враги и шары замирают
    }

    // Кнопка RESTART
    public void Restart()
    {
        Time.timeScale = 1f;              // ВАЖНО вернуть, иначе следующая сцена будет на паузе
        GameScenes.Load(SceneManager.GetActiveScene().name);
    }

    // Кнопка MAIN MENU
    public void ToMainMenu()
    {
        Time.timeScale = 1f;
        GameScenes.Load(SceneNames.MainMenu);
    }
}
