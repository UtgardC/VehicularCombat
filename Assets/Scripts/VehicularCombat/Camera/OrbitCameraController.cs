using Unity.Cinemachine;
using UnityEngine;

namespace VehicularCombat
{
    public sealed class OrbitCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Central input reader for the Vehicle action map.")]
        private VehicleInputReader inputReader;

        [SerializeField, Tooltip("Transform followed/looked at by the Cinemachine cameras. Usually PlayerVehicle/CameraTarget.")]
        private Transform cameraTarget;

        [SerializeField, Tooltip("Optional weapon reference used to switch to the aim camera after real shots.")]
        private VehicleWeapon vehicleWeapon;

        [Header("Orbit")]
        [SerializeField, Tooltip("Mouse delta sensitivity in degrees per input unit.")]
        private float mouseSensitivity = 0.12f;

        [SerializeField, Tooltip("Gamepad look speed in degrees per second.")]
        private float gamepadLookSpeed = 150f;

        [SerializeField, Tooltip("Minimum vertical orbit angle in degrees.")]
        private float minimumPitch = -20f;

        [SerializeField, Tooltip("Maximum vertical orbit angle in degrees.")]
        private float maximumPitch = 65f;

        [SerializeField, Tooltip("Ignore Look input while the cursor is unlocked.")]
        private bool requireLockedCursorForLook = true;

        [Header("Cinemachine Aim Blend")]
        [SerializeField, Tooltip("General orbital Cinemachine camera.")]
        private CinemachineVirtualCameraBase generalCamera;

        [SerializeField, Tooltip("Aiming Cinemachine camera used for a tighter combat view.")]
        private CinemachineVirtualCameraBase aimCamera;

        [SerializeField, Tooltip("Priority assigned to the general camera.")]
        private int generalCameraPriority = 10;

        [SerializeField, Tooltip("Priority assigned to the aim camera while aiming.")]
        private int activeAimCameraPriority = 20;

        [SerializeField, Tooltip("Priority assigned to the aim camera while inactive.")]
        private int inactiveAimCameraPriority = 0;

        [SerializeField, Min(0f), Tooltip("Seconds to keep the aim camera active after firing.")]
        private float aimCameraHoldTime = 3f;

        private float yaw;
        private float pitch;
        private float lastConfirmedShotTime = float.NegativeInfinity;
        private bool warnedMissingInput;
        private bool warnedMissingTarget;

        private void Reset()
        {
            inputReader = GetComponentInParent<VehicleInputReader>();
            vehicleWeapon = GetComponentInParent<VehicleWeapon>();
            cameraTarget = transform;
        }

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponentInParent<VehicleInputReader>();
            }

            if (vehicleWeapon == null)
            {
                vehicleWeapon = GetComponentInParent<VehicleWeapon>();
            }

            if (cameraTarget == null)
            {
                cameraTarget = transform;
            }

            Vector3 initialEuler = cameraTarget.rotation.eulerAngles;
            yaw = initialEuler.y;
            pitch = NormalizePitch(initialEuler.x);
        }

        private void OnEnable()
        {
            if (vehicleWeapon != null)
            {
                vehicleWeapon.Fired += HandleWeaponFired;
            }
        }

        private void OnDisable()
        {
            if (vehicleWeapon != null)
            {
                vehicleWeapon.Fired -= HandleWeaponFired;
            }
        }

        private void Update()
        {
            UpdateOrbit();
            UpdateCinemachinePriorities();
        }

        private void UpdateOrbit()
        {
            if (cameraTarget == null)
            {
                WarnMissingTargetOnce();
                return;
            }

            if (inputReader == null)
            {
                WarnMissingInputOnce();
                return;
            }

            if (requireLockedCursorForLook && Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 lookInput = inputReader.LookInput;
            if (lookInput.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float yawDelta;
            float pitchDelta;

            if (inputReader.LookInputUsesPointerDelta)
            {
                yawDelta = lookInput.x * mouseSensitivity;
                pitchDelta = lookInput.y * mouseSensitivity;
            }
            else
            {
                yawDelta = lookInput.x * gamepadLookSpeed * Time.unscaledDeltaTime;
                pitchDelta = lookInput.y * gamepadLookSpeed * Time.unscaledDeltaTime;
            }

            yaw += yawDelta;
            pitch = Mathf.Clamp(pitch - pitchDelta, minimumPitch, maximumPitch);
            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void UpdateCinemachinePriorities()
        {
            if (generalCamera != null)
            {
                generalCamera.Priority.Value = generalCameraPriority;
            }

            if (aimCamera == null)
            {
                return;
            }

            bool firedRecentlyFromInput =
                inputReader != null &&
                Time.time - inputReader.LastFirePressedTime <= aimCameraHoldTime;

            bool firedRecentlyFromWeapon =
                Time.time - lastConfirmedShotTime <= aimCameraHoldTime;

            bool aiming = firedRecentlyFromInput || firedRecentlyFromWeapon || (inputReader != null && inputReader.FireHeld);
            aimCamera.Priority.Value = aiming ? activeAimCameraPriority : inactiveAimCameraPriority;
        }

        private void HandleWeaponFired()
        {
            lastConfirmedShotTime = Time.time;
        }

        private static float NormalizePitch(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private void WarnMissingInputOnce()
        {
            if (warnedMissingInput)
            {
                return;
            }

            Debug.LogWarning($"{nameof(OrbitCameraController)} on {name} has no {nameof(VehicleInputReader)} assigned.", this);
            warnedMissingInput = true;
        }

        private void WarnMissingTargetOnce()
        {
            if (warnedMissingTarget)
            {
                return;
            }

            Debug.LogWarning($"{nameof(OrbitCameraController)} on {name} has no camera target assigned.", this);
            warnedMissingTarget = true;
        }

        private void OnValidate()
        {
            if (maximumPitch < minimumPitch)
            {
                maximumPitch = minimumPitch;
            }
        }
    }
}
