using UnityEngine;

public class ThrownSwordProjectile : MonoBehaviour
{
    [Header("Flight")]
    public float flySpeed = 20f;
    public float flyDuration = 1f;

    [Header("Spin")]
    public float spinSpeed = 720f;
    public Vector3 spinAxis = Vector3.forward;

    [Header("Wall Bouncing")]
    public float bounceRadius = 0.15f;
    public LayerMask wallLayers = ~0;

    [HideInInspector]
    public Transform owner;

    private Transform pivot;
    private Vector3 flyDirection;
    private float elapsed;
    private bool isFlying;
    private bool hasLaunched;

    public void Launch(Vector3 direction, Vector3 spawnPosition)
    {
        if (hasLaunched)
            return;

        hasLaunched = true;
        flyDirection = direction.normalized;

        // Find the center of the sword's visible mesh.
        Renderer swordRenderer = GetComponentInChildren<Renderer>();

        Vector3 center = swordRenderer != null
            ? swordRenderer.bounds.center
            : transform.position;

        // Create a pivot at the sword's visual center.
        GameObject pivotObject = new GameObject("SwordSpinPivot");
        pivot = pivotObject.transform;

        pivot.position = center;
        pivot.rotation = transform.rotation;

        // Keep the sword in the same world position.
        transform.SetParent(pivot, true);

        // Place the sword's center at the spawn position.
        pivot.position = spawnPosition;

        isFlying = true;
    }

    private void Update()
    {
        if (pivot == null || !hasLaunched)
            return;

        float deltaTime = Time.deltaTime;

        // Spin continuously during flight and after stopping.
        pivot.Rotate(
            spinAxis.normalized,
            spinSpeed * deltaTime,
            Space.Self
        );

        if (!isFlying)
            return;

        // Stop flying after the configured duration.
        float remainingTime = flyDuration - elapsed;
        float flightStep = Mathf.Min(deltaTime, remainingTime);

        if (flightStep <= 0f)
        {
            isFlying = false;
            return;
        }

        // Travel forward while checking for walls.
        float distance = flySpeed * flightStep;
        Vector3 position = pivot.position;
        float remainingDistance = distance;

        int bounceCount = 0;

        while (remainingDistance > 0.001f && bounceCount < 5)
        {
            RaycastHit hit;

            if (FindWallHit(
                position,
                flyDirection,
                remainingDistance,
                out hit))
            {
                // Move to the wall and reflect the direction.
                float traveled = Mathf.Max(
                    0f,
                    hit.distance - 0.001f
                );

                position += flyDirection * traveled;
                remainingDistance -= traveled;

                flyDirection = Vector3.Reflect(
                    flyDirection,
                    hit.normal
                ).normalized;

                // Push slightly away from the wall.
                position += hit.normal * 0.01f;

                bounceCount++;
            }
            else
            {
                position += flyDirection * remainingDistance;
                remainingDistance = 0f;
            }
        }

        pivot.position = position;
        elapsed += flightStep;

        if (elapsed >= flyDuration)
        {
            isFlying = false;
        }
    }

    private bool FindWallHit(
        Vector3 origin,
        Vector3 direction,
        float distance,
        out RaycastHit closestHit)
    {
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            bounceRadius,
            direction,
            distance,
            wallLayers,
            QueryTriggerInteraction.Ignore
        );

        closestHit = new RaycastHit();
        bool foundHit = false;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;

            // Ignore the sword's own colliders.
            if (hitTransform == pivot ||
                hitTransform.IsChildOf(pivot))
            {
                continue;
            }

            // Ignore the player who threw the sword.
            if (owner != null &&
                (hitTransform == owner ||
                 hitTransform.IsChildOf(owner)))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                foundHit = true;
            }
        }

        return foundHit;
    }
}