using UnityEngine;

namespace CrashClimb
{
    public class CrashClimbProceduralMap2D : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] private int platformCount = 32;
        [SerializeField] private float verticalSpacing = 1.65f;
        [SerializeField] private float towerHalfWidth = 4.4f;
        [SerializeField] private Vector2 platformSize = new Vector2(2.8f, 0.34f);
        [SerializeField] private bool buildOnStart = true;

        [Header("Player")]
        [SerializeField] private CrashClimbPlayerController2D playerPrefab;
        [SerializeField] private Vector2 playerSpawn = new Vector2(0f, 1.15f);

        [Header("Visuals")]
        [SerializeField] private Material platformMaterial;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Color backgroundColor = new Color(0.06f, 0.075f, 0.1f);

        private static readonly Color StoneColor = new Color(0.48f, 0.5f, 0.55f);
        private static readonly Color IceColor = new Color(0.35f, 0.85f, 1f);
        private static readonly Color GlueColor = new Color(0.48f, 0.82f, 0.28f);
        private static readonly Color CrystalColor = new Color(0.75f, 0.32f, 1f);
        private static readonly Color FragileColor = new Color(0.72f, 0.46f, 0.28f);
        private static readonly Color SpikeColor = new Color(0.92f, 0.12f, 0.16f);

        private void OnValidate()
        {
            verticalSpacing = Mathf.Clamp(verticalSpacing, 1.35f, 2.2f);
            towerHalfWidth = Mathf.Clamp(towerHalfWidth, 3.6f, 5.5f);
            platformSize.x = Mathf.Clamp(platformSize.x, 2.2f, 4f);
            platformSize.y = Mathf.Clamp(platformSize.y, 0.25f, 0.6f);
        }

        private void Start()
        {
            if (buildOnStart)
            {
                Build();
            }
        }

        [ContextMenu("Build Crash & Climb Map")]
        public void Build()
        {
            ClearGeneratedChildren();
            SetupCamera();
            CreateTowerWalls();
            CreatePlatform("Spawn Platform", new Vector2(0f, 0f), new Vector2(7f, 0.5f), CrashClimbSurfaceKind.Stone);

            for (int i = 1; i <= platformCount; i++)
            {
                float progress = i / (float)platformCount;
                float y = i * verticalSpacing;
                float sideBias = Mathf.Sin(i * 1.73f) * (towerHalfWidth - 1.4f);
                float width = Mathf.Lerp(platformSize.x + 0.8f, platformSize.x - 0.45f, progress);
                Vector2 size = new Vector2(width, platformSize.y);
                CrashClimbSurfaceKind kind = PickSurfaceKind(i);
                CreatePlatform($"Platform {i:00} - {kind}", new Vector2(sideBias, y), size, kind);

                if (ShouldPlaceSpike(i))
                {
                    float spikeXOffset = Mathf.Sign(Mathf.Sin(i * 2.31f)) * Mathf.Max(0.15f, width * 0.22f);
                    CreateSpike($"Spike {i:00}", new Vector2(sideBias + spikeXOffset, y + platformSize.y * 0.5f + 0.28f));
                }

                if (i % 5 == 0)
                {
                    float oppositeX = -Mathf.Sign(sideBias == 0f ? 1f : sideBias) * (towerHalfWidth - 1.2f);
                    CreatePlatform($"Side Recovery {i:00}", new Vector2(oppositeX, y - 0.9f), new Vector2(1.35f, platformSize.y), CrashClimbSurfaceKind.Stone);
                }
            }

            CreatePlatform("Goal", new Vector2(0f, (platformCount + 1) * verticalSpacing), new Vector2(5f, 0.5f), CrashClimbSurfaceKind.Crystal);
            CrashClimbPlayerController2D player = CreatePlayer();
            AttachCamera(player);
        }

        private void ClearGeneratedChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void SetupCamera()
        {
            sceneCamera = sceneCamera != null ? sceneCamera : Camera.main;
            if (sceneCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                sceneCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = 6f;
            sceneCamera.backgroundColor = backgroundColor;
            sceneCamera.transform.position = new Vector3(0f, 2.5f, -10f);
        }

        private CrashClimbPlayerController2D CreatePlayer()
        {
            if (playerPrefab != null)
            {
                return Instantiate(playerPrefab, playerSpawn, Quaternion.identity);
            }

            GameObject player = new GameObject("Crash Player");
            player.transform.position = playerSpawn;
            player.transform.localScale = new Vector3(0.8f, 1.1f, 1f);

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateUnitSprite();
            renderer.color = new Color(1f, 0.86f, 0.28f);
            renderer.sortingOrder = 10;

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            GameObject attackPoint = new GameObject("Attack Point");
            attackPoint.transform.SetParent(player.transform);
            attackPoint.transform.localPosition = new Vector3(0.85f, 0f, 0f);

            CrashClimbPlayerController2D controller = player.AddComponent<CrashClimbPlayerController2D>();
            return controller;
        }

        private void AttachCamera(CrashClimbPlayerController2D player)
        {
            if (sceneCamera == null || player == null)
            {
                return;
            }

            CrashClimbCameraFollow2D follow = sceneCamera.GetComponent<CrashClimbCameraFollow2D>();
            if (follow == null)
            {
                follow = sceneCamera.gameObject.AddComponent<CrashClimbCameraFollow2D>();
            }

            follow.SetTarget(player.transform);
        }

        private void CreateTowerWalls()
        {
            float height = (platformCount + 3) * verticalSpacing;
            CreatePlatform("Left Wall", new Vector2(-towerHalfWidth - 0.55f, height * 0.5f), new Vector2(0.55f, height), CrashClimbSurfaceKind.Stone);
            CreatePlatform("Right Wall", new Vector2(towerHalfWidth + 0.55f, height * 0.5f), new Vector2(0.55f, height), CrashClimbSurfaceKind.Stone);
        }

        private void CreatePlatform(string objectName, Vector2 position, Vector2 size, CrashClimbSurfaceKind kind)
        {
            GameObject platform = new GameObject(objectName);
            platform.transform.SetParent(transform);
            platform.transform.position = position;
            platform.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateUnitSprite();
            renderer.color = GetSurfaceColor(kind);
            if (platformMaterial != null)
            {
                renderer.material = platformMaterial;
            }

            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            CrashClimbSurface2D surface = platform.AddComponent<CrashClimbSurface2D>();
            surface.Configure(kind);
        }

        private void CreateSpike(string objectName, Vector2 position)
        {
            GameObject spike = new GameObject(objectName);
            spike.transform.SetParent(transform);
            spike.transform.position = position;

            SpriteRenderer renderer = spike.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateTriangleSprite();
            renderer.color = SpikeColor;
            renderer.sortingOrder = 5;

            PolygonCollider2D collider = spike.AddComponent<PolygonCollider2D>();
            collider.points = new[]
            {
                new Vector2(-0.42f, -0.28f),
                new Vector2(0f, 0.42f),
                new Vector2(0.42f, -0.28f)
            };
            collider.isTrigger = true;

            spike.AddComponent<CrashClimbSpikeHazard2D>();
        }

        private bool ShouldPlaceSpike(int index)
        {
            return index >= 4 && (index % 4 == 0 || index % 9 == 0);
        }

        private CrashClimbSurfaceKind PickSurfaceKind(int index)
        {
            if (index % 11 == 0)
            {
                return CrashClimbSurfaceKind.Crystal;
            }

            if (index % 7 == 0)
            {
                return CrashClimbSurfaceKind.FragileRock;
            }

            if (index % 5 == 0)
            {
                return CrashClimbSurfaceKind.Glue;
            }

            if (index % 3 == 0)
            {
                return CrashClimbSurfaceKind.Ice;
            }

            return CrashClimbSurfaceKind.Stone;
        }

        private Color GetSurfaceColor(CrashClimbSurfaceKind kind)
        {
            switch (kind)
            {
                case CrashClimbSurfaceKind.Ice:
                    return IceColor;
                case CrashClimbSurfaceKind.Glue:
                    return GlueColor;
                case CrashClimbSurfaceKind.Crystal:
                    return CrystalColor;
                case CrashClimbSurfaceKind.FragileRock:
                    return FragileColor;
                default:
                    return StoneColor;
            }
        }

        private Sprite CreateUnitSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private Sprite CreateTriangleSprite()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;
            Color clear = new Color(1f, 1f, 1f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedY = y / (float)(size - 1);
                    float halfWidth = Mathf.Lerp(0.5f, 0f, normalizedY);
                    float centeredX = Mathf.Abs((x / (float)(size - 1)) - 0.5f);
                    texture.SetPixel(x, y, centeredX <= halfWidth ? Color.white : clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.28f), size);
        }
    }
}
