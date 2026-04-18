using UnityEngine;

public class HybridCameraController : MonoBehaviour
{
    public enum CameraMode
    {
        FirstPerson,
        ThirdPerson
    }

    [Header("Mode")]
    [SerializeField] private CameraMode mode = CameraMode.ThirdPerson;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.6f, 0f);

    [Header("Camera")]
    [SerializeField] private Camera cam;

    [Header("Mouse Look")]
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("TPS Settings")]
    [SerializeField] private float distance = 3f;
    [SerializeField] private float tpsSmooth = 12f;

    [Header("FPS Settings")]
    [SerializeField] private float fpsSmooth = 12f;

    [Header("Zoom (FPS only)")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minFOV = 40f;
    [SerializeField] private float maxFOV = 90f;

    private float yaw;
    private float pitch = 10f;

    private float currentYaw;
    private float currentPitch;

    private void Start()
    {
        if (cam == null)
            cam = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;

        currentYaw = yaw;
        currentPitch = pitch;
    }

    private void LateUpdate()
    {
        if (target == null || cam == null) return;

        HandleMouse();
        HandleZoom();

        if (mode == CameraMode.FirstPerson)
            UpdateFPS();
        else
            UpdateTPS();

        // переключение камеры
        if (Input.GetKeyDown(KeyCode.V))
        {
            mode = mode == CameraMode.FirstPerson
                ? CameraMode.ThirdPerson
                : CameraMode.FirstPerson;
        }
    }

    // ---------------- INPUT ----------------
    private void HandleMouse()
    {
        yaw += Input.GetAxis("Mouse X") * sensitivity;
        pitch -= Input.GetAxis("Mouse Y") * sensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    // ---------------- ZOOM FPS ----------------
    private void HandleZoom()
    {
        if (mode != CameraMode.FirstPerson) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        cam.fieldOfView -= scroll * zoomSpeed;
        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minFOV, maxFOV);
    }

    // ---------------- FPS ----------------
    private void UpdateFPS()
    {
        currentYaw = Mathf.LerpAngle(currentYaw, yaw, fpsSmooth * Time.deltaTime);
        currentPitch = Mathf.LerpAngle(currentPitch, pitch, fpsSmooth * Time.deltaTime);

        Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0f);

        Vector3 headPos = target.position + offset;

        cam.transform.position = Vector3.Lerp(
            cam.transform.position,
            headPos,
            fpsSmooth * Time.deltaTime
        );

        cam.transform.rotation = rot;
    }

    // ---------------- TPS ----------------
    private void UpdateTPS()
    {
        currentYaw = Mathf.LerpAngle(currentYaw, yaw, tpsSmooth * Time.deltaTime);
        currentPitch = Mathf.LerpAngle(currentPitch, pitch, tpsSmooth * Time.deltaTime);

        Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0f);

        Vector3 targetPos = target.position + offset;
        Vector3 camPos = targetPos - rot * Vector3.forward * distance;

        cam.transform.position = Vector3.Lerp(
            cam.transform.position,
            camPos,
            tpsSmooth * Time.deltaTime
        );

        cam.transform.rotation = rot;
    }
}