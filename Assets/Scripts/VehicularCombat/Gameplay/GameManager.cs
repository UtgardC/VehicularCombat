using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VehicularCombat
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Central input reader used for Restart.")]
        private VehicleInputReader inputReader;

        [SerializeField, Tooltip("TextMeshPro label that shows remaining targets.")]
        private TextMeshProUGUI remainingTargetsText;

        [SerializeField, Tooltip("Victory panel GameObject. It starts hidden and is shown when all targets are destroyed.")]
        private GameObject victoryPanel;

        [SerializeField, Tooltip("Optional UI button that restarts the current scene.")]
        private Button restartButton;

        [Header("Targets")]
        [SerializeField, Tooltip("Automatically find active DamageableTarget components on Start.")]
        private bool findTargetsOnStart = true;

        [SerializeField, Tooltip("Optional manually assigned targets. Useful if auto-find should be disabled.")]
        private List<DamageableTarget> targets = new();

        [Header("Victory")]
        [SerializeField, Tooltip("Unlock and show the cursor when the victory panel appears.")]
        private bool unlockCursorOnVictory = true;

        private readonly List<DamageableTarget> registeredTargets = new();
        private int remainingTargets;
        private bool victoryShown;

        private void Reset()
        {
            inputReader = FindAnyObjectByType<VehicleInputReader>();
        }

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = FindAnyObjectByType<VehicleInputReader>();
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartScene);
            }
        }

        private void OnDisable()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartScene);
            }

            for (int i = 0; i < registeredTargets.Count; i++)
            {
                if (registeredTargets[i] != null)
                {
                    registeredTargets[i].Died -= HandleTargetDied;
                }
            }
        }

        private void Start()
        {
            RegisterInitialTargets();
            UpdateCounterText();

            if (remainingTargets <= 0)
            {
                ShowVictory();
            }
        }

        private void Update()
        {
            if (inputReader != null && inputReader.RestartWasPressedThisFrame)
            {
                RestartScene();
            }
        }

        public void RegisterTarget(DamageableTarget target)
        {
            if (target == null || registeredTargets.Contains(target))
            {
                return;
            }

            registeredTargets.Add(target);
            target.Died += HandleTargetDied;

            if (!target.IsDead)
            {
                remainingTargets++;
                UpdateCounterText();
            }
        }

        public void UnregisterTarget(DamageableTarget target)
        {
            if (target == null || !registeredTargets.Remove(target))
            {
                return;
            }

            target.Died -= HandleTargetDied;

            if (!target.IsDead)
            {
                remainingTargets = Mathf.Max(0, remainingTargets - 1);
                UpdateCounterText();
            }
        }

        public void RestartScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }

        private void RegisterInitialTargets()
        {
            registeredTargets.Clear();
            remainingTargets = 0;

            for (int i = 0; i < targets.Count; i++)
            {
                RegisterTarget(targets[i]);
            }

            if (!findTargetsOnStart)
            {
                return;
            }

            DamageableTarget[] foundTargets = FindObjectsByType<DamageableTarget>(FindObjectsInactive.Exclude);

            for (int i = 0; i < foundTargets.Length; i++)
            {
                RegisterTarget(foundTargets[i]);
            }
        }

        private void HandleTargetDied(DamageableTarget target)
        {
            remainingTargets = Mathf.Max(0, remainingTargets - 1);
            UpdateCounterText();

            if (remainingTargets <= 0)
            {
                ShowVictory();
            }
        }

        private void UpdateCounterText()
        {
            if (remainingTargetsText != null)
            {
                remainingTargetsText.text = $"Objetivos restantes: {remainingTargets}";
            }
        }

        private void ShowVictory()
        {
            if (victoryShown)
            {
                return;
            }

            victoryShown = true;

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            if (unlockCursorOnVictory)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
