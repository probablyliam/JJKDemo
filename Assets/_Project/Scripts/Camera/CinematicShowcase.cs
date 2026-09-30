using UnityEngine;
using UnityEngine.InputSystem;

namespace JJKDemo.Combat.Cameras
{
    /// <summary>
    /// A self-contained cinematic showcase camera for recording portfolio footage. It lives on its
    /// own Camera and never touches the Main Camera or any gameplay state. While OFF it does not
    /// render at all (your normal gameplay shows). While ON it renders OVER the game and automatically
    /// cuts between cinematic angles that follow the player, producing one clean multi-angle video.
    ///
    /// Controls (only while ON):
    ///   Toggle key  - show/hide the cinematic camera
    ///   1..N        - jump to a specific angle
    ///   0           - toggle auto-cutting on/off
    ///   Slow-mo key - dramatic slow motion (the only option that affects game speed; opt-in)
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CinematicShowcase : MonoBehaviour
    {
        [System.Serializable]
        public struct Angle
        {
            public string name;
            [Tooltip("Camera offset from the target.")]
            public Vector3 offset;
            [Tooltip("Rotate the offset to stay relative to the way the target is facing.")]
            public bool offsetRelativeToFacing;
            [Tooltip("Point on the target to look at.")]
            public Vector3 lookAtOffset;
            public float fieldOfView;
            [Tooltip("Degrees/sec to orbit the target (0 = none).")]
            public float orbitSpeed;
            [Tooltip("Position smoothing time (0 = a hard locked move).")]
            public float positionSmoothTime;
        }

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Angles (cut between these)")]
        [SerializeField]
        private Angle[] angles =
        {
            new Angle { name = "Cinematic Follow", offset = new Vector3(0f, 2.5f, -6f),  offsetRelativeToFacing = true,  lookAtOffset = new Vector3(0f, 1.3f, 0f), fieldOfView = 45f, orbitSpeed = 0f,  positionSmoothTime = 0.25f },
            new Angle { name = "Hero Low",         offset = new Vector3(3f, 0.6f, -4.5f), offsetRelativeToFacing = true,  lookAtOffset = new Vector3(0f, 1.4f, 0f), fieldOfView = 50f, orbitSpeed = 0f,  positionSmoothTime = 0.20f },
            new Angle { name = "Orbit Wide",       offset = new Vector3(0f, 4f, -10f),    offsetRelativeToFacing = false, lookAtOffset = new Vector3(0f, 1.5f, 0f), fieldOfView = 50f, orbitSpeed = 12f, positionSmoothTime = 0.30f },
            new Angle { name = "Close Up",         offset = new Vector3(1f, 1.6f, -2.4f), offsetRelativeToFacing = true,  lookAtOffset = new Vector3(0f, 1.5f, 0f), fieldOfView = 35f, orbitSpeed = 0f,  positionSmoothTime = 0.15f },
        };

        [Header("Auto-cut")]
        [SerializeField] private bool autoCut = true;
        [SerializeField] private float secondsPerAngle = 4f;

        [Header("Controls")]
        [SerializeField] private Key toggleKey = Key.Backslash;
        [SerializeField] private Key autoCutToggleKey = Key.Digit0;
        [SerializeField] private Key slowMoKey = Key.G;
        [Range(0.05f, 1f)]
        [SerializeField] private float slowMoScale = 0.3f;

        private static readonly Key[] NumberKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };

        private Camera cam;
        private bool active;
        private bool slowMo;
        private int angleIndex;
        private float cutTimer;
        private float orbitAngle;
        private Vector3 positionVelocity;
        private float defaultFixedDelta;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.enabled = false; // start hidden: gameplay shows, this does not render
            defaultFixedDelta = Time.fixedDeltaTime;

            if (autoFindPlayer && target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void OnDisable()
        {
            if (slowMo)
            {
                slowMo = false;
                Time.timeScale = 1f;
                Time.fixedDeltaTime = defaultFixedDelta;
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null)
            {
                return;
            }

            if (kb[toggleKey].wasPressedThisFrame)
            {
                SetActive(!active);
            }

            if (!active)
            {
                return;
            }

            if (kb[autoCutToggleKey].wasPressedThisFrame)
            {
                autoCut = !autoCut;
            }

            int count = Mathf.Min(angles.Length, NumberKeys.Length);
            for (int i = 0; i < count; i++)
            {
                if (kb[NumberKeys[i]].wasPressedThisFrame)
                {
                    SelectAngle(i);
                    autoCut = false; // manual pick pauses auto-cut so you can hold a shot
                }
            }

            if (kb[slowMoKey].wasPressedThisFrame)
            {
                ToggleSlowMo();
            }

            if (autoCut && angles.Length > 1)
            {
                cutTimer += Time.unscaledDeltaTime;
                if (cutTimer >= secondsPerAngle)
                {
                    SelectAngle((angleIndex + 1) % angles.Length);
                }
            }
        }

        private void LateUpdate()
        {
            if (!active || target == null || angles.Length == 0)
            {
                return;
            }

            Angle a = angles[Mathf.Clamp(angleIndex, 0, angles.Length - 1)];
            Vector3 lookPoint = target.position + a.lookAtOffset;

            Vector3 offset = a.offset;
            if (a.orbitSpeed != 0f)
            {
                orbitAngle += a.orbitSpeed * Time.deltaTime;
                offset = Quaternion.Euler(0f, orbitAngle, 0f) * offset;
            }
            else if (a.offsetRelativeToFacing)
            {
                offset = Quaternion.Euler(0f, target.eulerAngles.y, 0f) * offset;
            }

            Vector3 desired = target.position + offset;
            transform.position = a.positionSmoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desired, ref positionVelocity, a.positionSmoothTime)
                : desired;

            transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);

            if (cam.fieldOfView != a.fieldOfView)
            {
                cam.fieldOfView = a.fieldOfView;
            }
        }

        private void SetActive(bool on)
        {
            active = on;
            cam.enabled = on;
            if (on)
            {
                SelectAngle(angleIndex);
                // Snap straight to the shot so the first frame isn't a swoop from the old position.
                positionVelocity = Vector3.zero;
            }
            else if (slowMo)
            {
                ToggleSlowMo();
            }
        }

        private void SelectAngle(int index)
        {
            angleIndex = Mathf.Clamp(index, 0, angles.Length - 1);
            cutTimer = 0f;
            orbitAngle = 0f;
            positionVelocity = Vector3.zero;
        }

        private void ToggleSlowMo()
        {
            slowMo = !slowMo;
            Time.timeScale = slowMo ? slowMoScale : 1f;
            Time.fixedDeltaTime = defaultFixedDelta * (slowMo ? slowMoScale : 1f);
        }
    }
}
