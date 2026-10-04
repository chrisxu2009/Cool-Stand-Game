using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StandController : MonoBehaviour
{
    [Header("Arm Objects")]
    [SerializeField] private GameObject defaultArmsObject;
    [SerializeField] private GameObject standArmsObject;

    [Header("Animator")]
    [SerializeField] private Animator standAnimator;
    [SerializeField] private Animator defaultArmsAnimator;

    [Header("Controls")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Q;
    [SerializeField] private KeyCode barrageKey = KeyCode.E;

    [Header("Animator Parameters")]
    [SerializeField] private string barrageBoolName = "IsBarraging";

    [Header("Post Processing")]
    [SerializeField] private Volume barrageVolume;

    [Header("SFX Text Prefab")]
    [SerializeField] private GameObject sfxTextPrefab;

    private bool isSummoned = false;
    private bool isBarraging = false;
    private ColorAdjustments colorAdjustments;
    private int lastColorPicker = -1;

    private void Start()
    {
        // Hide Stand arms and show default arms on spawn
        if (standArmsObject != null) standArmsObject.SetActive(false);
        if (defaultArmsObject != null) defaultArmsObject.SetActive(true);

        // Set up post-processing reference
        if (barrageVolume != null && barrageVolume.profile != null)
        {
            barrageVolume.profile.TryGet(out colorAdjustments);
            barrageVolume.weight = 0f; // Hide volume at start
        }
    }

    private void Update()
    {
        // Toggle Stand summon
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleSummon();
        }

        // Handle barrage input only when Stand is active
        if (isSummoned)
        {
            HandleBarrageInput();
        }
    }

    private void ToggleSummon()
    {
        isSummoned = !isSummoned;

        if (standArmsObject != null) standArmsObject.SetActive(isSummoned);
        if (defaultArmsObject != null) defaultArmsObject.SetActive(!isSummoned);

        if (isSummoned)
        {
            // Play the startup animation from the start when summoned
            if (standAnimator != null)
            {
                standAnimator.Play("idle startup RON", 0, 0f);
            }
        }
        else
        {
            if (isBarraging)
            {
                EndBarrage();
            }

            // Reset default arms animator to idle when desummoned
            if (defaultArmsAnimator != null)
            {
                defaultArmsAnimator.Play("idle", 0, 0f);
            }

            // Reset Stand animator parameters so it starts clean on next summon
            if (standAnimator != null)
            {
                standAnimator.SetBool(barrageBoolName, false);
                standAnimator.Play("idle", 0, 0f);
            }
        }
    }

    private void HandleBarrageInput()
    {
        if (Input.GetKeyDown(barrageKey))
        {
            StartBarrage();
        }
        else if (Input.GetKeyUp(barrageKey))
        {
            EndBarrage();
        }
    }

    public void StartBarrage()
    {
        isBarraging = true;

        if (standAnimator != null)
        {
            standAnimator.SetBool(barrageBoolName, true);
        }

        if (barrageVolume != null)
        {
            // Put this directly inside your update loop, button click, or trigger block
            if (colorAdjustments != null)
            {
                float[] hueValues = { 168f, -102f, 133.4f };
                int colorPicker;

                do
                {
                    colorPicker = Random.Range(0, hueValues.Length);
                }
                while (colorPicker == lastColorPicker);

                lastColorPicker = colorPicker;
                colorAdjustments.hueShift.value = hueValues[colorPicker];
            }

            // Snap effect ON instantly
            barrageVolume.weight = 1f;
        }
    }

    public void EndBarrage()
    {
        isBarraging = false;

        if (standAnimator != null)
        {
            standAnimator.SetBool(barrageBoolName, false);
        }

        if (barrageVolume != null)
        {
            // Snap effect OFF instantly
            barrageVolume.weight = 0f;
        }
    }
}