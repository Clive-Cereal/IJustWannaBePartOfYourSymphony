using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public Image damageVignette;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI victoryScoreText;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;
    public GameObject gameOverPanel;
    public GameObject pausePanel;
    public GameObject victoryPanel;

    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {Player.Point}";

        UpdateCountdown();
        UpdateTimer();
        UpdateDamageVignette();

        bool isGameOver = GameManager.currentGameState == GameState.GameOver;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(isGameOver);
        if (finalScoreText != null && isGameOver)
            finalScoreText.text = $"Score: {Player.Point}";

        if (pausePanel != null)
            pausePanel.SetActive(GameManager.currentGameState == GameState.Paused);

        bool isVictory = GameManager.currentGameState == GameState.Victory;
        if (victoryScoreText != null && isVictory)
            victoryScoreText.text = $"Score: {Player.Point}";

        if (victoryPanel != null)
            victoryPanel.SetActive(GameManager.currentGameState == GameState.Victory);
    }

    void UpdateCountdown()
    {
        if (countdownText == null || GameManager.Instance == null) return;

        int value = GameManager.Instance.CountdownValue;
        if (value > 0)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = value.ToString();
        }
        else if (value == 0)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "GO!";
        }
        else
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    void UpdateDamageVignette()
    {
        if (damageVignette == null) return;
        Color c = damageVignette.color;
        c.a = Player.ObstacleHits / 20f;
        damageVignette.color = c;
    }

    void UpdateTimer()
    {
        if (timerText == null || GameManager.Instance == null) return;

        float t = GameManager.Instance.RunTimer;
        int minutes = (int)(t / 60f);
        int seconds = (int)(t % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
