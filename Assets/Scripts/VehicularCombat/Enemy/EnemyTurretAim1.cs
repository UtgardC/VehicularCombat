using UnityEngine;

namespace VehicularCombat
{
    public sealed class EnemyTurretAim1 : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("El objetivo al que va a disparar (arrastrá al Jugador acá).")]
        private Transform target;

        [SerializeField, Tooltip("Horizontal turret pivot.")]
        private Transform turretYawPivot;

        [SerializeField, Tooltip("Optional vertical cannon pivot.")]
        private Transform turretPitchPivot;

        [SerializeField, Tooltip("Root transform to ignore its own colliders.")]
        private Transform ignoredRoot;

        [Header("Aiming Settings")]
        [SerializeField, Min(0f), Tooltip("Yaw rotation speed. Slightly slower than player is recommended.")]
        private float yawRotationSpeed = 180f;

        [SerializeField, Min(0f), Tooltip("Pitch rotation speed.")]
        private float pitchRotationSpeed = 90f;

        [SerializeField] private float minimumPitch = -10f;
        [SerializeField] private float maximumPitch = 35f;

        [Header("Predictive Aiming (Leading)")]
        [SerializeField, Tooltip("Activar para disparar a donde el jugador VA a estar.")]
        private bool usePredictiveAiming = true;

        [SerializeField, Tooltip("ATENCIÓN: Debe ser igual a la velocidad del proyectil en VehicleWeapon.")]
        private float projectileSpeed = 35f;

        [Header("Inaccuracy (Not John Wick Mode)")]
        [SerializeField, Tooltip("Radio de error en metros. Más alto = peor puntería.")]
        private float inaccuracyRadius = 4f;

        [SerializeField, Tooltip("Qué tan rápido oscila la mira alrededor del objetivo.")]
        private float inaccuracySpeed = 1.5f;

        private Vector3 currentInaccuracyOffset;
        private float noiseOffset;
        private Rigidbody targetRb;

        private void Start()
        {
            // Iniciamos el ruido en un punto aleatorio para que cada enemigo tenga un patrón de error distinto
            noiseOffset = Random.Range(0f, 1000f);

            if (ignoredRoot == null)
            {
                ignoredRoot = transform.root;
            }
            // Buscamos el Rigidbody del jugador para poder leer su velocidad
            if (target != null)
            {
                targetRb = target.GetComponent<Rigidbody>();
                if (targetRb == null) targetRb = target.GetComponentInParent<Rigidbody>();
            }
        }

        private void LateUpdate()
        {
            if (target == null || turretYawPivot == null) return;

            Vector3 baseAimPoint = target.position + (Vector3.up * 1f);

            if (usePredictiveAiming && targetRb != null)
            {
                // ¿A qué distancia está el jugador y cuánto tarda la bala en llegar?
                float distanceToTarget = Vector3.Distance(turretYawPivot.position, baseAimPoint);
                float timeToTarget = distanceToTarget / projectileSpeed;

                // Movemos el punto de apuntado sumándole la velocidad del jugador multiplicada por el tiempo
                baseAimPoint += targetRb.linearVelocity * timeToTarget;
            }

            // 1. Calcular el margen de error orgánico (Perlin Noise)
            // Mathf.PerlinNoise devuelve valores entre 0 y 1. Multiplicamos * 2 - 1 para que vaya de -1 a 1.
            float noiseX = Mathf.PerlinNoise(Time.time * inaccuracySpeed, noiseOffset) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(noiseOffset, Time.time * inaccuracySpeed) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(Time.time * inaccuracySpeed + 100f, noiseOffset + 100f) * 2f - 1f;

            currentInaccuracyOffset = new Vector3(noiseX, noiseY, noiseZ) * inaccuracyRadius;

            // 4. Posición final
            Vector3 finalAimPoint = baseAimPoint + currentInaccuracyOffset;

            RotateYaw(finalAimPoint);
            RotatePitch(finalAimPoint);
        }

        private void RotateYaw(Vector3 aimPoint)
        {
            Vector3 direction = aimPoint - turretYawPivot.position;
            Vector3 up = ignoredRoot != null ? ignoredRoot.up : Vector3.up;
            Vector3 planarDirection = Vector3.ProjectOnPlane(direction, up);

            if (planarDirection.sqrMagnitude < 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(planarDirection.normalized, up);
            turretYawPivot.rotation = Quaternion.RotateTowards(
                turretYawPivot.rotation,
                targetRotation,
                yawRotationSpeed * Time.deltaTime);
        }

        private void RotatePitch(Vector3 aimPoint)
        {
            if (turretPitchPivot == null) return;

            Vector3 direction = aimPoint - turretPitchPivot.position;
            Vector3 localDirection = turretYawPivot.InverseTransformDirection(direction.normalized);
            float targetPitch = Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg;

            targetPitch = Mathf.Clamp(targetPitch, minimumPitch, maximumPitch);
            Quaternion targetLocalRotation = Quaternion.Euler(-targetPitch, 0f, 0f);

            turretPitchPivot.localRotation = Quaternion.RotateTowards(
                turretPitchPivot.localRotation,
                targetLocalRotation,
                pitchRotationSpeed * Time.deltaTime);
        }
    }
}
