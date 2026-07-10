using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VehicularCombat
{
    public sealed class VehicleInputReader : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField, Tooltip("Enable the assigned input actions when this component is enabled. Disable this if another component already enables the same action map.")]
        private bool enableActionsOnEnable = true;

        [SerializeField, Tooltip("Vehicle/Accelerate action. Recommended type: Value, control type: Axis.")]
        private InputActionReference accelerateActionReference;

        [SerializeField, Tooltip("Vehicle/Reverse action. Recommended type: Value, control type: Axis.")]
        private InputActionReference reverseActionReference;

        [SerializeField, Tooltip("Vehicle/Steer action. Recommended type: Value, control type: Axis.")]
        private InputActionReference steerActionReference;

        [SerializeField, Tooltip("Vehicle/Handbrake action. Recommended type: Button.")]
        private InputActionReference handbrakeActionReference;

        // --- NUEVO: Referencia para la acción del Turbo ---
        [SerializeField, Tooltip("Vehicle/Turbo action. Recommended type: Button.")]
        private InputActionReference turboActionReference;

        [SerializeField, Tooltip("Vehicle/Look action. Recommended type: Value, control type: Vector2.")]
        private InputActionReference lookActionReference;

        [SerializeField, Tooltip("Vehicle/Fire action. Recommended type: Button.")]
        private InputActionReference fireActionReference;

        [SerializeField, Tooltip("Vehicle/Restart action. Recommended type: Button.")]
        private InputActionReference restartActionReference;

        private InputAction accelerateAction;
        private InputAction reverseAction;
        private InputAction steerAction;
        private InputAction handbrakeAction;
        private InputAction turboAction; // <-- NUEVO
        private InputAction lookAction;
        private InputAction fireAction;
        private InputAction restartAction;

        private bool lookInputUsesPointerDelta;
        private bool callbacksRegistered;

        public event Action FirePressed;
        public event Action RestartPressed;

        public float Accelerate => ReadClamped01(accelerateAction);
        public float Reverse => ReadClamped01(reverseAction);
        public float Steering => ReadClampedAxis(steerAction);
        public bool HandbrakeHeld => handbrakeAction != null && handbrakeAction.IsPressed();

        // --- NUEVO: Propiedad pública para que PlayerTurbo la lea ---
        public bool TurboHeld => turboAction != null && turboAction.IsPressed();

        public Vector2 LookInput => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        public bool LookInputUsesPointerDelta => lookInputUsesPointerDelta;
        public bool FireHeld => fireAction != null && fireAction.IsPressed();
        public bool FireWasPressedThisFrame => fireAction != null && fireAction.WasPressedThisFrame();
        public bool RestartWasPressedThisFrame => restartAction != null && restartAction.WasPressedThisFrame();
        public float LastFirePressedTime { get; private set; } = float.NegativeInfinity;

        private void OnEnable()
        {
            CacheActions();
            RegisterCallbacks();

            if (enableActionsOnEnable)
            {
                SetActionsEnabled(true);
            }
        }

        private void OnDisable()
        {
            UnregisterCallbacks();

            if (enableActionsOnEnable)
            {
                SetActionsEnabled(false);
            }
        }

        private void CacheActions()
        {
            accelerateAction = ResolveAction(accelerateActionReference, "Accelerate");
            reverseAction = ResolveAction(reverseActionReference, "Reverse");
            steerAction = ResolveAction(steerActionReference, "Steer");
            handbrakeAction = ResolveAction(handbrakeActionReference, "Handbrake");
            turboAction = ResolveAction(turboActionReference, "Turbo"); // <-- NUEVO
            lookAction = ResolveAction(lookActionReference, "Look");
            fireAction = ResolveAction(fireActionReference, "Fire");
            restartAction = ResolveAction(restartActionReference, "Restart");
        }

        private InputAction ResolveAction(InputActionReference actionReference, string label)
        {
            InputAction action = actionReference != null ? actionReference.action : null;
            if (action == null)
            {
                Debug.LogWarning(
                    $"{nameof(VehicleInputReader)} on {name} has no valid {label} Input Action Reference assigned.",
                    this);
            }

            return action;
        }

        private void SetActionsEnabled(bool enabled)
        {
            SetActionEnabled(accelerateAction, enabled);
            SetActionEnabled(reverseAction, enabled);
            SetActionEnabled(steerAction, enabled);
            SetActionEnabled(handbrakeAction, enabled);
            SetActionEnabled(turboAction, enabled); // <-- NUEVO
            SetActionEnabled(lookAction, enabled);
            SetActionEnabled(fireAction, enabled);
            SetActionEnabled(restartAction, enabled);
        }

        private static void SetActionEnabled(InputAction action, bool enabled)
        {
            if (action == null)
            {
                return;
            }

            if (enabled)
            {
                action.Enable();
            }
            else
            {
                action.Disable();
            }
        }

        private void RegisterCallbacks()
        {
            if (callbacksRegistered)
            {
                return;
            }

            if (lookAction != null)
            {
                lookAction.performed += HandleLookPerformed;
            }

            if (fireAction != null)
            {
                fireAction.performed += HandleFirePerformed;
            }

            if (restartAction != null)
            {
                restartAction.performed += HandleRestartPerformed;
            }

            callbacksRegistered = true;
        }

        private void UnregisterCallbacks()
        {
            if (!callbacksRegistered)
            {
                return;
            }

            if (lookAction != null)
            {
                lookAction.performed -= HandleLookPerformed;
            }

            if (fireAction != null)
            {
                fireAction.performed -= HandleFirePerformed;
            }

            if (restartAction != null)
            {
                restartAction.performed -= HandleRestartPerformed;
            }

            callbacksRegistered = false;
        }

        private void HandleLookPerformed(InputAction.CallbackContext context)
        {
            lookInputUsesPointerDelta = context.control != null && context.control.device is Pointer;
        }

        private void HandleFirePerformed(InputAction.CallbackContext context)
        {
            LastFirePressedTime = Time.time;
            FirePressed?.Invoke();
        }

        private void HandleRestartPerformed(InputAction.CallbackContext context)
        {
            RestartPressed?.Invoke();
        }

        private static float ReadClamped01(InputAction action)
        {
            return action != null ? Mathf.Clamp01(action.ReadValue<float>()) : 0f;
        }

        private static float ReadClampedAxis(InputAction action)
        {
            return action != null ? Mathf.Clamp(action.ReadValue<float>(), -1f, 1f) : 0f;
        }
    }
}