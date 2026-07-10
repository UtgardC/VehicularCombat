using UnityEngine;

namespace VehicularCombat
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Regeneración")]
        public float maxHealth = 100f;
        public float regenRate = 10f;
        public float regenDelay = 3f;

        private float currentHealth;
        private float timeSinceLastHit;
        private bool isDead = false;

        void Start()
        {
            currentHealth = maxHealth;
            // Forzamos la actualización del HUD al nacer
            global::HUDManager.Instance?.UpdateHealth(currentHealth, maxHealth);
        }

        void Update()
        {
            if (isDead) return;

            // El temporizador cuenta cuánto tiempo pasó desde el último golpe
            timeSinceLastHit += Time.deltaTime;

            // Si pasó el delay y no tenemos la vida llena, curamos a la nave
            if (timeSinceLastHit >= regenDelay && currentHealth < maxHealth)
            {
                currentHealth += regenRate * Time.deltaTime;

                // Evitamos que se pase del máximo
                if (currentHealth > maxHealth) currentHealth = maxHealth;

                global::HUDManager.Instance?.UpdateHealth(currentHealth, maxHealth);
            }
        }

        // Esta es la función que llamarán los peligros o enemigos
        public void ReceiveDamage(float amount)
        {
            if (isDead) return;

            currentHealth -= amount;
            timeSinceLastHit = 0f; // Reiniciamos el reloj para cancelar la regeneración

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }

            global::HUDManager.Instance?.UpdateHealth(currentHealth, maxHealth);
        }

        private void Die()
        {
            isDead = true;
            Debug.Log("💀 ¡El jugador ha sido destruido!");

            // Acá podés avisarle a tu GameManager que el nivel se perdió
            // global::GameManager.Instance?.SetState(GameManager.GameState.GameOver);
        }
    }
}