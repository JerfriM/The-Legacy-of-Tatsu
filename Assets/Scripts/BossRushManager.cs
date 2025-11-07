using UnityEngine;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class BossRushManager : MonoBehaviour
{
    // ==========================================================
    // CONFIGURACIÓN DE ESCENA Y ENEMIGOS
    // ==========================================================
    [Header("Configuración del Rush")]
    [Tooltip("Punto donde se generarán todos los orcos.")]
    public Transform spawnPoint;

    [Tooltip("Lista ordenada de Prefabs de orcos (Mago, Green, Red).")]
    public GameObject[] orcPrefabs;

    // ⭐ VARIABLE DE TIEMPO
    [Header("Temporizador de Aparición")]
    [Tooltip("Tiempo en segundos que espera el manager antes de generar el siguiente orco.")]
    public float spawnInterval = 30f;

    // ==========================================================
    // RECOMPENSA DE VICTORIA
    // ==========================================================
    [Header("Recompensa de Victoria")]
    // Se mantiene IEnemy/RedOrc para chequear la muerte SOLO del jefe final.
    public ParedBloqueante paredFinal;
    public TextMeshProUGUI victoryText;
    public float delayBeforeWinMessage = 1.0f;

    // ==========================================================
    // ESTADO DE LA BATALLA
    // ==========================================================
    private int currentOrcIndex = 0;
    private GameObject currentOrcInstance;

    // ⭐ Necesitamos la interfaz IEnemy SOLO para el último orco.
    private IEnemy currentOrcComponent;

    void Start()
    {
        if (spawnPoint == null || orcPrefabs.Length < 3)
        {
            Debug.LogError("Faltan referencias en BossRushManager (spawnPoint o prefabs).");
            return;
        }

        if (victoryText != null)
        {
            victoryText.gameObject.SetActive(false);
        }

        StartCoroutine(BossRushSequence());
    }

    /// <summary>
    /// Coroutine principal: Genera un orco, espera 30 segundos, repite.
    /// </summary>
    IEnumerator BossRushSequence()
    {
        Debug.Log("--- INICIANDO NIVEL BOSS RUSH (30s Interval) ---");

        // Bucle para generar a los 3 orcos.
        while (currentOrcIndex < orcPrefabs.Length)
        {
            // 1. APARECE EL ORCO
            yield return StartCoroutine(SpawnNextOrc());

            // ⭐ 2. ESPERA EL INTERVALO DE TIEMPO FIJO (30 segundos)
            Debug.Log($"Esperando {spawnInterval} segundos para generar el siguiente orco.");
            yield return new WaitForSeconds(spawnInterval);
            // El ciclo ignora la muerte y avanza al siguiente orco.
        }

        // Ya se generaron los 3 orcos. Ahora, debemos esperar a que el ÚLTIMO muera para terminar el nivel.
        Debug.Log("Todos los orcos han sido generados. Esperando la derrota del jefe final...");

        // Esperamos la muerte del tercer orco, usando la referencia que quedó en 'currentOrcComponent'.
        if (currentOrcComponent != null)
        {
            // Requiere que el último orco muera para avanzar.
            yield return new WaitUntil(() => currentOrcComponent.IsDead);
        }

        // 3. LÓGICA DE VICTORIA FINAL 
        Debug.Log("--- ¡TODOS LOS ORCOS DERROTADOS! ---");
        HandleVictory();
    }


    /// <summary>
    /// Genera el siguiente orco de la secuencia.
    /// </summary>
    IEnumerator SpawnNextOrc()
    {
        if (currentOrcIndex >= orcPrefabs.Length)
        {
            yield break;
        }

        GameObject orcToSpawn = orcPrefabs[currentOrcIndex];

        // Instanciar en el punto de aparición
        currentOrcInstance = Instantiate(orcToSpawn, spawnPoint.position, Quaternion.identity);
        currentOrcInstance.name = orcToSpawn.name + $" (Rush {currentOrcIndex + 1})";

        // Obtener la interfaz SOLO si es el último orco, para poder esperar su muerte al final.
        if (currentOrcIndex == orcPrefabs.Length - 1)
        {
            currentOrcComponent = currentOrcInstance.GetComponent<IEnemy>();
        }

        Debug.Log($"Generado {orcToSpawn.name} ({currentOrcIndex + 1}/{orcPrefabs.Length}).");

        yield return new WaitForSeconds(0.5f);

        currentOrcIndex++;
    }

    private void HandleVictory()
    {
        GameManager.Instance.AdvanceLevel();

        if (paredFinal != null)
        {
            paredFinal.DesbloquearMundo();
        }

        StartCoroutine(DisplayWinMessage());
    }

    IEnumerator DisplayWinMessage()
    {
        yield return new WaitForSeconds(delayBeforeWinMessage);

        if (victoryText != null)
        {
            victoryText.gameObject.SetActive(true);
            victoryText.text = "¡HAS GANADO!";
        }

        Time.timeScale = 0f;
    }
}