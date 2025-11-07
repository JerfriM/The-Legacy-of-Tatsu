using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

// Asegura que PlayerController tiene el método TakeDamage(int damageAmount)
public class RedOrc : MonoBehaviour
{
    // ==========================================================
    // 1. CONFIGURACIÓN GENERAL Y COMPONENTES
    // ==========================================================
    [Header("Componentes")]
    [HideInInspector] public Rigidbody2D rb;
    private Animator anim;
    private Collider2D enemyCollider;
    private SpriteRenderer sr;
    private bool facingRight = true;
    [HideInInspector] public bool isDead = false;

    // --- EFECTOS VISUALES DE DAÑO Y TEMPORIZADOR ---
    [Header("Efectos de Daño")]
    public float flashDuration = 0.1f;
    public Color hurtColor = Color.red;
    private Color originalColor;

    [Header("Temporizador de Combate")]
    public float stompCooldown = 0.5f;
    private bool canBeStomped = true;

    // ==========================================================
    // 2. CONFIGURACIÓN DE MOVIMIENTO Y PATRULLA
    // ==========================================================
    [Header("Movimiento y Persecución")]
    public float moveSpeed = 1f;
    public float chaseSpeedMultiplier = 1.5f;
    public float detectionRange = 10.0f;
    [SerializeField] private Transform playerTransform;

    [Header("Controladores de Patrulla (Chequeos)")]
    public Transform controladorAbajo;
    public Transform controladorEnfrente;

    // Configuración para OverlapCircle
    [Tooltip("Radio de la esfera usada para la detección de suelo.")]
    public float checkRadius = 0.2f;

    public float groundCheckDistance = 0.05f;
    public float wallCheckDistance = 0.1f;
    public LayerMask layerMaskAbajo;
    public LayerMask layerMaskEnfrente;
    private bool isGrounded = false;

    // ==========================================================
    // 3. LÓGICA DE ATAQUE (Raycast Sincronizado) ⭐ MODIFICADO/AÑADIDO
    // ==========================================================
    [Header("Ataque y Rangos (Orco Rojo)")]
    public float attackCooldown = 1f;
    private float lastAttackTime;
    private bool isAttacking = false;

    // ⭐ NUEVAS VARIABLES PARA RAYCAST
    [Header("Configuración del Raycast de Ataque")]
    [Tooltip("Punto de inicio del Raycast de golpe. Debe ser un hijo del Orco.")]
    public Transform ataquePunto;
    [Tooltip("Distancia máxima a la que llega el golpe.")]
    public float distanciaAtaque = 1.0f;
    // FIN NUEVAS VARIABLES

    // ESTO YA NO ES NECESARIO SI USAS RAYCAST
    // public GameObject attackHitbox; 
    // public float midMeleeRange = 2f; // Usaremos 'distanciaAtaque' en su lugar

    private const string ANIM_ATTACK1 = "Attack1OrcRojo";
    private const string ANIM_ATTACK2 = "Attack2OrcRojo";
    private const string ANIM_ATTACK3 = "Attack3OrcRojo";
    private const string ANIM_RUN_ATTACK = "RunAttackOrcRojo";
    private string[] standardAttackTriggers;

    // ==========================================================
    // 4. LÓGICA DE SALTO
    // ==========================================================
    [Header("Salto Programado")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpInterval = 3f;
    [SerializeField] private string jumpTriggerName = "Jump";
    [SerializeField] private float jumpColliderDisableTime = 0.1f;

    // ==========================================================
    // 5. DEFENSA, SALUD Y ANIMATOR STRINGS
    // ==========================================================
    [Header("Defensa y Salud (Orco)")]
    public int maxHealth = 5;
    public int currentHealth = 5;
    public float bounceForce = 8f;
    public int attackDamage = 1;

    [Header("Configuración de Pisotón")]
    public int stompDamage = 1;
    public float stompSideForce = 10f;

    [Header("Animator Strings")]
    private const string ANIM_SPEED = "Speed";
    private const string ANIM_ISGROUNDED = "IsGrounded";
    private const string ANIM_DEAD = "Dead";
    private const string ANIM_HURT = "Hurt";

    [Header("Detección")]
    public LayerMask playerLayer;

    // ==========================================================
    // 6. CONFIGURACIÓN DE RECOMPENSA DE BOSS (PARED) ⭐ AÑADIDO
    // ==========================================================
    [Header("Recompensa de Boss (Pared)")]
    [Tooltip("Asigna la pared que debe ser destruida al morir. Requiere el script ParedBloqueante.")]
    public ParedBloqueante paredFinal;

    // ==========================================================
    // INICIALIZACIÓN
    // ==========================================================
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            originalColor = sr.color;
        }

