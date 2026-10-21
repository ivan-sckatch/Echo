using UnityEngine;
using UnityEngine.InputSystem;
namespace EchoGame
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        public Transform View;
        [Min(0)] public float Speed = 4.5f;
        public float MouseSensitivity = .1f;
        public float JumpHeight = 1.1f;
        public float Gravity = -22;
        public float PitchLimit = 85;
        float pitch, vertical;
        bool jumpRequested;
        CharacterController controller;
        public float Pitch => pitch;
        public Vector3 Feet => transform.position;
        void Awake() => controller = GetComponent<CharacterController>();
        public void Look()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) jumpRequested = true;
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 delta = mouse.delta.ReadValue() * MouseSensitivity;
            transform.Rotate(0, delta.x, 0);
            pitch = Mathf.Clamp(pitch - delta.y, -PitchLimit, PitchLimit);
            View.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        public void Simulate(float dt)
        {
            var k = Keyboard.current;
            Vector2 input = k == null ? Vector2.zero : new Vector2(
                (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
                (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            if (controller.isGrounded && vertical < 0) vertical = -2;
            if (jumpRequested && controller.isGrounded)
                vertical = Mathf.Sqrt(-2 * Gravity * JumpHeight);
            jumpRequested = false;
            vertical += Gravity * dt;
            Vector3 move = transform.TransformDirection(new Vector3(input.x, 0, input.y).normalized) * Speed;
            controller.Move((move + Vector3.up * vertical) * dt);
        }
        public EchoFrame Capture(float time) => new EchoFrame(time, Feet, transform.eulerAngles.y, pitch);
        public void Teleport(Transform spawn)
        {
            if (!controller) controller = GetComponent<CharacterController>();
            controller.enabled = false;
            transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            controller.enabled = true;
            vertical = 0; pitch = 0; jumpRequested = false; View.localRotation = Quaternion.identity;
        }
    }
}
