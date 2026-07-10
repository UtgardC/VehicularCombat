using UnityEngine;

namespace VehicularCombat
{
    public class Hazard : MonoBehaviour
    {
        [Header("Configuración de Daño")]
        [SerializeField] private float damageAmount = 15f; // Cambiado a float para el jugador

        [Tooltip("Si está activado, hace daño continuo mientras el jugador se quede encima")]
        [SerializeField] private bool continuousDamage = false;
        [SerializeField] private float damageCooldown = 1f;

        private float nextDamageTime;

        private void OnCollisionEnter(Collision collision)
        {
            TryDamage(collision.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            if (continuousDamage && Time.time >= nextDamageTime)
            {
                if (TryDamage(other.gameObject))
                {
                    nextDamageTime = Time.time + damageCooldown;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!continuousDamage)
            {
                TryDamage(other.gameObject);
            }
        }

        private bool TryDamage(GameObject target)
        {
            // 1. Primero chequeamos si chocamos contra el Jugador
            PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();
            if (playerHealth == null) playerHealth = target.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.ReceiveDamage(damageAmount);
                Debug.Log($"💥 Peligro le hizo {damageAmount} de daño al JUGADOR.");
                return true;
            }

            // 2. Si no es el jugador, chequeamos si chocamos contra un Objetivo destruible
            DamageableTarget objectiveHealth = target.GetComponentInParent<DamageableTarget>();
            if (objectiveHealth == null) objectiveHealth = target.GetComponent<DamageableTarget>();

            if (objectiveHealth != null && !objectiveHealth.IsDead)
            {
                // Pasamos el float a int por si el objetivo lo necesita en números enteros
                objectiveHealth.ReceiveDamage(Mathf.RoundToInt(damageAmount));
                return true;
            }

            return false;
        }
    }
}