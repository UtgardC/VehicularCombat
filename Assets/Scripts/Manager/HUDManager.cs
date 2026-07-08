using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("HUD")]
    public Image hudImage;

    [Header("Tiempo")]
    public TextMeshProUGUI timerText;

    [Header("Velocímetro")]
    public TextMeshProUGUI speedText;
    public Image speedNeedle;
    public float maxSpeed = 120f;

    [Header("Mira")]
    public RectTransform crosshairPlaceholder;

    [Header("Cargador")]
    public TextMeshProUGUI ammoText;
    private int currentAmmo;
    private int maxAmmo;

    [Header("Mapa")]
    public Image imagenDeZona;

    public Sprite spriteZona1;
    public Sprite spriteZona2;
    public Sprite spriteZona3;

    [Header("Monedas")]
    public TextMeshProUGUI coinsText;

    [Header("Objetivos — íconos (placeholder o sprite tuyo)")]
    // Un Image por objetivo, en orden: índice 0, 1, 2
    // Cuando el objetivo se destruye, el ícono se oscurece/tacha
    public Image[] objectiveIcons;
    public Color destroyedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    public Sprite destroyedSprite; // opcional: sprite de tachado/roto

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Timer ─────────────────────────────────────────────────────────────────
    public void UpdateTimer(float seconds)
    {
        if (seconds < 0) seconds = 0;
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        int ms = Mathf.FloorToInt((seconds % 1) * 60);
        timerText.text = $"{m:00}:{s:00}:{ms:00}";
        timerText.color = seconds <= 30f ? Color.red : Color.white;
    }

    // ── Velocímetro ───────────────────────────────────────────────────────────
    public void UpdateSpeed(float speed)
    {
        if (speedText != null)
            speedText.text = $"{Mathf.RoundToInt(speed)} km/h";

        if (speedNeedle != null)
        {
            float angle = Mathf.Lerp(135f, -135f, speed / maxSpeed);
            speedNeedle.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
        }
    }

    // ── Cargador ──────────────────────────────────────────────────────────────
    public void InitAmmo(int max)
    {
        maxAmmo = max;
        currentAmmo = max;
        RefreshAmmoUI();
    }

    public void ConsumeAmmo()
    {
        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        RefreshAmmoUI();
    }

    public void ReloadComplete()
    {
        currentAmmo = maxAmmo;
        RefreshAmmoUI();
    }

    void RefreshAmmoUI()
    {
        if (ammoText != null)
            ammoText.text = $"{currentAmmo}/{maxAmmo}";
    }

    // ── Objetivos ─────────────────────────────────────────────────────────────
    public void MarkObjectiveDestroyed(int index)
    {
        if (objectiveIcons == null || index >= objectiveIcons.Length) return;

        Image icon = objectiveIcons[index];
        if (icon == null) return;

        // Si tenés un sprite de "destruido" lo swappea, sino solo lo oscurece
        if (destroyedSprite != null)
            icon.sprite = destroyedSprite;

        icon.color = destroyedColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Identificamos la zona usando el nombre del objeto o un Tag personalizado
        if (other.CompareTag("Player")) // Asegúrate de que el jugador tiene este Tag
        {
            // Opcional: Esto detecta qué trigger tocó evaluando el nombre del objeto
            if (other.gameObject.tag == "ZonaATrigger")
            {
                CambiarSprite(spriteZona1);
            }
            else if (other.gameObject.tag == "ZonaBTrigger")
            {
                CambiarSprite(spriteZona2);
            }
            else if (other.gameObject.tag == "ZonaCTrigger")
            {
                CambiarSprite(spriteZona3);
            }
        }
    }

    private void CambiarSprite(Sprite nuevoSprite)
    {
        if (imagenDeZona != null && nuevoSprite != null)
        {
            imagenDeZona.sprite = nuevoSprite; // Cambia la imagen asignada
        }
    }

    // ── Monedas ───────────────────────────────────────────────────────────────
    public void UpdateCoins(int amount)
        {
            if (coinsText != null)
                coinsText.text = $"¢ {amount}";
        }
}