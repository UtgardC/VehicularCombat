using UnityEngine;

namespace VehicularCombat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeVehicleController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Central input reader for the Vehicle action map.")]
        private VehicleInputReader inputReader;

        [SerializeField, Tooltip("Rigidbody moved by this controller. Defaults to the Rigidbody on this GameObject.")]
        private Rigidbody vehicleRigidbody;

        [Header("Motor")]
        [SerializeField, Min(0f), Tooltip("Forward motor acceleration in meters per second squared.")]
        private float acceleration = 35f;

        [SerializeField, Min(0f), Tooltip("Reverse motor acceleration in meters per second squared.")]
        private float reverseAcceleration = 25f;

        [SerializeField, Min(0f), Tooltip("Motor stops adding forward force above this forward speed. It does not clamp Rigidbody velocity.")]
        private float maximumMotorForwardSpeed = 18f;

        [SerializeField, Min(0f), Tooltip("Motor stops adding reverse force below this reverse speed. It does not clamp Rigidbody velocity.")]
        private float maximumMotorReverseSpeed = 8f;

        [Header("Steering")]
        [SerializeField, Min(0f), Tooltip("Yaw rotation speed in degrees per second at full steering.")]
        private float turnSpeed = 110f;

        [SerializeField, Min(0f), Tooltip("Forward speed below which steering is ignored.")]
        private float minimumSteeringSpeed = 0.5f;

        [SerializeField, Min(0.01f), Tooltip("Forward speed where steering reaches full low-speed authority.")]
        private float speedForFullSteering = 6f;

        [SerializeField, Range(0.1f, 1f), Tooltip("Steering multiplier when close to maximum motor forward speed.")]
        private float highSpeedSteeringMultiplier = 0.55f;

        [Header("Grip And Handbrake")]
        [SerializeField, Min(0f), Tooltip("Artificial lateral grip used during normal driving.")]
        private float normalLateralGrip = 8f;

        [SerializeField, Min(0f), Tooltip("Artificial lateral grip used while handbrake is held. Lower values allow more drift.")]
        private float handbrakeLateralGrip = 1.5f;

        [SerializeField, Min(0f), Tooltip("Longitudinal deceleration applied in local forward/back direction while handbrake is held.")]
        private float handbrakeForce = 28f;

        [Header("Rigidbody Safety")]
        [SerializeField, Tooltip("Freeze Rigidbody rotation X and Z on Awake so the MVP vehicle does not roll over.")]
        private bool enforceUprightRotationConstraints = true;

        private bool warnedMissingInput;

        // --- NUEVO: Referencia al script del Turbo ---
        private PlayerTurbo playerTurbo;

        public Rigidbody VehicleRigidbody => vehicleRigidbody;
        public float ForwardSpeed => vehicleRigidbody != null ? Vector3.Dot(vehicleRigidbody.linearVelocity, transform.forward) : 0f;

        private void Reset()
        {
            vehicleRigidbody = GetComponent<Rigidbody>();
            inputReader = GetComponent<VehicleInputReader>();
            ApplyRecommendedRigidbodySettings();
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

            // Buscamos el componente del turbo en la nave
            playerTurbo = GetComponent<PlayerTurbo>();

            if (enforceUprightRotationConstraints && vehicleRigidbody != null)
            {
                vehicleRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }

        private void FixedUpdate()
        {
            if (vehicleRigidbody == null)
            {
                return;
            }

            if (inputReader == null)
            {
                WarnMissingInputOnce();
                ApplyArtificialGrip(false);
                return;
            }

            float accelerateInput = inputReader.Accelerate;
            float reverseInput = inputReader.Reverse;
            float steeringInput = inputReader.Steering;
            bool handbrakeHeld = inputReader.HandbrakeHeld;

            float driveInput = accelerateInput - reverseInput;
            float forwardSpeed = Vector3.Dot(vehicleRigidbody.linearVelocity, transform.forward);

            // --- NUEVO: Calculamos la velocidad y aceleración efectivas con el Turbo ---
            float effectiveMaxSpeed = maximumMotorForwardSpeed;
            float effectiveAcceleration = acceleration;

            if (playerTurbo != null && playerTurbo.IsBoosting)
            {
                effectiveMaxSpeed += playerTurbo.TurboSpeedBonus;
                // Le sumamos el bonus a la aceleración para que tenga un "empuje" real al activarse
                effectiveAcceleration += playerTurbo.TurboSpeedBonus;
            }

            // Pasamos los valores efectivos a las funciones
            ApplyMotorForce(driveInput, forwardSpeed, effectiveMaxSpeed, effectiveAcceleration);
            ApplySteering(steeringInput, forwardSpeed, effectiveMaxSpeed);
            ApplyArtificialGrip(handbrakeHeld);
        }

        // --- MODIFICADO: Ahora recibe la velocidad máxima y aceleración actualizadas ---
        private void ApplyMotorForce(float driveInput, float forwardSpeed, float currentMaxSpeed, float currentAcceleration)
        {
            if (driveInput > 0f && forwardSpeed < currentMaxSpeed)
            {
                vehicleRigidbody.AddForce(transform.forward * (driveInput * currentAcceleration), ForceMode.Acceleration);
            }
            else if (driveInput < 0f && forwardSpeed > -maximumMotorReverseSpeed)
            {
                vehicleRigidbody.AddForce(transform.forward * (driveInput * reverseAcceleration), ForceMode.Acceleration);
            }
        }

        // --- MODIFICADO: Ahora recibe la velocidad máxima actualizada para no endurecer de más la dirección ---
        private void ApplySteering(float steeringInput, float forwardSpeed, float currentMaxSpeed)
        {
            float absoluteForwardSpeed = Mathf.Abs(forwardSpeed);
            if (absoluteForwardSpeed <= minimumSteeringSpeed)
            {
                return;
            }

            float movementFactor = Mathf.Clamp01(absoluteForwardSpeed / speedForFullSteering);

            // Usamos currentMaxSpeed para calcular el highSpeedFactor
            float highSpeedFactor = currentMaxSpeed > 0f
                ? Mathf.Clamp01(absoluteForwardSpeed / currentMaxSpeed)
                : 0f;

            float speedSteeringMultiplier = Mathf.Lerp(1f, highSpeedSteeringMultiplier, highSpeedFactor);
            float directionSign = Mathf.Sign(forwardSpeed);

            float rotationAmount =
                steeringInput *
                turnSpeed *
                movementFactor *
                speedSteeringMultiplier *
                directionSign *
                Time.fixedDeltaTime;

            if (Mathf.Approximately(rotationAmount, 0f))
            {
                return;
            }

            Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
            vehicleRigidbody.MoveRotation(vehicleRigidbody.rotation * deltaRotation);
        }

        private void ApplyArtificialGrip(bool handbrakeHeld)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(vehicleRigidbody.linearVelocity);

            float lateralGrip = handbrakeHeld ? handbrakeLateralGrip : normalLateralGrip;
            float lateralBlend = 1f - Mathf.Exp(-lateralGrip * Time.fixedDeltaTime);
            localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, lateralBlend);

            if (handbrakeHeld)
            {
                localVelocity.z = Mathf.MoveTowards(localVelocity.z, 0f, handbrakeForce * Time.fixedDeltaTime);
            }

            vehicleRigidbody.linearVelocity = transform.TransformDirection(localVelocity);
        }

        private void ApplyRecommendedRigidbodySettings()
        {
            if (vehicleRigidbody == null)
            {
                return;
            }

            vehicleRigidbody.mass = 1000f;
            vehicleRigidbody.linearDamping = 0.5f;
            vehicleRigidbody.angularDamping = 2f;
            vehicleRigidbody.useGravity = true;
            vehicleRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            vehicleRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            vehicleRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void WarnMissingInputOnce()
        {
            if (warnedMissingInput)
            {
                return;
            }

            Debug.LogWarning($"{nameof(ArcadeVehicleController)} on {name} has no {nameof(VehicleInputReader)} assigned.", this);
            warnedMissingInput = true;
        }

        private void OnValidate()
        {
            maximumMotorForwardSpeed = Mathf.Max(0f, maximumMotorForwardSpeed);
            maximumMotorReverseSpeed = Mathf.Max(0f, maximumMotorReverseSpeed);
            speedForFullSteering = Mathf.Max(0.01f, speedForFullSteering);
        }
    }
}