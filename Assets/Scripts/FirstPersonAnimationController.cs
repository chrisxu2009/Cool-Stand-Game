using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonAnimationController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Player Movement")]
    [SerializeField] private RigidbodyPlayerMovement playerMovement;

    [Header("Sword Slash Combo")]
    [SerializeField] private float comboWindow = 0.5f;

    [Header("Animation Transition")]
    [SerializeField] private float transitionDuration = 0.03f;

    [Header("Animator Layer Indices")]
    [SerializeField] private int leftHandLayerIndex = 1;
    [SerializeField] private int rightHandLayerIndex = 2;

    private float lastLeftClickTime = -999f;
    private bool waitingForSecondSlash;

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
            HandleSwordSlash();
            HandleParry();
            HandleFingerAnimations();
        }

        UpdateComboTimer();
    }

    private void HandleSwordCharge()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            animator.CrossFadeInFixedTime("SwordCharge", 0f, leftHandLayerIndex, 0f);
            ResetSlashCombo();
        }

        if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            animator.CrossFadeInFixedTime("SwordChargeSlash", 0f, leftHandLayerIndex, 0f);

            if (playerMovement != null)
            {
                playerMovement.BeginSwordDash();
            }
        }
    }

    private void HandleSwordSlash()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        float timeSinceLastClick = Time.time - lastLeftClickTime;

        if (waitingForSecondSlash && timeSinceLastClick <= comboWindow)
        {
            PlayAnimation("SwordSlash2Unity", leftHandLayerIndex);
            waitingForSecondSlash = false;
        }
        else
        {
            PlayAnimation("SwordSlash1Unity", leftHandLayerIndex);
            waitingForSecondSlash = true;
        }

        lastLeftClickTime = Time.time;
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
        if (Keyboard.current.digit1Key.wasPressedThisFrame ||
            Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            PlayAnimation("IndexFinger_Full", rightHandLayerIndex);
            ResetSlashCombo();
        }
        else if (
            Keyboard.current.digit2Key.wasPressedThisFrame ||
            Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            PlayAnimation("Middlefinger extend", rightHandLayerIndex);
            ResetSlashCombo();
        }
        else if (
            Keyboard.current.digit3Key.wasPressedThisFrame ||
            Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            PlayAnimation("Ringfinger extend", rightHandLayerIndex);
            ResetSlashCombo();
        }
        else if (
            Keyboard.current.digit4Key.wasPressedThisFrame ||
            Keyboard.current.numpad4Key.wasPressedThisFrame)
        {
            PlayAnimation("pinkyfinger extend", rightHandLayerIndex);
            ResetSlashCombo();
        }
    }

    private void UpdateComboTimer()
    {
        if (!waitingForSecondSlash)
        {
            return;
        }

        if (Time.time - lastLeftClickTime > comboWindow)
        {
            ResetSlashCombo();
        }
    }

    private void ResetSlashCombo()
    {
        waitingForSecondSlash = false;
        lastLeftClickTime = -999f;
    }

    private void PlayAnimation(string stateName, int layerIndex = 1)
    {
        animator.CrossFadeInFixedTime(
            stateName,
            transitionDuration,
            layerIndex,
            0f
        );
    }
}