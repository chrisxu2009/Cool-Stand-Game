using UnityEngine;

public class SwordThrowController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject heldSword;
    public GameObject thrownSwordPrefab;

    [Header("Spawn Settings")]
    public float spawnDistance = 0.8f;

    private bool hasThrown;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G) && !hasThrown)
        {
            ThrowSword();
        }
    }

    private void ThrowSword()
    {
        if (playerCamera == null ||
            heldSword == null ||
            thrownSwordPrefab == null)
        {
            Debug.LogError(
                "Assign the camera, held sword, and thrown sword prefab."
            );
            return;
        }

        Vector3 direction = playerCamera.transform.forward;

        // Spawn from the center of the camera, slightly forward.
        Vector3 spawnPosition =
            playerCamera.transform.position +
            direction * spawnDistance;

        GameObject sword = Instantiate(
            thrownSwordPrefab,
            spawnPosition,
            thrownSwordPrefab.transform.rotation
        );

        ThrownSwordProjectile projectile =
            sword.GetComponent<ThrownSwordProjectile>();

        if (projectile == null)
        {
            Debug.LogError(
                "ThrownSword prefab needs ThrownSwordProjectile.cs."
            );

            Destroy(sword);
            return;
        }

        // Hide the sword still attached to the player's hand.
        heldSword.SetActive(false);

        projectile.owner = transform.root;
        projectile.Launch(direction, spawnPosition);

        hasThrown = true;
    }
}
