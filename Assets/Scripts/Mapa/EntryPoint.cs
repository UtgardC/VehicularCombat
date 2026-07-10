using UnityEngine;

public class EntryPoint : MonoBehaviour
{
    public enum ZonaAsociada { A, B, C }

    [Header("Configuración de Zona")]
    public ZonaAsociada zona;

    // --- NUEVO: Cada punto guarda su propia imagen ---
    [Header("Pantalla de Intro")]
    [Tooltip("La imagen exacta que se mostrará al spawnear en este punto")]
    public Sprite imagenEspecificaDeInicio;
}