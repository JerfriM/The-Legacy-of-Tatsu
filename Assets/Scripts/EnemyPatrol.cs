using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    // ==========================================================
    // 1. VARIABLES
    // ==========================================================
    [Header("Movimiento")]
    public float moveSpeed = 1f; // Velocidad base de patrullaje
    [Tooltip("Cantidad de daño que causa el enemigo al tocar al jugador.")]
    public int attackDamage = 1;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sr;
    private bool facingRight = true;
    private float horizontalDirection = 1f; // Controla la dirección actual (1 o -1)

    [Header("Detección de Giro")]
    [Tooltip("Punto de inicio para el Raycast de chequeo de borde/suelo.")]
    public Transform controladorAbajo; // Objeto hijo para el Raycast de borde
    [Tooltip("Punto de inicio para el Raycast de chequeo de pared/muro.")]
    public Transform controladorEnfrente; // Objeto hijo para el Raycast de pared
    public float groundCheckDistance = 0.05f;
    [Tooltip("0.0 para ignorar paredes (enemigos de suelo). >0.0 para activar chequeo de pared.")]
    public float wallCheckDistance = 0.1f;

    [Header("Capas de Detección")]
    [Tooltip("Capas que representan el suelo/plataforma para evitar caer.")]
    public LayerMask layerMaskAbajo; // Filtro solo para el suelo
    [Tooltip("Capas que representan las paredes/muros para evitar chocar.")]
    public LayerMask layerMaskEnfrente; // Filtro para paredes y obstáculos

    [Header("Configuración de Rebote")]
    [Tooltip("Fuerza con la que rebota el jugador al saltar sobre la cabeza.")]
    public float bounceForce = 8f;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Inicializar la dirección basado en la rotación inicial del objeto
        // Un objeto con Y > 0 o rotación alrededor de Z tendrá transform.right apuntando
        // Se asume que la rotación inicial es 0 o cerca de 0.
        if (transform.eulerAngles.y > 90f && transform.eulerAngles.y < 270f)
        {
            facingRight = false;
        }
    }

    void Update()
    {
        // La dirección horizontal se basa en si el enemigo está mirando a la derecha (1) o no (-1)
        horizontalDirection = facingRight ? 1f : -1f;

        // 1. Aplicar movimiento
        // NOTA: moveSpeed ya incluye el signo negativo para la dirección
        rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);

        // 2. Controlar la animación
        if (anim != null)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }

        // 3. Lógica de Giro: Chequeo de Borde y Pared
        CheckForTurnConditions();
    }

    // Función que consolida la lógica de borde y pared
    private void CheckForTurnConditions()
    {
        // Se necesitan ambos controladores para funcionar
        if (controladorAbajo == null || controladorEnfrente == null) return;

        // A. Chequeo de Borde (Raycast Hacia Abajo)
        bool isGroundedAhead = Physics2D.Raycast(
            controladorAbajo.position,
            -transform.up, // Abajo local
            groundCheckDistance,
            layerMaskAbajo
        );

        // B. Chequeo de Pared (Raycast Lateral) - CRÍTICO!
        bool hitWall = false;
        if (wallCheckDistance > 0.01f)
        {
            hitWall = Physics2D.Raycast(
                controladorEnfrente.position, // Desde el controlador delantero
                transform.right, // Adelante local (se invierte gracias a la rotación de 180°)
                wallCheckDistance,
                layerMaskEnfrente
            );
        }

        // C. Condiciones de Giro
        if (!isGroundedAhead || hitWall)
        {
            if (!isGroundedAhead)
                Debug.Log("ENEMIGO DETECTA BORDE. FLIP!");
            else if (hitWall)
                Debug.Log("ENEMIGO DETECTA PARED LATERAL CON RAYCAST. FLIP!");

            Flip();
        }
    }

    // *****************************************************************
    // FUNCIÓN FLIP ACTUALIZADA CON ROTACIÓN DE 180°
    // *****************************************************************
    void Flip()
    {
        // 1. Detener el movimiento ANTES de invertir para evitar el bucle
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // 2. Invierte la dirección de movimiento
        moveSpeed *= -1;

        // 3. FORZAR SEPARACIÓN (CRÍTICO para romper el bucle de colisión)
        float pushBackAmount = 0.05f;
        transform.position += new Vector3(pushBackAmount * -horizontalDirection, 0, 0);

        // 4. CAMBIO CLAVE: Gira el objeto 180 grados en Y (rotación de transform)
        // Esto asegura que transform.right y transform.up giren, y con ellos, los Raycasts.
        transform.eulerAngles += new Vector3(0, 180, 0);

        // 5. Invierte el estado lógico
        facingRight = !facingRight;

        // NOTA: Si tu sprite no está contenido en un objeto padre y solo usas la rotación
        // del objeto principal, debes asegurarte que la rotación Y no afecte la rotación Z
        // para un juego 2D, o que el SpriteRenderer esté bien configurado.
    }

    // *******************************************************************
    // LÓGICA DE COMBATE Y COLISIÓN (Se mantiene sin cambios)
    // *******************************************************************

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody2D playerRB = other.GetComponent<Rigidbody2D>();

            // Condición Crítica: SOLO MUERE SI EL JUGADOR ESTÁ CAYENDO (stomp)
            if (playerRB != null && playerRB.linearVelocity.y < 0)
            {
                // Aplicar rebote al jugador
                playerRB.linearVelocity = new Vector2(playerRB.linearVelocity.x, 0f);
                playerRB.AddForce(Vector3.up * bounceForce, ForceMode2D.Impulse);

                Die();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Verifica que la colisión sea con el jugador (Daño)
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();

            if (player != null)
            {
                // NOTA: Debes asegurarte de que tu clase PlayerController existe
                player.TakeDamage(attackDamage);
            }
        }
    }

    // --- DEBUG VISUAL (OnDrawGizmosSelected) ---
    private void OnDrawGizmosSelected()
    {
        if (controladorAbajo == null || controladorEnfrente == null) return;

        // 1. Dibuja el Raycast de Borde (Rojo)
        Gizmos.color = Color.red;
        // Dibuja la línea de detección de borde. Usa -transform.up (funciona con cualquier rotación)
        Gizmos.DrawLine(controladorAbajo.position, controladorAbajo.position + -transform.up * groundCheckDistance);

        // 2. Dibuja el Raycast de Muro (Amarillo)
        if (wallCheckDistance > 0.01f)
        {
            Gizmos.color = Color.yellow;
            // Dibuja la línea de detección de pared. Usa transform.right (funciona con la rotación de 180°)
            Gizmos.DrawLine(controladorEnfrente.position, controladorEnfrente.position + transform.right * wallCheckDistance);
        }
    }

    void Die()
    {
        Debug.Log(gameObject.name + " ha sido derrotado.");

        // Detener movimiento al morir (Bug Fix)
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
}