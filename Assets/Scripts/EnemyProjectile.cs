using UnityEngine;

// CORRECCIÓN CRÍTICA: Heredar de MonoBehaviour para acceder a 'gameObject' y 'transform'.
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float resetTime = 3f;
    [SerializeField] private float damageAmount = 1f; // Añadido para el daño

    private float lifetime;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void ActivateProjectile(Vector3 direction) // Acepta la dirección requerida
    {
        lifetime = 0;
        gameObject.SetActive(true);

        // CORRECCIÓN: Aplicar movimiento a través del Rigidbody para que sea limpio
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }
        else
        {
            // Fallback si no hay Rigidbody, aunque es mejor usar uno.
            transform.right = direction;
        }
    }

    private void Update()
    {
        // Si no usamos Rigidbody, usamos Translate
        if (rb == null)
        {
            float movementSpeed = speed * Time.deltaTime;
            transform.Translate(Vector3.right * movementSpeed);
        }

        // Time Out para devolver al pool si no golpea nada
        lifetime += Time.deltaTime;
        if (lifetime > resetTime)
            gameObject.SetActive(false);
    }

    // --- En EnemyProjectile.cs ---

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Lógica de daño
        if (collision.CompareTag("Player"))
        {
            // CORRECCIÓN: Apuntar al script que realmente maneja la vida
            PlayerController playerHealth = collision.GetComponent<PlayerController>();

            if (playerHealth != null)
            {
                // Aplicar daño al jugador
                playerHealth.TakeDamage((int)damageAmount);
            }
        }

        // ... lógica de desactivación
    }
}