        standardAttackTriggers = new string[]
        {
            ANIM_ATTACK1, ANIM_ATTACK2, ANIM_ATTACK3
        };

        currentHealth = maxHealth;

        // Limpiar lógica de Hitbox hijo si ya no se usa
        /*
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(false);
        }
        */
    }

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        StartCoroutine(JumpCycle());
    }

    void Update()
    {
        if (isDead || playerTransform == null)
        {
            if (isDead) rb.linearVelocity = Vector2.zero;
            return;
        }

        CheckGroundAndTurn();

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        float currentSpeed = moveSpeed;
        float targetDir = facingRight ? 1f : -1f;
        bool isChasing = distanceToPlayer <= detectionRange;

        // Lógica de Persecución y Ataque 
        if (isChasing)
        {
            targetDir = Mathf.Sign(playerTransform.position.x - transform.position.x);
            FlipCheck(targetDir);
            currentSpeed = moveSpeed * chaseSpeedMultiplier * targetDir;

            // ⭐ USAR 'distanciaAtaque'
            if (Time.time > lastAttackTime + attackCooldown && CheckAttackRange(distanciaAtaque))
            {
                string selectedAttack = (Mathf.Abs(rb.linearVelocity.x) > moveSpeed + 0.1f) ?
                                             ANIM_RUN_ATTACK :
                                             standardAttackTriggers[Random.Range(0, standardAttackTriggers.Length)];

                AttemptAttack(selectedAttack);
            }
        }
        else
        {
            currentSpeed = moveSpeed * targetDir;
        }

        if (!isAttacking)
        {
            rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        UpdateAnimator(currentSpeed);
    }

    // ==========================================================
    // MÉTODOS DE ATAQUE Y RAYCAST ⭐ NUEVA FUNCIÓN
    // ==========================================================

    /// <summary>
    /// Llamado por un Animation Event durante el frame de impacto del golpe.
    /// Ejecuta el Raycast para aplicar daño al jugador.
    /// </summary>
    public void EjecutarRaycastAtaque()
    {
        if (ataquePunto == null)
        {
            Debug.LogError("Error: 'ataquePunto' no está asignado en " + gameObject.name);
            return;
        }

        // 1. Determina la dirección del Raycast (hacia donde mira el Orco)
        Vector2 direccion = facingRight ? Vector2.right : Vector2.left;

        // 2. Lanza el Raycast desde 'ataquePunto'
        RaycastHit2D hit = Physics2D.Raycast(
            ataquePunto.position,
            direccion,
            distanciaAtaque,
            playerLayer // Solo detecta la capa del jugador
        );

        // 3. Aplica Daño por Raycast
        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            PlayerController player = hit.collider.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(attackDamage);
            }
        }
    }

    // Estos métodos ahora quedan vacíos ya que el Raycast reemplaza el hitbox hijo
    public void EnableAttackHitbox() { }
    public void DisableAttackHitbox() { }

    // ==========================================================
    // MÉTODOS AUXILIARES Y COROUTINES (Mismos que antes)
    // ==========================================================

    private void CheckGroundAndTurn()
    {
        if (controladorAbajo == null || controladorEnfrente == null) return;

        isGrounded = Physics2D.OverlapCircle(controladorAbajo.position, checkRadius, layerMaskAbajo);

        // La dirección para el Raycast de pared debe ser la dirección en la que está mirando.
        Vector2 checkDirection = facingRight ? Vector2.right : Vector2.left;
        // ⭐ Corregido para usar checkDirection, aunque transform.right funciona si la escala es 1/-1
        bool hitWall = Physics2D.Raycast(controladorEnfrente.position, checkDirection, wallCheckDistance, layerMaskEnfrente);

        if ((!isGrounded && rb.linearVelocity.y < 0.1f) || hitWall)
        {
            if (playerTransform == null || Vector3.Distance(transform.position, playerTransform.position) > detectionRange)
            {
                Flip();
            }
        }
    }

    void FlipCheck(float targetDir)
    {
        if (!isAttacking)
        {
            if (targetDir > 0 && !facingRight || targetDir < 0 && facingRight) Flip();
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    private IEnumerator JumpCycle()
    {
        yield return new WaitForSeconds(0.2f);
        while (!isDead)
        {
            yield return new WaitForSeconds(jumpInterval);
            if (isGrounded && !isAttacking) ExecuteJump();
        }
        yield break;
    }

    private void ExecuteJump()
    {
        if (enemyCollider != null && enemyCollider.enabled)
        {
            enemyCollider.enabled = false;
            StartCoroutine(ReactivateCollider(jumpColliderDisableTime));
        }
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        if (anim != null) anim.SetTrigger(jumpTriggerName);
    }

    private IEnumerator ReactivateCollider(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enemyCollider != null && !isDead) enemyCollider.enabled = true;
    }

    private void UpdateAnimator(float currentSpeed)
    {
        if (anim == null) return;
        anim.SetBool(ANIM_ISGROUNDED, isGrounded);
        anim.SetFloat(ANIM_SPEED, Mathf.Abs(currentSpeed));
    }

    private bool CheckAttackRange(float range)
    {
        // ⭐ Usando ataquePunto en el chequeo de rango
        if (playerLayer == 0 || ataquePunto == null) return false;
        Vector2 checkDirection = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(ataquePunto.position, checkDirection, range, playerLayer);
        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    private void AttemptAttack(string animTrigger)
    {
        if (isAttacking) return;
        isAttacking = true;
        anim.SetTrigger(animTrigger);
        StartCoroutine(ResetAttackState());
    }

    private IEnumerator ResetAttackState()
    {
        lastAttackTime = Time.time;
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
    }

    // ==========================================================
    // LÓGICA DE DAÑO, PISOTÓN Y MUERTE (MODIFICADO)
    // ==========================================================

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (anim != null) anim.SetTrigger(ANIM_HURT);
        StartCoroutine(FlashColor());
        if (currentHealth <= 0) Die();
    }

    private IEnumerator FlashColor()
    {
        if (sr != null)
        {
            sr.color = hurtColor;
            yield return new WaitForSeconds(flashDuration);
            sr.color = originalColor;
        }
    }

    private IEnumerator StompInvulnerabilityTimer()
    {
        canBeStomped = false;
        yield return new WaitForSeconds(stompCooldown);
        canBeStomped = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (other.CompareTag("Player"))
        {
            Rigidbody2D playerRB = other.GetComponent<Rigidbody2D>();
            if (playerRB != null && playerRB.linearVelocity.y < 0 && canBeStomped)
            {
                StartCoroutine(StompInvulnerabilityTimer());
                TakeDamage(stompDamage);
                float sideDirection = Mathf.Sign(playerRB.transform.position.x - transform.position.x);
                playerRB.linearVelocity = new Vector2(playerRB.linearVelocity.x, 0f);
                playerRB.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
                playerRB.AddForce(new Vector2(sideDirection * stompSideForce, 0f), ForceMode2D.Impulse);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || isAttacking) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(attackDamage);
            }
        }
    }

    void Die()
    {
        if (isDead) return;
        Debug.Log(gameObject.name + " ha sido derrotado.");
        isDead = true;

        // LÓGICA DE RECOMPENSA: Destruir la Pared ⭐ CÓDIGO AÑADIDO
        if (paredFinal != null)
        {
            paredFinal.DesbloquearMundo();
            Debug.Log("Pared Final Desbloqueada por " + gameObject.name);
        }
        else
        {
            Debug.LogWarning("La pared a destruir (paredFinal) NO está asignada en el Inspector de " + gameObject.name);
        }
        // FIN CÓDIGO AÑADIDO

        if (anim != null) anim.SetBool(ANIM_DEAD, true);
        if (sr != null) sr.color = originalColor;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
        Destroy(gameObject, 0.5f);
    }

    // --- DEBUG ---

    private void OnDrawGizmos()
    {
        // 🟥 Dibuja el GroundCheck como una esfera
        if (controladorAbajo != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(controladorAbajo.position, checkRadius);
        }

        // 🟦 Dibuja el Raycast de Chequeo de Pared
        if (controladorEnfrente != null)
        {
            Gizmos.color = Color.blue;
            Vector3 checkDirection = facingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(controladorEnfrente.position, controladorEnfrente.position + checkDirection * wallCheckDistance);
            Gizmos.DrawWireSphere(controladorEnfrente.position + checkDirection * wallCheckDistance, 0.05f);
        }

        // 🌟 Dibuja el Raycast de Ataque como un círculo (Hitbox de área) ⭐ NUEVO
        if (ataquePunto != null)
        {
            Gizmos.color = Color.magenta;
            // Dibuja el alcance del ataque
            Gizmos.DrawWireSphere(ataquePunto.position, distanciaAtaque);

            // Dibuja el punto inicial del Raycast
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ataquePunto.position, 0.1f);
        }
    }
}