using UnityEngine;

/// <summary>
/// Manages the layered music system. Each layer is an AudioSource playing a looping stem.
/// Collecting a SoundOrb calls ActivateLayer() to unmute/fade in that stem.
///
/// FMOD users: replace each AudioSource.Play() call with your FMOD event start,
/// and the volume fade with an FMOD parameter. The public interface (ActivateLayer,
/// ResetLayers, IsLayerActive, GetLayerColor, GetLayerName) stays the same.
/// </summary>
public class AudioLayerSystem : MonoBehaviour
{
    public static AudioLayerSystem Instance { get; private set; }

    [System.Serializable]
    public class AudioLayer
    {
        public string layerName = "Layer";
        public AudioClip clip;
        public Color orbColor = Color.white;
        [Range(0f, 1f)] public float targetVolume = 1f;

        [HideInInspector] public bool isActive;
        [HideInInspector] public AudioSource source;
    }

    [Header("Layers")]
    [SerializeField] private AudioLayer[] layers;

    [Header("Fade")]
    [SerializeField] private float fadeInSpeed = 2f;    // volume units per second on activate

    public int TotalLayers => layers != null ? layers.Length : 0;
    public int ActiveLayerCount { get; private set; }

    // Fired with the layer index whenever a new stem is switched on
    public event System.Action<int> OnLayerActivated;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildAudioSources();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        FadeActiveLayers();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Activate a stem by index. Returns false if already active or index out of range.
    /// </summary>
    public bool ActivateLayer(int layerId)
    {
        if (layers == null || layerId < 0 || layerId >= layers.Length) return false;

        AudioLayer layer = layers[layerId];
        if (layer.isActive) return false;

        layer.isActive = true;
        ActiveLayerCount++;

        if (layer.source != null && !layer.source.isPlaying)
            layer.source.Play();

        OnLayerActivated?.Invoke(layerId);
        Debug.Log($"[AudioLayerSystem] Layer {layerId} '{layer.layerName}' activated. " +
                  $"Active: {ActiveLayerCount}/{TotalLayers}");
        return true;
    }

    public bool IsLayerActive(int layerId)
    {
        if (layers == null || layerId < 0 || layerId >= layers.Length) return false;
        return layers[layerId].isActive;
    }

    public Color GetLayerColor(int layerId)
    {
        if (layers == null || layerId < 0 || layerId >= layers.Length) return Color.white;
        return layers[layerId].orbColor;
    }

    public string GetLayerName(int layerId)
    {
        if (layers == null || layerId < 0 || layerId >= layers.Length) return string.Empty;
        return layers[layerId].layerName;
    }

    /// <summary>
    /// Stop all stems and reset state. Called on run restart.
    /// </summary>
    public void ResetLayers()
    {
        if (layers == null) return;

        foreach (AudioLayer layer in layers)
        {
            if (layer.source != null)
            {
                layer.source.volume = 0f;
                layer.source.Stop();
            }
            layer.isActive = false;
        }

        ActiveLayerCount = 0;
    }

    // -------------------------------------------------------------------------
    // Internal
    // -------------------------------------------------------------------------

    void BuildAudioSources()
    {
        if (layers == null) return;

        foreach (AudioLayer layer in layers)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.clip = layer.clip;
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;        // start silent; fades in on activation
            layer.source = src;
        }
    }

    void FadeActiveLayers()
    {
        if (layers == null) return;

        foreach (AudioLayer layer in layers)
        {
            if (layer.source == null) continue;
            float target = layer.isActive ? layer.targetVolume : 0f;
            layer.source.volume = Mathf.MoveTowards(layer.source.volume, target, fadeInSpeed * Time.deltaTime);
        }
    }
}
