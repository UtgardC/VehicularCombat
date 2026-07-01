using UnityEngine;

namespace VehicularCombat
{
    public abstract class VehicleInputProvider : MonoBehaviour
    {
        public abstract float Accelerate { get; }
        public abstract float Reverse { get; }
        public abstract float Steering { get; }
        public abstract bool HandbrakeHeld { get; }
        public abstract bool FireHeld { get; }
        public abstract bool FireWasPressedThisFrame { get; }
    }
}