using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas

public class LevelEndFlag : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena a la que se debe ir.")]
    public string nextSceneName = "Level2";

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica si el objeto que tocó el trigger es el jugador
        if (other.CompareTag("Player"))
        {
            Debug.Log("¡Nivel Completado! Cargando: " + nextSceneName);

            // Llama a la función para cambiar de nivel
            LoadNextLevel();
        }
    }

    void LoadNextLevel()
    {
        // Nota: Asegúrate de que la escena 'nextSceneName' esté en Build Settings.
        SceneManager.LoadScene(nextSceneName);
    }
}