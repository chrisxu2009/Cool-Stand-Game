using UnityEngine;

public class SwordChargeShake : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animator State & Layer")]
    [SerializeField] private string loopStateName = "SwordCharge_Loop";
    [Tooltip("Index of the Animator layer containing the state. (Base Layer = 0)")]
    [SerializeField] private int layerIndex = 1;
    [Tooltip("Optional: Name of the Animator layer. If set, overrides layerIndex.")]
    [SerializeField] private string layerName = "";

    [Header("Position Shake")]
    [SerializeField] private Vector3 positionAmount = new Vector3(0.008f, 0.006f, 0.004f);

    [Header("Rotation Shake")]
    [SerializeField] private Vector3 rotationAmount = new Vector3(1.2f, 0.8f, 1.5f);

    [Header("Shake Speed")]
    [SerializeField] private float shakeFrequency = 18f;

    [Header("Smooth Start And Stop")]
    [SerializeField] private float blendSpeed = 12f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private int loopStateHash;
    private float shakeStrength;
    private float noiseSeed;

    private void Awake()
    {
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;

        loopStateHash = Animator.StringToHash(loopStateName);
        noiseSeed = Random.Range(0f, 1000f);

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Target layer index by name if provided
        if (animator != null && !string.IsNullOrEmpty(layerName))
        {
            int foundIndex = animator.GetLayerIndex(layerName);
            if (foundIndex != -1)
            {
                layerIndex = foundIndex;
            }
        }
    }

    private void LateUpdate()
    {
        if (animator == null)
        {
            return;
        }

        // Check target layer state
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(layerIndex);
        bool shouldShake = currentState.shortNameHash == loopStateHash;

        // Check target layer transitions
        if (animator.IsInTransition(layerIndex))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(layerIndex);
            if (nextState.shortNameHash == loopStateHash)
            {
                shouldShake = true;
            }
        }

        float targetStrength = shouldShake ? 1f : 0f;

        shakeStrength = Mathf.MoveTowards(
            shakeStrength,
            targetStrength,
            blendSpeed * Time.deltaTime
        );

        ApplyShake();
    }

    private void ApplyShake()
    {
        float time = Time.time * shakeFrequency;

        Vector3 positionNoise = new Vector3(
            GetNoise(time, 0f),
            GetNoise(time, 17.3f),
            GetNoise(time, 34.6f)
        );

        Vector3 rotationNoise = new Vector3(
            GetNoise(time, 51.9f),
            GetNoise(time, 69.2f),
            GetNoise(time, 86.5f)
        );

        Vector3 positionOffset = Vector3.Scale(positionNoise, positionAmount);
        Vector3 rotationOffset = Vector3.Scale(rotationNoise, rotationAmount);

        transform.localPosition = originalPosition + positionOffset * shakeStrength;
        transform.localRotation = originalRotation * Quaternion.Euler(rotationOffset * shakeStrength);
    }

    private float GetNoise(float time, float offset)
    {
        return Mathf.PerlinNoise(noiseSeed + offset, time) * 2f - 1f;
    }

    private void OnDisable()
    {
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        shakeStrength = 0f;
    }
}