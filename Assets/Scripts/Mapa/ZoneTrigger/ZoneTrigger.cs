using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    public enum Zona { A, B, C }

    [SerializeField] private Zona zona;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        switch (zona)
        {
            case Zona.A: HUDManager.Instance?.SetActiveZone('A'); break;
            case Zona.B: HUDManager.Instance?.SetActiveZone('B'); break;
            case Zona.C: HUDManager.Instance?.SetActiveZone('C'); break;
        }
    }
}