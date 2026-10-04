using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionAsset inputActions;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction rightMouse;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpHeight = 12f;
    [SerializeField] private float jumpForce;
    [SerializeField] private float secondJumpForce = 12f;
    [SerializeField] public float fallMultiplier = 2.5f; // Extra gravity while falling for snappiness
    [SerializeField] public float jumpMultiplier = 1f;
    public bool canReduce;
    public bool isJumping;
    public int jumpCount;

    [Header("Path Spawning")]
    public GameObject standablePath;
    public Transform creationPath;
    [SerializeField] private float spawnCooldown = 0.15f; // Prevents spawning 60 objects per second
    private float nextSpawnTime;

    [Header("Ground Check")]
    [SerializeField] public Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool jumpRequested;
    private bool isGrounded;
    public SpriteRenderer sprite;

    [Header("Path Creation")]
    [SerializeField] private GameObject pathPrefab;      // Assign DrawnPathPrefab here
    [SerializeField] private Transform creationPoint;
    private DrawnPath currentPath;

    // public Transform[] tailTransform;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer tailRenderer;
    [SerializeField] private Transform tailTransform;

    [SerializeField] private Collider2D bodyCollider; // the CircleCollider2D
    [SerializeField] private float groundCheckDepth = 0.1f;
    
    [Header("Path Creation")]
    public Slider trialSlider;
    
    public static event Action OnSliderReduced;
    public static event Action OnSliderIncreased;
    public Material lineMaterial;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Find actions in the asset
        moveAction = inputActions.FindAction("Move");
        jumpAction = inputActions.FindAction("Jump");
        rightMouse = inputActions.FindAction("mouse_right");

        sprite = transform.GetComponent<SpriteRenderer>();
        trialSlider.maxValue = 100f;
        isJumping = false;
    }

    private void OnEnable()
    {
        inputActions?.Enable();

        if (jumpAction != null)
        {
            jumpAction.performed += OnJumpPerformed;
        }
        OnSliderReduced += onSliderReduced;
        OnSliderIncreased += onSliderIncreased;
    }

    private void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.performed -= OnJumpPerformed;
        }
        OnSliderReduced -= onSliderReduced;
        OnSliderIncreased -= onSliderIncreased;
        inputActions?.Disable();
    }

    private void Update()
    {
        // 1. Read input
        if (moveAction != null)
        {
            moveInput = moveAction.ReadValue<Vector2>();
        }

        if(moveInput.x >= 1f)
        {
        
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else if(moveInput.x <= -1f)
        {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        if(canReduce){
            HandleScaleReduction();
        }

        // 3. Air-path creation with cooldown
        if (!isGrounded && rightMouse != null && rightMouse.IsPressed())
        {
            HandlePathDrawing();
        }
        else
        {
            OnSliderIncreased?.Invoke();
        }

        // HandlePathDrawing();
    }

    public void flip(bool isFlipped)
    {
        // 2. Invert tail offset position across the X-axis
        if (tailTransform != null)
        {
            Vector3 pos = tailTransform.localPosition;
            pos.x *= -1f;
            tailTransform.localPosition = pos;
        }
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        ApplyMovement();
        ApplyBetterJumpingFeel();

        if (jumpRequested)
        {
            ExecuteJump();
        }
    }

    private void ApplyMovement()
    {
        // Set horizontal velocity while preserving Rigidbody2D vertical gravity
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        rb.AddForce(new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y));
    }

    private void ApplyBetterJumpingFeel()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (jumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    private void ExecuteJump()
    {
        // Reset vertical velocity so jump height is consistent
        if(isJumping && jumpCount == 1)
        {
            isJumping = false;
            jumpHeight = secondJumpForce;
        }
        else
        {
            jumpHeight = jumpForce;
            isJumping = true;
        }
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpHeight);
        jumpRequested = false;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (isGrounded || (isJumping && jumpCount < 2))
        {
            jumpCount++;
            jumpRequested = true;
        }
    }

    private void onSliderReduced()
    {
        trialSlider.value -= 30f * Time.deltaTime;
    }

    private void onSliderIncreased()
    {
        trialSlider.value += 10f * Time.deltaTime;
    }

    private void HandlePathDrawing()
    {
        if (rightMouse == null || creationPoint == null) return;

        OnSliderReduced?.Invoke();

        // 1. Start a new line: create an empty object that builds its own line + collider
        if (rightMouse.WasPressedThisFrame() && !isGrounded)
        {
            GameObject go = new GameObject("DrawnPath");
            go.layer = LayerMask.NameToLayer("Ground"); // so your ground check sees it
            currentPath = go.AddComponent<DrawnPath>();
            currentPath.GetComponent<LineRenderer>().material = lineMaterial;
            currentPath.GetComponent<LineRenderer>().sortingOrder = 2;
            currentPath.AddPoint(creationPoint.position);
        }

        // 2. Keep adding points while held
        if (rightMouse.IsPressed() && currentPath != null)
        {
            currentPath.AddPoint(creationPoint.position);
        }

        // 3. Release: make it a platform and start the fade timer
        if (rightMouse.WasReleasedThisFrame() && currentPath != null)
        {
            // currentPath.Finish();
            currentPath = null;
        }
    }
    
    private void HandleScaleReduction()
    {
        if (!canReduce) return;

        if (Mathf.Abs(moveInput.x) > 0.01f || Mathf.Abs(moveInput.y) > 0.01f)
        {
            Vector3 currentScale = transform.localScale;
            float newX = Mathf.Max(0.3f, currentScale.x - 0.1f * Time.deltaTime);
            float newY = Mathf.Max(0.3f, currentScale.y - 0.1f * Time.deltaTime);

            transform.localScale = new Vector3(newX, newY, 1f);
        }
    }


    private void CheckGrounded()
    {
        if (groundCheckPoint != null)
        {
            Bounds b = bodyCollider.bounds;
            Vector2 pos  = new Vector2(b.center.x, b.min.y);           // straddles the bottom edge
            Vector2 size = new Vector2(b.size.x * 0.5f, groundCheckDepth);
            isGrounded = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0f, groundLayer);
            if (isGrounded)
            {
                jumpCount = 0;
                isJumping = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }
    }
}