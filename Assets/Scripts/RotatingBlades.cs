using UnityEngine;

public class RootMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Velocidad de movimiento horizontal de la trampa.")]
    [SerializeField] private float speed = 2f;

    [Tooltip("Capas que representan las paredes/muros para evitar chocar.")]
    [SerializeField] private LayerMask wallLayer;

    [Header("Detection Settings")]
    [Tooltip("Distancia que el rayo se extiende hacia adelante para detectar la pared.")]
    [SerializeField] private float rayDistance = 0.55f;

    private int direction = 1; // 1 para derecha, -1 para izquierda
    private bool facingRight = true;
    private float fixedYPosition; // NUEVA VARIABLE: Para mantener la altura

    void Start()
    {
        // Guardar la posición Y inicial al inicio para mantener la altura constante
        fixedYPosition = transform.position.y;
    }

    void Update()
    {
        // 1. Aplicar el movimiento
        float movementX = speed * direction * Time.deltaTime;

        // Mover solo en X. Forzamos la posición Y a ser la inicial (fija)
        Vector3 newPosition = new Vector3(transform.position.x + movementX, fixedYPosition, transform.position.z);
        transform.position = newPosition;

        // 2. Control de Detección (Raycasting)
        Vector3 rayOrigin = transform.position;
        Vector2 rayDirection = facingRight ? Vector2.right : Vector2.left;

        // Dibuja la línea del Raycast siempre en AMARILLO.
        // ¡Esta es la línea que ves en el Scene View!
        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.yellow);

        // Lanza el Rayo para buscar la pared
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, wallLayer);

        // 3. Evalúa la Detección
        if (hit.collider != null)
        {
            // El rayo ha golpeado algo en la capa 'wallLayer'. ¡Darse la vuelta!
            FlipDirection();
        }
    }

    void FlipDirection()
    {
        // 1. Invertir la dirección lógica y de movimiento
        direction *= -1;
        facingRight = !facingRight;

        // 2. Empuje mínimo para romper la colisión inmediata (ajuste de seguridad)
        float pushBackAmount = 0.02f;
        transform.position += new Vector3(pushBackAmount * direction, 0, 0);
    }

    // --- DEBUG VISUAL (OnDrawGizmosSelected) ---
    // Esto asegura que el Raycast se vea también cuando el objeto está seleccionado en el editor.
    private void OnDrawGizmosSelected()
    {
        // Usamos Gizmos.DrawLine para dibujar la distancia de detección cuando el objeto está seleccionado
        Gizmos.color = Color.yellow;
        Vector3 rayDirection = facingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawLine(transform.position, transform.position + rayDirection * rayDistance);
    }
}