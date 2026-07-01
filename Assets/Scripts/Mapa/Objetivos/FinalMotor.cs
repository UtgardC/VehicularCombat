using UnityEngine;

namespace VehicularCombat
{
    [RequireComponent(typeof(DamageableTarget))]
    public sealed class FinalMotor : MonoBehaviour
    {
        private DamageableTarget damageable;

        void Awake()
        {
            damageable = GetComponent<DamageableTarget>();
            damageable.Died += OnDied;
        }

        void OnDestroy()
        {
            if (damageable != null)
                damageable.Died -= OnDied;
        }

        void OnDied(DamageableTarget _)
        {
            global::GameManager.Instance?.RegisterMotorDestroyed();
        }
    }
}