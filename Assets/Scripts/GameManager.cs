using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, ISaveable
{
    public static GameManager Instance { get; private set; }

    [Header("Init")]
    [SerializeField] private bool initialiseOnStart = true;


    [Header("Events")]
    public UnityEvent onRunStarted;
    public UnityEvent onRunEnded;
    public UnityEvent onRunRestarted;
    public UnityEvent onBeatJump;

    public static GameState currentState = GameState.Init;
    public static GameState currentGameState => currentState;
    public static string targetScene;
    public static GameState targetState;

    public float CurrentDistance => _player != null ? _player.DistanceTravelled : 0f;
    public float BestDistance { get; private set; }
    public bool RunActive { get; private set; }
    public int Score { get; private set; }
    public int BestScore { get; private set; }
    public float RunTimer { get; private set; }
    public int CountdownValue { get; private set; } = -1;

    private Player _player;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!initialiseOnStart) currentState = GameState.Playing;
    }

    void Start()
    {
        _player = FindFirstObjectByType<Player>();
        if (currentState == GameState.Playing)
            StartRun();
    }

    void Update()
    {
        if (RunActive && currentState == GameState.Playing)
            RunTimer += Time.deltaTime;

        if (initialiseOnStart && currentState == GameState.Init)
            Initialise();

        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    void Initialise()
    {
        if (currentState == GameState.Init)
            SceneLoader("01_Menu", GameState.Menu);
    }

    // ── Run lifecycle ──────────────────────────────────────────────

    public void StartRun()
    {
        RunTimer = 0f;
        StartCoroutine(CountdownCoroutine());
    }

    private static readonly WaitForSeconds _wait1s = new(1f);
    private static readonly WaitForSeconds _waitGo = new(0.5f);

    IEnumerator CountdownCoroutine()
    {
        currentState = GameState.Countdown;
        for (int i = 5; i > 0; i--)
        {
            CountdownValue = i;
            yield return _wait1s;
        }
        CountdownValue = 0;
        yield return _waitGo;
        CountdownValue = -1;
        currentState = GameState.Playing;
        RunActive = true;
        onRunStarted.Invoke();
    }

    public void HandlePlayerDeath()
    {
        if (!RunActive) return;
        RunActive = false;

        if (CurrentDistance > BestDistance) BestDistance = CurrentDistance;
        if (Score > BestScore) BestScore = Score;

        onRunEnded.Invoke();
        if (AudioLayerSystem.Instance != null) AudioLayerSystem.Instance.ResetLayers();
    }

    public void RestartRun()
    {
        if (_player == null)
            _player = FindFirstObjectByType<Player>();

        Vector3 spawnPos = Vector3.zero;
        if (_player != null) _player.ResetPlayer(spawnPos);

        Score = 0;
        RunActive = false;
        if (BeatManager.Instance != null) BeatManager.Instance.ResetBeat();

        onRunRestarted.Invoke();
        StartRun();
    }

    public void AddBeatBonus(int points = 100)
    {
        if (!RunActive) return;
        Score += points;
        onBeatJump.Invoke();
    }

    // ── Scene loading ──────────────────────────────────────────────

    public void SceneLoader(string sceneName, GameState stateName)
    {
        targetScene = sceneName;
        targetState = stateName;
        SceneManager.LoadScene("_Loading");
    }

    public void StartNewGame()
    {
        SceneLoader("02_Main", GameState.Playing);
    }

    public void ContinueGame()
    {
        SaveManager.Instance.LoadGame();
        SceneLoader("02_Main", GameState.Playing);
    }

    public void LoadMenuScene()
    {
        SceneLoader("01_Menu", GameState.Menu);
    }

    public void ReturnToMenu() => LoadMenuScene();

    public void ExitGame()
    {
        Application.Quit();
    }

    // ── Pause / Resume ─────────────────────────────────────────────

    public void TogglePause()
    {
        if (currentState == GameState.Playing)
            PauseGame();
        else if (currentState == GameState.Paused)
            ResumeGame();
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;
        currentState = GameState.Paused;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        currentState = GameState.Playing;
        Time.timeScale = 1f;
    }

    // ── Save / Load ────────────────────────────────────────────────

    public void OnSave(SaveData data) { }

    public void OnLoad(SaveData data) { }

    public void ConsoleMessage(string message)
    {
        Debug.Log(message);
    }
}
