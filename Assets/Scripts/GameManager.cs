using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic; // Necesario para la lista de checkpoints

public class GameManager : MonoBehaviour
{
    // Instancia estática para el patrón Singleton (acceso global)
    public static GameManager Instance;

    // ==========================================================
    // ESTADO DEL JUEGO
    // ==========================================================
    [Header("Estado del Jugador")]
    public int currentLives = 3;
    public int currentCoins = 0;
    private const int COINS_FOR_EXTRA_LIFE = 100;

    // ⭐ El índice del nivel actual que se está JUGANDO.
    private int currentLevelIndex = 0;

    // ⭐ El índice del nivel de checkpoint más alto alcanzado (0, 3, 6, o 9).
    private int savedCheckpointIndex = 0;

    // Lista de niveles que actúan como checkpoint
    private readonly List<int> checkpointLevels = new List<int> { 3, 6, 9 };
    private const int FINAL_LEVEL_INDEX = 10;

    // ==========================================================
    // REFERENCIAS DE ESCENA (ASIGNAR EN INSPECTOR)
    // ==========================================================
    [Header("Nombres de Escena")]
    public string levelPrefix = "Level_";
    public string menuSceneName = "MainMenu";

    // NOTA: Estas variables ya no se usan en la lógica de carga secuencial,
    // pero se mantienen para referencias visuales en el Inspector.
    public string level1SceneName = "Level_01";
    public string level10SceneName = "Level_10";

    // ==========================================================
    // REFERENCIAS DE UI (ASIGNAR EN EL INSPECTOR)
    // ==========================================================
    [Header("UI Referencias")]
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI coinsText;

    [Header("Pantallas de Estado")]
    public GameObject deathScreenUI;
    public TextMeshProUGUI victoryText;
    public float deathDisplayDelay = 2.0f;
    public float victoryDisplayDelay = 2.0f;


    // ==========================================================
    // INICIALIZACIÓN (SINGLETON)
    // ==========================================================
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // CRÍTICO: Este objeto persiste entre escenas
            DontDestroyOnLoad(gameObject);
            Debug.Log("GameManager: Inicializado (Singleton).");
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (deathScreenUI != null) deathScreenUI.SetActive(false);
        if (victoryText != null) victoryText.gameObject.SetActive(false);
        UpdateUI();
    }

    // ==========================================================
    // LÓGICA DE INICIO Y CARGA
    // ==========================================================

    /// <summary>
    /// Llamada por el botón 'Play'. Resetea todo.
    /// </summary>
    public void StartGame()
    {
        ResetStats(); // Resetea Vidas y Monedas
        savedCheckpointIndex = 0; // Borra el checkpoint al iniciar un juego nuevo
        currentLevelIndex = 1;
        Time.timeScale = 1f;

        Debug.Log("GameManager: Recibida llamada StartGame. Cargando Level_01.");
        LoadLevel(currentLevelIndex);
    }

    /// <summary>
    /// Lógica de avance de nivel. Verifica y guarda el checkpoint.
    /// </summary>
    public void AdvanceLevel()
    {
        // ⭐ Verifica si el nivel que acabamos de completar es un checkpoint (3, 6, o 9)
        if (checkpointLevels.Contains(currentLevelIndex))
        {
            savedCheckpointIndex = currentLevelIndex;
            Debug.Log($"GameManager: ¡CHECKPOINT GUARDADO! Nivel {currentLevelIndex}.");
        }

        currentLevelIndex++;

        if (currentLevelIndex > FINAL_LEVEL_INDEX)
        {
            Debug.Log("GameManager: ¡Juego Completado!");
            StartCoroutine(ShowVictoryScreen());
        }
        else
        {
            LoadLevel(currentLevelIndex);
        }
    }

    // Método auxiliar para construir el nombre de la escena y cargarla
    void LoadLevel(int level)
    {
        // Usa el formato de dos dígitos (01, 02, ... 10)
        string sceneName = levelPrefix + level.ToString("00");
        Debug.Log($"GameManager: Llamando a SceneManager.LoadScene('{sceneName}')");
        SceneManager.LoadScene(sceneName);
    }

    void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // ==========================================================
    // LÓGICA DE MUERTE Y REINICIO (VIDAS PERSISTENTES)
    // ==========================================================

    public void PlayerDied()
    {
        if (Time.timeScale == 0f) return;

        currentLives--; // Quita una vida (el contador NO se resetea)
        Time.timeScale = 0f;
        UpdateUI(); // Actualiza el contador de vidas inmediatamente

        if (currentLives <= 0)
        {
            StartCoroutine(HandleGameOver()); // Game Over, ir al menú
        }
        else
        {
            // Reaparece en el último checkpoint (o Nivel 1 si es 0)
            StartCoroutine(HandleDeathAndRespawn());
        }
    }

    IEnumerator HandleDeathAndRespawn()
    {
        if (deathScreenUI != null) deathScreenUI.SetActive(true);
        yield return new WaitForSecondsRealtime(deathDisplayDelay);
        if (deathScreenUI != null) deathScreenUI.SetActive(false);
        Time.timeScale = 1f;

        // Determina el nivel de reinicio: si hay checkpoint (3, 6, 9), úsalo; si no, usa el Nivel 1.
        int restartLevel = (savedCheckpointIndex > 0) ? savedCheckpointIndex : 1;

        // Establece el índice del nivel actual al nivel de reinicio
        currentLevelIndex = restartLevel;

        Debug.Log($"GameManager: Muerte. Reiniciando en Level_{restartLevel.ToString("00")} con {currentLives} vidas restantes.");
        LoadLevel(restartLevel);
    }

    IEnumerator HandleGameOver()
    {
        if (deathScreenUI != null) deathScreenUI.SetActive(true);
        yield return new WaitForSecondsRealtime(deathDisplayDelay + 1f);

        ResetGame(); // Resetea Vidas y Checkpoint
        LoadScene(menuSceneName);
        Time.timeScale = 1f;
    }

    // ==========================================================
    // LÓGICA AUXILIAR Y MONEDAS (FIX para error CS1061)
    // ==========================================================

    /// <summary>
    /// Suma monedas al contador y verifica si el jugador gana una vida extra.
    /// FIX para el error CS1061 en Coin.cs
    /// </summary>
    public void CollectCoin(int amount)
    {
        currentCoins += amount;

        // Lógica para dar vida extra (si aplicas esta regla)
        if (currentCoins >= COINS_FOR_EXTRA_LIFE)
        {
            currentCoins -= COINS_FOR_EXTRA_LIFE;
            currentLives++; // Gana una vida
            Debug.Log("GameManager: ¡Vida extra obtenida!");
        }

        UpdateUI(); // Llama a la función que actualiza los TextMeshProUGUI
    }

    IEnumerator ShowVictoryScreen()
    {
        // Lógica de victoria, puedes mostrar victoryText aquí.
        yield break;
    }

    public void ResetStats() { currentLives = 3; currentCoins = 0; UpdateUI(); }
    void ResetGame() { ResetStats(); savedCheckpointIndex = 0; currentLevelIndex = 0; }

    void UpdateUI()
    {
        // Actualiza el texto de Vidas y Monedas
        if (livesText != null)
            livesText.text = "VIDAS: " + currentLives;

        if (coinsText != null)
            coinsText.text = "MONEDAS: " + currentCoins;
    }

    // Lógica para reconexión de UI al cargar una escena (crucial para Singletong)
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { UpdateUI(); }
}