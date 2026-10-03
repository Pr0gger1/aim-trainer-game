using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform playerCamera;

    [Header("Movement")]
    public float speed = 6f;
    public float sprintMultiplier = 1.7f;
    public float jumpHeight = 1.2f;
    public float gravity = -20f;

    [Header("Look")]
    public float mouseSensitivity = 2f;
    public float maxPitch = 90f;

    private float _pitch;
    private float _verticalVelocity;

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        if (Mouse.current == null || playerCamera == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();

        _pitch -= delta.y * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, -maxPitch, maxPitch);

        playerCamera.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        transform.Rotate(Vector3.up * (delta.x * mouseSensitivity));
    }

    private void HandleMovement()
    {
        if (controller == null || Keyboard.current == null)
            return;

        float right = (Keyboard.current.dKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.aKey.isPressed ? 1f : 0f);
        float forward = (Keyboard.current.wKey.isPressed ? 1f : 0f)
                      - (Keyboard.current.sKey.isPressed ? 1f : 0f);

        Vector3 move = transform.right * right + transform.forward * forward;
        if (move.magnitude > 1f)
            move.Normalize();

        bool sprinting = Keyboard.current.shiftKey.isPressed;
        move *= (sprinting ? speed * sprintMultiplier : speed) * Time.deltaTime;
        controller.Move(move);

        if (controller.isGrounded)
        {
            _verticalVelocity = -2f;
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }

        controller.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}