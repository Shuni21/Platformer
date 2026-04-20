using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 direction = Vector3.right; // Направление (вправо)
    [SerializeField] private float distance = 5f;              // На сколько метров
    [SerializeField] private float speed = 3f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool movingToTarget = true;
    private Vector3 currentVelocity;

    private void Awake()
    {
        startPos = transform.position;
        targetPos = startPos + (direction.normalized * distance);
    }

    private void Update()
    {
        Vector3 previousPosition = transform.position;
        Vector3 goal = movingToTarget ? targetPos : startPos;

        // Плавное движение к цели
        transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);

        // Расчет реальной скорости (нужно для игрока)
        currentVelocity = (transform.position - previousPosition) / Time.deltaTime;

        // Если дошли до края — разворачиваемся
        if (Vector3.Distance(transform.position, goal) < 0.01f)
        {
            movingToTarget = !movingToTarget;
        }
    }

    // Этот метод игрок вызывает в своем скрипте
    public Vector3 GetVelocity()
    {
        return currentVelocity;
    }

    private void OnDrawGizmos()
    {
        // Рисуем линию пути в редакторе для удобства
        Gizmos.color = Color.cyan;
        Vector3 visualStart = Application.isPlaying ? startPos : transform.position;
        Vector3 visualEnd = visualStart + (direction.normalized * distance);
        Gizmos.DrawLine(visualStart, visualEnd);
        Gizmos.DrawWireCube(visualEnd, transform.localScale);
    }
}