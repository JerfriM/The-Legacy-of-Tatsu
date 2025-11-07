using UnityEngine;
using System.Collections;

public class ShootingEnemy : MonoBehaviour
{
    // ==========================================================
    // 1. CONFIGURACIÓN GENERAL Y TEMPORIZACIÓN
    // ==========================================================
    [Header("Configuración de Disparo")]
    [Tooltip("El punto de la boca del cañón/mano desde donde se dispara.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Tiempo que el enemigo espera antes de iniciar el primer ciclo de ataque.")]
    [SerializeField] private float initialDelay = 1.0f; // Tiempo del primer disparo

    [Tooltip("Tiempo de pausa entre el fin del ciclo de disparo anterior y el inicio del siguiente.")]
    [SerializeField] private float cooldownBetweenCycles = 2f; // Tiempo del último disparo (Enfriamiento)

    // El array contiene todos los proyectiles YA INSTANCIADOS en la escena (el Pool)
    [Header("Object Pooling (Arrastrar Proyectiles Inactivos)")]
    [Tooltip("Arrastra aquí todas las copias de los proyectiles inactivos de la Jerarquía.")]
    [SerializeField] private GameObject[] projectilePool;

    [Header("Animación y Sincronización")]
    [Tooltip("Nombre del Trigger de la animación de ataque (ej: 'Attack').")]
    [SerializeField] private string attackTriggerName = "Attack";
    [Tooltip("Porcentaje de la animación de ataque donde se debe disparar (0.0 a 1.0).")]
    [SerializeField] private float shootTimePercentage = 0.6f;

    [Header("Sincronización de FlipX")]
    [Tooltip("La distancia local del FirePoint desde el centro del enemigo (X).")]
    [SerializeField] private float firePointLocalXOffset = 1.0f; // Ajusta este valor en el Inspector

    [Header("Detección")]
    [Tooltip("Referencia al Transform del jugador para apuntar.")]
    [SerializeField] private Transform playerTransform;

    private Animator anim;
    private SpriteRenderer parentSr; // <-- NUEVA REFERENCIA: Para leer el flipX

    // ==========================================================
    // INICIALIZACIÓN
    // ==========================================================
    void Awake()
    {
        anim = GetComponent<Animator>();
        parentSr = GetComponent<SpriteRenderer>(); // Obtener el SpriteRenderer

        // Búsqueda de jugador
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    void Start()
    {
        if (playerTransform != null)
        {
            StartCoroutine(AttackCycle());
        }
    }

    // ==========================================================
    // 2. CICLO DE ATAQUE SINCRONIZADO
    // ==========================================================
    private IEnumerator AttackCycle()
    {
        // 1. FRAME BUFFER: Asegura que el estado Idle/Patrol esté activo.
        yield return null;

        // 2. TIEMPO DEL PRIMER DISPARO: Espera el retardo inicial.
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // 3. Activar la animación de ataque
            if (anim != null)
            {
                anim.SetTrigger(attackTriggerName);
            }

            // 4. Obtener duración
            float animationLength = GetAnimationLength(attackTriggerName);

            // 5. Esperar hasta el momento de disparar
            float waitTimeBeforeShoot = animationLength * shootTimePercentage;
            yield return new WaitForSeconds(waitTimeBeforeShoot);

            // 6. DISPARAR el proyectil
            Shoot();

            // 7. Esperar el tiempo restante de la animación (la parte final del ataque)
            float waitTimeAfterShoot = animationLength * (1f - shootTimePercentage);
            yield return new WaitForSeconds(waitTimeAfterShoot);

            // 8. TIEMPO DEL ÚLTIMO DISPARO (Cooldown): Espera antes de empezar el siguiente ciclo.
            yield return new WaitForSeconds(cooldownBetweenCycles);
        }
    }

    // Función auxiliar de duración (usar valor fijo de seguridad)
    private float GetAnimationLength(string name)
    {
        // AJÚSTALO a la duración real de tu animación de ataque.
        return 0.5f;
    }

    // ==========================================================
    // 3. LÓGICA DE DISPARO Y POOLING
    // ==========================================================
    void Shoot()
    {
        if (projectilePool == null || firePoint == null || projectilePool.Length == 0)
        {
            Debug.LogError("Error: Pool vacío o Fire Point no asignado en " + gameObject.name);
            return;
        }

        int projectileIndex = FindProjectileIndex();

        if (projectileIndex != -1) // Si se encuentra un proyectil inactivo
        {
            GameObject projectileObj = projectilePool[projectileIndex];

            Vector3 direction;

            // Usamos la posición global del enemigo como base
            Vector3 finalFirePointPosition = transform.position;

            // =========================================================
            // LÓGICA DE AJUSTE DE POSICIÓN BASADA EN FLIPX (LA CLAVE)
            // =========================================================
            if (parentSr != null && parentSr.flipX)
            {
                // El enemigo mira a la izquierda: Posición ajustada hacia la izquierda
                finalFirePointPosition.x -= firePointLocalXOffset;
            }
            else
            {
                // El enemigo mira a la derecha: Posición ajustada hacia la derecha
                finalFirePointPosition.x += firePointLocalXOffset;
            }

            // 1. Reposicionar el proyectil con la posición ajustada
            projectileObj.transform.position = finalFirePointPosition;

            // 2. Calcular la dirección HASTA el jugador, usando la posición ajustada
            direction = (playerTransform.position - finalFirePointPosition).normalized;

            // 3. Activar y Lanzar
            EnemyProjectile projectileScript = projectileObj.GetComponent<EnemyProjectile>();
            projectileScript?.ActivateProjectile(direction);
        }
        else
        {
            Debug.LogWarning("Pool vacío! Todos los proyectiles están activos. Aumenta el tamaño del pool en el Inspector.");
        }
    }

    // Método eficiente para buscar el proyectil inactivo en el array
    private int FindProjectileIndex()
    {
        for (int i = 0; i < projectilePool.Length; i++)
        {
            // Busca el GameObject inactivo en la Jerarquía
            if (!projectilePool[i].activeInHierarchy)
                return i;
        }
        // Devuelve -1 si no se encuentra ninguno inactivo
        return -1;
    }
}