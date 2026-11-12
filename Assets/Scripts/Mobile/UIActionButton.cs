using UnityEngine;
using UnityEngine.UI;

// Serialized command survives scene saves. No editor-only delegate is required.
[RequireComponent(typeof(Button))]
public sealed class UIActionButton : MonoBehaviour
{
    public enum Command { Play, Quit, Settings, Back, Pause, Resume, Restart, Menu, Replay,
                          HealthDown, HealthUp, RateDown, RateUp, Hazards, Enemies }
    public Command command;
    void Awake() { Bind(); }
    void OnEnable() { Bind(); }
    public void Bind()
    {
        Button button = GetComponent<Button>();
        button.onClick.RemoveListener(Execute);
        button.onClick.AddListener(Execute);
    }
    public void Execute()
    {
        MobileGameUI ui = MobileGameUI.Instance;
        switch (command)
        {
            case Command.Play:
                MainMenu menu = Object.FindFirstObjectByType<MainMenu>();
                if (menu != null) menu.Play(); else GameScenes.Load(SceneNames.Level1);
                break;
            case Command.Replay: GameScenes.Load(SceneNames.EnemyLab); break;
            case Command.Menu: GameScenes.Load(SceneNames.MainMenu); break;
            case Command.Restart: if (ui != null) ui.Restart(); break;
            case Command.Pause: if (ui != null) ui.SetPaused(true); break;
            case Command.Resume: if (ui != null) ui.SetPaused(false); break;
            case Command.Settings: if (ui != null) ui.ShowSettings(true); break;
            case Command.Back: if (ui != null) ui.ShowSettings(false); break;
            case Command.Quit:
                if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer) Application.Quit();
                else if (ui != null) ui.ShowQuitNotice();
                break;
            default: if (ui != null) ui.ChangeSetting(command); break;
        }
    }
}
