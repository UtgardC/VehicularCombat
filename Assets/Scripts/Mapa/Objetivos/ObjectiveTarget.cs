using UnityEngine;

namespace VehicularCombat
{
    [RequireComponent(typeof(DamageableTarget))]
    public sealed class ObjectiveTarget : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField, Tooltip("Índice del ícono en el HUD (0, 1 o 2)")]
        private int hudIndex = 0;

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
            global::GameManager.Instance?.RegisterObjectiveDestroyed();
            global::HUDManager.Instance?.MarkObjectiveDestroyed(hudIndex);
        }
    }
}