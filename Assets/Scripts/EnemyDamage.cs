using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] protected float damage = 1f;

    protected void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            // CORRECCIÓN: Apuntar al script que realmente maneja la vida
            collision.GetComponent<PlayerController>()?.TakeDamage((int)damage);
            // Usamos (int)damage porque PlayerController.TakeDamage espera un entero
        }
    }
}