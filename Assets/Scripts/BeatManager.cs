using System;
using UnityEngine;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [SerializeField] private float bpm = 120f;
    [SerializeField] private float hitWindowSeconds = 0.15f;

    public event Action OnBeat;

    public float BeatInterval { get; private set; }
    public float BeatProgress => _beatTimer / BeatInterval; // 0–1 within current beat

    private float _beatTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BeatInterval = 60f / bpm;
    }

    void Update()
    {
        if (GameManager.currentGameState != GameState.Playing) return;

        _beatTimer += Time.deltaTime;
        if (_beatTimer >= BeatInterval)
        {
            _beatTimer -= BeatInterval;
            OnBeat?.Invoke();
        }
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
