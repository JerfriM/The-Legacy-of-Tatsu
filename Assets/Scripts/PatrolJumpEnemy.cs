using UnityEngine;
using System.Collections;

public class PatrolJumpEnemy : MonoBehaviour
{
    // ==========================================================
    // 1. VARIABLES DE MOVIMIENTO Y ESTADO
    // ==========================================================
    [Header("Movimiento General")]
    public float moveSpeed = 1f; // Velocidad base de patrullaje
    private Rigidbody2D rb;
    private Animator anim;
    private bool facingRight = true;
    private float horizontalDirection = 1f; // Dirección actual (1 o -1)

    // ==========================================================
    // 2. CONTROLADORES Y DETECCIÓN
    // ==========================================================
    [Header("Controladores de Patrulla")]
    [Tooltip("Punto de inicio para el Raycast de chequeo de borde/suelo.")]
    public Transform controladorAbajo;
    [Tooltip("Punto de inicio para el Raycast de chequeo de pared/muro.")]
    public Transform controladorEnfrente;
    public float groundCheckDistance = 0.05f;
    [Tooltip("Distancia para activar chequeo de pared.")]
    public float wallCheckDistance = 0.1f;

    [Header("Capas de Detección")]
    public LayerMask layerMaskAbajo; // Filtro solo para el suelo (Borde)
    public LayerMask layerMaskEnfrente; // Filtro para paredes

    // ==========================================================
    // 3. LÓGICA DE SALTO Y SEGUIMIENTO
    // ==========================================================
    [Header("Salto Programado")]
    [Tooltip("Fuerza vertical aplicada al Rigidbody2D para el salto.")]
    [SerializeField] private float jumpForce = 5f;
    [Tooltip("Tiempo que transcurre entre saltos.")]
    [SerializeField] private float jumpInterval = 3f;
    [SerializeField] private string jumpTriggerName = "Jump";
    [Tooltip("Tiempo que el Collider estará deshabilitado al saltar.")]
    [SerializeField] private float jumpColliderDisableTime = 0.1f;

    private Collider2D enemyCollider;
    private bool isGrounded = false; // Estado de suelo

    [Header("Seguimiento y Combate")]
    [Tooltip("Referencia al Transform del jugador.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Radio de detección. Si el jugador está dentro, el enemigo persigue horizontalmente.")]
    [SerializeField] private float detectionRange = 5.0f;
    [Tooltip("Multiplicador de velocidad de persecución.")]
    [SerializeField] private float chaseSpeedMultiplier = 1.5f;

    [Tooltip("Cantidad de daño que causa el enemigo al tocar al jugador.")]
    public int attackDamage = 1;
    [Tooltip("Fuerza con la que rebota el jugador al saltar sobre la cabeza.")]
    public float bounceForce = 8f;

    // Nombres de parámetros del Animator
    private const string ANIM_SPEED = "Speed";
    private const string ANIM_ISGROUNDED = "IsGrounded";

    // ==========================================================
    // INICIALIZACIÓN
    // ==========================================================
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        // Inicializar la dirección basada en la rotación inicial del objeto
        if (Mathf.Abs(transform.eulerAngles.y - 180f) < 1f)
        {
            facingRight = false;
        }

