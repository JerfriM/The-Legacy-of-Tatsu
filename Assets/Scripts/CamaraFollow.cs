using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("Arrastra tu objeto Player (Target) aquí.")]
    public Transform target;

    [Tooltip("Qué tan rápido la cámara se mueve hacia el objetivo (0.125f es suave).")]
    public float smoothSpeed = 0.125f;

    [Tooltip("Ajuste manual para centrar la cámara, ej: (0, 2, -10).")]
    public Vector3 offset = new Vector3(0f, 1f, -10f); // Un offset típico

    // Se usa LateUpdate() para que la cámara se mueva después de que el personaje ha terminado de moverse en Update() y FixedUpdate().
    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("¡Advertencia! El objetivo (Player) no ha sido asignado a la cámara.");
            return;
        }

        // 1. Definir la posición deseada (posición del Player + el offset)
        Vector3 desiredPosition = target.position + offset;

        // 2. Aplicar suavizado (interpolación suave)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // 3. Aplicar la nueva posición
        transform.position = smoothedPosition;
    }
}