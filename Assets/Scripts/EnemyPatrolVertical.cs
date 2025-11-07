using UnityEngine;
using System.Collections;

public class VerticalPatrolEnemy : MonoBehaviour
{
    // ==========================================================
    // 1. VARIABLES DE MOVIMIENTO VERTICAL Y TIEMPO
    // ==========================================================
    [Header("Patrulla Vertical")]
    [Tooltip("Distancia vertical que el enemigo recorrerá hacia abajo (en unidades de Unity).")]
    [SerializeField] private float patrolDistance = 3.0f;

    [Tooltip("Velocidad de movimiento del enemigo.")]
    [SerializeField] private float speed = 1.5f;
    [Tooltip("Tiempo de pausa en cada punto final (arriba y abajo).")]
    [SerializeField] private float pauseTime = 1.0f;

    // Puntos Internos de Control
    private Vector3 topPoint;
    private Vector3 bottomPoint;
    private Vector3 nextTarget;

    // Componentes
    private Animator anim;
    private SpriteRenderer sr;

    // ==========================================================
    // 2. VARIABLES DE COMBATE Y DETECCIÓN
    // ==========================================================
    [Header("Detección y Combate")]
    [Tooltip("Referencia al Transform del jugador.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Cantidad de daño que causa el enemigo.")]
    [SerializeField] private int attackDamage = 1;

    // ==========================================================
    // INICIALIZACIÓN
    // ==========================================================
    void Awake()
    {
        // Obtener componentes en Awake para asegurar que estén disponibles en Start
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // Intenta encontrar el jugador si no está asignado
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        // 1. Establecer el PUNTO SUPERIOR como la posición inicial en la escena
        topPoint = transform.position;

        // 2. Calcular el PUNTO INFERIOR basado en la distancia
        bottomPoint = new Vector3(topPoint.x, topPoint.y - patrolDistance, topPoint.z);

        // 3. El primer destino siempre será el punto inferior
        nextTarget = bottomPoint;

        // Iniciar el ciclo de patrulla
        StartCoroutine(PatrolCycle());
    }

    void Update()
    {
        // Lógica de Flip Horizontal (Mirar al Jugador)
        FlipTowardsPlayer();
    }

    // ==========================================================
    // FUNCIÓN DE FLIP BASADA EN LA POSICIÓN DEL JUGADOR
    // ==========================================================
    private void FlipTowardsPlayer()
    {
        if (sr == null || playerTransform == null) return;

        float playerDirX = playerTransform.position.x - transform.position.x;

        // Usamos sr.flipX. Si el enemigo mira a la derecha (positivo), flipX es false.
        // Si el enemigo mira a la izquierda (negativo), flipX es true.
        if (playerDirX > 0.01f) // Pequeño margen para evitar jitter
        {
            sr.flipX = false;
        }
        else if (playerDirX < -0.01f)
        {
            sr.flipX = true;
        }
    }

    // ==========================================================
    // CORRUTINAS DE PATRULLA (Movimiento Cíclico y Pausa con Scream)
    // ==========================================================
    private IEnumerator PatrolCycle()
    {
        while (true) // Bucle infinito
        {
            // 1. Mover hacia el destino actual (topPoint o bottomPoint)
            yield return StartCoroutine(MoveToTarget(nextTarget));

            // 2. Pausa y Scream (El enemigo llega al punto y grita/alerta)

            // --- ANIMACIÓN DE PAUSA: Activar el Grito/Pausa ---
            if (anim != null)
            {
                // Dispara el Trigger 'Scream' (y detiene la animación de movimiento si existe)
                anim.SetTrigger("Scream");
            }

            // Esperar el tiempo de pausa (el grito se reproduce durante esta pausa)
            yield return new WaitForSeconds(pauseTime);

            // --- ANIMACIÓN DE MOVIMIENTO: Desactivar Scream/Activar Movimiento ---
            if (anim != null)
            {
                // Si tienes un Trigger para el movimiento (ej: "Move"), actívalo aquí,
                // o si tienes una variable bool 'IsMoving', ponla en true.
                // Ejemplo: anim.SetBool("IsMoving", true); 
            }


            // 3. Invertir la dirección y el target
            if (nextTarget == topPoint)
            {
                nextTarget = bottomPoint;
            }
            else
            {
                nextTarget = topPoint;
            }
        }
    }

    private IEnumerator MoveToTarget(Vector3 target)
    {
        // --- ANIMACIÓN DE MOVIMIENTO: Activar el Movimiento ---
        if (anim != null)
        {
            // Si tienes un Trigger para el movimiento (ej: "Move"), actívalo aquí,
            // o si tienes una variable bool 'IsMoving', ponla en true.
            // Ejemplo: anim.SetTrigger("StartMoving"); 
        }

        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }

        // Ajuste final para asegurar que la posición sea exacta
        transform.position = target;
    }

    // ==========================================================
    // LÓGICA DE COLISIÓN (DAÑO)
    // ==========================================================
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Lógica de daño solo si el jugador colisiona
        if (collision.gameObject.CompareTag("Player"))
        {
            // Nota: Se asume que el jugador tiene el script 'PlayerController' y el método 'TakeDamage(int)'.
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();

            if (player != null)
            {
                player.TakeDamage(attackDamage);
            }
        }
    }

    // ==========================================================
    // DEBUG VISUAL
    // ==========================================================
    private void OnDrawGizmos()
    {
        // Calcular los puntos de patrulla solo para visualización en el Editor
        Vector3 currentTop = Application.isPlaying ? topPoint : transform.position;
        Vector3 currentBottom = new Vector3(currentTop.x, currentTop.y - patrolDistance, currentTop.z);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(currentTop, 0.2f);
        Gizmos.DrawWireSphere(currentBottom, 0.2f);
        Gizmos.DrawLine(currentTop, currentBottom);
    }
}