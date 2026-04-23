using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
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
    [SerializeField] private FMODUnity.EventReference deathSfx;
    [SerializeField] private FMODUnity.EventReference jumpSfx;
    [SerializeField] private FMODUnity.EventReference footstepSfx;

    public bool IsAlive { get; private set; } = true;
    public float DistanceTravelled { get; private set; }

    private CharacterController _cc;
    private Animator _anim;
    private InputAction _moveAction;
    private InputAction _jumpAction;

    // Lane state
    private int _laneIndex;             // which lane we're targeting
    private float _lateralOffset;       // current interpolated offset from center
    private float _targetLateralOffset; // target offset for the current lane
    private Vector3 _right;             // right direction captured at start, never changes

    // Jump state
    private float _verticalVelocity;
    private int _jumpsRemaining;
    private bool _jumpBuffered;

    private float _footstepTimer;
    private const float FootstepInterval = 0.4f;

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
        HandleFootsteps();
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
        if (x < -0.5f && _laneIndex > 0)
        {
            _laneIndex--;
            _targetLateralOffset = (_laneIndex - laneCount / 2) * laneWidth;
        }
        else if (x > 0.5f && _laneIndex < laneCount - 1)
        {
            _laneIndex++;
            _targetLateralOffset = (_laneIndex - laneCount / 2) * laneWidth;
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
                if (GameManager.Instance != null) GameManager.Instance.AddBeatBonus();
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

    void HandleFootsteps()
    {
        if (!_cc.isGrounded || footstepSfx.IsNull) return;

        _footstepTimer -= Time.deltaTime;
        if (_footstepTimer <= 0f)
        {
            _footstepTimer = FootstepInterval;
            FMODUnity.RuntimeManager.PlayOneShot(footstepSfx, transform.position);
        }
    }

    void OnJumpPressed(InputAction.CallbackContext ctx)
    {
        if (!IsAlive || GameManager.currentGameState != GameState.Playing) return;
        _jumpBuffered = true;
    }

    public void OnOrbCollected(int layerId)
    {
        if (!collectSfx.IsNull)
            FMODUnity.RuntimeManager.PlayOneShot(collectSfx, transform.position);
        if (AudioLayerSystem.Instance != null)
            AudioLayerSystem.Instance.ActivateLayer(layerId);
    }

    public void OnHitObstacle()
    {
        if (!IsAlive) return;
        IsAlive = false;

        if (!deathSfx.IsNull)
            FMODUnity.RuntimeManager.PlayOneShot(deathSfx, transform.position);

        if (GameManager.Instance != null) GameManager.Instance.HandlePlayerDeath();
    }

    public void OnHitCoins()
    {
        point++;
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
    }
}
