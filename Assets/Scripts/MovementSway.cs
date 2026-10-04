using UnityEngine;

public class MovementSway : MonoBehaviour
{
    [Header("References")]
    public RigidbodyPlayerMovement playerMovement;

    [Header("Bobbing Settings")]
    public float swaySpeed = 10f;       // Speed of the sine wave cycle
    public float swayAmountX = 0.04f;   // Horizontal swaying range
    public float swayAmountY = 0.02f;   // Vertical swaying range

    [Header("Strafe Tilt")]
    public float tiltAmount = 2f;       // Angle in degrees to tilt when strafing
    public float tiltSpeed = 5f;        // Smoothness of tilt transition

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float timer = 0f;

    void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        // Automatically fetch reference from parent if not assigned in Inspector
        if (playerMovement == null)
        {
            playerMovement = GetComponentInParent<RigidbodyPlayerMovement>();
        }
    }

    void Update()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        bool isMoving = Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputZ) > 0.1f;

        // Evaluate if player is on the ground
        bool isGrounded = playerMovement != null && playerMovement.isGrounded;

        // 1. Handle Position Swaying (Bobbing) - Only when moving and grounded
        if (isMoving && isGrounded)
        {
            timer += Time.deltaTime * swaySpeed;

            float targetX = initialPosition.x + Mathf.Sin(timer) * swayAmountX;
            float targetY = initialPosition.y + Mathf.Sin(timer * 2f) * swayAmountY;

            transform.localPosition = new Vector3(targetX, targetY, initialPosition.z);
        }
        else
        {
            // Smoothly return to center when idle or airborne
            timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition, Time.deltaTime * swaySpeed);
        }

        // 2. Handle Strafe Tilt (Camera / Weapon Roll)
        float targetTiltZ = -inputX * tiltAmount;
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0, 0, targetTiltZ);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * tiltSpeed);
    }
}