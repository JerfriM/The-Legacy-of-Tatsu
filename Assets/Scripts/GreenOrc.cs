using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

// Asegura que PlayerController tiene el método TakeDamage(int damageAmount)
public class GreenOrc : MonoBehaviour
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
    public Color hurtColor = Color.red; // Color de daño se mantiene ROJO
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

    // Configuración para OverlapCircle (Ground Check)
    [Tooltip("Radio de la esfera usada para la detección de suelo.")]
    public float checkRadius = 0.2f;

    public float groundCheckDistance = 0.05f;
    public float wallCheckDistance = 0.1f;
    public LayerMask layerMaskAbajo; // Capa de suelo
    public LayerMask layerMaskEnfrente; // Capa de paredes
    private bool isGrounded = false;

    // ==========================================================
    // 3. LÓGICA DE ATAQUE (Raycast Sincronizado)
    // ==========================================================
    [Header("Ataque y Rangos (Orco Verde)")]
    public float attackCooldown = 1f;
    private float lastAttackTime;
    private bool isAttacking = false;

    [Header("Configuración del Raycast de Ataque")]
    [Tooltip("Punto de inicio del Raycast de golpe. Debe ser un hijo del Orco.")]
    public Transform ataquePunto;
    [Tooltip("Distancia máxima a la que llega el golpe.")]
    public float distanciaAtaque = 1.0f;

    // NOMBRES DE TRIGGERS ACTUALIZADOS A VERDE
    private const string ANIM_ATTACK1 = "Attack1OrcVerde";
    private const string ANIM_ATTACK2 = "Attack2OrcVerde";
    private const string ANIM_ATTACK3 = "Attack3OrcVerde";
    private const string ANIM_RUN_ATTACK = "RunAttackOrcVerde";
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
    public int attackDamage = 1; // ⭐ Daño por Raycast y Contacto Físico (1 de daño)

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
    // ⭐ NUEVA CONFIGURACIÓN: Recompensa (Para la pared)
    // ==========================================================
    [Header("Recompensa de Boss")]
    [Tooltip("Asigna la pared que debe ser destruida al morir.")]
    public ParedBloqueante paredFinal; // <-- Usamos ParedBloqueante, necesitas este script!

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
    // MÉTODOS DE ATAQUE Y RAYCAST (Llamados por Animation Event)
    // ==========================================================

    /// <summary>
    /// Llamado por un Animation Event durante el frame de impacto del golpe.
    /// Ejecuta el Raycast para aplicar daño al jugador (1 de daño).
    /// </summary>
    public void EjecutarRaycastAtaque()
    {
        if (ataquePunto == null) return;

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

    // Estos métodos quedan vacíos ya que el ataque usa Raycast, no un Collider que se activa/desactiva.
    public void EnableAttackHitbox() { }
    public void DisableAttackHitbox() { }

    // ==========================================================
    // MÉTODOS AUXILIARES Y COROUTINES
    // ==========================================================

    private void CheckGroundAndTurn()
    {
        if (controladorAbajo == null || controladorEnfrente == null) return;

        isGrounded = Physics2D.OverlapCircle(controladorAbajo.position, checkRadius, layerMaskAbajo);
        bool hitWall = Physics2D.Raycast(controladorEnfrente.position, transform.right, wallCheckDistance, layerMaskEnfrente);

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
    // LÓGICA DE DAÑO, PISOTÓN Y MUERTE
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

        // ⭐ Daño por Colisión (Contacto Físico) - Hace 1 de daño
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

        // ⭐ LÓGICA DE RECOMPENSA: Destruir la Pared ⭐
        if (paredFinal != null)
        {
            // Llama al método público en el script ParedBloqueante
            paredFinal.DesbloquearMundo();
        }
        else
        {
            Debug.LogWarning("La pared a destruir (paredFinal) no está asignada en el Inspector del Orco Verde.");
        }
        // ⭐ FIN LÓGICA DE RECOMPENSA ⭐

        if (anim != null) anim.SetBool(ANIM_DEAD, true);
        if (sr != null) sr.color = originalColor;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Desactivar todos los colliders del enemigo y sus hijos
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }
        Destroy(gameObject, 0.5f);
    }

    // ==========================================================
    // MÉTODOS DE DEBUG (Gizmos)
    // ==========================================================

    private void OnDrawGizmos()
    {
        // 🟥 Dibuja el GroundCheck (OverlapCircle)
        if (controladorAbajo != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(controladorAbajo.position, checkRadius);
        }

        // 🟦 Dibuja el Raycast de Chequeo de Pared
        if (controladorEnfrente != null)
        {
            Gizmos.color = Color.blue;
            Vector3 targetPosition = controladorEnfrente.position + transform.right * (facingRight ? wallCheckDistance : -wallCheckDistance);
            Gizmos.DrawLine(controladorEnfrente.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.05f);
        }

        // 🌟 Dibuja el Raycast de Ataque
        if (ataquePunto != null)
        {
            Gizmos.color = Color.magenta;
            // Dibuja la línea del Raycast
            Vector3 endPoint = ataquePunto.position + transform.right * (facingRight ? distanciaAtaque : -distanciaAtaque);
            Gizmos.DrawLine(ataquePunto.position, endPoint);

            // Dibuja un punto en el final del Raycast
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(endPoint, 0.1f);
        }
    }
}