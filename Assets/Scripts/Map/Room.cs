using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Room : MonoBehaviour
{
    [Header("Connection Points")]
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform exitPoint;

    [Header("Collapse")]
    [SerializeField] private float collapseDelay = 10f;

    [Header("Events")]
    public UnityEvent onPlayerEntered;
    public UnityEvent onCollapseWarning;   // fired at half the timer for UI/audio cues
    public UnityEvent onCollapsed;

    public event Action PlayerEntered;

    public Vector3 EntryPosition => entryPoint != null ? entryPoint.position : transform.position;
    public Vector3 ExitPosition  => exitPoint  != null ? exitPoint.position  : transform.position;
    public Vector3 ExitForward   => exitPoint  != null ? exitPoint.forward   : transform.forward;

    public float CollapseDelay => collapseDelay;
    public float TimeRemaining { get; private set; }

    private bool _playerInside;
    private bool _timerStarted;
    private bool _collapsed;
    private bool _warningSent;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        if (!_timerStarted || _collapsed) return;

        TimeRemaining -= Time.deltaTime;

        if (!_warningSent && TimeRemaining <= collapseDelay * 0.5f)
        {
            _warningSent = true;
            onCollapseWarning.Invoke();
        }

        if (TimeRemaining <= 0f)
            Collapse(playerCaught: _playerInside);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_collapsed) return;
        if (other.GetComponent<Player>() == null) return;

        _playerInside = true;

        if (!_timerStarted)
        {
            _timerStarted = true;
            TimeRemaining = collapseDelay;
            PlayerEntered?.Invoke();
            onPlayerEntered.Invoke();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Player>() != null)
            _playerInside = false;
    }

    public void CollapseImmediate()
    {
        if (_collapsed) return;
        Collapse(playerCaught: false);
    }

    void Collapse(bool playerCaught)
    {
        if (_collapsed) return;
        _collapsed = true;

        if (playerCaught && GameManager.Instance != null)
            GameManager.Instance.HandlePlayerDeath();

        onCollapsed.Invoke();
        Destroy(gameObject, 1f);
    }

    void OnDrawGizmos()
    {
        if (entryPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(entryPoint.position, 0.3f);
            Gizmos.DrawRay(entryPoint.position, entryPoint.forward);
        }
        if (exitPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(exitPoint.position, 0.3f);
            Gizmos.DrawRay(exitPoint.position, exitPoint.forward);
        }
    }
}
