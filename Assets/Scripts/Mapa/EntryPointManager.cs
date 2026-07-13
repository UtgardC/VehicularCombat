using System.Collections;
using UnityEngine;
using VehicularCombat;

public class EntryPointManager : MonoBehaviour
{
    public static EntryPointManager Instance { get; private set; }

    [Header("Puntos de Acceso")]
    public EntryPoint[] entryPoints;

    [Header("Referencia al jugador")]
    public GameObject player;

    // --- AGREGADO PARA ENEMIGOS ---
    [Header("Enemigos")]
    [Tooltip("Arrastrá acá el PREFAB (cubo azul) del enemigo")]
    public GameObject enemyPrefab;

    [Tooltip("Cantidad de enemigos que van a aparecer en cada punto libre")]
    public int enemiesPerSpawnPoint = 2;

    [Tooltip("Radio en metros para que los autos no spawneen pegados y salgan volando")]
    public float spawnRadius = 4f;

    [Header("Configuración de Intro")]
    [Tooltip("Segundos que dura la pantalla de inicio bloqueando al jugador")]
    public float introDuration = 3f;

    private int lastUsedIndex = -1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SelectRandomEntry()
    {
        if (entryPoints == null || entryPoints.Length == 0)
        {
            Debug.LogWarning("EntryPointManager: no hay puntos de acceso asignados.");
            return;
        }

        int index;
        do { index = Random.Range(0, entryPoints.Length); }
        while (index == lastUsedIndex && entryPoints.Length > 1);

        lastUsedIndex = index;
        EntryPoint selectedEntry = entryPoints[index];

        StartCoroutine(RunIntroSequence(selectedEntry));
    }

    private IEnumerator RunIntroSequence(EntryPoint entry)
    {
        // 1. BLOQUEAR CONTROLES
        VehicleInputReader inputReader = player.GetComponent<VehicleInputReader>();
        if (inputReader != null)
        {
            inputReader.enabled = false;
        }

        // 2. POSICIONAR AL JUGADOR
        SpawnPlayerAt(entry.transform);

        // --- 3. SPAWNEAR MÚLTIPLES ENEMIGOS EN LA MISMA ZONA ---
        SpawnEnemiesInSameZone(entry);

        char zonaChar = entry.zona switch
        {
            EntryPoint.ZonaAsociada.A => 'A',
            EntryPoint.ZonaAsociada.B => 'B',
            EntryPoint.ZonaAsociada.C => 'C',
            _ => 'A'
        };

        HUDManager.Instance?.SetActiveZone(zonaChar);

        // 4. PREPARAR PANTALLA
        HUDManager.Instance?.SetupIntroScreen(entry.imagenEspecificaDeInicio);

        // 5. ANIMACIÓN DE ENTRADA
        if (HUDManager.Instance != null && HUDManager.Instance.introPanelAnimator != null)
        {
            yield return StartCoroutine(HUDManager.Instance.introPanelAnimator.Animate(true));
        }

        // 6. TIEMPO DE LECTURA
        yield return new WaitForSecondsRealtime(introDuration);

        // 7. ANIMACIÓN DE SALIDA
        if (HUDManager.Instance != null && HUDManager.Instance.introPanelAnimator != null)
        {
            yield return StartCoroutine(HUDManager.Instance.introPanelAnimator.Animate(false));
        }

        // 8. DEVOLVER CONTROLES AL JUGADOR
        if (inputReader != null)
        {
            inputReader.enabled = true;
        }

        // 9. INICIAR EL JUEGO OFICIALMENTE
        if (global::GameManager.Instance != null)
        {
            global::GameManager.Instance.SetState(global::GameManager.GameState.Playing);
        }

        Debug.Log("Secuencia de introducción completada. Estado del juego cambiado a Playing.");
    }

    void SpawnPlayerAt(Transform entry)
    {
        if (player == null) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = entry.position;
            rb.rotation = entry.rotation;
        }
        else
        {
            player.transform.SetPositionAndRotation(entry.position, entry.rotation);
        }
    }

    // --- METODO PARA GENERAR LOS MULTIPLES ENEMIGOS ---
    private void SpawnEnemiesInSameZone(EntryPoint playerEntry)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Falta asignar el Prefab del Enemigo en el EntryPointManager.");
            return;
        }

        // Recorremos todos los puntos de la lista
        foreach (EntryPoint ep in entryPoints)
        {
            // Verificamos que sea la misma zona y NO sea el punto del jugador
            if (ep.zona == playerEntry.zona && ep != playerEntry)
            {
                // Spawneamos la cantidad que elegiste en el Inspector
                for (int i = 0; i < enemiesPerSpawnPoint; i++)
                {
                    // Calculamos una posición al azar alrededor del punto central
                    Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                    Vector3 spawnOffset = new Vector3(randomCircle.x, 1.5f, randomCircle.y);
                    Vector3 finalSpawnPosition = ep.transform.position + spawnOffset;

                    // Instanciamos al enemigo
                    Instantiate(enemyPrefab, finalSpawnPosition, ep.transform.rotation);
                }
            }
        }
    }
}