using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BarrageColorShift : MonoBehaviour
{
    public Volume barrageVolume;
    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        if (barrageVolume != null)
        {
            // Make a unique instance of the profile so we don't permanently edit project files
            barrageVolume.profile = Instantiate(barrageVolume.profile);

            if (!barrageVolume.profile.TryGet(out colorAdjustments))
            {
                Debug.LogWarning("No Color Adjustments override found on Barrage Volume!");
            }
        }
    }

    public void OnBarrageStart()
    {
        if (colorAdjustments != null && barrageVolume != null)
        {
            // 1. Pick a completely random Hue Shift between -180 and 180
            float newHue = Random.Range(-180f, 180f);
            colorAdjustments.hueShift.value = newHue;

            // Debug log to confirm it changes in the Console:
            Debug.Log("<color=cyan>[JoJo Shift]</color> New Hue Angle: " + newHue);

            // 2. Snap weight to 1 instantly for that crisp anime palette jump
            barrageVolume.weight = 1f;
        }
    }

    public void OnBarrageEnd()
    {
        if (barrageVolume != null)
        {
            // Snap or turn off instantly when barrage stops
            barrageVolume.weight = 0f;
        }
    }
}