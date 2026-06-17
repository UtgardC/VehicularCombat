using UnityEngine;

namespace VehicularCombat
{
    public sealed class TurretAimController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Camera used to raycast from the center of the screen. Defaults to Camera.main if empty.")]
        private Camera playerCamera;

        [SerializeField, Tooltip("Horizontal turret pivot. This transform rotates in world space toward the aim point.")]
        private Transform turretYawPivot;

        [SerializeField, Tooltip("Optional vertical cannon pivot. Leave empty for yaw-only turrets.")]
        private Transform turretPitchPivot;

        [SerializeField, Tooltip("Root transform whose colliders should be ignored by aiming raycasts.")]
        private Transform ignoredRoot;

        [SerializeField, Tooltip("Additional colliders ignored by the aiming raycast.")]
        private Collider[] ignoredColliders;

        [Header("Aiming")]
        [SerializeField, Tooltip("Layers considered aimable by the center-screen raycast.")]
        private LayerMask aimMask = ~0;

        [SerializeField, Min(1f), Tooltip("Fallback distance used when the center-screen raycast hits nothing.")]
        private float maximumAimDistance = 500f;

        [SerializeField, Min(0f), Tooltip("Yaw rotation speed in degrees per second.")]
        private float yawRotationSpeed = 360f;

        [SerializeField, Min(0f), Tooltip("Pitch rotation speed in degrees per second.")]
        private float pitchRotationSpeed = 180f;

        [SerializeField, Tooltip("Lowest pitch angle in degrees. Positive pitch aims upward.")]
        private float minimumPitch = -10f;

        [SerializeField, Tooltip("Highest pitch angle in degrees. Positive pitch aims upward.")]
        private float maximumPitch = 35f;

        [SerializeField, Min(0.01f), Tooltip("Aim points closer than this are ignored to avoid unstable rotations.")]
        private float minimumAimDistance = 0.5f;

        private readonly RaycastHit[] aimHits = new RaycastHit[32];
        private bool warnedMissingReferences;

        public Vector3 CurrentAimPoint { get; private set; }
        public bool HasAimPoint { get; private set; }

        private void Reset()
        {
            playerCamera = Camera.main;
            ignoredRoot = transform.root;
        }

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (ignoredRoot == null)
            {
                ignoredRoot = transform.root;
            }
        }

        private void LateUpdate()
        {
            if (!HasRequiredReferences())
            {
                WarnMissingReferencesOnce();
                return;
            }

            CurrentAimPoint = ResolveAimPoint();
            HasAimPoint = true;

            RotateYaw(CurrentAimPoint);
            RotatePitch(CurrentAimPoint);
        }

        private Vector3 ResolveAimPoint()
        {
            Ray aimRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int hitCount = Physics.RaycastNonAlloc(
                aimRay,
                aimHits,
                maximumAimDistance,
                aimMask,
                QueryTriggerInteraction.Ignore);

            float nearestDistance = float.PositiveInfinity;
            Vector3 nearestPoint = aimRay.GetPoint(maximumAimDistance);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = aimHits[i].collider;
                if (hitCollider == null || ShouldIgnoreCollider(hitCollider))
                {
                    continue;
                }

                if (aimHits[i].distance < nearestDistance)
                {
                    nearestDistance = aimHits[i].distance;
                    nearestPoint = aimHits[i].point;
                }
            }

            return nearestPoint;
        }

        private void RotateYaw(Vector3 aimPoint)
        {
            Vector3 direction = aimPoint - turretYawPivot.position;
            if (direction.sqrMagnitude < minimumAimDistance * minimumAimDistance)
            {
                return;
            }

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
            if (direction.sqrMagnitude < minimumAimDistance * minimumAimDistance)
            {
                return;
            }

            Vector3 localDirection = turretYawPivot.InverseTransformDirection(direction.normalized);
            float targetPitch = Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg;
            targetPitch = Mathf.Clamp(targetPitch, minimumPitch, maximumPitch);

            Quaternion targetLocalRotation = Quaternion.Euler(-targetPitch, 0f, 0f);
            turretPitchPivot.localRotation = Quaternion.RotateTowards(
                turretPitchPivot.localRotation,
                targetLocalRotation,
                pitchRotationSpeed * Time.deltaTime);
        }

        private bool ShouldIgnoreCollider(Collider hitCollider)
        {
            if (ignoredRoot != null && hitCollider.transform.IsChildOf(ignoredRoot))
            {
                return true;
            }

            if (ignoredColliders == null)
            {
                return false;
            }

            for (int i = 0; i < ignoredColliders.Length; i++)
            {
                if (ignoredColliders[i] == hitCollider)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasRequiredReferences()
        {
            return playerCamera != null && turretYawPivot != null;
        }

        private void WarnMissingReferencesOnce()
        {
            if (warnedMissingReferences)
            {
                return;
            }

            Debug.LogWarning(
                $"{nameof(TurretAimController)} on {name} needs a Camera and a Turret Yaw Pivot assigned.",
                this);
            warnedMissingReferences = true;
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
