using UnityEngine;
using UnityEngine.SceneManagement;

// Resource fallbacks keep the sounds available when a scene is opened directly.
public static class GameAudio
{
    public static AudioClip Clip(string name) => Resources.Load<AudioClip>("GameAudio/" + name);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!MobileGameUI.IsGameScene(scene.name)) return;
        foreach (PlayerController hero in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            if (hero.jumpSound == null) hero.jumpSound = Clip("jump");
        foreach (Health health in Object.FindObjectsByType<Health>(FindObjectsSortMode.None))
            if (health.damageSound == null) health.damageSound = Clip("hurt");
        foreach (Collectible coin in Object.FindObjectsByType<Collectible>(FindObjectsSortMode.None))
            if (coin.pickupSound == null) coin.pickupSound = Clip("coin");
        Camera camera = Camera.main;
        if (camera != null && Object.FindFirstObjectByType<AudioListener>() == null)
            camera.gameObject.AddComponent<AudioListener>();
        if (Music.Instance == null) new GameObject("BackgroundMusic").AddComponent<Music>();
    }
}
