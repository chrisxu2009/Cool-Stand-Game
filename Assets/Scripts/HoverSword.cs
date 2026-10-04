using UnityEngine;

public class HoverSword : MonoBehaviour
{
    [Header("Movement Settings")]
    public float travelDistance = 5f;    // Flies exactly 5 meters straight ahead
    public float travelSpeed = 22f;      // Travel speed

    /* SPINNING DISABLED FOR NOW
    [Header("Center Spin Settings")]
    public Vector3 rotationAxis = new Vector3(1f, 0f, 0f);
    public float spinSpeed = 1200f;
    */

    private Transform meshTransform;
    private Vector3 targetPosition;
    private bool reachedTarget = false;
    private bool isReturning = false;
    private Transform returnTarget;
    private float returnSpeed = 35f;

    void Awake()
    {
        // Auto-detect mesh center and shift local position to eliminate pivot offsets
        MeshFilter mf = GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            meshTransform = mf.transform;
            meshTransform.localPosition = -Vector3.Scale(mf.sharedMesh.bounds.center, meshTransform.localScale);
        }
        else
        {
            SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null)
            {
                meshTransform = smr.transform;
                meshTransform.localPosition = -Vector3.Scale(smr.sharedMesh.bounds.center, meshTransform.localScale);
            }
        }

        if (meshTransform == null)
        {
            meshTransform = transform;
        }
    }

    public void Initialize(Vector3 startPos, Vector3 facingDirection)
    {
        transform.position = startPos;
        transform.forward = facingDirection;
        targetPosition = startPos + (facingDirection * travelDistance);
    }

    void Update()
    {
        // 1. Recall state back to player
        if (isReturning)
        {
            if (returnTarget == null) return;

            transform.position = Vector3.MoveTowards(transform.position, returnTarget.position, returnSpeed * Time.deltaTime);

            // SpinMesh(); // <--- Spin commented out

            if (Vector3.Distance(transform.position, returnTarget.position) < 1.0f)
            {
                SwordThrower thrower = returnTarget.GetComponentInParent<SwordThrower>();
                if (thrower != null) thrower.PickupSword();
                Destroy(gameObject);
            }
            return;
        }

        // 2. Fly straight forward in character facing direction
        if (!reachedTarget)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, travelSpeed * Time.deltaTime);

            // SpinMesh(); // <--- Spin commented out

            if (Vector3.Distance(transform.position, targetPosition) < 0.001f)
            {
                transform.position = targetPosition; // Hard stop at 5 meters
                reachedTarget = true;
            }
        }
        // 3. Hover motionless in place
        else
        {
            // SpinMesh(); // <--- Spin commented out
        }
    }

    /* SPIN METHOD COMMENTED OUT
    void SpinMesh()
    {
        if (meshTransform != null)
        {
            meshTransform.Rotate(rotationAxis * (spinSpeed * Time.deltaTime), Space.Self);
        }
    }
    */

    public void StartRecall(Transform playerTransform)
    {
        isReturning = true;
        returnTarget = playerTransform;
    }
}