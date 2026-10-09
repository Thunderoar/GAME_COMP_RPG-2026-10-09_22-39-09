using Cainos.PixelArtTopDown_Basic;
using UnityEngine;

namespace Unisza
{
    /// <summary>
    /// Attach to a chest. Opens it and shows a popup when the player interacts.
    /// Expects a child trigger collider (see the installer) to detect player range.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ChestInteract : MonoBehaviour
    {
        [Header("Sprites")]
        public Sprite closedSprite;
        public Sprite openSprite;
        public Sprite closedShadowSprite;
        public Sprite openShadowSprite;

        [Header("References")]
        public SpriteRenderer spriteRenderer;
        public SpriteRenderer shadowRenderer;
        public WelcomePopup popup;

        [Header("Interaction")]
        public KeyCode interactKey = KeyCode.E;
        [Tooltip("Off = the chest opens automatically on contact instead of waiting for a key press.")]
        public bool requireKeyPress = true;
        public bool onlyOnce = true;

        bool playerInRange;
        bool isOpen;

        void Awake()
        {
            if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();

            if (!shadowRenderer)
            {
                foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr == spriteRenderer) continue;
                    shadowRenderer = sr;
                    break;
                }
            }

            ApplyState(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other)) playerInRange = true;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other)) playerInRange = false;
        }

        void Update()
        {
            if (onlyOnce && isOpen) return;
            if (!playerInRange) return;

            if (requireKeyPress && !Input.GetKeyDown(interactKey)) return;

            Open();
        }

        static bool IsPlayer(Collider2D other)
        {
            // Accept either controller: the package's own, or the point-and-click one
            // that replaces it at runtime.
            var t = other.transform;
            return t.GetComponentInParent<TopDownCharacterController>() != null
                || t.GetComponentInParent<PointAndClickController>() != null;
        }

        public void Open()
        {
            if (isOpen) return;

            isOpen = true;
            ApplyState(true);

            if (popup) popup.Show();
        }

        void ApplyState(bool open)
        {
            var target = open ? openSprite : closedSprite;
            if (spriteRenderer && target) spriteRenderer.sprite = target;

            var shadowTarget = open ? openShadowSprite : closedShadowSprite;
            if (shadowRenderer && shadowTarget) shadowRenderer.sprite = shadowTarget;
        }
    }
}
