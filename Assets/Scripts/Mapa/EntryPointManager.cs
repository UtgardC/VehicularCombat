using System.Collections;
using UnityEngine;
using VehicularCombat; // Asegura el acceso a tu VehicleInputReader sin errores

public class EntryPointManager : MonoBehaviour
{
    public static EntryPointManager Instance { get; private set; }

    [Header("Puntos de Acceso")]
    public EntryPoint[] entryPoints;

    [Header("Referencia al jugador")]
    public GameObject player;

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

        char zonaChar = entry.zona switch
        {
            EntryPoint.ZonaAsociada.A => 'A',
            EntryPoint.ZonaAsociada.B => 'B',
            EntryPoint.ZonaAsociada.C => 'C',
            _ => 'A'
        };

        HUDManager.Instance?.SetActiveZone(zonaChar);

        // 3. PREPARAR PANTALLA (Pone el Alpha en 0 y activa el objeto)
        HUDManager.Instance?.SetupIntroScreen(entry.imagenEspecificaDeInicio);

        // 4. ANIMACIÓN DE ENTRADA (Fade In del fondo + Flicker de la imagen)
        if (HUDManager.Instance != null && HUDManager.Instance.introPanelAnimator != null)
        {
            yield return StartCoroutine(HUDManager.Instance.introPanelAnimator.Animate(true));
        }

        // 5. TIEMPO DE LECTURA
        yield return new WaitForSecondsRealtime(introDuration);

        // 6. ANIMACIÓN DE SALIDA (Fade Out completo)
        if (HUDManager.Instance != null && HUDManager.Instance.introPanelAnimator != null)
        {
            yield return StartCoroutine(HUDManager.Instance.introPanelAnimator.Animate(false));
        }

        // 7. DEVOLVER CONTROLES AL JUGADOR
        if (inputReader != null)
        {
            inputReader.enabled = true;
        }

        // 8. INICIAR EL JUEGO OFICIALMENTE
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
}