using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class RigidbodyPlayerMovement : MonoBehaviour
{
    [Header("Effects & Visuals")]
    [SerializeField] private Volume dashVolume;
    [SerializeField] private float blurFadeSpeed = 10f;
    [SerializeField] private Transform playerVisuals;
    [SerializeField] private float visualSlideYOffset = -0.4f;
    [SerializeField] private float cameraTiltAngle = 4f;
    [SerializeField] private float cameraTiltSpeed = 12f;

    [Header("References")]
    [SerializeField] private Transform playerCamera;

    [Header("Running")]
    [SerializeField] private float runSpeed = 12f;
    [SerializeField] private float groundAcceleration = 90f;
    [SerializeField] private float groundDeceleration = 100f;
    [SerializeField] private float airAcceleration = 32f;
    [SerializeField] private float maximumAirSpeed = 22f;

    [Header("Jumping")]
    [SerializeField] private float jumpVelocity = 11f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Gravity")]
    [SerializeField] private float risingGravityMultiplier = 1.4f;
    [SerializeField] private float fallingGravityMultiplier = 2.8f;
    [SerializeField] private float maximumFallSpeed = 45f;

    [Header("Normal Shift Dash")]
    [SerializeField] private float dashSpeed = 28f;
    [SerializeField] private float dashDuration = 0.16f;
    [SerializeField] private float dashCooldown = 0.65f;
    [SerializeField] private float dashMomentumRetention = 0.85f;

    [Header("Sword Dash")]
    [SerializeField] private float swordDashSpeed = 38f;
    [SerializeField] private float swordDashDuration = 0.20f;
    [SerializeField] private float swordDashCooldown = 0.4f;
    [SerializeField] private float swordDashMomentumRetention = 1f;

    [Header("Wall Slide")]
    [SerializeField] private float wallCheckDistance = 0.4f;
    [SerializeField] private float wallSlideSpeed = 3f;
    [SerializeField] private float wallJumpUpVelocity = 11f;
    [SerializeField] private float wallJumpAwayVelocity = 12f;

    [Header("Ground Slide")]
    [SerializeField] private float slideSpeed = 22f;
    [SerializeField] private float slideHeight = 1.0f;
    [SerializeField] private float slideDeceleration = 12f;
    [SerializeField] private float slideSteerStrength = 5f;

    [Header("Air Slam")]
    [SerializeField] private float slamSpeed = 40f;

    [Header("Chain Grapple")]
    [SerializeField] private float grappleMaxDistance = 25f;
    [SerializeField] private float grappleSpeed = 35f;
    [SerializeField] private float grappleStopDistance = 2.0f;
    [SerializeField] private float grappleMaxDuration = 1.2f;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private Vector2 moveInput;
    private Vector3 wallNormal;
    private Vector3 dashDirection;

    public bool isGrounded;
    private bool isWallSliding;
    private bool isDashing;

    private float lastGroundContactTime = -999f;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float dashTimer;
    private float dashCooldownTimer;

    private float activeDashSpeed;
    private float activeDashMomentumRetention;

    private bool isSliding;
    private Vector3 slideDirection;
    private float currentSlideSpeed;
    private float originalCapsuleHeight;
    private Vector3 originalCapsuleCenter;
    private Vector3 originalVisualLocalPos;
    private float currentCameraTilt;

    private bool isGrappling;
    private Vector3 grappleTarget;
    private float grappleTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        if (capsule != null)
        {
            originalCapsuleHeight = capsule.height;
            originalCapsuleCenter = capsule.center;
        }

        if (playerVisuals != null)
        {
            originalVisualLocalPos = playerVisuals.localPosition;
        }

        if (rb != null)
        {
            rb.useGravity = true;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        ReadMovementInput();
        HandleNewInputs();
        HandleCameraTilt();

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpBufferCounter = jumpBufferTime;

            // Allow cancelling grapple by jumping
            if (isGrappling)
            {
                EndGrapple();
            }
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        bool normalDashPressed =
            Keyboard.current.leftShiftKey.wasPressedThisFrame ||
            Keyboard.current.rightShiftKey.wasPressedThisFrame;

        if (normalDashPressed && dashCooldownTimer <= 0f && !isDashing)
        {
            BeginNormalDash();
        }
    }

    private void FixedUpdate()
    {
        if (isGrappling)
        {
            HandleGrapple();
            return;
        }

        if (isSliding)
        {
            HandleSlide();
        }

        CheckGround();
        CheckWall();
        UpdateTimers();

        if (isDashing)
        {
            HandleDash();
            HandleJumping();
            return;
        }

        if (!isSliding)
        {
            HandleMovement();
        }

        HandleJumping();
        ApplyHeavyGravity();
        ApplyWallSlide();
    }

    private void ReadMovementInput()
    {
        float x = 0f;
        float y = 0f;

        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) y -= 1f;
        if (Keyboard.current.wKey.isPressed) y += 1f;

        moveInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    private void HandleCameraTilt()
    {
        if (playerCamera == null) return;

        float targetTilt = -moveInput.x * cameraTiltAngle;
        currentCameraTilt = Mathf.Lerp(currentCameraTilt, targetTilt, Time.deltaTime * cameraTiltSpeed);

        Vector3 currentEuler = playerCamera.localEulerAngles;
        playerCamera.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, currentCameraTilt);
    }

    private void HandleMovement()
    {
        Vector3 desiredDirection = GetCameraRelativeDirection();
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (isGrounded)
        {
            Vector3 targetVelocity = desiredDirection * runSpeed;
            float acceleration = moveInput.sqrMagnitude > 0.01f ? groundAcceleration : groundDeceleration;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                acceleration * Time.fixedDeltaTime
            );
        }
        else if (desiredDirection.sqrMagnitude > 0.01f)
        {
            horizontalVelocity += desiredDirection * airAcceleration * Time.fixedDeltaTime;
            float allowedSpeed = Mathf.Max(runSpeed, Mathf.Min(horizontalVelocity.magnitude, maximumAirSpeed));
            horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, allowedSpeed);
        }

        rb.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
    }

    private void BeginNormalDash()
    {
        Vector3 direction = GetCameraRelativeDirection();
        if (direction.sqrMagnitude < 0.01f) direction = GetCameraForward();

        BeginDash(direction, dashSpeed, dashDuration, dashCooldown, dashMomentumRetention);
    }

    public void BeginSwordDash()
    {
        if (dashCooldownTimer > 0f || isDashing) return;

        Vector3 direction = GetCameraForward();
        BeginDash(direction, swordDashSpeed, swordDashDuration, swordDashCooldown, swordDashMomentumRetention);
    }

    private void BeginDash(Vector3 direction, float speed, float duration, float cooldown, float momentumRetention)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        dashDirection = direction.normalized;
        activeDashSpeed = speed;
        activeDashMomentumRetention = momentumRetention;

        isDashing = true;
        dashTimer = duration;
        dashCooldownTimer = cooldown;

        rb.useGravity = false;
        rb.linearVelocity = dashDirection * activeDashSpeed;
    }

    private void HandleDash()
    {
        dashTimer -= Time.fixedDeltaTime;
        rb.linearVelocity = dashDirection * activeDashSpeed;

        if (dashTimer <= 0f)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        if (!isDashing) return;

        isDashing = false;
        rb.useGravity = true;

        Vector3 velocity = rb.linearVelocity;
        Vector3 retainedMomentum = dashDirection * activeDashSpeed * activeDashMomentumRetention;

        rb.linearVelocity = new Vector3(retainedMomentum.x, velocity.y, retainedMomentum.z);
    }

    private void HandleJumping()
    {
        if (jumpBufferCounter <= 0f) return;

        if (coyoteCounter > 0f)
        {
            bool jumpedDuringDash = isDashing;
            float retainedDashSpeed = activeDashSpeed;
            float retainedAmount = activeDashMomentumRetention;
            Vector3 retainedDirection = dashDirection;

            if (isDashing) EndDash();

            bool jumpedDuringSlide = isSliding;
            Vector3 preservedSlideVel = Vector3.zero;
            if (isSliding)
            {
                preservedSlideVel = slideDirection * currentSlideSpeed;
                StopSlide();
            }

            Vector3 velocity = rb.linearVelocity;
            velocity.y = jumpVelocity;

            if (jumpedDuringDash)
            {
                Vector3 dashMomentum = retainedDirection * retainedDashSpeed * retainedAmount;
                velocity.x = dashMomentum.x;
                velocity.z = dashMomentum.z;
            }
            else if (jumpedDuringSlide)
            {
                velocity.x = preservedSlideVel.x;
                velocity.z = preservedSlideVel.z;
            }

            rb.linearVelocity = velocity;

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            isGrounded = false;
        }
        else if (isWallSliding)
        {
            if (isDashing) EndDash();
            if (isSliding) StopSlide();

            Vector3 velocity = wallNormal * wallJumpAwayVelocity;
            velocity.y = wallJumpUpVelocity;
            rb.linearVelocity = velocity;

            jumpBufferCounter = 0f;
            isWallSliding = false;
        }
    }

    private void ApplyHeavyGravity()
    {
        if (isGrounded || isDashing) return;

        Vector3 velocity = rb.linearVelocity;
        float multiplier = velocity.y > 0f ? risingGravityMultiplier : fallingGravityMultiplier;
        Vector3 extraGravity = Physics.gravity * (multiplier - 1f);

        rb.AddForce(extraGravity, ForceMode.Acceleration);

        if (rb.linearVelocity.y < -maximumFallSpeed)
        {
            velocity = rb.linearVelocity;
            velocity.y = -maximumFallSpeed;
            rb.linearVelocity = velocity;
        }
    }

    private void ApplyWallSlide()
    {
        if (!isWallSliding) return;

        Vector3 velocity = rb.linearVelocity;
        if (velocity.y < -wallSlideSpeed)
        {
            velocity.y = -wallSlideSpeed;
            rb.linearVelocity = velocity;
        }
    }

    private void CheckGround()
    {
        isGrounded = Time.time - lastGroundContactTime < 0.15f;
    }

    private void CheckWall()
    {
        isWallSliding = false;
        wallNormal = Vector3.zero;

        if (isGrounded || rb.linearVelocity.y > 0f) return;

        Vector3 origin = transform.TransformPoint(capsule.center);
        float distance = capsule.radius + wallCheckDistance;
        const int directionCount = 12;

        for (int i = 0; i < directionCount; i++)
        {
            float angle = i * (360f / directionCount);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, ~0, QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == capsule || hit.rigidbody == rb) continue;

                bool verticalWall = Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) < 0.25f;
                if (verticalWall)
                {
                    isWallSliding = true;
                    wallNormal = hit.normal;
                    return;
                }
            }
        }
    }

    private Vector3 GetCameraRelativeDirection()
    {
        if (playerCamera == null) return Vector3.zero;

        Vector3 forward = playerCamera.forward;
        Vector3 right = playerCamera.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return (forward * moveInput.y + right * moveInput.x).normalized;
    }

    private Vector3 GetCameraForward()
    {
        if (playerCamera == null)
        {
            Vector3 fallback = transform.forward;
            fallback.y = 0f;
            return fallback.normalized;
        }

        Vector3 forward = playerCamera.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }

        return forward.normalized;
    }

    private void UpdateTimers()
    {
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.fixedDeltaTime;

        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.fixedDeltaTime;
    }

    private void OnCollisionEnter(Collision collision)
    {
        DetectGroundCollision(collision);

        // Cancel grapple immediately upon colliding with any wall or surface
        if (isGrappling)
        {
            EndGrapple();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        DetectGroundCollision(collision);

        if (isGrappling)
        {
            EndGrapple();
        }
    }

    private void DetectGroundCollision(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f)
            {
                lastGroundContactTime = Time.time;
                return;
            }
        }
    }

    private void HandleNewInputs()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame && isGrounded && !isSliding)
        {
            StartSlide();
        }

        if (Keyboard.current.cKey.wasPressedThisFrame && !isGrounded && !isDashing)
        {
            ExecuteAirSlam();
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (isGrappling)
            {
                EndGrapple(); // Press F again to detach
            }
            else
            {
                TryBeginGrapple();
            }
        }
    }

    private void StartSlide()
    {
        isSliding = true;

        originalCapsuleHeight = capsule.height;
        originalCapsuleCenter = capsule.center;

        capsule.height = slideHeight;
        capsule.center = new Vector3(originalCapsuleCenter.x, slideHeight * 0.5f, originalCapsuleCenter.z);

        if (playerVisuals != null)
        {
            playerVisuals.localPosition = originalVisualLocalPos + new Vector3(0f, visualSlideYOffset, 0f);
        }

        slideDirection = GetCameraRelativeDirection();
        if (slideDirection.sqrMagnitude < 0.01f) slideDirection = GetCameraForward();

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        currentSlideSpeed = Mathf.Max(horizontalVelocity.magnitude, slideSpeed);

        if (isDashing) EndDash();
    }

    private void HandleSlide()
    {
        if (!isSliding) return;

        if (Keyboard.current == null || !Keyboard.current.cKey.isPressed || !isGrounded)
        {
            StopSlide();
            return;
        }

        Vector3 targetSteerDir = GetCameraRelativeDirection();
        if (targetSteerDir.sqrMagnitude > 0.01f)
        {
            slideDirection = Vector3.Slerp(slideDirection, targetSteerDir, slideSteerStrength * Time.fixedDeltaTime).normalized;
        }

        if (currentSlideSpeed > slideSpeed)
        {
            currentSlideSpeed = Mathf.MoveTowards(currentSlideSpeed, slideSpeed, slideDeceleration * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector3(
            slideDirection.x * currentSlideSpeed,
            rb.linearVelocity.y,
            slideDirection.z * currentSlideSpeed
        );
    }

    private void StopSlide()
    {
        isSliding = false;
        capsule.height = originalCapsuleHeight;
        capsule.center = originalCapsuleCenter;

        if (playerVisuals != null)
        {
            playerVisuals.localPosition = originalVisualLocalPos;
        }
    }

    private void ExecuteAirSlam()
    {
        rb.linearVelocity = new Vector3(0f, -slamSpeed, 0f);
    }

    private void TryBeginGrapple()
    {
        Transform cam = playerCamera != null ? playerCamera : transform;

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, grappleMaxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == capsule || hit.rigidbody == rb) return;

            isGrappling = true;
            grappleTarget = hit.point;
            grappleTimer = grappleMaxDuration;
            rb.useGravity = false;
        }
    }

    private void HandleGrapple()
    {
        if (!isGrappling) return;

        grappleTimer -= Time.fixedDeltaTime;

        Vector3 dirToTarget = (grappleTarget - transform.position).normalized;
        rb.linearVelocity = dirToTarget * grappleSpeed;

        float distanceToTarget = Vector3.Distance(transform.position, grappleTarget);

        if (distanceToTarget < grappleStopDistance || grappleTimer <= 0f)
        {
            EndGrapple();
        }
    }

    private void EndGrapple()
    {
        if (!isGrappling) return;

        isGrappling = false;
        rb.useGravity = true;
    }
}