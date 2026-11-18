// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
// Week 2: Claude updated the bindings to the new GDD controls (section 5.1).
//
// Reads keyboard + mouse with the new Input System and exposes simple values.
//   WASD move, mouse aim, left click Normal Attack,
//   scroll = switch skill 1/2, hold E + scroll = pick delivery style, release E = use it,
//   Q = Chain Reaction, Space = dodge, F = talk, Tab = workbench, Esc = pause.
// The actions are defined in code so nothing needs to be wired in the Inspector.

using UnityEngine;
using UnityEngine.InputSystem;

namespace HollowTower.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        private InputAction move, cast, dodge, interact, workbench, pause;
        private InputAction delivery, chainReaction, scroll, slot1, slot2;

        public Vector2 Move => move.ReadValue<Vector2>();
        public Vector2 MouseScreenPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        public bool CastHeld => cast.IsPressed();
        public bool CastPressed => cast.WasPressedThisFrame();
        public bool DodgePressed => dodge.WasPressedThisFrame();
        public bool InteractPressed => interact.WasPressedThisFrame();
        public bool WorkbenchPressed => workbench.WasPressedThisFrame();
        public bool PausePressed => pause.WasPressedThisFrame();

        /// <summary>E is being held (picking a delivery style).</summary>
        public bool DeliveryHeld => delivery.IsPressed();
        /// <summary>E was let go this frame (use the delivery style).</summary>
        public bool DeliveryReleased => delivery.WasReleasedThisFrame();
        public bool ChainReactionPressed => chainReaction.WasPressedThisFrame();

        /// <summary>+1 / -1 when the wheel scrolls this frame, otherwise 0.</summary>
        public int Scroll
        {
            get
            {
                float y = scroll.ReadValue<Vector2>().y;
                return y > 0.01f ? 1 : y < -0.01f ? -1 : 0;
            }
        }

        /// <summary>0 or 1 if the 1 / 2 key was pressed this frame (extra shortcut), otherwise -1.</summary>
        public int SlotPressed => slot1.WasPressedThisFrame() ? 0 : slot2.WasPressedThisFrame() ? 1 : -1;

        private void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            cast          = new InputAction("Cast",          InputActionType.Button, "<Mouse>/leftButton");
            dodge         = new InputAction("Dodge",         InputActionType.Button, "<Keyboard>/space");
            interact      = new InputAction("Interact",      InputActionType.Button, "<Keyboard>/f");
            workbench     = new InputAction("Workbench",     InputActionType.Button, "<Keyboard>/tab");
            pause         = new InputAction("Pause",         InputActionType.Button, "<Keyboard>/escape");
            delivery      = new InputAction("Delivery",      InputActionType.Button, "<Keyboard>/e");
            chainReaction = new InputAction("ChainReaction", InputActionType.Button, "<Keyboard>/q");
            scroll        = new InputAction("Scroll",        InputActionType.Value,  "<Mouse>/scroll");
            slot1         = new InputAction("Slot1",         InputActionType.Button, "<Keyboard>/1");
            slot2         = new InputAction("Slot2",         InputActionType.Button, "<Keyboard>/2");
        }

        private InputAction[] All => new[]
        {
            move, cast, dodge, interact, workbench, pause, delivery, chainReaction, scroll, slot1, slot2
        };

        private void OnEnable()  { foreach (var a in All) a.Enable(); }
        private void OnDisable() { foreach (var a in All) a.Disable(); }
        private void OnDestroy() { foreach (var a in All) a.Dispose(); }
    }
}
