using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    // Эти поля заполняет Tools -> Build Main Menu. Можно перетащить и вручную.
    public Slider hpSlider;          // 1..5, только целые
    public Slider cooldownSlider;    // 0.1..1.5 секунд
    public Toggle hazardsToggle;     // огонь / шипы
    public Toggle enemiesToggle;     // враги
    public TMP_Text hpText;          // подпись "HP: 3"
    public TMP_Text cooldownText;    // подпись "Cooldown: 0.50 s"

    public string firstLevelName = SceneNames.Level1; // имя сцены ровно как в проекте

    void Start()
    {
        // Показываем текущие значения (при возврате в меню выбор не сбрасывается)
        if (hpSlider != null) hpSlider.value = GameSettings.playerMaxHp;
        if (cooldownSlider != null) cooldownSlider.value = GameSettings.fireCooldown;
        if (hazardsToggle != null) hazardsToggle.isOn = GameSettings.hazardsEnabled;
        if (enemiesToggle != null) enemiesToggle.isOn = GameSettings.enemiesEnabled;

        // Обновляем подписи при движении слайдеров
        if (hpSlider != null) hpSlider.onValueChanged.AddListener(_ => RefreshTexts());
        if (cooldownSlider != null) cooldownSlider.onValueChanged.AddListener(_ => RefreshTexts());
        RefreshTexts();
    }

    void RefreshTexts()
    {
        if (hpText != null && hpSlider != null)
            hpText.text = "HP: " + Mathf.RoundToInt(hpSlider.value);
        if (cooldownText != null && cooldownSlider != null)
            cooldownText.text = "Cooldown: " + cooldownSlider.value.ToString("0.00") + " s";
    }

    // Кнопка Play
    public void Play()
    {
        // Записываем выбор игрока в статический класс
        if (hpSlider != null) GameSettings.playerMaxHp = Mathf.RoundToInt(hpSlider.value);
        if (cooldownSlider != null) GameSettings.fireCooldown = cooldownSlider.value;
        if (hazardsToggle != null) GameSettings.hazardsEnabled = hazardsToggle.isOn;
        if (enemiesToggle != null) GameSettings.enemiesEnabled = enemiesToggle.isOn;

        Time.timeScale = 1f;   // если пришли с экрана смерти, где была пауза
        GameScenes.Load(firstLevelName);
    }

    // Кнопка Quit
    public void Quit()
    {
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        if (MobileGameUI.Instance != null) MobileGameUI.Instance.ShowQuitNotice();
        return;
#endif
        Debug.Log("Quit"); // в редакторе Application.Quit() ничего не делает
        Application.Quit();
    }
}
