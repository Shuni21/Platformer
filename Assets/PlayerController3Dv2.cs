using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3Dv2 : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float airControl = 0.5f;
    [SerializeField] private float jumpHeight = 1.8f;
    [SerializeField] private float gravity = -25f;

    [Header("Advanced Jump")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckDistance = 0.4f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform playerBody;
    [SerializeField] private float mouseSensitivity = 180f;
    [SerializeField] private float minLookAngle = -75f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private float lookSmooth = 12f;

    private CharacterController controller;
    private Animator animator;

    private Vector3 velocity;
    private float pitch;
    private float currentPitch;
    private bool isGrounded;
    private RaycastHit groundHit;

    private float coyoteCounter;
    private float jumpBufferCounter;

    // RAGDOLL
    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private bool isRagdoll = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        if (cameraPivot == null && Camera.main != null)
        {
            cameraPivot = Camera.main.transform.parent != null
                ? Camera.main.transform.parent
                : Camera.main.transform;
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetRagdoll(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            EnableRagdoll();

        if (Input.GetKeyDown(KeyCode.T))
            DisableRagdoll();

        if (isRagdoll) return;

        UpdateGroundedState();
        HandleLook();
        HandleJumpTimers();
        HandleMovement();
    }

    // ---------------- GROUND CHECK ----------------
    private void UpdateGroundedState()
    {
        Vector3 origin = groundCheckPoint.position;

        isGrounded = Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out groundHit,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
    }

    // ---------------- LOOK ----------------
    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
        else
            transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minLookAngle, maxLookAngle);

        currentPitch = Mathf.Lerp(currentPitch, pitch, lookSmooth * Time.deltaTime);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    // ---------------- JUMP TIMERS ----------------
    private void HandleJumpTimers()
    {
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;
    }

    // ---------------- MOVEMENT ----------------
    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = (transform.right * horizontal + transform.forward * vertical).normalized;

        float control = isGrounded ? 1f : airControl;
        Vector3 move = inputDir * moveSpeed * control;

        controller.Move(move * Time.deltaTime);

        bool isMoving = inputDir.magnitude > 0.1f;

        if (animator != null)
            animator.SetBool("isMoving", isMoving);

        // ---------------- JUMP ----------------
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        // ---------------- GRAVITY IMPROVED ----------------
        if (velocity.y < 0)
        {
            velocity.y += gravity * fallMultiplier * Time.deltaTime;
        }
        else if (velocity.y > 0 && !Input.GetButton("Jump"))
        {
            velocity.y += gravity * lowJumpMultiplier * Time.deltaTime;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    // ---------------- RAGDOLL ----------------
    private void EnableRagdoll()
    {
        isRagdoll = true;
        SetRagdoll(true);
    }

    private void DisableRagdoll()
    {
        isRagdoll = false;
        SetRagdoll(false);

        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    private void SetRagdoll(bool state)
    {
        foreach (Rigidbody rb in ragdollBodies)
        {
            if (rb.gameObject != gameObject)
            {
                rb.isKinematic = !state;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }

        foreach (Collider col in ragdollColliders)
        {
            if (col.gameObject != gameObject)
                col.enabled = state;
        }

        controller.enabled = !state;

        if (animator != null)
            animator.enabled = !state;
    }

    // ---------------- DEBUG ----------------
    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}