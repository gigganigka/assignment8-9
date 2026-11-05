using UnityEngine;

// One continuous, quiet loop across menu, levels and victory. Duplicate scene
// music objects are stopped immediately so tracks never stack on transitions.
[RequireComponent(typeof(AudioSource))]
public class Music : MonoBehaviour
{
    public static Music Instance { get; private set; }
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = 0.18f;
    AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        if (Instance != null && Instance != this)
        {
            source.Stop();
            source.enabled = false;
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        if (Instance != this) return;
        if (musicClip == null) musicClip = GameAudio.Clip("music");
        source.clip = musicClip;
        source.loop = true;
        source.volume = Mathf.Min(volume, 0.25f);
        source.spatialBlend = 0f;
        source.ignoreListenerPause = false;
        if (source.clip != null) source.Play();
    }
    void OnDestroy() { if (Instance == this) Instance = null; }
}
