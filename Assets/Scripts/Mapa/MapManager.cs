using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("Zona Final")]
    public GameObject finalZone;
    public GameObject finalZoneBlocker; // Collider/barrera física que impide el acceso

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (finalZone != null) finalZone.SetActive(false);
        if (finalZoneBlocker != null) finalZoneBlocker.SetActive(true);
    }

    public void EnableFinalZone()
    {
        if (finalZone != null) finalZone.SetActive(true);
        if (finalZoneBlocker != null) finalZoneBlocker.SetActive(false);
        Debug.Log("Zona final desbloqueada.");
    }
}