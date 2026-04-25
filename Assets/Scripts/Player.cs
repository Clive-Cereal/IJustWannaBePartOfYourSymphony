using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Debug")]
    public bool godMode = false;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float laneWidth = 2f;
    [SerializeField] private float laneSnapSpeed = 12f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private int laneCount = 3;

    [Header("FMOD SFX")]
    [SerializeField] private FMODUnity.EventReference collectSfx;
    [SerializeField] private FMODUnity.EventReference hitSfx;
    [SerializeField] private FMODUnity.EventReference jumpSfx;
    [SerializeField] private FMODUnity.EventReference strafeSfx;


    public bool IsAlive { get; private set; } = true;
    public float DistanceTravelled { get; private set; }
    public static int ObstacleHits { get; private set; }

    private CharacterController _cc;
    private Animator _anim;
    private InputAction _moveAction;
    private InputAction _jumpAction;

    // Lane state
    private int _laneIndex;             
    private float _lateralOffset;
    private float _targetLateralOffset; 
    private Vector3 _right;          

    // Jump state
    private float _verticalVelocity;
    private int _jumpsRemaining;
    private bool _jumpBuffered;

    private float _lastHitTime = -10f;

private static int point = 0;
    public static int Point => point;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _anim = GetComponentInChildren<Animator>();

        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            _moveAction = playerInput.actions["Move"];
            _jumpAction = playerInput.actions["Jump"];
        }
    }

    void OnEnable()
    {
        if (_jumpAction != null)
            _jumpAction.performed += OnJumpPressed;
        if (_moveAction != null)
            _moveAction.performed += OnMovePerformed;
    }

    void OnDisable()
    {
        if (_jumpAction != null)
            _jumpAction.performed -= OnJumpPressed;
        if (_moveAction != null)
            _moveAction.performed -= OnMovePerformed;
    }

    void Start()
    {
        IsAlive = true;
        _right = transform.right;
        _laneIndex = laneCount / 2;
        _lateralOffset = 0f;
        _targetLateralOffset = 0f;
        _jumpsRemaining = maxJumps;

    }

    void Update()
    {
        if (!IsAlive || GameManager.currentGameState != GameState.Playing)
            return;

        HandleLane();
        HandleGravityAndJump();
        ApplyMovement();
    }

    void HandleLane()
    {
        if (_anim != null)
            _anim.SetBool("isMoving", true);
    }

    void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        if (!IsAlive || GameManager.currentGameState != GameState.Playing) return;

        float x = ctx.ReadValue<Vector2>().x;
        bool laneChanged = false;
        if (x < -0.5f && _laneIndex > 0)
        {
            _laneIndex--;
            _targetLateralOffset = (_laneIndex - laneCount / 2) * laneWidth;
            laneChanged = true;
        }
        else if (x > 0.5f && _laneIndex < laneCount - 1)
        {
            _laneIndex++;
            _targetLateralOffset = (_laneIndex - laneCount / 2) * laneWidth;
            laneChanged = true;
        }
        if (laneChanged)
        {
            if (!strafeSfx.IsNull)
                FMODUnity.RuntimeManager.PlayOneShot(strafeSfx, transform.position);
            if (BeatManager.Instance != null && BeatManager.Instance.IsOnBeat())
                point++;
        }
    }

    void HandleGravityAndJump()
    {
        if (_cc.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
            _jumpsRemaining = maxJumps;
        }

        if (_jumpBuffered && _jumpsRemaining > 0)
        {
            _verticalVelocity = jumpForce;
            _jumpsRemaining--;
            _jumpBuffered = false;
            if (_anim != null) _anim.SetTrigger("isJumping");
            if (!jumpSfx.IsNull)
                FMODUnity.RuntimeManager.PlayOneShot(jumpSfx, transform.position);

            if (BeatManager.Instance != null && BeatManager.Instance.IsOnBeat())
            {
                point++;
                if (GameManager.Instance != null) GameManager.Instance.AddBeatBonus();
            }
        }

        _verticalVelocity += gravity * Time.deltaTime;
    }

    void ApplyMovement()
    {
        // Lerp lateral offset and apply only the delta this frame so CC collision still works
        float prevLateral = _lateralOffset;
        _lateralOffset = Mathf.Lerp(_lateralOffset, _targetLateralOffset, laneSnapSpeed * Time.deltaTime);

        Vector3 motion = transform.forward * (moveSpeed * Time.deltaTime)
                       + _right * (_lateralOffset - prevLateral)
                       + Vector3.up * (_verticalVelocity * Time.deltaTime);
        _cc.Move(motion);
        DistanceTravelled += moveSpeed * Time.deltaTime;
    }

    void OnJumpPressed(InputAction.CallbackContext ctx)
    {
        if (!IsAlive || GameManager.currentGameState != GameState.Playing) return;
        _jumpBuffered = true;
    }
    public void OnHitObstacle()
    {
        if (!IsAlive || godMode) return;
        if (Time.time - _lastHitTime < 2f) return;
        _lastHitTime = Time.time;

        point = Mathf.Max(0, point - 5);
        ObstacleHits++;

        if (!hitSfx.IsNull)
            FMODUnity.RuntimeManager.PlayOneShot(hitSfx, transform.position);

        if (ObstacleHits >= 10)
        {
            IsAlive = false;
            if (GameManager.Instance != null) GameManager.Instance.HandlePlayerDeath();
        }
    }

    public void OnHitCoins()
    {
        if (!collectSfx.IsNull)
            FMODUnity.RuntimeManager.PlayOneShot(collectSfx, transform.position);
        point += 10 + (point / 10);
    }

    public void ResetPlayer(Vector3 spawnPosition)
    {
        _cc.enabled = false;
        transform.position = spawnPosition;
        _cc.enabled = true;

        IsAlive = true;
        DistanceTravelled = 0f;
        _verticalVelocity = 0f;
        _laneIndex = laneCount / 2;
        _lateralOffset = 0f;
        _targetLateralOffset = 0f;
        _jumpsRemaining = maxJumps;
        _jumpBuffered = false;
        ObstacleHits = 0;
        point = 0;
    }
}
