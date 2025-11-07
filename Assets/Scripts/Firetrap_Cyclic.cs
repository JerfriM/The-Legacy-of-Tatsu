using UnityEngine;
using System.Collections;

public class Firetrap_Cyclic : MonoBehaviour
{
    [SerializeField] private int damage = 1; // Daño que se aplica

    [Header("Timers")]
    [Tooltip("Tiempo que la trampa permanece inactiva (segundos).")]
    [SerializeField] private float activationDelay = 2.5f; // Tiempo de reposo
    [Tooltip("Tiempo total que el fuego permanece encendido (segundos).")]
    [SerializeField] private float activeTime = 1.2f;      // Tiempo total de peligro

    [Header("Componentes de Flama")]
    // Asegúrate de arrastrar el objeto hijo 'FlameArea' a este campo en el Inspector
    [SerializeField] private GameObject flameArea;

    private Animator anim;
    private bool active = false; // Indica si el fuego está en fase de daño

    void Start()
    {
        // Intentamos obtener el Animator del objeto hijo asignado
        if (flameArea != null)
        {
            anim = flameArea.GetComponent<Animator>();
        }
        else
        {
            Debug.LogError("El objeto 'Flame Area' no está asignado en el Inspector de " + gameObject.name);
        }

        // Iniciamos el ciclo de la trampa inmediatamente
        StartCoroutine(TrapCycle());
    }

    // El Collider del 'FlameArea' debe estar en Is Trigger y se detecta aquí
    private void OnTriggerStay2D(Collider2D collision)
    {
        // Solo aplicamos daño si el jugador está en el collider Y la trampa está en su fase activa
        if (collision.CompareTag("Player"))
        {
            if (active)
            {
                collision.GetComponent<PlayerController>()?.TakeDamage(damage);
            }
        }
    }

    private IEnumerator TrapCycle()
    {
        // Verificación de seguridad para evitar errores si activeTime es muy corto
        if (activeTime < 0.2f)
        {
            activeTime = 0.2f; // Mínimo necesario para la sincronización
        }

        while (true) // Bucle constante
        {
            // 1. FASE INACTIVA (El fuego está apagado)
            active = false;
            if (anim != null)
            {
                anim.SetBool("activated", false);
            }

            yield return new WaitForSeconds(activationDelay);

            // --- INICIA EL ENCENDIDO ---

            // 2. FASE DE ENCENDIDO (La animación comienza. El daño es false)
            if (anim != null)
            {
                anim.SetBool("activated", true); // Comienza la animación de la flama/subida
            }

            // ESPERA DE SINCRONIZACIÓN CLAVE: Damos tiempo a la animación para que se vea
            // completamente activa antes de activar el daño lógico.
            float syncDelay = 0.4f; // <-- Ajusta este valor si la animación tarda más o menos
            yield return new WaitForSeconds(syncDelay);

            // 3. FASE ACTIVA (El fuego está completamente ENCENDIDO y HACE daño)
            active = true; // El daño lógico está activo

            // ESPERA: Resto del tiempo activo (activeTime)
            // Restamos el tiempo de sincronización que ya esperamos
            yield return new WaitForSeconds(activeTime - syncDelay);

            // --- TERMINA EL FUEGO, VUELVE AL REPOSO ---
        }
    }
}