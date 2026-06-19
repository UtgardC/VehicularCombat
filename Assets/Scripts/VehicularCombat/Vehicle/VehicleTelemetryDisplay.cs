using System.Text;
using TMPro;
using UnityEngine;

namespace VehicularCombat
{
    public sealed class VehicleTelemetryDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Rigidbody to measure.")]
        private Rigidbody vehicleRigidbody;

        [SerializeField, Tooltip("Optional reference frame used for local speed and acceleration. Defaults to the Rigidbody transform.")]
        private Transform referenceFrame;

        [SerializeField, Tooltip("Optional input reader used to show drive/steering/handbrake values.")]
        private VehicleInputReader inputReader;

        [SerializeField, Tooltip("Optional TextMeshProUGUI target. Leave empty to use the OnGUI overlay.")]
        private TextMeshProUGUI telemetryText;

        [Header("Display")]
        [SerializeField, Tooltip("Draw a lightweight debug overlay with OnGUI.")]
        private bool showOnGui = true;

        [SerializeField, Tooltip("Screen position for the OnGUI overlay.")]
        private Vector2 guiPosition = new(16f, 16f);

        [SerializeField, Tooltip("OnGUI overlay size.")]
        private Vector2 guiSize = new(330f, 210f);

        [SerializeField, Min(8), Tooltip("OnGUI font size.")]
        private int guiFontSize = 14;

        [SerializeField, Min(0f), Tooltip("Low-pass smoothing applied to acceleration readouts.")]
        private float accelerationSmoothing = 10f;

        private readonly StringBuilder builder = new();
        private Vector3 previousVelocity;
        private Vector3 worldAcceleration;
        private Vector3 localAcceleration;
        private Vector3 smoothedWorldAcceleration;
        private Vector3 smoothedLocalAcceleration;
        private string cachedText;
        private GUIStyle guiStyle;

        public float Speed { get; private set; }
        public float ForwardSpeed { get; private set; }
        public float LateralSpeed { get; private set; }
        public float VerticalSpeed { get; private set; }
        public float AccelerationMagnitude => smoothedWorldAcceleration.magnitude;
        public float LongitudinalAcceleration => smoothedLocalAcceleration.z;
        public float LateralAcceleration => smoothedLocalAcceleration.x;

        private void Reset()
        {
            vehicleRigidbody = GetComponent<Rigidbody>();
            inputReader = GetComponent<VehicleInputReader>();
            referenceFrame = transform;
        }

        private void Awake()
        {
            if (vehicleRigidbody == null)
            {
                vehicleRigidbody = GetComponent<Rigidbody>();
            }

            if (inputReader == null)
            {
                inputReader = GetComponent<VehicleInputReader>();
            }

            if (referenceFrame == null && vehicleRigidbody != null)
            {
                referenceFrame = vehicleRigidbody.transform;
            }
        }

        private void OnEnable()
        {
            previousVelocity = vehicleRigidbody != null ? vehicleRigidbody.linearVelocity : Vector3.zero;
        }

        private void FixedUpdate()
        {
            if (vehicleRigidbody == null)
            {
                cachedText = $"{nameof(VehicleTelemetryDisplay)}: missing Rigidbody";
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            Vector3 velocity = vehicleRigidbody.linearVelocity;
            worldAcceleration = deltaTime > 0f ? (velocity - previousVelocity) / deltaTime : Vector3.zero;
            previousVelocity = velocity;

            Transform frame = referenceFrame != null ? referenceFrame : vehicleRigidbody.transform;
            Vector3 localVelocity = frame.InverseTransformDirection(velocity);
            localAcceleration = frame.InverseTransformDirection(worldAcceleration);

            float blend = 1f - Mathf.Exp(-accelerationSmoothing * deltaTime);
            smoothedWorldAcceleration = Vector3.Lerp(smoothedWorldAcceleration, worldAcceleration, blend);
            smoothedLocalAcceleration = Vector3.Lerp(smoothedLocalAcceleration, localAcceleration, blend);

            Speed = velocity.magnitude;
            LateralSpeed = localVelocity.x;
            VerticalSpeed = localVelocity.y;
            ForwardSpeed = localVelocity.z;

            cachedText = BuildTelemetryText();

            if (telemetryText != null)
            {
                telemetryText.text = cachedText;
            }
        }

        private void OnGUI()
        {
            if (!showOnGui)
            {
                return;
            }

            if (guiStyle == null)
            {
                guiStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = guiFontSize,
                    normal = { textColor = Color.white }
                };
            }

            Rect rect = new(guiPosition.x, guiPosition.y, guiSize.x, guiSize.y);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 6f, rect.width - 16f, rect.height - 12f), cachedText, guiStyle);
        }

        private string BuildTelemetryText()
        {
            builder.Clear();
            builder.AppendLine("Vehicle Telemetry");
            builder.Append("Speed: ").Append(Speed.ToString("0.00")).Append(" m/s  ");
            builder.Append((Speed * 3.6f).ToString("0.0")).AppendLine(" km/h");
            builder.Append("Forward: ").Append(ForwardSpeed.ToString("0.00")).Append("  ");
            builder.Append("Lateral: ").Append(LateralSpeed.ToString("0.00")).Append("  ");
            builder.Append("Vertical: ").Append(VerticalSpeed.ToString("0.00")).AppendLine();
            builder.Append("Accel mag: ").Append(AccelerationMagnitude.ToString("0.00")).AppendLine(" m/s2");
            builder.Append("Accel local Z: ").Append(LongitudinalAcceleration.ToString("0.00")).Append("  ");
            builder.Append("X: ").Append(LateralAcceleration.ToString("0.00")).AppendLine();
            builder.Append("Raw accel: ");
            builder.Append(worldAcceleration.x.ToString("0.0")).Append(", ");
            builder.Append(worldAcceleration.y.ToString("0.0")).Append(", ");
            builder.Append(worldAcceleration.z.ToString("0.0")).AppendLine();

            if (inputReader != null)
            {
                builder.Append("Input A/R/S: ");
                builder.Append(inputReader.Accelerate.ToString("0.00")).Append(" / ");
                builder.Append(inputReader.Reverse.ToString("0.00")).Append(" / ");
                builder.Append(inputReader.Steering.ToString("0.00")).AppendLine();
                builder.Append("Handbrake: ").Append(inputReader.HandbrakeHeld ? "ON" : "off");
            }

            return builder.ToString();
        }

        private void OnValidate()
        {
            accelerationSmoothing = Mathf.Max(0f, accelerationSmoothing);
            guiFontSize = Mathf.Max(8, guiFontSize);
        }
    }
}
