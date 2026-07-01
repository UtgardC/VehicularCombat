using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ══════════════════════════════════════════════════════════════════════
    // PAUSA — animación y cursor (basado en UIMenuController)
    // ══════════════════════════════════════════════════════════════════════

    [Header("Panel Pausa")]
    [SerializeField] private RectTransform pausePanel;
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private Button pauseHubButton;

    [Header("Input")]
    [SerializeField] private InputActionReference toggleMenuActionReference;
    [SerializeField] private bool enableToggleActionOnEnable = true;

    [Header("Configuración de Pausa")]
    [SerializeField] private bool pauseTimeScale = true;
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private CursorLockMode gameplayCursorLockMode = CursorLockMode.Locked;

    [Header("Animación")]
    [SerializeField] private float hiddenPositionY = -1000f;
    [SerializeField] private float visiblePositionY = 0f;
    [SerializeField, Min(0f)] private float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private InputAction toggleMenuAction;
    private Coroutine currentAnimation;
    private bool menuOpen;
    private bool warnedMissingAction;
    private bool warnedMissingPanel;
    private float previousTimeScale = 1f;

    public bool IsMenuOpen => menuOpen;

    // ══════════════════════════════════════════════════════════════════════
    // VICTORIA Y DERROTA
    // ══════════════════════════════════════════════════════════════════════

    [Header("Panel Victoria")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI victoryCoinsText;
    [SerializeField] private Button victoryHubButton;

    [Header("Panel Derrota")]
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private TextMeshProUGUI defeatReasonText;
    [SerializeField] private TextMeshProUGUI defeatCoinsText;
    [SerializeField] private Button defeatHubButton;

    // ══════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        toggleMenuAction = toggleMenuActionReference != null ? toggleMenuActionReference.action : null;

        if (toggleMenuAction == null) { WarnMissingActionOnce(); return; }

        toggleMenuAction.performed += HandleToggleMenu;
        if (enableToggleActionOnEnable) toggleMenuAction.Enable();
    }

    void OnDisable()
    {
        if (toggleMenuAction != null)
        {
            toggleMenuAction.performed -= HandleToggleMenu;
            if (enableToggleActionOnEnable) toggleMenuAction.Disable();
        }

        if (menuOpen)
        {
            menuOpen = false;
            if (pauseTimeScale) Time.timeScale = previousTimeScale;
            if (lockCursorOnStart) ApplyGameplayCursorState();
        }
    }

    void Start()
    {
        // Pausa
        if (pausePanel != null)
        {
            pausePanel.anchoredPosition = new Vector2(pausePanel.anchoredPosition.x, hiddenPositionY);
            pausePanel.gameObject.SetActive(false);
        }
        else WarnMissingPanelOnce();

        if (pauseTimeScale) Time.timeScale = 1f;
        if (lockCursorOnStart) ApplyGameplayCursorState();

        // Victoria y Derrota
        victoryPanel?.SetActive(false);
        defeatPanel?.SetActive(false);

        // Botones
        pauseResumeButton?.onClick.AddListener(CloseMenu);
        pauseHubButton?.onClick.AddListener(OnHubButton);
        victoryHubButton?.onClick.AddListener(OnHubButton);
        defeatHubButton?.onClick.AddListener(OnHubButton);
    }

    // ══════════════════════════════════════════════════════════════════════
    // PAUSA — métodos públicos
    // ══════════════════════════════════════════════════════════════════════

    public void ToggleMenu() => SetMenuOpen(!menuOpen);
    public void OpenMenu() => SetMenuOpen(true);
    public void CloseMenu() => SetMenuOpen(false);
    public void ResumeGame() => CloseMenu();

    private void HandleToggleMenu(InputAction.CallbackContext context) => ToggleMenu();

    private void SetMenuOpen(bool open)
    {
        if (menuOpen == open) return;
        menuOpen = open;

        if (menuOpen)
        {
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            SetPausePanelActive(true);
            ApplyMenuCursorState();
            if (pauseTimeScale) Time.timeScale = 0f;
            GameManager.Instance?.SetStatePaused();
        }
        else
        {
            if (pauseTimeScale) Time.timeScale = previousTimeScale;
            ApplyGameplayCursorState();
            GameManager.Instance?.ResumeFromPause();
        }

        AnimateMenu(menuOpen ? visiblePositionY : hiddenPositionY);
    }

    private void AnimateMenu(float targetPositionY)
    {
        if (pausePanel == null) return;
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(AnimateMenuPosition(targetPositionY));
    }

    private IEnumerator AnimateMenuPosition(float targetPositionY)
    {
        float elapsed = 0f;
        float startPositionY = pausePanel.anchoredPosition.y;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = transitionDuration > 0f ? Mathf.Clamp01(elapsed / transitionDuration) : 1f;
            float curvedT = transitionCurve.Evaluate(t);
            float nextPositionY = Mathf.LerpUnclamped(startPositionY, targetPositionY, curvedT);
            pausePanel.anchoredPosition = new Vector2(pausePanel.anchoredPosition.x, nextPositionY);
            yield return null;
        }

        pausePanel.anchoredPosition = new Vector2(pausePanel.anchoredPosition.x, targetPositionY);
        if (!menuOpen) SetPausePanelActive(false);
        currentAnimation = null;
    }

    private void SetPausePanelActive(bool active)
    {
        if (pausePanel == null) { WarnMissingPanelOnce(); return; }
        pausePanel.gameObject.SetActive(active);
    }

    // ══════════════════════════════════════════════════════════════════════
    // VICTORIA Y DERROTA — métodos públicos
    // ══════════════════════════════════════════════════════════════════════

    public void ShowVictoryScreen()
    {
        if (menuOpen) CloseMenu();
        victoryPanel?.SetActive(true);
        Time.timeScale = 0f;
        ApplyMenuCursorState();

        if (victoryCoinsText != null && CurrencyManager.Instance != null)
            victoryCoinsText.text = $"Monedas ganadas: {CurrencyManager.Instance.GetCoins()} ¢";
    }

    public void ShowDefeatScreen(string reason = "Nave destruida")
    {
        if (menuOpen) CloseMenu();
        defeatPanel?.SetActive(true);
        Time.timeScale = 0f;
        ApplyMenuCursorState();

        if (defeatReasonText != null)
            defeatReasonText.text = reason;

        if (defeatCoinsText != null && CurrencyManager.Instance != null)
            defeatCoinsText.text = $"Monedas ganadas: {CurrencyManager.Instance.GetCoins()} ¢";
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════

    private void OnHubButton()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.ReturnToHub();
    }

    private void ApplyMenuCursorState() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    private void ApplyGameplayCursorState() { Cursor.lockState = gameplayCursorLockMode; Cursor.visible = false; }

    private void WarnMissingActionOnce()
    {
        if (warnedMissingAction) return;
        Debug.LogWarning($"{nameof(UIManager)} on {name} has no valid Toggle Menu Input Action Reference assigned.", this);
        warnedMissingAction = true;
    }

    private void WarnMissingPanelOnce()
    {
        if (warnedMissingPanel) return;
        Debug.LogWarning($"{nameof(UIManager)} on {name} has no Pause Panel assigned.", this);
        warnedMissingPanel = true;
    }
}