using UnityEngine;

public class EnemyHeadStomp : MonoBehaviour
{
    [Tooltip("Referencia al script principal del enemigo (PatrolMageOrc).")]
    public PatrolMageOrc enemyScript;

    [Tooltip("Cantidad de daño que el pisotón hace al enemigo.")]
    public int stompDamage = 1;

    // Usamos OnTriggerEnter2D porque el Head Collider DEBE ser un Trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enemyScript == null) return;
        if (enemyScript.isDead) return;

        // 1. Chequeamos si es el jugador
        if (other.CompareTag("Player"))
        {
            Rigidbody2D playerRB = other.GetComponent<Rigidbody2D>();

            // 2. Confirmamos que el jugador está cayendo (pisotón)
            if (playerRB != null && playerRB.linearVelocity.y < 0)
            {
                // Rebote del jugador (usando la fuerza definida en PatrolMageOrc)
                playerRB.linearVelocity = new Vector2(playerRB.linearVelocity.x, 0f);
                playerRB.AddForce(Vector3.up * enemyScript.bounceForce, ForceMode2D.Impulse);

                // El Orco recibe daño
                enemyScript.TakeDamage(stompDamage);
            }
        }
    }
}