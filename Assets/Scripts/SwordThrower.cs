using UnityEngine;

public class SwordThrower : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;     // Main Camera
    public GameObject viewmodelSword;     // Sword_Zepelli_Hand[cite: 1]
    public GameObject thrownSwordPrefab;  // Prefab with HoverSword.cs attached

    [Header("Controls")]
    public KeyCode throwOrRecallKey = KeyCode.Mouse1; // Right Click

    private GameObject activeSword;
    private HoverSword activeHoverScript;

    void Update()
    {
        if (Input.GetKeyDown(throwOrRecallKey))
        {
            if (activeSword == null)
            {
                ThrowSword();
            }
            else
            {
                RecallSword();
            }
        }
    }

    void ThrowSword()
    {
        if (viewmodelSword != null) viewmodelSword.SetActive(false);

        // Get exact facing direction from camera crosshair
        Vector3 facingDir = cameraTransform.forward;
        Vector3 spawnPos = cameraTransform.position + (facingDir * 0.8f);

        activeSword = Instantiate(thrownSwordPrefab, spawnPos, Quaternion.LookRotation(facingDir));
        activeHoverScript = activeSword.GetComponent<HoverSword>();

        if (activeHoverScript != null)
        {
            activeHoverScript.Initialize(spawnPos, facingDir);
        }
    }

    void RecallSword()
    {
        if (activeHoverScript != null)
        {
            activeHoverScript.StartRecall(cameraTransform);
        }
    }

    public void PickupSword()
    {
        if (viewmodelSword != null) viewmodelSword.SetActive(true);
        activeSword = null;
        activeHoverScript = null;
    }
}