using UnityEngine;

namespace VehicularCombat
{
    public sealed class HoverVehicleVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Rigidbody used as the source of velocity and acceleration.")]
        private Rigidbody vehicleRigidbody;

        [SerializeField, Tooltip("Central input reader. Used only for responsive visual banking while steering.")]
        private VehicleInputReader inputReader;

        [SerializeField, Tooltip("Child transform that contains the ship model. Do not assign the physics root.")]
        private Transform visualRoot;

        [Header("Pitch From Acceleration")]
        [SerializeField, Tooltip("Degrees of visual pitch per local longitudinal acceleration unit.")]
        private float pitchFromLongitudinalAcceleration = 0.35f;

        [SerializeField, Min(0f), Tooltip("Maximum nose-down pitch in degrees.")]
        private float maximumNoseDownPitch = 10f;

        [SerializeField, Min(0f), Tooltip("Maximum nose-up pitch in degrees.")]
        private float maximumNoseUpPitch = 16f;

        [Header("Handbrake Visual Boost")]
        [SerializeField, Min(1f), Tooltip("Multiplier applied to pitch caused by acceleration while handbrake is held.")]
        private float handbrakePitchMultiplier = 1.5f;

        [SerializeField, Min(0f), Tooltip("Extra pitch added while handbraking, scaled by local forward/back speed.")]
        private float handbrakeExtraPitch = 7f;

        [SerializeField, Min(0.01f), Tooltip("Local speed at which the handbrake extra pitch reaches full strength.")]
        private float handbrakeSpeedForFullPitch = 12f;

        [SerializeField, Min(0f), Tooltip("Extra pitch limit added while the handbrake is held.")]
        private float handbrakeAdditionalPitchLimit = 8f;

        [Header("Roll From Steering")]
        [SerializeField, Tooltip("Maximum roll in degrees produced directly by steering input.")]
        private float rollFromSteering = 14f;

        [SerializeField, Tooltip("Additional roll in degrees per local yaw angular velocity in radians per second.")]
        private float rollFromYawAngularVelocity = 4f;

        [SerializeField, Tooltip("Degrees of visual roll per local lateral acceleration unit.")]
        private float rollFromLateralAcceleration = 0.15f;

        [SerializeField, Min(0f), Tooltip("Maximum roll in degrees.")]
        private float maximumRoll = 22f;

        [Header("Response")]
        [SerializeField, Min(0.001f), Tooltip("Time used to smooth the visual pitch response.")]
        private float pitchSmoothTime = 0.18f;

        [SerializeField, Min(0.001f), Tooltip("Time used to smooth the visual roll response.")]
        private float rollSmoothTime = 0.14f;

        [SerializeField, Min(0f), Tooltip("Low-pass smoothing applied to measured acceleration.")]
        private float accelerationSmoothing = 12f;

        [SerializeField, Min(1f), Tooltip("Acceleration values are clamped to this magnitude before calculating tilt.")]
        private float maximumAccelerationForTilt = 45f;

        [Header("Hover Idle")]
        [SerializeField, Min(0f), Tooltip("Vertical idle hover amplitude in local units.")]
        private float hoverAmplitude = 0.08f;

        [SerializeField, Min(0f), Tooltip("Vertical idle hover frequency in cycles per second.")]
        private float hoverFrequency = 1.25f;

        [SerializeField, Min(0f), Tooltip("Small idle pitch amplitude in degrees.")]
        private float idlePitchAmplitude = 0.8f;

        [SerializeField, Min(0f), Tooltip("Small idle roll amplitude in degrees.")]
        private float idleRollAmplitude = 0.5f;

        [SerializeField, Min(0f), Tooltip("Idle rotation frequency in cycles per second.")]
        private float idleRotationFrequency = 0.7f;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 previousLocalVelocity;
        private Vector3 smoothedLocalAcceleration;
        private float currentPitch;
        private float currentRoll;
        private float pitchVelocity;
        private float rollVelocity;
        private bool initialized;
        private bool warnedMissingReferences;
        private bool warnedRootAssigned;

        public float CurrentPitch => currentPitch;
        public float CurrentRoll => currentRoll;

        private void Reset()
        {
            vehicleRigidbody = GetComponent<Rigidbody>();
            inputReader = GetComponent<VehicleInputReader>();
        }

        private void Awake()
        {
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponent<Rigidbody>();
            }

