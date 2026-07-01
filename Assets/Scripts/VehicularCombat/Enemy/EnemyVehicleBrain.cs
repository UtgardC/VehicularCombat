using UnityEngine;

using UnityEngine;

namespace VehicularCombat
{
    public class EnemyVehicleBrain : VehicleInputProvider
    {
        [Header("AI Navigation & Target")]
        [SerializeField, Tooltip("El transform del jugador o punto a perseguir.")]
        private Transform target;

        [Header("Behavior Ranges")]
        [SerializeField, Tooltip("Distancia a partir de la cual el enemigo empieza a disparar (Distancia Media).")]
        private float engageRange = 50f;

        [SerializeField, Tooltip("Distancia a partir de la cual el enemigo intenta embestir (Distancia Cerca).")]
        private float rammingRange = 15f;

        [SerializeField, Tooltip("Ángulo frontal máximo en grados para permitir el disparo.")]
        private float fireAngleTolerance = 20f;

        private float currentAccelerate;
        private float currentReverse;
        private float currentSteering;
        private bool currentFireHeld;

        // Implementación del "joystick simulado"
        public override float Accelerate => currentAccelerate;
        public override float Reverse => currentReverse;
        public override float Steering => currentSteering;
        public override bool HandbrakeHeld => false;
        public override bool FireHeld => currentFireHeld;
        public override bool FireWasPressedThisFrame => currentFireHeld;

        private void Update()
        {
            if (target == null)
            {
                StopVehicle();
                return;
            }

            // Cálculos espaciales básicos
            Vector3 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;

            // Transformamos la posición del objetivo al espacio local para saber si está a la izquierda, derecha, adelante o atrás
            Vector3 localTargetPos = transform.InverseTransformPoint(target.position);

            HandleSteering(localTargetPos);
            HandleBehavior(distance, localTargetPos, toTarget);
        }

        private void HandleSteering(Vector3 localTargetPos)
        {
            // El valor 'x' local nos da un input de giro natural. Si el jugador está a la derecha, x es positivo.
            currentSteering = Mathf.Clamp(localTargetPos.x, -1f, 1f);
        }

        private void HandleBehavior(float distance, Vector3 localTargetPos, Vector3 toTarget)
        {
            // Verificamos si el jugador está relativamente al frente para habilitar el disparo
            float angleToTarget = Vector3.Angle(transform.forward, toTarget.normalized);
            bool canSeeTarget = angleToTarget <= fireAngleTolerance;

            if (distance > engageRange)
            {
                // ESTADO: LEJOS (Perseguir)
                // Va a fondo para acortar distancia. Fuera de rango de armas.
                currentAccelerate = 1f;
                currentReverse = 0f;
                currentFireHeld = false;
            }
            else if (distance <= engageRange && distance > rammingRange)
            {
                // ESTADO: DISTANCIA MEDIA (Seguir y Disparar)
                // Mantiene el acelerador (lo bajé a 0.8f para que priorice un poco más la maniobrabilidad al apuntar)
                currentAccelerate = 0.8f;
                currentReverse = 0f;
                currentFireHeld = canSeeTarget;
            }
            else
            {
                // ESTADO: CERCA (Embestir y Disparar)
                // Acelerador a fondo ignorando todo, buscando el impacto físico.
                currentAccelerate = 1f;
                currentReverse = 0f;
                currentFireHeld = canSeeTarget;
            }

            // MECÁNICA DE DESATASCO: Si el jugador le quedó pegado justo detrás, da marcha atrás para no quedarse como un tonto contra una pared.
            if (localTargetPos.z < 0 && distance < rammingRange)
            {
                currentAccelerate = 0f;
                currentReverse = 1f;
            }
        }

        private void StopVehicle()
        {
            currentAccelerate = 0f;
            currentReverse = 0f;
            currentSteering = 0f;
            currentFireHeld = false;
        }

        // Método público por si un GameManager le asigna el objetivo al spawnear
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}