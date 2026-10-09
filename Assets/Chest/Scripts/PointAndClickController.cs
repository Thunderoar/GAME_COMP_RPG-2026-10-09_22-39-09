using Cainos.PixelArtTopDown_Basic;
using UnityEngine;

namespace Unisza
{
    /// <summary>
    /// Click anywhere to send the character there. WASD still works and takes over
    /// the moment a key is pressed, so the two schemes never fight.
    ///
    /// Drives the same Rigidbody2D the package's TopDownCharacterController uses, so
    /// that script must stay disabled (it writes linearVelocity every frame too).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PointAndClickController : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 3f;
        [Tooltip("Distance at which the character stops and considers itself arrived.")]
        public float arriveThreshold = 0.15f;

        [Header("Click")]
        [Tooltip("Press a movement key to cancel the current walk and steer manually.")]
        public bool manualInputCancelsClick = true;
        [Tooltip("Show a short-lived marker where the player clicked.")]
        public bool showClickMarker = true;
        public Color clickMarkerColor = new Color(1f, 0.95f, 0.6f, 0.9f);
        public float markerLifetime = 0.6f;

        [Header("Obstacles")]
        [Tooltip("Seconds without getting measurably closer before the walk is cancelled.")]
        public float stuckTimeout = 1.1f;

        Animator animator;
        Rigidbody2D body;
        Camera cam;
        SpriteRenderer marker;

        Vector3 target;
        bool hasTarget;
        float lastDistance;
        float noProgressTime;
        float markerLife;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            cam = Camera.main;

            // The package controller also writes velocity each frame; keep it off.
            var legacy = GetComponent<TopDownCharacterController>();
            if (legacy) legacy.enabled = false;
        }

        void Update()
        {
            ReadClick();

            Vector2 dir = DesiredDirection();
            body.linearVelocity = dir * speed;

            UpdateAnimator(dir);
            TrackProgress();
            UpdateMarker();
        }

        void ReadClick()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            world.z = transform.position.z;

            target = world;
            hasTarget = true;
            lastDistance = 0f;
            noProgressTime = 0f;

            if (showClickMarker) SpawnMarker(world);
        }

        Vector2 DesiredDirection()
        {
            Vector2 manual = ReadManual();
            if (manual.sqrMagnitude > 0.0001f)
            {
                if (manualInputCancelsClick) hasTarget = false;
                return manual.normalized;
            }

            if (!hasTarget) return Vector2.zero;

            Vector2 delta = target - transform.position;
            float distance = delta.magnitude;

            if (distance <= arriveThreshold)
            {
                hasTarget = false;
                return Vector2.zero;
            }

            return delta / distance;
        }

        static Vector2 ReadManual()
        {
            Vector2 dir = Vector2.zero;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir.x += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dir.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dir.y -= 1f;
            return Vector2.ClampMagnitude(dir, 1f);
        }

        void UpdateAnimator(Vector2 dir)
        {
            if (!animator) return;

            bool moving = dir.sqrMagnitude > 0.0001f;
            animator.SetBool("IsMoving", moving);
            if (!moving) return;

            // Matches the package's mapping: 0 = South, 1 = North, 2 = East, 3 = West.
            int direction;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) direction = dir.x > 0f ? 2 : 3;
            else direction = dir.y > 0f ? 1 : 0;

            animator.SetInteger("Direction", direction);
        }

        /// <summary>
        /// Walls have no navmesh here, so steering straight at the target can wedge the
        /// character against geometry. Cancel the walk if it stops making progress.
        /// </summary>
        void TrackProgress()
        {
            if (!hasTarget)
            {
                lastDistance = 0f;
                noProgressTime = 0f;
                return;
            }

            float distance = Vector2.Distance(target, transform.position);

            if (lastDistance > 0f && lastDistance - distance < 0.01f)
                noProgressTime += Time.deltaTime;
            else
                noProgressTime = 0f;

            lastDistance = distance;

            if (noProgressTime >= stuckTimeout)
            {
                hasTarget = false;
                noProgressTime = 0f;
            }
        }

        void SpawnMarker(Vector3 world)
        {
            if (marker == null) marker = BuildMarker();

            marker.transform.position = world;
            marker.color = clickMarkerColor;
            markerLife = markerLifetime;
        }

        void UpdateMarker()
        {
            if (marker == null) return;

            if (markerLife <= 0f)
            {
                if (marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
                return;
            }

            markerLife -= Time.deltaTime;

            var c = marker.color;
            c.a = Mathf.Clamp01(markerLife / markerLifetime) * clickMarkerColor.a;
            marker.color = c;
        }

        static SpriteRenderer BuildMarker()
        {
            // A tiny generated ring; avoids shipping another texture for one decal.
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            float radius = size * 0.5f - 2f;
            var centre = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), centre);
                    float ring = Mathf.Abs(d - radius);
                    byte a = (byte)(ring <= 2f ? 255 : 0);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            var go = new GameObject("ClickMarker");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sr.sortingOrder = 500;
            DontDestroyOnLoad(go);
            return sr;
        }
    }
}
