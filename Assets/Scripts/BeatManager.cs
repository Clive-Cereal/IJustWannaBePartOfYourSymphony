using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private FMODUnity.EventReference musicEvent;

    [Header("Timing")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float hitWindowSeconds = 0.3f;
    [SerializeField] public Animator beatIndicator;

    private FMOD.Studio.EventInstance _musicInstance;

    public event Action OnBeat;

    public float BeatInterval { get; private set; }
    public float BeatProgress => _beatTimer / BeatInterval;

    private float _beatTimer;
    private volatile bool _fmodBeatPending;
    private volatile bool _fmodEndPending;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BeatInterval = 60f / bpm;

        if (!musicEvent.IsNull)
        {
            _musicInstance = FMODUnity.RuntimeManager.CreateInstance(musicEvent);
            _musicInstance.setCallback(FmodCallback,
                FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER);
        }
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.onRunStarted.AddListener(StartMusic);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.onRunStarted.RemoveListener(StartMusic);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        _musicInstance.release();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "02_Main")
        {
            GameObject obj = GameObject.Find("BackgroundPillars");
            if (obj != null) beatIndicator = obj.GetComponent<Animator>();
        }
    }

    void StartMusic()
    {
        ResetBeat();
        _musicInstance.start();
    }

    void Update()
    {
        if (GameManager.currentGameState != GameState.Playing) return;

        _beatTimer += Time.deltaTime;

        if (_fmodBeatPending)
        {
            _fmodBeatPending = false;
            _beatTimer = 0f;
            Debug.Log("BEAT");
            if (beatIndicator != null) beatIndicator.SetTrigger("onBeat");
            OnBeat?.Invoke();
        }

        if (_fmodEndPending)
        {
            _fmodEndPending = false;
            if (GameManager.Instance != null) GameManager.Instance.HandleVictory();
        }
    }

    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    static FMOD.RESULT FmodCallback(
        FMOD.Studio.EVENT_CALLBACK_TYPE type,
        IntPtr instancePtr, IntPtr paramPtr)
    {
        if (type == FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER && Instance != null)
        {
            var param = (FMOD.Studio.TIMELINE_MARKER_PROPERTIES)
                Marshal.PtrToStructure(paramPtr,
                    typeof(FMOD.Studio.TIMELINE_MARKER_PROPERTIES));

            if (param.name == "BEAT")
                Instance._fmodBeatPending = true;
            else if (param.name == "END")
                Instance._fmodEndPending = true;
        }
        return FMOD.RESULT.OK;
    }

    public bool IsOnBeat()
    {
        float distToPrev = _beatTimer;
        float distToNext = BeatInterval - _beatTimer;
        return Mathf.Min(distToPrev, distToNext) <= hitWindowSeconds;
    }

    public void ResetBeat()
    {
        _beatTimer = 0f;
    }
}
