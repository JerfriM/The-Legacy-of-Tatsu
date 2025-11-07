using UnityEngine;
using TMPro; // Necesario para TextMeshPro

public class UIManager : MonoBehaviour
{
    // Singleton para acceso global
    public static UIManager Instance;

    [Tooltip("Arrastra aquí el objeto TextMeshPro (ScoreText)")]
    public TextMeshProUGUI scoreText;

    void Awake()
    {
        // Asegura que solo haya una instancia de UIManager
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Método que el GameManager llamará para actualizar la puntuación visible
    public void UpdateScoreText(int newScore)
    {
        if (scoreText != null)
        {
            // Actualiza el texto con el nuevo valor
            scoreText.text = ": " + newScore.ToString();
        }
    }
}