// AI-assisted: Claude (Anthropic) wrote the first version of this script.
// Team changes: (none yet)
//
// Reads keyboard + mouse with the new Input System and exposes simple values
// other scripts can read. Bindings follow the GDD controls list (section 3).
// The actions are defined in code so nothing needs to be wired in the Inspector.

using UnityEngine;
using UnityEngine.InputSystem;

namespace HollowTower.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        private InputAction move, cast, dodge, interact, workbench, pause;
        private InputAction chain, bounce, slotScroll;
        private InputAction slot1, slot2, slot3, slot4;

        /// <summary>WASD as a -1..1 vector (x = left/right, y = forward/back).</summary>
        public Vector2 Move => move.ReadValue<Vector2>();
        /// <summary>Mouse position in screen pixels.</summary>
        public Vector2 MouseScreenPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        public bool CastHeld => cast.IsPressed();
        public bool CastPressed => cast.WasPressedThisFrame();
        public bool DodgePressed => dodge.WasPressedThisFrame();
        public bool InteractPressed => interact.WasPressedThisFrame();
        public bool WorkbenchPressed => workbench.WasPressedThisFrame();
        public bool PausePressed => pause.WasPressedThisFrame();
        public bool ChainPressed => chain.WasPressedThisFrame();
        public bool BouncePressed => bounce.WasPressedThisFrame();

        /// <summary>0-3 if a number key was pressed this frame, otherwise -1.</summary>
        public int SlotPressed
        {
            get
            {
                if (slot1.WasPressedThisFrame()) return 0;
                if (slot2.WasPressedThisFrame()) return 1;
                if (slot3.WasPressedThisFrame()) return 2;
                if (slot4.WasPressedThisFrame()) return 3;
                return -1;
            }
        }

        /// <summary>+1 / -1 when the wheel scrolls this frame, otherwise 0.</summary>
        public int SlotScroll
        {
            get
            {
                float y = slotScroll.ReadValue<Vector2>().y;
                return y > 0.01f ? 1 : y < -0.01f ? -1 : 0;
            }
        }

        private void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            cast       = new InputAction("Cast",      InputActionType.Button, "<Mouse>/leftButton");
            dodge      = new InputAction("Dodge",     InputActionType.Button, "<Keyboard>/space");
            interact   = new InputAction("Interact",  InputActionType.Button, "<Keyboard>/f");
            workbench  = new InputAction("Workbench", InputActionType.Button, "<Keyboard>/tab");
            pause      = new InputAction("Pause",     InputActionType.Button, "<Keyboard>/escape");
            chain      = new InputAction("Chain",     InputActionType.Button, "<Keyboard>/q");
            bounce     = new InputAction("Bounce",    InputActionType.Button, "<Keyboard>/e");
            slotScroll = new InputAction("SlotScroll", InputActionType.Value, "<Mouse>/scroll");
            slot1      = new InputAction("Slot1", InputActionType.Button, "<Keyboard>/1");
            slot2      = new InputAction("Slot2", InputActionType.Button, "<Keyboard>/2");
            slot3      = new InputAction("Slot3", InputActionType.Button, "<Keyboard>/3");
            slot4      = new InputAction("Slot4", InputActionType.Button, "<Keyboard>/4");
        }

        private InputAction[] All => new[]
        {
            move, cast, dodge, interact, workbench, pause, chain, bounce, slotScroll, slot1, slot2, slot3, slot4
        };

        private void OnEnable()  { foreach (var a in All) a.Enable(); }
        private void OnDisable() { foreach (var a in All) a.Disable(); }
        private void OnDestroy() { foreach (var a in All) a.Dispose(); }
    }
}
