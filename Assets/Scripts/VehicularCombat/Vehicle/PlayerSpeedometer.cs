using UnityEngine;

namespace VehicularCombat
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerSpeedometer : MonoBehaviour
    {
        private Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            // Forzamos el HUD a 0 al nacer para limpiar el texto del Editor
            global::HUDManager.Instance?.UpdateSpeed(0f);
        }

        void Update()
        {
            // Si el juego no está en Playing, el script se pausa aquí
            if (global::GameManager.Instance?.State != global::GameManager.GameState.Playing) return;

            // Esta es la línea clave que se había borrado:
            float speedKmh = rb.linearVelocity.magnitude * 3.6f;

            global::HUDManager.Instance?.UpdateSpeed(speedKmh);
        }
    }
}