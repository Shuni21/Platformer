using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController3Dv2 : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float airControl = 0.5f;
    [SerializeField] private float jumpHeight = 1.8f;
    [SerializeField] private float gravity = -25f;

    [Header("Jump Physics (Feel)")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Ground Check (Automatic)")]
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private float groundOffset = 0.1f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform playerBody;
    [SerializeField] private float mouseSensitivity = 180f;
    [SerializeField] private float minLookAngle = -75f;
    [SerializeField] private float maxLookAngle = 80f;
    [SerializeField] private float lookSmooth = 12f;

    // Ссылки на компоненты
    private CharacterController controller;
    private Animator animator;
    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;

    // Состояние движения
    private Vector3 verticalVelocity;
    private Vector3 platformVelocity; // Дополнительная скорость от платформ
    private float pitch;
    private float currentPitch;
    private bool isGrounded;
    private bool isRagdoll = false;

    // Таймеры
    private float coyoteCounter;
    private float jumpBufferCounter;

    private void Awake()
    {
        // Инициализация компонентов
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Сбор костей Ragdoll
        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        // Настройка камеры, если не задана вручную
        if (cameraPivot == null && Camera.main != null)
        {
            cameraPivot = Camera.main.transform.parent != null
                ? Camera.main.transform.parent
                : Camera.main.transform;
        }
    }

    private void Start()
    {
        // Прячем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Выключаем Ragdoll при старте
        SetRagdoll(false);
    }

    private void Update()
    {
        // Проверка клавиш Ragdoll
        if (Input.GetKeyDown(KeyCode.Y)) EnableRagdoll();
        if (Input.GetKeyDown(KeyCode.T)) DisableRagdoll();

        // Если мы "кукла", управление не работает
        if (isRagdoll) return;

        UpdateGroundedState();
        HandleLook();
        HandleJumpTimers();
        HandleMovement();
    }

    // --- ПРОВЕРКА ЗЕМЛИ И ПЛАТФОРМ ---
    private void UpdateGroundedState()
    {
        Vector3 origin = GetGroundCheckPosition();
        RaycastHit hit;

        // Используем SphereCast для более надежной проверки
        isGrounded = Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (isGrounded)
        {
            // Сброс вертикальной скорости при приземлении
            if (verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }

            // ПРОВЕРКА ПОДВИЖНОЙ ПЛАТФОРМЫ
            MovingPlatform platform = hit.collider.GetComponent<MovingPlatform>();
            if (platform != null)
            {
                platformVelocity = platform.GetVelocity();
            }
            else
            {
                platformVelocity = Vector3.zero;
            }
        }
        else
        {
            // Плавное затухание скорости платформы в прыжке
            platformVelocity = Vector3.Lerp(platformVelocity, Vector3.zero, Time.deltaTime * 2f);
        }
    }

    private Vector3 GetGroundCheckPosition()
    {
        // Автоматически вычисляем позицию стоп персонажа
        return transform.position + controller.center + (Vector3.down * (controller.height / 2f)) + (Vector3.up * groundOffset);
    }

    // --- ПОВОРОТ КАМЕРЫ ---
    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Вращение тела влево-вправо
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
        else
            transform.Rotate(Vector3.up * mouseX);

        // Вращение головы/камеры вверх-вниз
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minLookAngle, maxLookAngle);
        currentPitch = Mathf.Lerp(currentPitch, pitch, lookSmooth * Time.deltaTime);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    // --- ТАЙМЕРЫ ПРЫЖКА ---
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

    // --- ПЕРЕМЕЩЕНИЕ И АНИМАЦИИ ---
    private void HandleMovement()
    {
        // Горизонтальный ввод (WASD)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = (transform.right * horizontal + transform.forward * vertical).normalized;

        float control = isGrounded ? 1f : airControl;
        Vector3 move = inputDir * moveSpeed * control;

        // Расчет прыжка и гравитации
        ApplyJumpAndGravity();

        // ФИНАЛЬНОЕ ДВИЖЕНИЕ (Сумма всех векторов)
        Vector3 finalMotion = move + verticalVelocity + platformVelocity;
        controller.Move(finalMotion * Time.deltaTime);

        // Обновление аниматора
        if (animator != null)
        {
            animator.SetBool("isMoving", inputDir.magnitude > 0.1f);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetFloat("yVelocity", verticalVelocity.y);
        }
    }

    private void ApplyJumpAndGravity()
    {
        // Прыгаем, только если есть заряд Coyote и Buffer
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // Сброс таймеров
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;

            // Триггер для мгновенной анимации прыжка
            if (animator != null)
            {
                animator.SetTrigger("JumpTrigger");
            }
        }

        // Физика падения
        if (verticalVelocity.y < 0) // Мы падаем вниз
        {
            verticalVelocity.y += gravity * fallMultiplier * Time.deltaTime;
        }
        else if (verticalVelocity.y > 0 && !Input.GetButton("Jump")) // Отпустили пробел (короткий прыжок)
        {
            verticalVelocity.y += gravity * lowJumpMultiplier * Time.deltaTime;
        }
        else // Обычный полет вверх
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }
    }

    // --- СИСТЕМА RAGDOLL ---
    private void EnableRagdoll()
    {
        isRagdoll = true;
        SetRagdoll(true);
    }

    private void DisableRagdoll()
    {
        isRagdoll = false;
        SetRagdoll(false);
        // Выпрямляем объект после падения
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
            }
        }

        foreach (Collider col in ragdollColliders)
        {
            if (col.gameObject != gameObject)
                col.enabled = state;
        }

        controller.enabled = !state;
        if (animator != null) animator.enabled = !state;
    }

    // --- ОТЛАДКА В РЕДАКТОРЕ ---
    private void OnDrawGizmosSelected()
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        Vector3 origin = GetGroundCheckPosition();

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
    }
}