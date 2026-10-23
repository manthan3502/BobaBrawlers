using UnityEngine;
using UnityEngine.InputSystem;

namespace BobaBrawlers
{
    [RequireComponent(typeof(Fighter))]
    public sealed class PlayerController : MonoBehaviour
    {
        Fighter fighter;
        public bool scriptedInput;
        void Awake() { fighter = GetComponent<Fighter>(); }
        void Update()
        {
            if (scriptedInput) return;
            var keyboard = GameManager.Instance.ActiveKeyboard;
            if (keyboard == null || !GameManager.Instance.Running || !fighter.Alive) { fighter.Intent = Vector3.zero; return; }
            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            fighter.Intent = Vector3.ClampMagnitude(new Vector3(x, 0, z), 1);
            if (keyboard.spaceKey.wasPressedThisFrame) fighter.Dash.BeginCharge();
            if (keyboard.spaceKey.wasReleasedThisFrame) fighter.Dash.Release();
        }
        void OnApplicationFocus(bool focused)
        {
            if (!focused && GameManager.Instance && GameManager.Instance.Running && !scriptedInput &&
                System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--qa") < 0 &&
                System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--final-qa") < 0 &&
                System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--record-demo") < 0 &&
                System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--editor-check") < 0 &&
                System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--performance-check") < 0)
                GameManager.Instance.Pause();
        }
    }
}
