using System.Collections.Generic;
using UnityEngine;

namespace CrashClimb
{
    public class CrashClimbProceduralMap2D : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] private int platformCount = 32;
        [SerializeField] private float verticalSpacing = 1.65f;
        [SerializeField] private float towerHalfWidth = 4.1f;
        [SerializeField] private Vector2 platformSize = new Vector2(2.8f, 0.34f);
        [SerializeField] private bool buildOnStart = true;

        [Header("Player")]
        [SerializeField] private CrashClimbPlayerController2D playerPrefab;
        [SerializeField] private Vector2 playerSpawn = new Vector2(0f, 1.15f);

        [Header("Visuals")]
        [SerializeField] private Material platformMaterial;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private Color backgroundColor = new Color(0.06f, 0.075f, 0.1f);
        [SerializeField] private bool useCraftpixSprites = true;

        private static readonly Color StoneColor = new Color(0.48f, 0.5f, 0.55f);
        private static readonly Color IceColor = new Color(0.35f, 0.85f, 1f);
        private static readonly Color GlueColor = new Color(0.48f, 0.82f, 0.28f);
        private static readonly Color CrystalColor = new Color(0.75f, 0.32f, 1f);
        private static readonly Color FragileColor = new Color(0.72f, 0.46f, 0.28f);
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

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
            CreateBackground();
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
            sceneCamera.orthographicSize = 5.3f;
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
            player.transform.localScale = new Vector3(1.05f, 1.05f, 1f);

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSprite("CrashClimb/Wraith_01/PNG Sequences/Idle/Wraith_01_Idle_000", 360f) ?? CreateUnitSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = 10;
            player.AddComponent<CrashClimbSpriteAnimator2D>();

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.58f, 0.92f);
            collider.offset = new Vector2(0f, -0.06f);

            GameObject attackPoint = new GameObject("Attack Point");
            attackPoint.transform.SetParent(player.transform);
            attackPoint.transform.localPosition = new Vector3(0.62f, 0f, 0f);

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
            CreateWall("Left Wall", new Vector2(-towerHalfWidth - 0.25f, height * 0.5f), new Vector2(0.5f, height));
            CreateWall("Right Wall", new Vector2(towerHalfWidth + 0.25f, height * 0.5f), new Vector2(0.5f, height));
        }

        private void CreateWall(string objectName, Vector2 position, Vector2 size)
        {
            GameObject wall = new GameObject(objectName);
            wall.transform.SetParent(transform);
            wall.transform.position = position;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateWallSprite();
            renderer.sortingOrder = 4;

            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            CrashClimbSurface2D surface = wall.AddComponent<CrashClimbSurface2D>();
            surface.Configure(CrashClimbSurfaceKind.Stone);
        }

        private void CreateBackground()
        {
            if (!useCraftpixSprites)
            {
                return;
            }

            float height = (platformCount + 3) * verticalSpacing;
            float width = sceneCamera != null ? sceneCamera.orthographicSize * sceneCamera.aspect * 2f : towerHalfWidth * 2f + 2.4f;

            GameObject root = new GameObject("Craftpix Background");
            root.transform.SetParent(transform);
            root.transform.position = Vector3.zero;

            CreateBackgroundLayer(root.transform, "Sky", "CrashClimb/Background/Game_Background_1/Sky", width, height, -50);
            CreateBackgroundLayer(root.transform, "BackGround", "CrashClimb/Background/Game_Background_1/BackGround", width, height, -45);
            CreateBackgroundLayer(root.transform, "Clouds", "CrashClimb/Background/Game_Background_1/Clouds", width, height, -40);
            CreateBackgroundLayer(root.transform, "Decor", "CrashClimb/Background/Game_Background_1/Decor", width, height, -35);
            CreateBackgroundLayer(root.transform, "Sides", "CrashClimb/Background/Game_Background_1/Sides", width, height, -30);
            CreateStartGround(root.transform, width);
        }

        private void CreateBackgroundLayer(Transform parent, string objectName, string resourcePath, float targetWidth, float targetHeight, int sortingOrder)
        {
            Sprite sprite = GetSprite(resourcePath, 150f);
            if (sprite == null)
            {
                return;
            }

            GameObject layer = new GameObject(objectName);
            layer.transform.SetParent(parent);
            layer.transform.position = new Vector3(0f, targetHeight * 0.5f - 1.2f, 2f);
            layer.transform.localScale = new Vector3(targetWidth / sprite.bounds.size.x, targetHeight / sprite.bounds.size.y, 1f);

            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
        }

        private void CreateStartGround(Transform parent, float targetWidth)
        {
            Sprite sprite = GetSprite("CrashClimb/Background/Game_Background_1/Ground_Start", 150f);
            if (sprite == null)
            {
                return;
            }

            GameObject ground = new GameObject("Start Ground Art");
            ground.transform.SetParent(parent);
            ground.transform.position = new Vector3(0f, -0.45f, 1f);
            float scale = targetWidth / sprite.bounds.size.x;
            ground.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -8;
        }

        private void CreatePlatform(string objectName, Vector2 position, Vector2 size, CrashClimbSurfaceKind kind)
        {
            GameObject platform = new GameObject(objectName);
            platform.transform.SetParent(transform);
            platform.transform.position = position;

            SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
            bool isWall = objectName.Contains("Wall");
            Sprite platformSprite = useCraftpixSprites && !isWall ? GetPlatformSprite(kind) : null;
            renderer.sprite = platformSprite != null ? platformSprite : CreateUnitSprite();
            renderer.color = platformSprite != null ? GetSurfaceTint(kind) : GetSurfaceColor(kind);
            renderer.sortingOrder = isWall ? -6 : 2;
            if (platformMaterial != null)
            {
                renderer.material = platformMaterial;
            }

            Vector2 spriteSize = renderer.sprite.bounds.size;
            float visualHeight = platformSprite != null ? Mathf.Max(size.y, Mathf.Min(0.75f, size.x * spriteSize.y / spriteSize.x)) : size.y;
            platform.transform.localScale = new Vector3(size.x / spriteSize.x, visualHeight / spriteSize.y, 1f);

            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(spriteSize.x, size.y / platform.transform.localScale.y);

            CrashClimbSurface2D surface = platform.AddComponent<CrashClimbSurface2D>();
            surface.Configure(kind);
        }

        private void CreateSpike(string objectName, Vector2 position)
        {
            GameObject spike = new GameObject(objectName);
            spike.transform.SetParent(transform);
            spike.transform.position = position;

            SpriteRenderer renderer = spike.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSprite("CrashClimb/Pads/Pad_2_2", 354f) ?? CreateUnitSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = 5;
            Vector2 spriteSize = renderer.sprite.bounds.size;
            spike.transform.localScale = new Vector3(1.28f / spriteSize.x, 0.62f / spriteSize.y, 1f);

            PolygonCollider2D collider = spike.AddComponent<PolygonCollider2D>();
            collider.points = new[]
            {
                new Vector2(-0.5f, -0.24f),
                new Vector2(-0.35f, 0.24f),
                new Vector2(-0.16f, -0.24f),
                new Vector2(0f, 0.32f),
                new Vector2(0.18f, -0.24f),
                new Vector2(0.36f, 0.24f),
                new Vector2(0.5f, -0.24f)
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

        private Color GetSurfaceTint(CrashClimbSurfaceKind kind)
        {
            switch (kind)
            {
                case CrashClimbSurfaceKind.Ice:
                    return new Color(0.76f, 0.94f, 1f);
                case CrashClimbSurfaceKind.Glue:
                    return new Color(0.68f, 0.94f, 0.5f);
                case CrashClimbSurfaceKind.Crystal:
                    return new Color(0.9f, 0.7f, 1f);
                case CrashClimbSurfaceKind.FragileRock:
                    return new Color(1f, 0.78f, 0.56f);
                default:
                    return Color.white;
            }
        }

        private Sprite GetPlatformSprite(CrashClimbSurfaceKind kind)
        {
            switch (kind)
            {
                case CrashClimbSurfaceKind.Ice:
                    return GetSprite("CrashClimb/Pads/Pad_2_1", 354f);
                case CrashClimbSurfaceKind.Glue:
                    return GetSprite("CrashClimb/Pads/Pad_3_2", 397f);
                case CrashClimbSurfaceKind.Crystal:
                    return GetSprite("CrashClimb/Pads/Pad_4_1", 395f);
                case CrashClimbSurfaceKind.FragileRock:
                    return GetSprite("CrashClimb/Pads/Pad_1_2", 394f);
                default:
                    return GetSprite("CrashClimb/Pads/Pad_1_1", 394f);
            }
        }

        private Sprite GetSprite(string resourcePath, float pixelsPerUnit)
        {
            string cacheKey = $"{resourcePath}:{pixelsPerUnit}";
            if (spriteCache.TryGetValue(cacheKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        private Sprite CreateUnitSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private Sprite CreateWallSprite()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size);
            texture.filterMode = FilterMode.Point;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float edge = x < 5 || x > size - 6 ? 0.18f : 0f;
                    float stripe = Mathf.Sin(y * 0.45f) * 0.04f;
                    texture.SetPixel(x, y, new Color(0.21f + edge + stripe, 0.15f + edge, 0.12f + edge, 1f));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
