using UnityEngine;

public class ParedBloqueante : MonoBehaviour
{
    // Método público que será llamado por el Jefe.
    // Usamos el nombre 'DesbloquearMundo' para que sea claro.
    public void DesbloquearMundo()
    {
        Debug.Log("Pared destruida. ¡Mundo desbloqueado!");

        // 1. Opcional: Ejecutar animación o efecto de partículas de destrucción.
        // GetComponent<Animator>().SetTrigger("Destruir");

        // 2. Destruir la pared después de un pequeño retraso
        // para que la animación/efecto se vea completo.
        Destroy(gameObject, 0.1f);
    }
}