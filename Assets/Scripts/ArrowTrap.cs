using UnityEngine;

public class ArrowTrap : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject[] arrows; // Asume que cada GameObject tiene EnemyProjectile.cs
    private float cooldownTimer;

    private void Attack()
    {
        cooldownTimer = 0;

        // 1. Encontrar el proyectil disponible (index)
        int arrowIndex = FindArrow();

        // 2. Reposicionar el proyectil en el punto de disparo
        arrows[arrowIndex].transform.position = firePoint.position;
        arrows[arrowIndex].transform.rotation = firePoint.rotation; // Copia la rotación del punto de disparo

        // 3. Obtener el script y la dirección
        EnemyProjectile projectileScript = arrows[arrowIndex].GetComponent<EnemyProjectile>();

        // Usamos transform.right del punto de disparo como dirección
        Vector3 launchDirection = firePoint.right;

        // 4. Activar el proyectil y darle la dirección (¡La corrección final!)
        projectileScript?.ActivateProjectile(launchDirection);
    }

    private int FindArrow()
    {
        for (int i = 0; i < arrows.Length; i++)
        {
            if (!arrows[i].activeInHierarchy)
                return i;
        }
        // Si no hay flechas disponibles, devuelve la primera para reutilizar (aunque esto no es ideal, evita un error)
        return 0;
    }

    private void Update()
    {
        cooldownTimer += Time.deltaTime;

        // 1. Asegúrate de que firePoint no sea null (error de Inspector)
        if (cooldownTimer >= attackCooldown && firePoint != null)
            Attack();
    }

    // NOTA: Necesitas llenar el array 'arrows' con tus prefabs de flecha
    // en el Inspector antes de jugar (Object Pooling).
}