using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenFractureController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Material fractureMaterial;

    [Header("Teleport")]
    [SerializeField] private float teleportDistance = 10f;
    [SerializeField] private float teleportDelay = 0.07f;

    [Header("Shatter")]
    [SerializeField] private float effectDuration = 0.5f;
    [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 3;
    [SerializeField] private float gridJitter = 0.08f;

    private GameObject overlayObject;
    private MeshFilter shardMeshFilter;
    private MeshRenderer shardRenderer;
    private Material runtimeMaterial;
    private RenderTexture capturedFrame;

    private bool isShattering;

    private static readonly int AmountID =
        Shader.PropertyToID("_Amount");

    private static readonly int CapturedFrameID =
        Shader.PropertyToID("_CapturedFrame");

    private void Awake()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (
            gameplayCamera == null ||
            player == null ||
            fractureMaterial == null
        )
        {
            Debug.LogError(
                "Assign Player, Gameplay Camera, and Fracture Material."
            );

            enabled = false;
            return;
        }

        CreateOverlay();
    }

    private void Update()
    {
        if (
            Input.GetKeyDown(KeyCode.R) &&
            !isShattering
        )
        {
            StartCoroutine(ShatterAndTeleport());
        }
    }

    private void CreateOverlay()
    {
        overlayObject =
            new GameObject("Frozen Screen Shards");

        overlayObject.transform.SetParent(
            gameplayCamera.transform,
            false
        );

        overlayObject.transform.localPosition =
            new Vector3(0f, 0f, 1f);

        shardMeshFilter =
            overlayObject.AddComponent<MeshFilter>();

        shardRenderer =
            overlayObject.AddComponent<MeshRenderer>();

        runtimeMaterial =
            new Material(fractureMaterial);

        shardRenderer.sharedMaterial =
            runtimeMaterial;

        shardRenderer.shadowCastingMode =
            ShadowCastingMode.Off;

        shardRenderer.receiveShadows = false;
        shardRenderer.enabled = false;
    }

    private IEnumerator ShatterAndTeleport()
    {
        isShattering = true;
        shardRenderer.enabled = false;

        // Wait until the current frame is completely rendered.
        // Temporarily capture the scene without post-processing.
        // The panes will receive the Volume normally when rendered.
        UniversalAdditionalCameraData cameraData =
            gameplayCamera.GetUniversalAdditionalCameraData();

        bool postProcessingWasEnabled =
            cameraData.renderPostProcessing;

        cameraData.renderPostProcessing = false;

        yield return new WaitForEndOfFrame();

        CreateCaptureTexture();

        ScreenCapture.CaptureScreenshotIntoRenderTexture(
            capturedFrame
        );

        // Restore post-processing for the following frame.
        cameraData.renderPostProcessing =
            postProcessingWasEnabled;

        BuildShardMesh();

        runtimeMaterial.SetTexture(
            CapturedFrameID,
            capturedFrame
        );

        runtimeMaterial.SetFloat(AmountID, 0f);
        shardRenderer.enabled = true;

        float elapsed = 0f;
        bool hasTeleported = false;

        while (elapsed < effectDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (
                !hasTeleported &&
                elapsed >= teleportDelay
            )
            {
                TeleportPlayer();
                hasTeleported = true;
            }

            float progress =
                Mathf.Clamp01(elapsed / effectDuration);

            runtimeMaterial.SetFloat(
                AmountID,
                progress
            );

            yield return null;
        }

        if (!hasTeleported)
            TeleportPlayer();

        runtimeMaterial.SetFloat(AmountID, 0f);
        shardRenderer.enabled = false;
        isShattering = false;
    }

    private void CreateCaptureTexture()
    {
        if (
            capturedFrame != null &&
            capturedFrame.width == Screen.width &&
            capturedFrame.height == Screen.height
        )
        {
            return;
        }

        if (capturedFrame != null)
        {
            capturedFrame.Release();
            Destroy(capturedFrame);
        }

        capturedFrame = new RenderTexture(
    Screen.width,
    Screen.height,
    0,
    RenderTextureFormat.ARGB32,
    RenderTextureReadWrite.sRGB
);

        capturedFrame.filterMode = FilterMode.Bilinear;
        capturedFrame.wrapMode = TextureWrapMode.Clamp;
        capturedFrame.Create();
    }

    private void BuildShardMesh()
    {
        if (shardMeshFilter.sharedMesh != null)
            Destroy(shardMeshFilter.sharedMesh);

        Vector2[,] grid =
            new Vector2[columns + 1, rows + 1];

        for (int y = 0; y <= rows; y++)
        {
            for (int x = 0; x <= columns; x++)
            {
                float horizontal =
                    (float)x / columns;

                float vertical =
                    (float)y / rows;

                if (x > 0 && x < columns)
                {
                    horizontal += Random.Range(
                        -gridJitter,
                        gridJitter
                    );
                }

                if (y > 0 && y < rows)
                {
                    vertical += Random.Range(
                        -gridJitter,
                        gridJitter
                    );
                }

                grid[x, y] =
                    new Vector2(horizontal, vertical);
            }
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> textureCoordinates = new List<Vector2>();
        List<Vector4> motions = new List<Vector4>();
        List<Vector2> pivots = new List<Vector2>();
        List<Color> colors = new List<Color>();
        List<int> triangles = new List<int>();

        void AddShardTriangle(
            Vector2 first,
            Vector2 second,
            Vector2 third
        )
        {
            Vector2 pivotUV =
                (first + second + third) / 3f;

            Vector2 pivotClip =
                pivotUV * 2f - Vector2.one;

            Vector2 outward =
                pivotUV - new Vector2(0.5f, 0.5f);

            if (outward.sqrMagnitude < 0.001f)
                outward = Random.insideUnitCircle;

            outward.Normalize();

            Vector2 randomDirection =
                Random.insideUnitCircle.normalized;

            Vector2 direction =
                (
                    outward * 0.7f +
                    randomDirection * 0.8f
                ).normalized;

            float rotation =
                Random.Range(-1f, 1f);

            float delay =
                Random.Range(0f, 0.12f);

            float brightness =
                Random.Range(0.85f, 1.15f);

            Vector4 motion = new Vector4(
                direction.x,
                direction.y,
                rotation,
                delay
            );

            int firstIndex = vertices.Count;

            Vector2[] trianglePoints =
            {
                first,
                second,
                third
            };

            Color[] barycentricColors =
            {
                new Color(1f, 0f, 0f, brightness),
                new Color(0f, 1f, 0f, brightness),
                new Color(0f, 0f, 1f, brightness)
            };

            for (int index = 0; index < 3; index++)
            {
                Vector2 uv = trianglePoints[index];

                Vector2 clipPosition =
                    uv * 2f - Vector2.one;

                vertices.Add(
                    new Vector3(
                        clipPosition.x,
                        clipPosition.y,
                        0f
                    )
                );

                textureCoordinates.Add(uv);
                motions.Add(motion);
                pivots.Add(pivotClip);
                colors.Add(barycentricColors[index]);
            }

            triangles.Add(firstIndex);
            triangles.Add(firstIndex + 1);
            triangles.Add(firstIndex + 2);
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector2 bottomLeft = grid[x, y];
                Vector2 bottomRight = grid[x + 1, y];
                Vector2 topLeft = grid[x, y + 1];
                Vector2 topRight = grid[x + 1, y + 1];

                if (Random.value > 0.5f)
                {
                    AddShardTriangle(
                        bottomLeft,
                        bottomRight,
                        topRight
                    );

                    AddShardTriangle(
                        bottomLeft,
                        topRight,
                        topLeft
                    );
                }
                else
                {
                    AddShardTriangle(
                        bottomLeft,
                        bottomRight,
                        topLeft
                    );

                    AddShardTriangle(
                        bottomRight,
                        topRight,
                        topLeft
                    );
                }
            }
        }

        Mesh shardMesh = new Mesh
        {
            name = "Generated Screen Shards"
        };

        shardMesh.SetVertices(vertices);
        shardMesh.SetUVs(0, textureCoordinates);
        shardMesh.SetUVs(1, motions);
        shardMesh.SetUVs(2, pivots);
        shardMesh.SetColors(colors);
        shardMesh.SetTriangles(triangles, 0);

        // Prevent camera frustum culling while shards move.
        shardMesh.bounds = new Bounds(
            Vector3.zero,
            Vector3.one * 10000f
        );

        shardMeshFilter.sharedMesh = shardMesh;
    }

    private void TeleportPlayer()
    {
        Vector3 direction =
            gameplayCamera.transform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = player.forward;

        direction.Normalize();

        Vector3 destination =
            player.position +
            direction * teleportDistance;

        Rigidbody playerRigidbody =
            player.GetComponent<Rigidbody>();

        if (playerRigidbody != null)
        {
            playerRigidbody.position = destination;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
        else
        {
            player.position = destination;
        }
    }

    private void OnDisable()
    {
        if (shardRenderer != null)
            shardRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (capturedFrame != null)
        {
            capturedFrame.Release();
            Destroy(capturedFrame);
        }

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);

        if (
            shardMeshFilter != null &&
            shardMeshFilter.sharedMesh != null
        )
        {
            Destroy(shardMeshFilter.sharedMesh);
        }
    }
}