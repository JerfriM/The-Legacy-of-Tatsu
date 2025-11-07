using UnityEngine;
using System.Collections;

public class MovingPlatform : MonoBehaviour
{
    private enum MovementType { X_Horizontal, Y_Vertical, XY_Diagonal }

    // ==========================================================
    // 1. VARIABLES AJUSTABLES
    // ==========================================================
    [Header("Configuración de Movimiento")]
    public float speed = 2f;
    public float pauseTime = 1f;

    [Header("Patrulla Basada en Distancia")]
    [SerializeField] private MovementType movementType = MovementType.XY_Diagonal;
    [SerializeField] private float patrolDistanceX = 5f;
    [SerializeField] private float patrolDistanceY = 5f;

    // ==========================================================
    // 2. VARIABLES INTERNAS
    // ==========================================================
    private Vector3 startPoint;
    private Vector3 endPoint;
    private Vector3 nextTarget;

    // >>> VARIABLES AÑADIDAS PARA LA CORRECCIÓN DEL ERROR DE JERARQUÍA
    private Transform playerToDetach = null;
    private bool detachScheduled = false;

    // ==========================================================
    // INICIALIZACIÓN Y CICLO
    // ==========================================================
    void Start()
    {
        startPoint = transform.position;

        float targetX = startPoint.x;
        float targetY = startPoint.y;

        if (movementType == MovementType.X_Horizontal || movementType == MovementType.XY_Diagonal)
        {
            targetX += patrolDistanceX;
        }

        if (movementType == MovementType.Y_Vertical || movementType == MovementType.XY_Diagonal)
        {
            targetY += patrolDistanceY;
        }

        endPoint = new Vector3(targetX, targetY, startPoint.z);
        nextTarget = endPoint;
        StartCoroutine(MovementCycle());
    }

    void LateUpdate()
    {
        // EJECUTAMOS LA DESVINCULACIÓN AQUÍ DE FORMA SEGURA
        if (detachScheduled && playerToDetach != null)
        {
            playerToDetach.SetParent(null);

            playerToDetach = null;
            detachScheduled = false;
        }
    }

    // Coroutine y MoveToTarget... (sin cambios)
    private IEnumerator MovementCycle()
    {
        while (true)
        {
            yield return StartCoroutine(MoveToTarget(nextTarget));
            yield return new WaitForSeconds(pauseTime);

            if (nextTarget == startPoint)
            {
                nextTarget = endPoint;
            }
            else
            {
                nextTarget = startPoint;
            }
        }
    }

    private IEnumerator MoveToTarget(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime
            );
            yield return null;
        }
        transform.position = target;
    }

    // ==========================================================
    // LÓGICA DE HERENCIA DE VELOCIDAD
    // ==========================================================

    // Al entrar (Vinculación inmediata)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.transform.SetParent(transform);

            // Si estaba programado para desvincularse, cancelamos.
            detachScheduled = false;
            playerToDetach = null;
        }
    }

    // Al salir (Solo programamos la desvinculación)
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Programar la desvinculación para LateUpdate
            playerToDetach = collision.gameObject.transform;
            detachScheduled = true;
        }
    }

    // ==========================================================
    // DEBUG VISUAL (Gizmos)
    // ==========================================================
    private void OnDrawGizmosSelected()
    {
        Vector3 currentStart = Application.isPlaying ? startPoint : transform.position;
        float debugTargetX = currentStart.x;
        float debugTargetY = currentStart.y;

        if (movementType == MovementType.X_Horizontal || movementType == MovementType.XY_Diagonal)
        {
            debugTargetX += patrolDistanceX;
        }

        if (movementType == MovementType.Y_Vertical || movementType == MovementType.XY_Diagonal)
        {
            debugTargetY += patrolDistanceY;
        }

        Vector3 currentEnd = new Vector3(debugTargetX, debugTargetY, currentStart.z);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(currentStart, currentEnd);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(currentStart, 0.2f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(currentEnd, 0.2f);
    }
}