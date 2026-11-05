using UnityEngine;

// Помощник для звуков. Любой скрипт вызывает Sfx.Play(clip).
// Один общий AudioSource, который НЕ удаляется при смене сцены (DontDestroyOnLoad),
// поэтому звук портала / смерти / победы не обрывается, когда грузится следующая сцена.
// spatialBlend = 0 -> звук 2D: громкость не зависит от того, где камера.
public static class Sfx
{
    static AudioSource source;

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;               // клип не назначен - просто молчим

        if (source == null)                     // создаём источник один раз за всю игру
        {
            GameObject go = new GameObject("SfxPlayer");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true; // let the final hit finish on the death screen
            source.spatialBlend = 0f;           // 2D-звук
        }
        source.PlayOneShot(clip, volume);       // PlayOneShot: несколько звуков могут звучать одновременно
    }
}
