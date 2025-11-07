using UnityEngine;
using System.Collections; // Necesario para la corrutina de invulnerabilidad
using UnityEngine.SceneManagement; // <-- IMPORTANTE: Necesario para reiniciar el nivel

// Asegura que los componentes necesarios estén siempre presentes en el Player
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    // VARIABLES AJUSTABLES EN EL INSPECTOR
    // ------------------------------------
    [Header("Movement Settings")]
    [Range(1f, 15f)]
    public float moveSpeed = 7f;

    [Header("Jump Settings")]
    [Range(5f, 25f)]
    public float jumpForce = 15f;

    // Configuración del sensor de suelo (¡Recuerda asignar estos campos en el Inspector!)
    public Transform groundCheck;
    public LayerMask groundLayer;
    private readonly float checkRadius = 0.2f;

    // SISTEMA DE SALUD
    [Header("Health Settings")]
    public int maxHealth = 3; // Vida máxima
    [Tooltip("Tiempo en segundos que el jugador es invulnerable después de recibir daño.")]
    public float invulnerabilityTime = 1f;
    private int currentHealth; // Vida actual
    private bool isInvulnerable = false;

    // COMPONENTES Y ESTADO
    // ----------------------
    private Rigidbody2D rb;
    private SpriteRenderer spRd;
    private Animator anim;
    private bool isGrounded;
    private float moveInput;

    // --- MÉTODOS ESTÁNDAR DE UNITY ---

    void Start()
    {
        // Obtener y asociar los componentes al inicio
        rb = GetComponent<Rigidbody2D>();
        spRd = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // Inicialización de la vida
        currentHealth = maxHealth;

        if (anim != null)
        {
            anim.SetBool("IsGrounded", true);
        }
    }

    void Update()
    {
        // 1. Entrada de Salto (Jump Input)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Aplicamos el salto usando rb.velocity para un control físico estable
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); // <-- CORRECCIÓN a rb.velocity
        }

        // 2. Control de Animaciones 
        HandleAnimations();
    }

    void FixedUpdate()
    {
        // 3. Detección de Suelo
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // 4. Movimiento Horizontal (Física)
        moveInput = Input.GetAxisRaw("Horizontal"); // Lee -1, 0, o 1

        // Aplica el movimiento horizontal manteniendo la velocidad vertical.
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y); // <-- CORRECCIÓN a rb.velocity

        // 5. Sentido del Sprite (Girar el render)
        HandleSpriteFlip(moveInput);
    }

    // --- LÓGICA DE SALUD Y DAÑO ---

    public void TakeDamage(int damageAmount)
    {
        // Solo recibe daño si no es invulnerable
        if (isInvulnerable)
        {
            return;
        }

        // 1. Reduce la vida
        currentHealth -= damageAmount;
        Debug.Log("Jugador Tocado. Vida restante: " + currentHealth);

        // 2. Comprobar si muere
        if (currentHealth <= 0)
        {
            Die(); // Ahora llama a la función Die que reinicia la escena
        }
        else
        {
            // 3. Aplicar invulnerabilidad temporal
            StartCoroutine(BecomeTemporarilyInvulnerable(invulnerabilityTime));
        }
    }

    void Die()
    {
        // ¡LÓGICA ACTUALIZADA PARA REINICIAR EL NIVEL!
        Debug.Log("¡GAME OVER! Reiniciando nivel...");

        // Obtiene el nombre de la escena actual y la recarga
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    // Rutina de invulnerabilidad
    private IEnumerator BecomeTemporarilyInvulnerable(float duration)
    {
        isInvulnerable = true;
        // Opcional: Flashear el sprite para dar feedback visual

        yield return new WaitForSeconds(duration);

        isInvulnerable = false;
        // Opcional: Resetear el color del sprite
    }

    // --- MÉTODOS AUXILIARES ---

    private void HandleSpriteFlip(float moveInput)
    {
        // CORRECCIÓN CLAVE: Asumimos que el sprite original mira a la DERECHA.
        if (moveInput != 0)
        {
            // Si moveInput es negativo (izquierda), flipX es TRUE (voltear).
            spRd.flipX = moveInput < 0;
        }
    }

    private void HandleAnimations()
    {
        // 1. Animación de Correr / Quieto
        anim.SetBool("IsRunning", Mathf.Abs(rb.linearVelocity.x) > 0.1f); // <-- CORRECCIÓN a rb.velocity.x

        // 2. Animación de Salto / Caída
        anim.SetBool("IsGrounded", isGrounded);
    }

    // --- DEBUG ---

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
}