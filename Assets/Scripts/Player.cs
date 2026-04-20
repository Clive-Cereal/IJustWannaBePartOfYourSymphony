using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float moveSmoothing = 12f;
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravity = -25f;

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
    private float _verticalVelocity;
    private float _currentForwardVel;
    private bool _jumpBuffered;
    private float _footstepTimer;
    private const float FootstepInterval = 0.4f;

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
    }

    void OnDisable()
    {
        if (_jumpAction != null)
            _jumpAction.performed -= OnJumpPressed;
    }

    void Start()
    {
        IsAlive = true;
    }

    void Update()
    {
        if (!IsAlive || GameManager.currentGameState != GameState.Playing)
            return;

        HandleMove();
        HandleGravityAndJump();
        ApplyMovement();
        HandleFootsteps();
    }

    void HandleMove()
    {
        Vector2 input = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

        // Move relative to player facing so camera-parented-under-player works naturally
        // A/D rotates the player (camera turns with it), W/S moves forward/back
        transform.Rotate(Vector3.up, input.x * rotationSpeed * Time.deltaTime);

        _currentForwardVel = Mathf.Lerp(_currentForwardVel, input.y * moveSpeed, moveSmoothing * Time.deltaTime);

        if (_anim != null)
        {
            _anim.SetBool("isMoving", _currentForwardVel > 0.1f);
            _anim.SetBool("isMovingBackward", input.y < -0.01f);
        }
    }

    void HandleGravityAndJump()
    {
        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        if (_jumpBuffered && _cc.isGrounded)
        {
            _verticalVelocity = jumpForce;
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
        Vector3 motion = (transform.forward * _currentForwardVel + Vector3.up * _verticalVelocity) * Time.deltaTime;
        _cc.Move(motion);
        DistanceTravelled += Mathf.Abs(_currentForwardVel) * Time.deltaTime;
    }

    void HandleFootsteps()
    {
        if (!_cc.isGrounded || footstepSfx.IsNull) return;
        if (Mathf.Abs(_currentForwardVel) < 0.1f) return;

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
        AudioLayerSystem.Instance?.ActivateLayer(layerId);
    }

    public void OnHitObstacle()
    {
        if (!IsAlive) return;
        IsAlive = false;

        if (!deathSfx.IsNull)
            FMODUnity.RuntimeManager.PlayOneShot(deathSfx, transform.position);

        if (GameManager.Instance != null) GameManager.Instance.HandlePlayerDeath();
    }

    public void ResetPlayer(Vector3 spawnPosition)
    {
        _cc.enabled = false;
        transform.position = spawnPosition;
        _cc.enabled = true;

        IsAlive = true;
        DistanceTravelled = 0f;
        _verticalVelocity = 0f;
        _currentForwardVel = 0f;
        _jumpBuffered = false;
    }
}
