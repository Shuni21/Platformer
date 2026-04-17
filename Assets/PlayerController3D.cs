using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float airControl = 0.5f;
    [SerializeField] private float jumpHeight = 1.8f;
    [SerializeField] private float gravity = -25f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float mouseSensitivity = 180f;
    [SerializeField] private float minLookAngle = -75f;
    [SerializeField] private float maxLookAngle = 80f;

    private CharacterController controller;
    private Vector3 velocity;
    private float pitch;
    private bool isGrounded;

    // 🔥 ДОБАВИЛИ
    private Animator animator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

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
    }

    private void Update()
    {
        UpdateGroundedState();
        HandleLook();
        HandleMovement();
    }

    private void UpdateGroundedState()
    {
        if (groundCheckPoint != null)
        {
            isGrounded = Physics.CheckSphere(
                groundCheckPoint.position,
                groundCheckRadius,
                groundMask,
                QueryTriggerInteraction.Ignore
            );
        }
        else
        {
            isGrounded = controller.isGrounded;
        }

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        if (cameraPivot == null) return;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minLookAngle, maxLookAngle);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = (transform.right * horizontal + transform.forward * vertical).normalized;

        float control = isGrounded ? 1f : airControl;
        Vector3 move = inputDirection * moveSpeed * control;

        controller.Move(move * Time.deltaTime);


        bool isMoving = inputDirection.magnitude > 0.1f;

        if (animator != null)
        {
            animator.SetBool("isMoving", isMoving);
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
    }
}