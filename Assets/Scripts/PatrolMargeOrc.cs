using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

// Asume que PlayerController tiene el método TakeDamage(int damageAmount)
// y que existe el script ParedBloqueante con el método DesbloquearMundo().
public class PatrolMageOrc : MonoBehaviour
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
    // 2. CONFIGURACIÓN DE MOVIMIENTO Y PERSECUCIÓN
    // ==========================================================
    [Header("Movimiento y Persecución")]
    public float moveSpeed = 1f;
    public float chaseSpeedMultiplier = 1.5f;
    [Tooltip("Distancia a la que el orco deja de patrullar y empieza a perseguir al jugador.")]
    public float detectionRange = 10.0f;
    [SerializeField] private Transform playerTransform;

    [Header("Controladores de Patrulla (Chequeos)")]
    public Transform controladorAbajo;
    public Transform controladorEnfrente;

    [Tooltip("Radio de la esfera usada para la detección de suelo.")]
    public float checkRadius = 0.2f;

    public float wallCheckDistance = 0.1f;
    public LayerMask layerMaskAbajo;
    public LayerMask layerMaskEnfrente;
    private bool isGrounded = false;

    // ==========================================================
    // 3. LÓGICA DE ATAQUE (OverlapCircle de Área)
    // ==========================================================
    [Header("Ataque y Rangos (Mago)")]
    public float attackCooldown = 1.5f;
    private float lastAttackTime;
    // Controlado por Animation Events.
    private bool isAttacking = false;

    [Header("Configuración de Área de Ataque")]
    [Tooltip("Punto central del ataque circular (debe ser un hijo del Orco).")]
    public Transform ataquePunto;
    [Tooltip("Radio del círculo de daño. Aplica a todos los ataques.")]
    public float distanciaAtaque = 1.5f;

    [Header("Rangos de Ataque")]
    public float attackDecisionRange = 7.0f; // Rango para decidir si atacar

    // NOMBRES DE TRIGGERS ACTUALIZADOS PARA COINCIDIR CON TU ANIMATOR
    private const string ANIM_ATTACK1 = "Attack1OrcMago";
    private const string ANIM_PROJECTILE = "ProjectileAttackOrcMago"; // Nombre del Trigger en tu Animator
    // Se mantiene el MidMelee por si lo usas en el futuro, pero puedes eliminarlo si solo tienes 2 ataques.
    private const string ANIM_MID_MELEE = "MidMelee";
    private string[] attackTriggers;

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
    private const string ANIM_JUMP = "Jump"; // Se añade para mayor claridad si no está en JumpTriggerName

    [Header("Detección")]
    public LayerMask playerLayer;

    // ==========================================================
    // 6. CONFIGURACIÓN DE RECOMPENSA DE BOSS (PARED)
    // ==========================================================
    [Header("Recompensa de Boss (Pared)")]
    [Tooltip("Asigna la pared que debe ser destruida al morir.")]
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

        if (sr != null) originalColor = sr.color;

        // Se inicializan los triggers con los nombres de tu Animator
        attackTriggers = new string[]
        {
            ANIM_ATTACK1,
            ANIM_PROJECTILE,
            ANIM_MID_MELEE // Opcional, si no lo usas, quítalo
        };

        currentHealth = maxHealth;
    }

    void Start()
    {
        // Intenta encontrar el Transform del jugador automáticamente usando la etiqueta "Player"
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

        if (distanceToPlayer <= detectionRange)
        {
            // --- LÓGICA DE PERSECUCIÓN ---
            targetDir = Mathf.Sign(playerTransform.position.x - transform.position.x);
            FlipCheck(targetDir);
            currentSpeed = moveSpeed * chaseSpeedMultiplier * targetDir;

            // Lógica de Ataque
            if (Time.time > lastAttackTime + attackCooldown && CheckAttackRange(attackDecisionRange))
            {
                int randomIndex = Random.Range(0, attackTriggers.Length);
                AttemptAttack(attackTriggers[randomIndex]);
            }
        }
        else
        {
            // --- LÓGICA DE PATRULLA ---
            currentSpeed = moveSpeed * targetDir;
        }

        // Control de Movimiento: Detiene el Rigidbody si está en estado de ataque
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
    // MÉTODOS DE ATAQUE Y ANIMATION EVENTS
    // ==========================================================

    /// <summary>
    /// Llamado por un Animation Event al inicio del clip de ataque. Bloquea el movimiento.
    /// </summary>
    public void LockMovement()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// Llamado por un Animation Event al final del clip de ataque. Desbloquea el movimiento.
    /// </summary>
    public void UnlockMovement()
    {
        isAttacking = false;
        lastAttackTime = Time.time;
    }

    /// <summary>
    /// Llamado por un Animation Event para aplicar daño (OverlapCircle).
    /// </summary>
    public void EjecutarRaycastAtaque()
    {
        if (ataquePunto == null) return;

        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(
            ataquePunto.position,
            distanciaAtaque,
            playerLayer
        );

        foreach (Collider2D collider in hitObjects)
        {
            if (collider.CompareTag("Player"))
            {
                PlayerController player = collider.GetComponent<PlayerController>();
                if (player != null) player.TakeDamage(attackDamage);
            }
        }
    }

    // Métodos vacíos (opcionales)
    public void EnableAttackHitbox() { }
    public void DisableAttackHitbox() { }

    // ==========================================================
    // MÉTODOS AUXILIARES Y COROUTINES
    // ==========================================================

    private void CheckGroundAndTurn()
    {
        if (controladorAbajo == null || controladorEnfrente == null) return;

        isGrounded = Physics2D.OverlapCircle(controladorAbajo.position, checkRadius, layerMaskAbajo);

        Vector2 checkDirection = facingRight ? Vector2.right : Vector2.left;
        bool hitWall = Physics2D.Raycast(controladorEnfrente.position, checkDirection, wallCheckDistance, layerMaskEnfrente);

        if ((!isGrounded && rb.linearVelocity.y < 0.1f) || hitWall)
        {
            // Solo voltea si está patrullando o persiguiendo sin obstáculo
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
        anim.SetTrigger(animTrigger);
    }

    // ==========================================================
    // LÓGICA DE DAÑO, PISOTÓN Y MUERTE (con pared)
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

        // LÓGICA DE RECOMPENSA: Destruir la Pared
        if (paredFinal != null)
        {
            paredFinal.DesbloquearMundo();
        }
        else
        {
            Debug.LogWarning("La pared a destruir (paredFinal) no está asignada en el Inspector de " + gameObject.name);
        }

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
        // Dibuja el GroundCheck 
        if (controladorAbajo != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(controladorAbajo.position, checkRadius);
        }

        // Dibuja el Raycast de Chequeo de Pared
        if (controladorEnfrente != null)
        {
            Gizmos.color = Color.blue;
            Vector3 checkDirection = facingRight ? Vector3.right : Vector3.left;
            Vector3 targetPosition = controladorEnfrente.position + checkDirection * wallCheckDistance;
            Gizmos.DrawLine(controladorEnfrente.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.05f);
        }

        // Dibuja el círculo de Ataque (OverlapCircle)
        if (ataquePunto != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ataquePunto.position, distanciaAtaque);
        }
    }
}