using UnityEngine;
using System.Collections;

public class FallingPlatform : MonoBehaviour
{
    [Header("Configuración de Caída")]
    [Tooltip("Tiempo de espera (en segundos) antes de que la plataforma comience a caer.")]
    public float fallDelay = 0.5f;
    [Tooltip("Tiempo (en segundos) antes de que la plataforma sea destruida después de caer.")]
    public float destroyDelay = 2f;

    private Rigidbody2D rb;
    private bool isTriggered = false; // Asegura que la caída solo se active una vez

    void Awake()
    {
        // 1. Obtener el Rigidbody2D
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("¡ERROR! La Plataforma que Cae necesita un Rigidbody2D en el GameObject.");
            enabled = false;
            return;
        }

        // Configuración inicial: Sin gravedad y cinemática (estática)
        rb.isKinematic = true;
        rb.gravityScale = 0f;
    }

    // Usamos OnCollisionEnter2D para detectar cuando el jugador se posa sobre la plataforma
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 2. Comprobar si el objeto es el "Player" y si la caída aún no se ha activado
        if (collision.gameObject.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;
            StartCoroutine(FallAfterDelay());
        }
    }

    // Coroutine que gestiona la secuencia de caída y destrucción
    private IEnumerator FallAfterDelay()
    {
        // Opcional: Feedback visual (p.ej., la plataforma se tambalea o cambia de color)
        // Puedes agregar aquí una animación o un efecto de shake.

        // 1. Esperar el tiempo de retardo (tensión)
        yield return new WaitForSeconds(fallDelay);

        // 2. Activar la física: la plataforma cae
        if (rb != null)
        {
            rb.isKinematic = false; // Desactivar Kinematic para que la física tome el control
            rb.gravityScale = 1f;   // Aplicar gravedad
        }

        // 3. Esperar antes de destruirse
        yield return new WaitForSeconds(destroyDelay);

        // 4. Destruir el objeto para limpiar la escena
        Destroy(gameObject);
    }
}