using UnityEngine;

namespace VehicularCombat
{
    public sealed class PlayerTurbo : MonoBehaviour
    {
        public static PlayerTurbo Instance { get; private set; }

        [Header("Turbo")]
        [SerializeField] private float maxTurbo = 100f;
        [SerializeField] private float consumeRate = 30f;      // unidades por segundo al usar
        [SerializeField] private float regenRate = 25f;        // unidades por segundo al regenerar
        [SerializeField] private float regenDelay = 3f;        // segundos sin usar para arrancar regen

        [Header("Velocidad")]
        [SerializeField] private float turboSpeedBonus = 20f;  // bonus sumado a la velocidad base
        [SerializeField] private VehicleInputReader inputReader;

        private float currentTurbo;
        private float timeSinceLastUse;
        private bool isBoosting;

        public bool IsBoosting => isBoosting;
        public float CurrentTurbo => currentTurbo;
        public float MaxTurbo => maxTurbo;
        public float TurboSpeedBonus => turboSpeedBonus;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            currentTurbo = maxTurbo;
            UpdateHUD();
        }

        void Update()
        {
            // Puedes descomentar la línea del GameManager si ya lo arreglaste en tu escena
            if (global::GameManager.Instance?.State != global::GameManager.GameState.Playing) return;

            // --- NUEVO: Leemos el turbo exclusivamente desde el nuevo Input System ---
            bool wantsBoost = false;
            if (inputReader != null)
            {
                wantsBoost = inputReader.TurboHeld;
            }

            // Intentar usar turbo
            if (wantsBoost && currentTurbo > 0f)
            {
                isBoosting = true;
                timeSinceLastUse = 0f;
                currentTurbo = Mathf.Max(0f, currentTurbo - consumeRate * Time.deltaTime);
            }
            else
            {
                isBoosting = false;
                timeSinceLastUse += Time.deltaTime;

                // Regenerar luego del delay
                if (timeSinceLastUse >= regenDelay && currentTurbo < maxTurbo)
                    currentTurbo = Mathf.Min(maxTurbo, currentTurbo + regenRate * Time.deltaTime);
            }

            UpdateHUD();
        }

        void UpdateHUD()
        {
            global::HUDManager.Instance?.UpdateTurbo(currentTurbo, maxTurbo);
        }
    }
}