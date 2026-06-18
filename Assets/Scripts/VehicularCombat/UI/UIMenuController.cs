using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VehicularCombat
{
    public sealed class UIMenuController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Panel root that is shown while the game is paused.")]
        private RectTransform menuPanel;

        [Header("Input")]
        [SerializeField, Tooltip("Pause/Menu toggle action. Recommended type: Button, binding: Escape. Avoid Gamepad Start if Restart also uses Start.")]
        private InputActionReference toggleMenuActionReference;

        [SerializeField, Tooltip("Enable the toggle action when this component is enabled.")]
        private bool enableToggleActionOnEnable = true;

        [Header("Pause")]
        [SerializeField, Tooltip("Set Time.timeScale to 0 while the menu is open.")]
        private bool pauseTimeScale = true;

        [SerializeField, Tooltip("Lock and hide the cursor when the scene starts.")]
        private bool lockCursorOnStart = true;

        [SerializeField, Tooltip("Cursor lock mode used during gameplay.")]
        private CursorLockMode gameplayCursorLockMode = CursorLockMode.Locked;

        [Header("Animation")]
        [SerializeField, Tooltip("Hidden menu Y position in anchored UI coordinates.")]
        private float hiddenPositionY = -1000f;

        [SerializeField, Tooltip("Visible menu Y position in anchored UI coordinates.")]
        private float visiblePositionY = 0f;

        [SerializeField, Min(0f), Tooltip("Menu transition duration in unscaled seconds.")]
        private float transitionDuration = 0.5f;

        [SerializeField, Tooltip("Animation curve used for the menu slide.")]
        private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private InputAction toggleMenuAction;
        private Coroutine currentAnimation;
        private bool menuOpen;
        private bool warnedMissingAction;
        private bool warnedMissingPanel;
        private float previousTimeScale = 1f;

        public bool IsMenuOpen => menuOpen;

        private void OnEnable()
        {
            toggleMenuAction = toggleMenuActionReference != null ? toggleMenuActionReference.action : null;

            if (toggleMenuAction == null)
            {
                WarnMissingActionOnce();
                return;
            }

            toggleMenuAction.performed += HandleToggleMenu;

            if (enableToggleActionOnEnable)
            {
                toggleMenuAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (toggleMenuAction != null)
            {
                toggleMenuAction.performed -= HandleToggleMenu;

                if (enableToggleActionOnEnable)
                {
                    toggleMenuAction.Disable();
                }
            }

            if (menuOpen)
            {
                menuOpen = false;

                if (pauseTimeScale)
                {
                    Time.timeScale = previousTimeScale;
                }

                if (lockCursorOnStart)
                {
                    ApplyGameplayCursorState();
                }
            }
        }

        private void Start()
        {
            if (menuPanel != null)
            {
                menuPanel.anchoredPosition = new Vector2(menuPanel.anchoredPosition.x, hiddenPositionY);
                menuPanel.gameObject.SetActive(false);
            }
            else
            {
                WarnMissingPanelOnce();
            }

            if (pauseTimeScale)
            {
                Time.timeScale = 1f;
            }

            if (lockCursorOnStart)
            {
                ApplyGameplayCursorState();
            }
        }

        public void ToggleMenu()
        {
            SetMenuOpen(!menuOpen);
        }

        public void OpenMenu()
        {
            SetMenuOpen(true);
        }

        public void CloseMenu()
        {
            SetMenuOpen(false);
        }

        public void ResumeGame()
        {
            CloseMenu();
        }

        private void HandleToggleMenu(InputAction.CallbackContext context)
        {
            ToggleMenu();
        }

        private void SetMenuOpen(bool open)
        {
            if (menuOpen == open)
            {
                return;
            }

            menuOpen = open;

            if (menuOpen)
            {
                previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                SetMenuPanelActive(true);
                ApplyMenuCursorState();

                if (pauseTimeScale)
                {
                    Time.timeScale = 0f;
                }
            }
            else
            {
                if (pauseTimeScale)
                {
                    Time.timeScale = previousTimeScale;
                }

                ApplyGameplayCursorState();
            }

            AnimateMenu(menuOpen ? visiblePositionY : hiddenPositionY);
        }

        private void AnimateMenu(float targetPositionY)
        {
            if (menuPanel == null)
            {
                return;
            }

            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            currentAnimation = StartCoroutine(AnimateMenuPosition(targetPositionY));
        }

        private IEnumerator AnimateMenuPosition(float targetPositionY)
        {
            float elapsed = 0f;
            float startPositionY = menuPanel.anchoredPosition.y;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = transitionDuration > 0f ? Mathf.Clamp01(elapsed / transitionDuration) : 1f;
                float curvedT = transitionCurve.Evaluate(t);
                float nextPositionY = Mathf.LerpUnclamped(startPositionY, targetPositionY, curvedT);

                menuPanel.anchoredPosition = new Vector2(menuPanel.anchoredPosition.x, nextPositionY);
                yield return null;
            }

            menuPanel.anchoredPosition = new Vector2(menuPanel.anchoredPosition.x, targetPositionY);

            if (!menuOpen)
            {
                SetMenuPanelActive(false);
            }

            currentAnimation = null;
        }

        private void SetMenuPanelActive(bool active)
        {
            if (menuPanel == null)
            {
                WarnMissingPanelOnce();
                return;
            }

            menuPanel.gameObject.SetActive(active);
        }

        private void ApplyMenuCursorState()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ApplyGameplayCursorState()
        {
            Cursor.lockState = gameplayCursorLockMode;
            Cursor.visible = false;
        }

        private void WarnMissingActionOnce()
        {
            if (warnedMissingAction)
            {
                return;
            }

            Debug.LogWarning($"{nameof(UIMenuController)} on {name} has no valid Toggle Menu Input Action Reference assigned.", this);
            warnedMissingAction = true;
        }

        private void WarnMissingPanelOnce()
        {
            if (warnedMissingPanel)
            {
                return;
            }

            Debug.LogWarning($"{nameof(UIMenuController)} on {name} has no Menu Panel assigned.", this);
            warnedMissingPanel = true;
        }
    }
}
