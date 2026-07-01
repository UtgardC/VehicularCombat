using UnityEngine;

namespace VehicularCombat
{
    public class EnemyTurretAim : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("El cerebro de la IA para obtener el target.")]
        private EnemyVehicleBrain aiBrain;

        [SerializeField] private Transform turretYawPivot;
        [SerializeField] private Transform turretPitchPivot;
        [SerializeField] private Transform ignoredRoot;

        [Header("Aiming Settings")]
        [SerializeField, Min(0f)] private float yawRotationSpeed = 360f;
        [SerializeField, Min(0f)] private float pitchRotationSpeed = 180f;
        [SerializeField] private float minimumPitch = -10f;
        [SerializeField] private float maximumPitch = 35f;

        private void LateUpdate()
        {
            // Si usamos reflection para buscar el target privado del brain:
            Transform target = aiBrain != null ? aiBrain.transform.Find("Target") : null;
            // Como 'target' es privado en el brain, lo ideal es acceder a él. 
            // Nota: En el script anterior no expusimos la propiedad Target. Vamos a rotar hacia donde esté mirando el chasis por defecto si no hay target.

            // Alternativa rápida para acceder al target del EnemyVehicleBrain:
            var brainField = typeof(EnemyVehicleBrain).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Transform currentTarget = brainField?.GetValue(aiBrain) as Transform;

            if (currentTarget == null || turretYawPivot == null) return;

            // Idealmente apuntar a una posición un poco más alta que los pies del jugador (ej. centro de masa)
            Vector3 aimPoint = currentTarget.position + Vector3.up * 1f;

            RotateYaw(aimPoint);
            RotatePitch(aimPoint);
        }

        private void RotateYaw(Vector3 aimPoint)
        {
            Vector3 direction = aimPoint - turretYawPivot.position;
            Vector3 up = ignoredRoot != null ? ignoredRoot.up : Vector3.up;
            Vector3 planarDirection = Vector3.ProjectOnPlane(direction, up);

            if (planarDirection.sqrMagnitude < 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(planarDirection.normalized, up);
            turretYawPivot.rotation = Quaternion.RotateTowards(turretYawPivot.rotation, targetRotation, yawRotationSpeed * Time.deltaTime);
        }

        private void RotatePitch(Vector3 aimPoint)
        {
            if (turretPitchPivot == null) return;

            Vector3 direction = aimPoint - turretPitchPivot.position;
            Vector3 localDirection = turretYawPivot.InverseTransformDirection(direction.normalized);
            float targetPitch = Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg;

            targetPitch = Mathf.Clamp(targetPitch, minimumPitch, maximumPitch);
            Quaternion targetLocalRotation = Quaternion.Euler(-targetPitch, 0f, 0f);

            turretPitchPivot.localRotation = Quaternion.RotateTowards(turretPitchPivot.localRotation, targetLocalRotation, pitchRotationSpeed * Time.deltaTime);
        }
    }
}