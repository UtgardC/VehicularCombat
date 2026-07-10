using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunStartScreen : MonoBehaviour
{
    [Header("Imagen principal")]
    public UnityEngine.UI.Image screenImage;
    public Sprite screenSprite;

    [Header("Elementos que flickean (CanvasGroup por cada uno)")]
    public CanvasGroup[] flickerTargets;

    [Header("CanvasGroup raíz del panel (para ocultar todo junto)")]
    public CanvasGroup rootCanvasGroup;

    [Header("Configuración de Flicker")]
    public float flickerDuration = 2.5f;
    public float stabilizeAfter = 1.8f;
    public float flickerIntervalMin = 0.04f;
    public float flickerIntervalMax = 0.18f;
    public AnimationCurve stabilizeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool inputEnabled = false;

    void Start()
    {
        // Asegurarse de que el GameManager esté en WaitingToStart
        if (GameManager.Instance != null &&
            GameManager.Instance.State != GameManager.GameState.WaitingToStart)
        {
            // Si ya está corriendo (ej. recarga de escena), igualmente mostramos la pantalla
            GameManager.Instance.SetStateWaitingToStart();
        }

        Time.timeScale = 0f;
        SetFlickerTargetsAlpha(0f);

        if (screenImage != null && screenSprite != null)
            screenImage.sprite = screenSprite;

        StartCoroutine(PlayIntroFlicker(() => inputEnabled = true));
    }

    void Update()
    {
        if (!inputEnabled) return;

        // if (Input.GetKeyDown(KeyCode.Space))
           // StartRun();
        // else if (Input.GetKeyDown(KeyCode.Escape))
           //  ReturnToHub();
    }

    // ── Acciones ──────────────────────────────────────────────────────────────

    void StartRun()
    {
        inputEnabled = false;
        StartCoroutine(PlayOutroFlicker(() =>
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
            GameManager.Instance?.StartRun();
        }));
    }

    void ReturnToHub()
    {
        inputEnabled = false;
        Time.timeScale = 1f;
        GameManager.Instance?.ReturnToHub();
    }

    // ── Flicker intro ─────────────────────────────────────────────────────────

    IEnumerator PlayIntroFlicker(System.Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < flickerDuration)
        {
            float stabilizeProgress = Mathf.Clamp01(
                (elapsed - stabilizeAfter) / (flickerDuration - stabilizeAfter)
            );
            float stableAlpha = stabilizeCurve.Evaluate(stabilizeProgress);
            float noise = Mathf.Max(0f, 1f - stabilizeProgress) * Random.Range(0f, 1f);
            float targetAlpha = Mathf.Clamp01(stableAlpha + noise);

            float interval = Random.Range(flickerIntervalMin, flickerIntervalMax);
            float tickElapsed = 0f;

            while (tickElapsed < interval && elapsed < flickerDuration)
            {
                float dt = Time.unscaledDeltaTime;
                tickElapsed += dt;
                elapsed += dt;
                SetFlickerTargetsAlpha(targetAlpha);
                yield return null;
            }
        }

        SetFlickerTargetsAlpha(1f);
        onComplete?.Invoke();
    }

    // ── Flicker outro ─────────────────────────────────────────────────────────

    IEnumerator PlayOutroFlicker(System.Action onComplete)
    {
        float elapsed = 0f;
        float outDuration = 1.0f;

        while (elapsed < outDuration)
        {
            float progress = elapsed / outDuration;
            float baseAlpha = 1f - stabilizeCurve.Evaluate(progress);
            float noise = (1f - progress) * Random.Range(0f, 1f) * 0.4f;
            float targetAlpha = Mathf.Clamp01(baseAlpha + noise);

            float interval = Random.Range(flickerIntervalMin, flickerIntervalMax * 0.5f);
            float tickElapsed = 0f;

            while (tickElapsed < interval && elapsed < outDuration)
            {
                float dt = Time.unscaledDeltaTime;
                tickElapsed += dt;
                elapsed += dt;
                SetFlickerTargetsAlpha(targetAlpha);
                yield return null;
            }
        }

        SetFlickerTargetsAlpha(0f);
        onComplete?.Invoke();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    void SetFlickerTargetsAlpha(float alpha)
    {
        if (flickerTargets == null) return;
        foreach (var cg in flickerTargets)
            if (cg != null) cg.alpha = alpha;
    }
}