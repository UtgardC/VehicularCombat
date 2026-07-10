using UnityEngine;

namespace VehicularCombat
{
    [DisallowMultipleComponent]
    public sealed class EnemyVehicleBrain : VehicleInputProvider
    {
        [Header("Target")]
        [SerializeField, Tooltip("Player transform pursued by this enemy. Can also be assigned through SetTarget.")]
        private Transform target;

        [SerializeField, Tooltip("Find the active VehicleInputReader once on Start when Target is not assigned.")]
        private bool findPlayerOnStart = true;

        [SerializeField, Tooltip("Optional turret aimer. It receives the same target and is used to decide when to fire.")]
        private EnemyTurretAim1 turretAim;

        [Header("Behavior")]
        [SerializeField, Min(0f), Tooltip("Maximum distance at which the enemy fires while pursuing the target.")]
        private float engageRange = 50f;

        [SerializeField, Min(0f), Tooltip("Distance at which the enemy commits to ramming the target.")]
        private float rammingRange = 15f;

        [SerializeField, Min(0f), Tooltip("Distance treated as a collision so the enemy backs away and disengages.")]
        private float impactDistance = 4f;

        [SerializeField, Min(0f), Tooltip("Seconds spent reversing after reaching Impact Distance.")]
        private float disengageDuration = 1.2f;

        [SerializeField, Range(0f, 180f), Tooltip("Maximum aiming error allowed before firing when no turret aimer is assigned.")]
        private float fireAngleTolerance = 20f;

        [SerializeField, Min(0.01f), Tooltip("Local horizontal distance that produces full steering input.")]
        private float steeringSensitivityDistance = 5f;

        private float currentAccelerate;
        private float currentReverse;
        private float currentSteering;
        private bool currentFireHeld;
        private float disengageTimer;

        public override float Accelerate => currentAccelerate;
        public override float Reverse => currentReverse;
        public override float Steering => currentSteering;
        public override bool HandbrakeHeld => false;
        public override bool FireHeld => currentFireHeld;
        public override bool FireWasPressedThisFrame => currentFireHeld;
        public Transform Target => target;

        private void Reset()
        {
            turretAim = GetComponentInChildren<EnemyTurretAim1>();
        }

        private void Awake()
        {
            if (turretAim == null)
            {
                turretAim = GetComponentInChildren<EnemyTurretAim1>();
            }
        }

        private void Start()
        {
            if (target == null && findPlayerOnStart)
            {
                VehicleInputReader playerInput = FindAnyObjectByType<VehicleInputReader>();
                if (playerInput != null)
                {
                    SetTarget(playerInput.transform);
                }
            }
            else if (target != null)
            {
                SetTarget(target);
            }
        }

        private void Update()
        {
            if (target == null)
            {
                StopVehicle();
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;
            Vector3 localTargetPosition = transform.InverseTransformPoint(target.position);

            currentSteering = Mathf.Clamp(
                localTargetPosition.x / steeringSensitivityDistance,
                -1f,
                1f);

            bool canFire = CanFireAtTarget(toTarget);
            UpdateDriving(distance, localTargetPosition, canFire);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (turretAim != null)
            {
                turretAim.SetTarget(newTarget);
            }
        }

        private bool CanFireAtTarget(Vector3 toTarget)
        {
            if (turretAim != null)
            {
                return turretAim.IsAimedAtTarget(fireAngleTolerance);
            }

            return toTarget.sqrMagnitude > 0.0001f &&
                   Vector3.Angle(transform.forward, toTarget) <= fireAngleTolerance;
        }

        private void UpdateDriving(float distance, Vector3 localTargetPosition, bool canFire)
        {
            if (disengageTimer > 0f)
            {
                disengageTimer -= Time.deltaTime;
                currentAccelerate = 0f;
                currentReverse = 1f;
                currentSteering = -currentSteering;
                currentFireHeld = distance <= engageRange && canFire;
                return;
            }

            if (distance <= impactDistance && localTargetPosition.z > -1f)
            {
                disengageTimer = disengageDuration;
                currentAccelerate = 0f;
                currentReverse = 1f;
                currentFireHeld = false;
                return;
            }

            currentAccelerate = 1f;
            currentReverse = 0f;
            currentFireHeld = distance <= engageRange && canFire;

            if (localTargetPosition.z < 0f && distance < rammingRange)
            {
                currentAccelerate = 0f;
                currentReverse = 1f;
            }
        }

        private void StopVehicle()
        {
            currentAccelerate = 0f;
            currentReverse = 0f;
            currentSteering = 0f;
            currentFireHeld = false;
        }

        private void OnValidate()
        {
            engageRange = Mathf.Max(0f, engageRange);
            rammingRange = Mathf.Clamp(rammingRange, 0f, engageRange);
            impactDistance = Mathf.Clamp(impactDistance, 0f, rammingRange);
            steeringSensitivityDistance = Mathf.Max(0.01f, steeringSensitivityDistance);
        }
    }
}
