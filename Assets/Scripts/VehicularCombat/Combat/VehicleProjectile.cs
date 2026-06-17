using UnityEngine;

namespace VehicularCombat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public sealed class VehicleProjectile : MonoBehaviour
    {
        [Header("Defaults")]
        [SerializeField, Min(0.01f), Tooltip("Lifetime used if the projectile is placed in a scene without being initialized by a weapon.")]
        private float defaultLifetime = 4f;

        [SerializeField, Min(0), Tooltip("Damage used if the projectile is placed in a scene without being initialized by a weapon.")]
        private int defaultDamage = 1;

        [Header("Setup")]
        [SerializeField, Tooltip("Force the projectile collider to be a trigger on Awake.")]
        private bool makeColliderTriggerOnAwake = true;

        [Header("Optional Feedback")]
        [SerializeField, Tooltip("Fallback impact effect used when the weapon does not provide one.")]
        private GameObject defaultImpactEffectPrefab;

        private Rigidbody projectileRigidbody;
        private Collider projectileCollider;
        private Transform ownerRoot;
        private GameObject impactEffectPrefab;
        private int damage;
        private bool initialized;
        private bool hasImpacted;

        private void Awake()
        {
            projectileRigidbody = GetComponent<Rigidbody>();
            projectileCollider = GetComponent<Collider>();

            projectileRigidbody.useGravity = false;
            projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            projectileRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            if (makeColliderTriggerOnAwake)
            {
                projectileCollider.isTrigger = true;
            }
        }

        private void Start()
        {
            if (!initialized)
            {
                damage = defaultDamage;
                Destroy(gameObject, defaultLifetime);
            }
        }

        public void Initialize(
            Vector3 initialVelocity,
            float lifetime,
            int damageAmount,
            Transform projectileOwnerRoot,
            GameObject impactEffectOverride = null)
        {
            initialized = true;
            damage = Mathf.Max(0, damageAmount);
            ownerRoot = projectileOwnerRoot;
            impactEffectPrefab = impactEffectOverride != null ? impactEffectOverride : defaultImpactEffectPrefab;

            if (projectileRigidbody == null)
            {
                projectileRigidbody = GetComponent<Rigidbody>();
            }

            projectileRigidbody.linearVelocity = initialVelocity;
            Destroy(gameObject, Mathf.Max(0.01f, lifetime));
        }

        private void OnTriggerEnter(Collider other)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - hitPoint).sqrMagnitude > 0.0001f
                ? (transform.position - hitPoint).normalized
                : -transform.forward;

            HandleHit(other, hitPoint, hitNormal);
        }

        private void OnCollisionEnter(Collision collision)
        {
            ContactPoint contact = collision.contactCount > 0
                ? collision.GetContact(0)
                : new ContactPoint();

            Vector3 hitPoint = collision.contactCount > 0 ? contact.point : transform.position;
            Vector3 hitNormal = collision.contactCount > 0 ? contact.normal : -transform.forward;
            HandleHit(collision.collider, hitPoint, hitNormal);
        }

        private void HandleHit(Collider other, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (hasImpacted || other == null || ShouldIgnoreCollider(other))
            {
                return;
            }

            hasImpacted = true;

            if (TryGetDamageable(other, out DamageableTarget damageableTarget))
            {
                damageableTarget.ReceiveDamage(damage);
            }

            SpawnImpactEffect(hitPoint, hitNormal);
            Destroy(gameObject);
        }

        private bool TryGetDamageable(Collider hitCollider, out DamageableTarget damageableTarget)
        {
            if (hitCollider.TryGetComponent(out damageableTarget))
            {
                return true;
            }

            damageableTarget = hitCollider.GetComponentInParent<DamageableTarget>();
            return damageableTarget != null;
        }

        private bool ShouldIgnoreCollider(Collider hitCollider)
        {
            return ownerRoot != null && hitCollider.transform.IsChildOf(ownerRoot);
        }

        private void SpawnImpactEffect(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (impactEffectPrefab == null)
            {
                return;
            }

            Quaternion rotation = hitNormal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(hitNormal)
                : Quaternion.identity;

            Instantiate(impactEffectPrefab, hitPoint, rotation);
        }
    }
}
