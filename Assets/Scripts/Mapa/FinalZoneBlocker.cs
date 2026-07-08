using UnityEngine;

namespace VehicularCombat
{
    [RequireComponent(typeof(Collider))]
    public sealed class FinalZoneBlocker : MonoBehaviour
    {
        [Header("Opcional: mesh visual a ocultar al desbloquear")]
        [SerializeField] private GameObject visualMesh;

        private Collider blockCollider;

        void Awake()
        {
            blockCollider = GetComponent<Collider>();
        }

        void OnEnable()
        {
            if (global::GameManager.Instance != null)
                global::GameManager.Instance.OnFinalZoneUnlocked += Unlock;
        }

        void OnDisable()
        {
            if (global::GameManager.Instance != null)
                global::GameManager.Instance.OnFinalZoneUnlocked -= Unlock;
        }

        void Unlock()
        {
            blockCollider.enabled = false;

            if (visualMesh != null)
                visualMesh.SetActive(false);
        }
    }
}