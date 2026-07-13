using UnityEngine;
using UnityEngine.AI;

namespace VehicularCombat
{
    [DisallowMultipleComponent]
    public sealed class EnemyVehicleBrain : VehicleInputProvider
    {
        [Header("Target")]
        [SerializeField, Tooltip("Player transform pursued by this enemy. Can also be assigned through SetTarget.")]
        private Transform target;

        [SerializeField, Tooltip("Find the active VehicleInputReader once on Start when Target is not assigned.")]
        private bool findPlayerOnStart = true;

        [SerializeField, Tooltip("Optional turret aimer. It receives the same target and is used to decide when to fire.")]
        private EnemyTurretAim1 turretAim;

        [Header("Behavior")]
        [SerializeField, Min(0f), Tooltip("Maximum distance at which the enemy fires while pursuing the target.")]
        private float engageRange = 50f;

        [SerializeField, Min(0f), Tooltip("Distance at which the enemy commits to ramming the target.")]
        private float rammingRange = 15f;

        [SerializeField, Min(0f), Tooltip("Distance treated as a collision so the enemy backs away and disengages.")]
        private float impactDistance = 4f;

        [SerializeField, Min(0f), Tooltip("Seconds spent reversing after reaching Impact Distance.")]
        private float disengageDuration = 1.2f;

        [SerializeField, Range(0f, 180f), Tooltip("Maximum aiming error allowed before firing when no turret aimer is assigned.")]
        private float fireAngleTolerance = 20f;

        [SerializeField, Min(0.01f), Tooltip("Local horizontal distance that produces full steering input.")]
        private float steeringSensitivityDistance = 5f;

        [Header("Navegación NavMesh (GPS)")]
        [Tooltip("Cada cuántos segundos recalcula la ruta. 0.5 es ideal para rendimiento.")]
        public float pathUpdateInterval = 0.5f;

        [Tooltip("A qué distancia de la esquina actual empieza a doblar hacia la siguiente")]
        public float waypointTolerance = 6f;

        private NavMeshPath path;
        private float pathTimer;
        private int currentWaypointIndex;

        private float currentAccelerate;
        private float currentReverse;
        private float currentSteering;
        private bool currentFireHeld;
        private float disengageTimer;
        private Rigidbody rb;
        private float stuckTimer;

        public override float Accelerate => currentAccelerate;
        public override float Reverse => currentReverse;
        public override float Steering => currentSteering;
        public override bool HandbrakeHeld => false;
        public override bool FireHeld => currentFireHeld;
        public override bool FireWasPressedThisFrame => currentFireHeld;
        public Transform Target => target;

        private void Reset()
        {
            turretAim = GetComponentInChildren<EnemyTurretAim1>();
        }

        private void Awake()
        {
            if (turretAim == null)
            {
                turretAim = GetComponentInChildren<EnemyTurretAim1>();
            }
            path = new NavMeshPath();
            rb = GetComponent<Rigidbody>(); // <-- AGREGADO
        }

        private void Start()
        {
            if (target == null && findPlayerOnStart)
            {
                VehicleInputReader playerInput = FindAnyObjectByType<VehicleInputReader>();
                if (playerInput != null)
                {
                    SetTarget(playerInput.transform);
                }
            }
            else if (target != null)
            {
                SetTarget(target);
            }
        }

        private void Update()
        {
            if (target == null)
            {
                StopVehicle();
                return;
            }

            // --- 1. EL GPS CALCULA LA RUTA ---
            pathTimer += Time.deltaTime;
            if (pathTimer >= pathUpdateInterval)
            {
                NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path);
                currentWaypointIndex = 1; // 0 es el origen (abajo del auto)
                pathTimer = 0f;
            }

            // --- 2. BUSCAR A QUÉ ESQUINA APUNTAR ---
            Vector3 positionToSteerTowards = target.position; // Por defecto, apuntamos al jugador

            if (path != null && path.corners.Length > 1 && currentWaypointIndex < path.corners.Length)
            {
                // Si llegamos a la esquina actual, pasamos a la siguiente miga de pan
                if (Vector3.Distance(transform.position, path.corners[currentWaypointIndex]) < waypointTolerance)
                {
                    if (currentWaypointIndex < path.corners.Length - 1)
                    {
                        currentWaypointIndex++;
                    }
                }
                positionToSteerTowards = path.corners[currentWaypointIndex];
            }

            // --- 3. CALCULAR DIRECCIONES ---
            // Distancia real al jugador (para saber cuándo disparar)
            Vector3 toActualTarget = target.position - transform.position;
            float distanceToPlayer = toActualTarget.magnitude;

            // Posición local de la esquina del GPS (para saber cómo doblar)
            Vector3 localWaypointPosition = transform.InverseTransformPoint(positionToSteerTowards);

            // --- 4. MOVER EL AUTO ---
            // Movemos el volante apuntando a la esquina, NO atravesando la pared
            currentSteering = Mathf.Clamp(
                localWaypointPosition.x / steeringSensitivityDistance,
                -1f,
                1f);

            bool canFire = CanFireAtTarget(toActualTarget);

            //la IA no intenta hacer reversa pensando que estás a su espalda.
            UpdateDriving(distanceToPlayer, localWaypointPosition, canFire);

            if (path != null && path.corners.Length > 1)
            {
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
                }
            }
        }


        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (turretAim != null)
            {
                turretAim.SetTarget(newTarget);
            }
        }

        private bool CanFireAtTarget(Vector3 toTarget)
        {
            if (turretAim != null)
            {
                return turretAim.IsAimedAtTarget(fireAngleTolerance);
            }

            return toTarget.sqrMagnitude > 0.0001f &&
                   Vector3.Angle(transform.forward, toTarget) <= fireAngleTolerance;
        }

        private void UpdateDriving(float distance, Vector3 localTargetPosition, bool canFire)
        {
            // --- 1. SISTEMA ANTI-ESTANCAMIENTO (¡NUEVO!) ---
            // Si el motor intenta avanzar (acelerador > 0.5), pero físicamente casi no nos movemos...
            if (currentAccelerate > 0.5f && rb.linearVelocity.magnitude < 2.5f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > 0.8f) // Si paso casi 1 segundo frenado contra algo
                {
                    disengageTimer = disengageDuration; // ¡Activar reversa de emergencia!
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f; // Si me estoy moviendo bien, reseteo el cronómetro
            }

            // --- 2. LÓGICA DE REVERSA ---
            if (disengageTimer > 0f)
            {
                disengageTimer -= Time.deltaTime;
                currentAccelerate = 0f;
                currentReverse = 1f;
                currentSteering = -currentSteering; // Girar al revés para "desengancharse" de la pared
                currentFireHeld = distance <= engageRange && canFire;
                return;
            }

            // --- 3. CHOQUE CONTRA JUGADOR ---
            if (distance <= impactDistance && localTargetPosition.z > -1f)
            {
                disengageTimer = disengageDuration;
                currentAccelerate = 0f;
                currentReverse = 1f;
                currentFireHeld = false;
                return;
            }

            // --- 4. CONDUCCIÓN NORMAL ---
            currentAccelerate = 1f;
            currentReverse = 0f;
            currentFireHeld = distance <= engageRange && canFire;

            if (localTargetPosition.z < 0f && distance < rammingRange)
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

        private void OnValidate()
        {
            engageRange = Mathf.Max(0f, engageRange);
            rammingRange = Mathf.Clamp(rammingRange, 0f, engageRange);
            impactDistance = Mathf.Clamp(impactDistance, 0f, rammingRange);
            steeringSensitivityDistance = Mathf.Max(0.01f, steeringSensitivityDistance);
        }
    }
}