        // Búsqueda del jugador
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        // Iniciar el ciclo de salto/ataque periódico
        StartCoroutine(JumpCycle());
    }

    void Update()
    {
        // Actualizar la dirección horizontal
        horizontalDirection = facingRight ? 1f : -1f;

        // 1. Detección de Suelo/Borde
        CheckGroundAndTurn();

        // 2. Determinar si perseguir o patrullar
        float currentSpeed = moveSpeed * horizontalDirection;

        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distanceToPlayer <= detectionRange)
            {
                // MODO PERSECUCIÓN
                currentSpeed = CalculateChaseSpeed();
            }
            // MODO PATRULLA: Si no hay jugador o está fuera de rango, usa moveSpeed * horizontalDirection
        }

        // 3. Aplicar movimiento
        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);

        // 4. Controlar la animación
        UpdateAnimator(currentSpeed);
    }

    // ==========================================================
    // LÓGICA DE MOVIMIENTO Y GIRO
    // ==========================================================

    private float CalculateChaseSpeed()
    {
        // Dirigir el movimiento horizontal hacia el jugador
        float targetDir = Mathf.Sign(playerTransform.position.x - transform.position.x);

        // Forzar el giro del sprite/transform para mirar al jugador
        if (targetDir > 0 && !facingRight)
            Flip();
        else if (targetDir < 0 && facingRight)
            Flip();

        // Regresar la velocidad de persecución
        return moveSpeed * chaseSpeedMultiplier * targetDir;
    }

    private void CheckGroundAndTurn()
    {
        // El chequeo de suelo/pared solo ocurre si los controladores están asignados
        if (controladorAbajo == null || controladorEnfrente == null) return;

        // A. Chequeo de Borde (Controlador Abajo)
        isGrounded = Physics2D.Raycast(
            controladorAbajo.position,
            -transform.up,
            groundCheckDistance,
            layerMaskAbajo
        );

        // B. Chequeo de Pared (Controlador Enfrente)
        bool hitWall = false;
        if (wallCheckDistance > 0.01f)
        {
            hitWall = Physics2D.Raycast(
                controladorEnfrente.position,
                transform.right,
                wallCheckDistance,
                layerMaskEnfrente
            );
        }

        // Si no hay suelo adelante o golpea pared, gira.
        if (!isGrounded || hitWall)
        {
            Flip();
        }
    }

    void Flip()
    {
        // Detener el movimiento ANTES de invertir para evitar el bucle
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Forzar separación para romper el bucle de colisión
        float pushBackAmount = 0.05f;
        transform.position += new Vector3(pushBackAmount * -horizontalDirection, 0, 0);

        // Gira el objeto 180 grados en Y
        transform.eulerAngles += new Vector3(0, 180, 0);

        // Invierte el estado lógico
        facingRight = !facingRight;
    }

    // ==========================================================
    // LÓGICA DE SALTO PROGRAMADO
    // ==========================================================

    private IEnumerator JumpCycle()
    {
        yield return new WaitForSeconds(0.2f); // Buffer inicial

        while (true)
        {
            yield return new WaitForSeconds(jumpInterval);

            // Solo saltar si está firmemente en el suelo
            if (isGrounded)
            {
                ExecuteJump();
            }
        }
    }

    // *** FUNCIÓN CORREGIDA PARA SALTAR EN LA DIRECCIÓN CORRECTA ***
    private void ExecuteJump()
    {
        // 1. Deshabilitar temporalmente el Collider
        if (enemyCollider != null && enemyCollider.enabled)
        {
            enemyCollider.enabled = false;
            StartCoroutine(ReactivateCollider(jumpColliderDisableTime));
        }

        // 2. Aplicar la fuerza
        // Mantiene la velocidad horizontal actual (rb.velocity.x) y reinicia la velocidad Y.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);

        // Aplica la fuerza vertical
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // 3. Ejecutar Trigger de animación
        if (anim != null)
        {
            anim.SetTrigger(jumpTriggerName);
        }
    }

    private IEnumerator ReactivateCollider(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (enemyCollider != null)
        {
            enemyCollider.enabled = true;
        }
    }

    private void UpdateAnimator(float currentSpeed)
    {
        if (anim == null) return;

        // IsGrounded
        anim.SetBool(ANIM_ISGROUNDED, isGrounded);

        // Speed (Usar la velocidad absoluta para las transiciones Idle/Run)
        anim.SetFloat(ANIM_SPEED, Mathf.Abs(currentSpeed));
    }

    // ==========================================================
    // LÓGICA DE COMBATE
    // ==========================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody2D playerRB = other.GetComponent<Rigidbody2D>();

            // Stomp (Morir al ser pisado)
            if (playerRB != null && playerRB.linearVelocity.y < 0)
            {
                playerRB.linearVelocity = new Vector2(playerRB.linearVelocity.x, 0f);
                playerRB.AddForce(Vector3.up * bounceForce, ForceMode2D.Impulse);
                Die();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Daño por contacto
        if (collision.gameObject.CompareTag("Player"))
        {
            // Se asume que la clase PlayerController existe
            // 
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();

            if (player != null)
            {
                player.TakeDamage(attackDamage);
            }
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " ha sido derrotado.");

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        Collider2D mainCollider = GetComponent<Collider2D>();
        if (mainCollider != null)
        {
            mainCollider.enabled = false;
        }

        Destroy(gameObject, 0.5f);
    }

    // ==========================================================
    // DEBUG VISUAL EN EL EDITOR (Gizmos)
    // ==========================================================
    private void OnDrawGizmosSelected()
    {
        // Dibuja el rango de detección del jugador (azul)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // --- 1. Dibuja el Raycast de Pared (Amarillo) ---
        if (controladorEnfrente != null && wallCheckDistance > 0.01f)
        {
            Gizmos.color = Color.yellow;
            Vector3 currentRayDirection = transform.right;
            Gizmos.DrawLine(controladorEnfrente.position, controladorEnfrente.position + currentRayDirection * wallCheckDistance);
        }

        // --- 2. Dibuja el Raycast de Borde (Rojo/Verde) ---
        if (controladorAbajo != null)
        {
            // Rojo si hay peligro (no detecta suelo), Verde si está seguro (detecta suelo)
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(controladorAbajo.position, controladorAbajo.position + -transform.up * groundCheckDistance);
        }
    }
}