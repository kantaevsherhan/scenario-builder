using ScenarioBuilder.Core;
using ScenarioBuilder.Sources;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScenarioBuilder.Demo
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMover : ScenarioRunnerLink
    {
        [SerializeField, Min(0f)] private float speed = 6f;

        private Rigidbody body;
        private Vector3 startPosition;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            startPosition = transform.position;
        }

        private void OnEnable()
        {
            if (Runner != null)
            {
                Runner.SessionStarted += OnSessionStarted;
            }
        }

        private void OnDisable()
        {
            if (Runner != null)
            {
                Runner.SessionStarted -= OnSessionStarted;
            }
        }

        private void FixedUpdate()
        {
            Vector2 input = ReadInput();
            Vector3 velocity = new Vector3(input.x, 0f, input.y) * speed;
            body.linearVelocity = new Vector3(velocity.x, body.linearVelocity.y, velocity.z);
        }

        private static Vector2 ReadInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float x = Axis(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed,
                keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed);
            float y = Axis(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed,
                keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private static float Axis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);

        private void OnSessionStarted(ScenarioSession session)
        {
            body.linearVelocity = Vector3.zero;
            body.position = startPosition;
            transform.position = startPosition;
        }
    }
}
