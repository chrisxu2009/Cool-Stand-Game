using UnityEngine;
using UnityEngine.InputSystem;

public class camerafollowplayer : MonoBehaviour
{
    [SerializeField] private float sensitivity = 0.12f;
    [SerializeField] private float minPitch = -90f;
    [SerializeField] private float maxPitch = 90f;

    private float pitch;
    private float yaw;

    private void Start()
    {
        Vector3 currentRotation = transform.eulerAngles;

        pitch = currentRotation.x;
        yaw = currentRotation.y;

        if (pitch > 180f)
            pitch -= 360f;

        LockCursor();
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            LockCursor();
        }

        if (Cursor.lockState != CursorLockMode.Locked ||
            Mouse.current == null)
        {
            return;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * sensitivity;
        pitch -= mouseDelta.y * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}