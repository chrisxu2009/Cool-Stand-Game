using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonAnimationController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Player Movement")]
    [SerializeField] private RigidbodyPlayerMovement playerMovement;

    [Header("Sword Slash Combo Settings")]
    [Tooltip("Time window after an attack finishes during which the player can trigger the next hit.")]
    [SerializeField] private float comboResetTime = 1.0f;

    [Header("Animation Transition")]
    [SerializeField] private float transitionDuration = 0.03f;

    [Header("Animator Layer Indices")]
    [SerializeField] private int leftHandLayerIndex = 1;
    [SerializeField] private int rightHandLayerIndex = 2;

    [Header("Animation State Names")]
    [SerializeField] private string attack1State = "SwordSlash1Unity";
    [SerializeField] private string attack2State = "SwordStab";
    [SerializeField] private string attack3State = "SwordSlash3";

    // Combo State Tracking
    private int currentComboStep = 0; // 0 = ready, 1 = Slash 1, 2 = Stab, 3 = Slash 3
    private float lastAttackFinishTime = -999f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponentInParent<RigidbodyPlayerMovement>();
        }
    }

    private void Update()
    {
        if (Mouse.current == null || Keyboard.current == null || animator == null)
        {
            return;
        }

        HandleSwordCharge();

        if (!Keyboard.current.eKey.isPressed)
        {
            HandleSwordSlashCombo();
            HandleParry();
            HandleFingerAnimations();
        }

        UpdateComboResetTimer();
    }

    private void HandleSwordCharge()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            PlayAnimation("SwordCharge", leftHandLayerIndex);
            ResetSlashCombo();
        }

        if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            PlayAnimation("SwordChargeSlash", leftHandLayerIndex);

            if (playerMovement != null)
            {
                playerMovement.BeginSwordDash();
            }
        }
    }

    private void HandleSwordSlashCombo()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(leftHandLayerIndex);

        // Ignore clicks if the current attack hasn't reached 70% completion
        if (IsAttackingState(stateInfo) && stateInfo.normalizedTime < 0.70f)
        {
            return;
        }

        // Execute next attack in the 3-step sequence
        if (currentComboStep == 0 || currentComboStep >= 3)
        {
            PlayAnimation(attack1State, leftHandLayerIndex);
            currentComboStep = 1;
        }
        else if (currentComboStep == 1)
        {
            PlayAnimation(attack2State, leftHandLayerIndex);
            currentComboStep = 2;
        }
        else if (currentComboStep == 2)
        {
            PlayAnimation(attack3State, leftHandLayerIndex);
            currentComboStep = 3;
        }

        lastAttackFinishTime = Time.time;
    }

    private void UpdateComboResetTimer()
    {
        if (currentComboStep == 0) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(leftHandLayerIndex);

        // Reset if step 3 finished completely or if timeout elapsed
        if (currentComboStep == 3 && stateInfo.IsName(attack3State) && stateInfo.normalizedTime >= 1.0f)
        {
            ResetSlashCombo();
            return;
        }

        if (stateInfo.normalizedTime >= 1.0f && (Time.time - lastAttackFinishTime > comboResetTime))
        {
            ResetSlashCombo();
        }
    }

    private bool IsAttackingState(AnimatorStateInfo stateInfo)
    {
        return stateInfo.IsName(attack1State) ||
               stateInfo.IsName(attack2State) ||
               stateInfo.IsName(attack3State);
    }

    private void HandleParry()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame)
        {
            return;
        }

        PlayAnimation("parry variation1", leftHandLayerIndex);
        ResetSlashCombo();
    }

    private void HandleFingerAnimations()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            PlayAnimation("IndexFinger_Full", rightHandLayerIndex);
            ResetSlashCombo();
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            PlayAnimation("Middlefinger extend", rightHandLayerIndex);
            ResetSlashCombo();
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            PlayAnimation("Ringfinger extend", rightHandLayerIndex);
            ResetSlashCombo();
        }
        else if (Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame)
        {
            PlayAnimation("pinkyfinger extend", rightHandLayerIndex);
            ResetSlashCombo();
        }
    }

    private void ResetSlashCombo()
    {
        currentComboStep = 0;
        lastAttackFinishTime = -999f;
    }

    private void PlayAnimation(string stateName, int layerIndex)
    {
        int stateHash = Animator.StringToHash(stateName);

        if (animator.HasState(layerIndex, stateHash))
        {
            animator.CrossFadeInFixedTime(stateName, transitionDuration, layerIndex, 0f);
        }
        else
        {
            Debug.LogError($"Animator Controller is missing state '{stateName}' on layer index {layerIndex}!");
        }
    }
}