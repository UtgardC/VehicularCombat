using System;
using UnityEngine;
using UnityEngine.Events;

namespace VehicularCombat
{
    public sealed class DamageableTarget : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField, Min(1), Tooltip("Maximum target health. Current health starts at this value.")]
        private int maximumHealth = 3;

        [SerializeField, Tooltip("Destroy the GameObject on death. Disable this only if another system hides it.")]
        private bool destroyOnDeath = true;

        [Header("Optional Feedback")]
        [SerializeField, Tooltip("Optional effect spawned when this target receives damage.")]
        private GameObject hitEffectPrefab;

        [SerializeField, Tooltip("Optional effect spawned when this target dies.")]
        private GameObject deathEffectPrefab;

        [Header("Events")]
        [SerializeField, Tooltip("Invoked after damage is applied. Int parameter is current health.")]
        private UnityEvent<int> damaged;

        [SerializeField, Tooltip("Invoked once when health reaches zero.")]
        private UnityEvent died;

        public event Action<DamageableTarget> Died;

        public int MaximumHealth => maximumHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        private void Awake()
        {
            ResetHealth();
        }

        public void ResetHealth()
        {
            CurrentHealth = maximumHealth;
            IsDead = false;
        }

        public void ReceiveDamage(int amount)
        {
            if (IsDead || amount <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            SpawnOptionalEffect(hitEffectPrefab);
            damaged?.Invoke(CurrentHealth);

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            Died?.Invoke(this);
            died?.Invoke();
            SpawnOptionalEffect(deathEffectPrefab);

            if (destroyOnDeath)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void SpawnOptionalEffect(GameObject effectPrefab)
        {
            if (effectPrefab == null)
            {
                return;
            }

            Instantiate(effectPrefab, transform.position, Quaternion.identity);
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(1, maximumHealth);
        }
    }
}
