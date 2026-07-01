using UnityEngine;

public class EntryPointManager : MonoBehaviour
{
    public static EntryPointManager Instance { get; private set; }

    [Header("Puntos de Acceso A-G (asignar en Inspector)")]
    public Transform[] entryPoints; // 7 entradas: A=0, B=1 ... G=6

    [Header("Referencia al jugador")]
    public GameObject player; // Arrastrar el GameObject del vehículo aquí

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
        SpawnPlayerAt(entryPoints[index]);

        char entryLabel = (char)('A' + index);
        Debug.Log($"Entrada seleccionada: {entryLabel}");
    }

    void SpawnPlayerAt(Transform entry)
    {
        if (player == null)
        {
            Debug.LogWarning("EntryPointManager: no hay un jugador asignado en el Inspector.");
            return;
        }

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