            if (inputReader == null)
            {
                inputReader = GetComponent<VehicleInputReader>();
            }
        }

        private void OnEnable()
        {
            InitializeState();
        }

        private void LateUpdate()
        {
            if (!HasValidReferences())
            {
                return;
            }

            if (!initialized)
            {
                InitializeState();
            }

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 localVelocity = transform.InverseTransformDirection(vehicleRigidbody.linearVelocity);
            Vector3 localAcceleration = (localVelocity - previousLocalVelocity) / deltaTime;
            previousLocalVelocity = localVelocity;

            localAcceleration = Vector3.ClampMagnitude(localAcceleration, maximumAccelerationForTilt);
            float accelerationBlend = 1f - Mathf.Exp(-accelerationSmoothing * deltaTime);
            smoothedLocalAcceleration = Vector3.Lerp(smoothedLocalAcceleration, localAcceleration, accelerationBlend);

            float targetPitch = smoothedLocalAcceleration.z * pitchFromLongitudinalAcceleration;
            bool handbrakeHeld = inputReader != null && inputReader.HandbrakeHeld;

            if (handbrakeHeld)
            {
                targetPitch *= handbrakePitchMultiplier;

                float signedForwardSpeed = localVelocity.z;
                float handbrakeSpeedFactor = Mathf.Clamp01(Mathf.Abs(signedForwardSpeed) / handbrakeSpeedForFullPitch);
                if (handbrakeSpeedFactor > 0f)
                {
                    targetPitch += -Mathf.Sign(signedForwardSpeed) * handbrakeExtraPitch * handbrakeSpeedFactor;
                }
            }

            float noseUpLimit = maximumNoseUpPitch + (handbrakeHeld ? handbrakeAdditionalPitchLimit : 0f);
            float noseDownLimit = maximumNoseDownPitch + (handbrakeHeld ? handbrakeAdditionalPitchLimit : 0f);
            targetPitch = Mathf.Clamp(targetPitch, -noseUpLimit, noseDownLimit);

            float steeringInput = inputReader != null ? inputReader.Steering : 0f;
            float localYawAngularVelocity = transform.InverseTransformDirection(vehicleRigidbody.angularVelocity).y;
            float targetRoll =
                -steeringInput * rollFromSteering -
                localYawAngularVelocity * rollFromYawAngularVelocity -
                smoothedLocalAcceleration.x * rollFromLateralAcceleration;
            targetRoll = Mathf.Clamp(targetRoll, -maximumRoll, maximumRoll);

            currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, pitchSmoothTime, Mathf.Infinity, deltaTime);
            currentRoll = Mathf.SmoothDampAngle(currentRoll, targetRoll, ref rollVelocity, rollSmoothTime, Mathf.Infinity, deltaTime);

            float hoverTime = Time.time * Mathf.PI * 2f;
            float hoverOffset = Mathf.Sin(hoverTime * hoverFrequency) * hoverAmplitude;
            float idlePitch = Mathf.Sin(hoverTime * idleRotationFrequency + 1.37f) * idlePitchAmplitude;
            float idleRoll = Mathf.Sin(hoverTime * idleRotationFrequency + 2.91f) * idleRollAmplitude;

            visualRoot.localPosition = baseLocalPosition + Vector3.up * hoverOffset;
            visualRoot.localRotation =
                baseLocalRotation *
                Quaternion.Euler(currentPitch + idlePitch, 0f, currentRoll + idleRoll);
        }

        private void InitializeState()
        {
            if (visualRoot != null)
            {
                baseLocalPosition = visualRoot.localPosition;
                baseLocalRotation = visualRoot.localRotation;
            }

            previousLocalVelocity = vehicleRigidbody != null
                ? transform.InverseTransformDirection(vehicleRigidbody.linearVelocity)
                : Vector3.zero;

            smoothedLocalAcceleration = Vector3.zero;
            currentPitch = 0f;
            currentRoll = 0f;
            pitchVelocity = 0f;
            rollVelocity = 0f;
            initialized = visualRoot != null && vehicleRigidbody != null;
        }

        private bool HasValidReferences()
        {
            if (vehicleRigidbody == null || visualRoot == null)
            {
                WarnMissingReferencesOnce();
                return false;
            }

            if (visualRoot == transform)
            {
                WarnRootAssignedOnce();
                return false;
            }

            return true;
        }

        private void WarnMissingReferencesOnce()
        {
            if (warnedMissingReferences)
            {
                return;
            }

            Debug.LogWarning($"{nameof(HoverVehicleVisuals)} on {name} needs a Rigidbody and a child Visual Root assigned.", this);
            warnedMissingReferences = true;
        }

        private void WarnRootAssignedOnce()
        {
            if (warnedRootAssigned)
            {
                return;
            }

            Debug.LogWarning($"{nameof(HoverVehicleVisuals)} on {name} will not modify the physics root. Assign a child transform as Visual Root.", this);
            warnedRootAssigned = true;
        }

        private void OnValidate()
        {
            pitchSmoothTime = Mathf.Max(0.001f, pitchSmoothTime);
            rollSmoothTime = Mathf.Max(0.001f, rollSmoothTime);
            maximumAccelerationForTilt = Mathf.Max(1f, maximumAccelerationForTilt);
            handbrakePitchMultiplier = Mathf.Max(1f, handbrakePitchMultiplier);
            handbrakeSpeedForFullPitch = Mathf.Max(0.01f, handbrakeSpeedForFullPitch);
        }
    }
}
