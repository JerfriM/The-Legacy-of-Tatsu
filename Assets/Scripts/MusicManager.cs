using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Música General")]
    [Tooltip("Música estándar para la mayoría de los niveles (ej. Level_01, 02, 04, 05, etc.)")]
    public AudioClip standardMusic;

    [Header("Música Especial")]
    [Tooltip("Música para Level_03")]
    public AudioClip level3Music;
    [Tooltip("Música para Level_06")]
    public AudioClip level6Music;
    [Tooltip("Música para Level_09")]
    public AudioClip level9Music;

    private AudioSource audioSource;

    void Awake()
    {
        // Lógica Singleton (asegura que solo haya una copia y que persista)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true;
        audioSource.playOnAwake = false; // Desactivamos esto ya que lo controlaremos por código
    }

    void OnEnable()
    {
        // Se suscribe al evento de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Se desuscribe al evento para evitar errores
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Función que se llama CADA VEZ que Unity termina de cargar una escena.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        if (sceneName == "MainMenu")
        {
            // Opcional: Podrías cargar una música de menú aquí, o simplemente la estándar
            ChangeMusic(standardMusic);
        }
        else if (sceneName == "Level_03" && level3Music != null)
        {
            ChangeMusic(level3Music);
        }
        else if (sceneName == "Level_06" && level6Music != null)
        {
            ChangeMusic(level6Music);
        }
        else if (sceneName == "Level_09" && level9Music != null)
        {
            ChangeMusic(level9Music);
        }
        else
        {
            // Para todas las demás escenas (Level_01, 02, 04, 05, 07, 08, 10, etc.)
            ChangeMusic(standardMusic);
        }
    }

    /// <summary>
    /// Cambia el clip de audio actual del AudioSource y empieza a reproducirlo.
    /// </summary>
    private void ChangeMusic(AudioClip newClip)
    {
        if (audioSource.clip == newClip)
        {
            return; // Ya estamos reproduciendo esta música, no hacer nada
        }

        if (newClip != null)
        {
            audioSource.clip = newClip;
            audioSource.Play();
            Debug.Log("MusicManager: Música cambiada a " + newClip.name);
        }
    }

    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}