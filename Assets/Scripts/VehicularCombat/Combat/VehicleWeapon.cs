using System;
using UnityEngine;

namespace VehicularCombat
{
    public sealed class VehicleWeapon : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Central input reader for the Vehicle action map.")]
        private VehicleInputReader inputReader;

        [SerializeField, Tooltip("Projectile spawn point. Its forward direction is used as shot direction.")]
        private Transform firePoint;

        [SerializeField, Tooltip("Projectile prefab with a VehicleProjectile component.")]
        private VehicleProjectile projectilePrefab;

        [SerializeField, Tooltip("Vehicle Rigidbody whose current velocity is inherited by projectiles.")]
        private Rigidbody vehicleRigidbody;

        [SerializeField, Tooltip("Root transform ignored by projectiles, usually the player vehicle root.")]
        private Transform ownerRoot;

        [Header("Weapon")]
        [SerializeField, Min(0.01f), Tooltip("Seconds between shots.")]
        private float fireCooldown = 0.25f;

        [SerializeField, Min(0f), Tooltip("Projectile muzzle speed before adding inherited vehicle velocity.")]
        private float projectileSpeed = 35f;

        [SerializeField, Min(0.01f), Tooltip("Seconds before the projectile destroys itself.")]
        private float projectileLifetime = 4f;

        [SerializeField, Min(0), Tooltip("Damage applied by each projectile.")]
        private int projectileDamage = 1;

        [SerializeField, Tooltip("Hold Fire to keep shooting with cooldown.")]
        private bool automaticFire = true;

        [SerializeField, Tooltip("Prevent mouse clicks from firing while the cursor is unlocked.")]
        private bool requireLockedCursor = true;

        // --- NUEVO: Sección de Munición y Recarga ---
        [Header("Ammo & Reloading")]
        [SerializeField, Min(1), Tooltip("Tamaño del cargador.")]
        private int maxAmmo = 30;

        [SerializeField, Min(0f), Tooltip("Tiempo que tarda en recargar en segundos.")]
        private float reloadTime = 2f;

        private int currentAmmo;
        private bool isReloading;
        private float reloadEndTime;

        [Header("Optional Feedback")]
        [SerializeField, Tooltip("Optional muzzle flash particle system attached to the weapon.")]
        private ParticleSystem muzzleFlash;

        [SerializeField, Tooltip("Optional impact effect prefab passed to spawned projectiles.")]
        private GameObject projectileImpactEffectPrefab;

        private float nextAllowedFireTime;
        private bool warnedMissingInput;
        private bool warnedMissingFirePoint;
        private bool warnedMissingProjectile;

        public event Action Fired;
        public float LastFireTime { get; private set; } = float.NegativeInfinity;

        private void Reset()
        {
            inputReader = GetComponentInParent<VehicleInputReader>();
            vehicleRigidbody = GetComponentInParent<Rigidbody>();
            ownerRoot = transform.root;
        }

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponentInParent<VehicleInputReader>();
            }

            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponentInParent<Rigidbody>();
            }

            if (ownerRoot == null)
            {
                ownerRoot = transform.root;
            }
        }

        // --- NUEVO: Inicializamos la munición al arrancar ---
        private void Start()
        {
            currentAmmo = maxAmmo;
            global::HUDManager.Instance?.InitAmmo(maxAmmo);
        }

        private void Update()
        {
            // Si el juego está pausado o el HUDManager dice que no estamos jugando, puedes agregar el chequeo aquí

            if (inputReader == null)
            {
                WarnMissingInputOnce();
                return;
            }

            // --- NUEVO: Manejo de la recarga en el tiempo ---
            if (isReloading)
            {
                if (Time.time >= reloadEndTime)
                {
                    CompleteReload();
                }
                return; // Bloquea los disparos mientras se está recargando
            }

            if (requireLockedCursor && Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            bool wantsToFire = automaticFire ? inputReader.FireHeld : inputReader.FireWasPressedThisFrame;
            if (wantsToFire)
            {
                TryFire();
            }
        }

        public bool TryFire()
        {
            if (Time.time < nextAllowedFireTime)
            {
                return false;
            }

            // --- NUEVO: Chequeo extra por seguridad (no debería disparar si no hay balas) ---
            if (currentAmmo <= 0)
            {
                StartReload();
                return false;
            }

            if (!HasRequiredReferences())
            {
                return false;
            }

            Vector3 inheritedVelocity = vehicleRigidbody != null ? vehicleRigidbody.linearVelocity : Vector3.zero;
            Vector3 projectileVelocity = firePoint.forward * projectileSpeed + inheritedVelocity;

            VehicleProjectile projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            projectile.Initialize(projectileVelocity, projectileLifetime, projectileDamage, ownerRoot, projectileImpactEffectPrefab);

            if (muzzleFlash != null)
            {
                muzzleFlash.Play(true);
            }

            // --- NUEVO: Descontamos munición y actualizamos la UI ---
            currentAmmo--;
            global::HUDManager.Instance?.ConsumeAmmo();

            // Si nos quedamos sin balas tras este disparo, iniciamos la recarga automáticamente
            if (currentAmmo <= 0)
            {
                StartReload();
            }

            LastFireTime = Time.time;
            nextAllowedFireTime = Time.time + fireCooldown;
            Fired?.Invoke();
            return true;
        }

        // --- NUEVO: Funciones de recarga ---
        private void StartReload()
        {
            isReloading = true;
            reloadEndTime = Time.time + reloadTime;
        }

        private void CompleteReload()
        {
            currentAmmo = maxAmmo;
            isReloading = false;

            // Avisamos a la UI que la recarga terminó
            global::HUDManager.Instance?.ReloadComplete();
        }

        private bool HasRequiredReferences()
        {
            bool hasReferences = true;

            if (firePoint == null)
            {
                WarnMissingFirePointOnce();
                hasReferences = false;
            }

            if (projectilePrefab == null)
            {
                WarnMissingProjectileOnce();
                hasReferences = false;
            }

            return hasReferences;
        }

        private void WarnMissingInputOnce()
        {
            if (warnedMissingInput)
            {
                return;
            }

            Debug.LogWarning($"{nameof(VehicleWeapon)} on {name} has no {nameof(VehicleInputReader)} assigned.", this);
            warnedMissingInput = true;
        }

        private void WarnMissingFirePointOnce()
        {
            if (warnedMissingFirePoint)
            {
                return;
            }

            Debug.LogWarning($"{nameof(VehicleWeapon)} on {name} cannot fire because Fire Point is missing.", this);
            warnedMissingFirePoint = true;
        }

        private void WarnMissingProjectileOnce()
        {
            if (warnedMissingProjectile)
            {
                return;
            }

            Debug.LogWarning($"{nameof(VehicleWeapon)} on {name} cannot fire because Projectile Prefab is missing.", this);
            warnedMissingProjectile = true;
        }
    }
}