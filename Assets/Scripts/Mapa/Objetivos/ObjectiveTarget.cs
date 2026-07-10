using UnityEngine;

namespace VehicularCombat
{
    [RequireComponent(typeof(DamageableTarget))]
    public sealed class ObjectiveTarget : MonoBehaviour
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
            // Ahora el objetivo solo le avisa al GameManager.
            // El GameManager se encargará de hacer la matemática y actualizar el HUDManager.
            global::GameManager.Instance?.RegisterObjectiveDestroyed();
        }
    }
}