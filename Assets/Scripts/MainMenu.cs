using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    // ⭐ Asegúrate de que este script esté en un GameObject en la escena MainMenu ⭐

    [Header("Configuración de Paneles")]
    [Tooltip("Panel principal del menú. Debe contener los 3 botones.")]
    public GameObject mainPanel;

    [Tooltip("Panel de Controles. Debe estar oculto al inicio.")]
    public GameObject controlsPanel;

    void Start()
    {
        // Aseguramos que el tiempo esté corriendo normalmente al entrar al menú
        Time.timeScale = 1f;

        // Configuramos la visibilidad inicial de los paneles
        if (mainPanel != null) mainPanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    // ==========================================================
    // LÓGICA DE BOTONES
    // ==========================================================

    /// <summary>
    /// Inicia el juego. Llama a la función StartGame en el GameManager persistente.
    /// </summary>
    public void StartGameButton()
    {
        // El GameManager maneja la carga del Level_01 y el reseteo de stats
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Debug.LogError("ERROR: GameManager.Instance no encontrado. ¿Está en la escena y funciona como Singleton?");
        }
    }

    /// <summary>
    /// Muestra el panel de controles y oculta el menú principal.
    /// </summary>
    public void ShowControlsButton()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);
    }

    /// <summary>
    /// Oculta el panel de controles y muestra el menú principal (Botón "Volver").
    /// </summary>
    public void HideControlsButton()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    /// <summary>
    /// Cierra la aplicación (solo funciona en compilaciones).
    /// </summary>
    public void ExitGameButton()
    {
        Debug.Log("Saliendo del juego...");

#if UNITY_EDITOR
        // Si estamos en el editor, detiene la reproducción
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // Si estamos en una compilación, cierra la aplicación
            Application.Quit();
#endif
    }
}