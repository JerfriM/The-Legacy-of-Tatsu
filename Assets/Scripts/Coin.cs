using UnityEngine;

public class Coin : MonoBehaviour
{
    [Tooltip("El sonido que se reproduce al recoger la moneda (opcional)")]
    public AudioClip collectSound;

    [Tooltip("Valor de los puntos que otorga esta moneda.")]
    public int coinValue = 1;

    // Esta función se llama cuando otro Collider (el jugador) entra en el área del Trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Verificar si el objeto que tocó la moneda es el jugador
        // Se asume que el objeto Player tiene la etiqueta "Player"
        if (other.CompareTag("Player"))
        {
            // Opcional: Reproducir sonido de recolección
            if (collectSound != null)
            {
                // Reproduce el sonido en la posición de la moneda. Es mejor que AudioSource.Play()
                // porque el AudioSource no se destruye inmediatamente con la moneda.
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            // 2. Sumar el puntaje al GameManager (El Cerebro)
            // Verificamos si la instancia del GameManager existe
            if (GameManager.Instance != null)
            {
                // Llamamos al método central para que sume los puntos
                GameManager.Instance.CollectCoin(coinValue);
            }
            else
            {
                // Si el GameManager no está en la escena (algo raro), mostramos una advertencia.
                Debug.LogWarning("GameManager no encontrado. No se pudo sumar la puntuación.");
            }

            // 3. Destruir la moneda para que desaparezca
            // Al ser un Trigger, la acción ocurre inmediatamente después de la colisión.
            Destroy(gameObject);
        }
    }
}