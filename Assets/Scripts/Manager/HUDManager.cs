using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    // ── Cronómetro ────────────────────────────────────────────────────────────
    [Header("Cronómetro")]
    public TextMeshProUGUI timerText;

    // ── Objetivos ─────────────────────────────────────────────────────────────
    [Header("Objetivos (Contador de texto)")]
    public TextMeshProUGUI objectiveCounterText; // <-- El texto que dice "0/3"

    // ── Munición ──────────────────────────────────────────────────────────────
    [Header("Munición")]
    public TextMeshProUGUI ammoText;        // "30/30"
    private int currentAmmo;
    private int maxAmmo;

    // ── Vida ──────────────────────────────────────────────────────────────────
    [Header("Vida")]
    [Tooltip("Asegúrate de que esta imagen tenga su Image Type en 'Filled'")]
    public Image healthFill;
    public TextMeshProUGUI healthText;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    [Tooltip("Porcentaje de vida (0-1) por debajo del cual el fill se vuelve rojo")]
    public float lowHealthThreshold = 0.3f;

    // ── Velocímetro ───────────────────────────────────────────────────────────
    [Header("Velocímetro")]
    public TextMeshProUGUI speedText;       // número de velocidad
    public Image speedNeedle;              // aguja rotatoria (opcional)
    public float maxDisplaySpeed = 120f;

    [Tooltip("Color hexadecimal para los ceros a la izquierda (ej: #4A4A4A80 para gris semi-transparente)")]
    public string zeroColorHex = "#555555"; // Puedes cambiar este color en el Inspector

    // ── Turbo ─────────────────────────────────────────────────────────────────
    [Header("Turbo")]
    [Tooltip("Asegúrate de que esta imagen tenga su Image Type en 'Filled'")]
    public Image turboFill;

    public Color turboFullColor = Color.cyan;
    public Color turboDepletedColor = Color.gray;

    // ── Mapa / Zona ───────────────────────────────────────────────────────────
    [Header("Indicador de Zona (A / B / C)")]
    public Image imagenDeZona;
    public Sprite spriteZona1;  // Zona A
    public Sprite spriteZona2;  // Zona B
    public Sprite spriteZona3;  // Zona C

    // ── Monedas ───────────────────────────────────────────────────────────────
    [Header("Monedas")]
    public TextMeshProUGUI coinsText;

    // ── Pantalla de Inicio Animada ────────────────────────────────────────────
    [Header("Pantalla de Inicio")]
    public PanelAnimator introPanelAnimator;
    public Image introLocationImage;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Cronómetro ────────────────────────────────────────────────────────────
    public void UpdateTimer(float seconds)
    {
        if (seconds < 0) seconds = 0;
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        int ms = Mathf.FloorToInt((seconds % 1) * 60);
        if (timerText != null)
        {
            timerText.text = $"{m:00}:{s:00}:{ms:00}";
            timerText.color = seconds <= 30f ? Color.red : Color.white;
        }
    }

    // ── Objetivos ─────────────────────────────────────────────────────────────
    public void UpdateObjectiveText(int current, int total)
    {
        if (objectiveCounterText != null)
        {
            objectiveCounterText.text = $"{current}/{total}";
        }
    }

    // ── Munición ──────────────────────────────────────────────────────────────
    public void InitAmmo(int max)
    {
        maxAmmo = max;
        currentAmmo = max;
        RefreshAmmo();
    }

    public void ConsumeAmmo()
    {
        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        RefreshAmmo();
    }

    public void ReloadComplete()
    {
        currentAmmo = maxAmmo;
        RefreshAmmo();
    }

    void RefreshAmmo()
    {
        if (ammoText != null)
        {
            ammoText.text = currentAmmo.ToString();
        }
    }

    // ── Vida ──────────────────────────────────────────────────────────────────

    public void UpdateHealth(float current, float max)
    {
        float ratio = current / max;

        if (healthFill != null)
        {
            healthFill.fillAmount = ratio;
            healthFill.color = ratio <= lowHealthThreshold
                ? lowHealthColor
                : Color.Lerp(lowHealthColor, fullHealthColor, (ratio - lowHealthThreshold) / (1f - lowHealthThreshold));
        }

        if (healthText != null)
            healthText.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
    }

    // ── Velocímetro ───────────────────────────────────────────────────────────
    public void UpdateSpeed(float speed)
    {
        if (speedText != null)
        {
            int speedInt = Mathf.Clamp(Mathf.RoundToInt(speed), 0, 999);
            string speedStr = speedInt.ToString();

            if (speedInt < 10)
            {
                speedText.text = $"{speedStr}<color={zeroColorHex}>00</color>";
            }
            else if (speedInt < 100)
            {
                speedText.text = $"{speedStr}<color={zeroColorHex}>0</color>";
            }
            else
            {
                speedText.text = speedStr;
            }
        }

        if (speedNeedle != null)
        {
            float angle = Mathf.Lerp(135f, -135f, Mathf.Clamp01(speed / maxDisplaySpeed));
            speedNeedle.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }

    // ── Turbo ──────────────────────────────────────────────────────────────────

    public void UpdateTurbo(float current, float max)
    {
        if (turboFill == null) return;

        float ratio = current / max;

        turboFill.fillAmount = ratio;
        turboFill.color = Color.Lerp(turboDepletedColor, turboFullColor, ratio);
    }

    // ── Zona ──────────────────────────────────────────────────────────────────
    public void SetActiveZone(char zone)
    {
        Sprite sprite = zone switch
        {
            'A' => spriteZona1,
            'B' => spriteZona2,
            'C' => spriteZona3,
            _ => null
        };

        if (imagenDeZona != null && sprite != null)
            imagenDeZona.sprite = sprite;
    }

    // ── Monedas ───────────────────────────────────────────────────────────────
    public void UpdateCoins(int amount)
    {
        if (coinsText != null)
            coinsText.text = $"¢ {amount}";
    }

    public void SetupIntroScreen(Sprite sprite)
    {
        if (introLocationImage != null && sprite != null)
        {
            introLocationImage.sprite = sprite;
        }

        if (introPanelAnimator != null)
        {
            introPanelAnimator.SetupHidden();
        }
    }
}