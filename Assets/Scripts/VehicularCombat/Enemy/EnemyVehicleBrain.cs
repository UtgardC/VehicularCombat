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

        [SerializeField, Tooltip("Distancia considerada como impacto físico (Ajustar según el largo de tu nave).")]
        private float impactDistance = 4f;

        [SerializeField, Tooltip("Tiempo en segundos que retrocede tras chocar.")]
        private float disengageDuration = 1.2f;

        [SerializeField, Tooltip("Ángulo frontal máximo en grados para permitir el disparo.")]
        private float fireAngleTolerance = 20f;

        private float currentAccelerate;
        private float currentReverse;
        private float currentSteering;
        private bool currentFireHeld;
        private float disengageTimer;

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

            // 1. MANEJO DEL DESENGANCHE (Prioridad Máxima)
            if (disengageTimer > 0f)
            {
                disengageTimer -= Time.deltaTime;

                // ESTADO: RETIRADA TÁCTICA
                currentAccelerate = 0f;
                currentReverse = 1f; // Clava la marcha atrás

                // Invertimos la dirección del volante para que al retroceder saque la trompa hacia afuera
                currentSteering = -Mathf.Clamp(localTargetPos.x, -1f, 1f);

                currentFireHeld = canSeeTarget; // Sigue disparando si le da el ángulo
                return; // Cortamos acá para que no evalúe los otros estados mientras huye
            }

            // 2. DETECCIÓN DE IMPACTO
            // Si está muy cerca y el jugador está hacia adelante, consideramos que hubo choque
            if (distance <= impactDistance && localTargetPos.z > -1f)
            {
                disengageTimer = disengageDuration; // Arrancamos el timer para el próximo frame
                return;
            }

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
                currentAccelerate = 1f;
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