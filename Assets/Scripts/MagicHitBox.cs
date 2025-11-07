using UnityEngine;

public class MagicHitbox : MonoBehaviour
{
    [Tooltip("Daño que causa este ataque mágico.")]
    public int damage = 1;
    [Tooltip("Tiempo de vida de la hitbox. Debe coincidir con la duración de tu efecto visual.")]
    public float hitboxDuration = 0.5f;

    void Start()
    {
        // 1. Aseguramos que el objeto (la hitbox y el efecto visual) se destruya
        // después de un tiempo para limpiar la escena.
        Destroy(gameObject, hitboxDuration);

        // NOTA: El Collider2D de este objeto DEBE estar marcado como Is Trigger.
    }

    // Usamos OnTriggerEnter2D porque las hitboxes son elementos de detección (Triggers)
    void OnTriggerEnter2D(Collider2D other)
    {
        // 2. Verifica si la hitbox colisionó con el jugador
        if (other.CompareTag("Player"))
        {
            // Asume que el jugador tiene un componente llamado PlayerController
            // con un método público TakeDamage(int)
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                // Aplica el daño al jugador
                player.TakeDamage(damage);

                // 3. Desactiva la hitbox después de golpear
                // Esto asegura que solo dañe al jugador una vez por ataque
                GetComponent<Collider2D>().enabled = false;
            }
        }
    }
}