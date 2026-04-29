using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class TopDownCharacterController2D : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;

    private Rigidbody2D body;
    private Vector2 movementInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        movementInput = ReadMovement();
    }

    private void FixedUpdate()
    {
        body.linearVelocity = movementInput * moveSpeed;
    }

    private static Vector2 ReadMovement()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        var horizontal = 0f;
        var vertical = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            vertical += 1f;
        }

        return new Vector2(horizontal, vertical).normalized;
    }
}
