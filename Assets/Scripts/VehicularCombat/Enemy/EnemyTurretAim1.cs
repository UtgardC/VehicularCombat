using UnityEngine;

namespace VehicularCombat
{
    [DisallowMultipleComponent]
    public sealed class EnemyTurretAim1 : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Target followed by the turret. Can also be assigned through SetTarget.")]
        private Transform target;

        [SerializeField, Tooltip("Horizontal turret pivot.")]
        private Transform turretYawPivot;

        [SerializeField, Tooltip("Optional vertical cannon pivot.")]
        private Transform turretPitchPivot;

        [SerializeField, Tooltip("Vehicle root whose up direction defines the horizontal aiming plane.")]
        private Transform ignoredRoot;

        [Header("Aiming")]
        [SerializeField, Min(0f), Tooltip("Maximum horizontal rotation speed in degrees per second.")]
        private float yawRotationSpeed = 180f;

        [SerializeField, Min(0f), Tooltip("Maximum vertical rotation speed in degrees per second.")]
        private float pitchRotationSpeed = 90f;

        [SerializeField] private float minimumPitch = -10f;
        [SerializeField] private float maximumPitch = 35f;

        [SerializeField, Tooltip("World-space offset from the target transform used as the base aim point.")]
        private Vector3 aimOffset = Vector3.zero;

        [Header("Prediction")]
        [SerializeField, Tooltip("Lead the target using its current Rigidbody velocity.")]
        private bool usePredictiveAiming = true;

        [SerializeField, Min(0.01f), Tooltip("Projectile speed used to estimate travel time. Match VehicleWeapon Projectile Speed.")]
        private float projectileSpeed = 35f;

        [Header("Inaccuracy")]
        [SerializeField, Min(0f), Tooltip("Maximum world-space aiming offset in meters.")]
        private float inaccuracyRadius = 4f;

        [SerializeField, Min(0f), Tooltip("Speed of the procedural aiming drift.")]
        private float inaccuracySpeed = 1.5f;

        private float noiseOffset;
        private Rigidbody targetRigidbody;
        private Vector3 finalAimPoint;

        public Transform Target => target;
        public Vector3 CurrentAimPoint => finalAimPoint;

        private void Reset()
        {
            ignoredRoot = transform.root;
        }

        private void Awake()
        {
            noiseOffset = Random.Range(0f, 1000f);
            if (ignoredRoot == null)
            {
                ignoredRoot = transform.root;
            }

            CacheTargetRigidbody();
        }

        private void Start()
        {
            // Si nazco sin objetivo, busco al objeto que tenga la etiqueta "Player"
            if (target == null)
            {
                target = GameObject.FindGameObjectWithTag("Player")?.transform;
            }

        }

        private void LateUpdate()
        {
            if (target == null || turretYawPivot == null)
            {
                return;
            }

            finalAimPoint = CalculateAimPoint();
            RotateYaw(finalAimPoint);
            RotatePitch(finalAimPoint);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            finalAimPoint = target != null ? target.position + aimOffset : Vector3.zero;
            CacheTargetRigidbody();
        }

        public bool IsAimedAtTarget(float angleTolerance)
        {
            if (target == null || turretYawPivot == null)
            {
                return false;
            }

            Vector3 direction = finalAimPoint - turretYawPivot.position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            return Vector3.Angle(turretYawPivot.forward, direction) <= angleTolerance;
        }

        private Vector3 CalculateAimPoint()
        {
            Vector3 aimPoint = target.position + aimOffset;

            if (usePredictiveAiming && targetRigidbody != null)
            {
                float travelTime = Vector3.Distance(turretYawPivot.position, aimPoint) / projectileSpeed;
                aimPoint += targetRigidbody.linearVelocity * travelTime;
            }

            float time = Time.time * inaccuracySpeed;
            float noiseX = Mathf.PerlinNoise(time, noiseOffset) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(noiseOffset, time) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(time + 100f, noiseOffset + 100f) * 2f - 1f;

            return aimPoint + new Vector3(noiseX, noiseY * 0.1f, noiseZ) * inaccuracyRadius;
        }

        private void RotateYaw(Vector3 aimPoint)
        {
            Vector3 direction = aimPoint - turretYawPivot.position;
            Vector3 up = ignoredRoot != null ? ignoredRoot.up : Vector3.up;
            Vector3 planarDirection = Vector3.ProjectOnPlane(direction, up);

            if (planarDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(planarDirection.normalized, up);
            turretYawPivot.rotation = Quaternion.RotateTowards(
                turretYawPivot.rotation,
                targetRotation,
                yawRotationSpeed * Time.deltaTime);
        }

        private void RotatePitch(Vector3 aimPoint)
        {
            if (turretPitchPivot == null)
            {
                return;
            }

            Vector3 direction = aimPoint - turretPitchPivot.position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 localDirection = turretYawPivot.InverseTransformDirection(direction.normalized);
            float pitch = Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);

            Quaternion targetLocalRotation = Quaternion.Euler(-pitch, 0f, 0f);
            turretPitchPivot.localRotation = Quaternion.RotateTowards(
                turretPitchPivot.localRotation,
                targetLocalRotation,
                pitchRotationSpeed * Time.deltaTime);
        }

        private void CacheTargetRigidbody()
        {
            targetRigidbody = target != null ? target.GetComponentInParent<Rigidbody>() : null;
        }

        private void OnValidate()
        {
            if (maximumPitch < minimumPitch)
            {
                maximumPitch = minimumPitch;
            }

            projectileSpeed = Mathf.Max(0.01f, projectileSpeed);
        }
    }
}
