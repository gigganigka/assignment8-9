using UnityEngine;
using UnityEngine.SceneManagement;

// Экран победы: на кнопку "Back to menu" вешаем BackToMenu() через On Click.
public class WinMenu : MonoBehaviour
{
    public void BackToMenu()
    {
        Time.timeScale = 1f;
        GameScenes.Load(SceneNames.MainMenu);
    }
}
