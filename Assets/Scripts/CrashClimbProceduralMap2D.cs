using System.Collections.Generic;
using UnityEngine;

namespace CrashClimb
{
    public class CrashClimbProceduralMap2D : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] private int platformCount = 42;
        [SerializeField] private float verticalSpacing = 1.58f;
        [SerializeField] private float towerHalfWidth = 4.6f;
        [SerializeField] private Vector2 platformSize = new Vector2(2.65f, 0.34f);
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private int levelDesignVersion;

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
        private static readonly LevelZone[] LevelZones =
        {
            new LevelZone(1, 7, "Entrada de Pedra", CrashClimbSurfaceKind.Stone, CrashClimbSurfaceKind.Ice, 2.45f, 0),
            new LevelZone(8, 15, "Cornijas de Gelo", CrashClimbSurfaceKind.Ice, CrashClimbSurfaceKind.Stone, 3.05f, 4),
            new LevelZone(16, 23, "Passagem de Cola", CrashClimbSurfaceKind.Glue, CrashClimbSurfaceKind.FragileRock, 3.35f, 5),
            new LevelZone(24, 31, "Rochas Quebraveis", CrashClimbSurfaceKind.FragileRock, CrashClimbSurfaceKind.Stone, 3.55f, 4),
            new LevelZone(32, 38, "Subida de Cristal", CrashClimbSurfaceKind.Crystal, CrashClimbSurfaceKind.Ice, 3.2f, 3),
            new LevelZone(39, 42, "Topo Final", CrashClimbSurfaceKind.Stone, CrashClimbSurfaceKind.Crystal, 2.65f, 2)
        };
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private const int CurrentLevelDesignVersion = 5;

        private struct LevelZone
        {
            public readonly int Start;
            public readonly int End;
            public readonly string Name;
            public readonly CrashClimbSurfaceKind Primary;
            public readonly CrashClimbSurfaceKind Secondary;
            public readonly float MaxX;
            public readonly int SpikeEvery;

            public LevelZone(int start, int end, string name, CrashClimbSurfaceKind primary, CrashClimbSurfaceKind secondary, float maxX, int spikeEvery)
            {
                Start = start;
                End = end;
                Name = name;
                Primary = primary;
                Secondary = secondary;
                MaxX = maxX;
                SpikeEvery = spikeEvery;
            }
        }

        public float TotalHeight => (platformCount + 1) * verticalSpacing;
        public string GetZoneName(float worldY)
        {
            int platformIndex = Mathf.Clamp(Mathf.RoundToInt(worldY / Mathf.Max(0.01f, verticalSpacing)), 1, platformCount);
            return GetLevelZone(platformIndex).Name;
        }

        private void OnValidate()
        {
            verticalSpacing = Mathf.Clamp(verticalSpacing, 1.35f, 2.1f);
            towerHalfWidth = Mathf.Clamp(towerHalfWidth, 3.8f, 5.4f);
            platformSize.x = Mathf.Clamp(platformSize.x, 2.15f, 3.6f);
            platformSize.y = Mathf.Clamp(platformSize.y, 0.25f, 0.6f);
        }

        private void Start()
        {
            CrashClimbBootstrap2D.EnsureRuntimeObjects();

            if (!buildOnStart)
            {
                return;
            }

            if (levelDesignVersion != CurrentLevelDesignVersion)
            {
                ApplyDefaultFields();
                Build();
                return;
            }

            if (transform.childCount == 0)
            {
                Build();
            }
        }

        [ContextMenu("Build Crash & Climb Map")]
        public void Build()
        {
            levelDesignVersion = CurrentLevelDesignVersion;
            ClearGeneratedChildren();
            SetupCamera();
            CreateBackground();
            CreateTowerWalls();
            CreatePlatform("Spawn Platform", new Vector2(0f, 0f), new Vector2(7f, 0.5f), CrashClimbSurfaceKind.Stone);
            CreateZoneMarker("Area 1 - Entrada de Pedra", new Vector2(-towerHalfWidth + 0.55f, 1.15f), StoneColor);

            for (int i = 1; i <= platformCount; i++)
            {
                LevelZone zone = GetLevelZone(i);
                float progress = i / (float)platformCount;
                float y = i * verticalSpacing;
                float sideBias = GetPlatformX(i, zone);
                float width = Mathf.Lerp(platformSize.x + 0.55f, platformSize.x - 0.45f, progress);
                if (zone.Primary == CrashClimbSurfaceKind.FragileRock)
                {
                    width -= 0.25f;
                }

                Vector2 size = new Vector2(width, platformSize.y);
                CrashClimbSurfaceKind kind = PickSurfaceKind(i, zone);
                CreatePlatform($"Platform {i:00} - {kind}", new Vector2(sideBias, y), size, kind);

                if (i == zone.Start && i > 1)
                {
                    CreateZoneMarker($"Area {GetZoneNumber(i)} - {zone.Name}", new Vector2(-towerHalfWidth + 0.55f, y + 0.72f), GetSurfaceColor(zone.Primary));
                }

                if (ShouldPlaceSpike(i, zone))
                {
                    float spikeXOffset = Mathf.Sign(Mathf.Sin(i * 2.31f)) * Mathf.Max(0.15f, width * 0.22f);
                    CreateSpike($"Spike {i:00}", new Vector2(sideBias + spikeXOffset, y + platformSize.y * 0.5f + 0.28f));
                }

                if (i % 6 == 0)
                {
                    float oppositeX = -Mathf.Sign(sideBias == 0f ? 1f : sideBias) * (towerHalfWidth - 1.65f);
                    CreatePlatform($"Checkpoint Recovery {i:00}", new Vector2(oppositeX, y - 0.85f), new Vector2(1.55f, platformSize.y), CrashClimbSurfaceKind.Stone);
                }
            }

            Vector2 goalPosition = new Vector2(0f, TotalHeight);
            CreatePlatform("Goal - Pad_5_1", goalPosition, new Vector2(5f, 0.5f), CrashClimbSurfaceKind.Crystal, "CrashClimb/Pads/New/Pad_5_1");
            CreateGoalTrigger(goalPosition + Vector2.up * 0.85f);
            CrashClimbPlayerController2D player = CreatePlayer();
            AttachCamera(player);
        }

        [ContextMenu("Apply Platformer Map Defaults")]
        public void ApplyPlatformerMapDefaults()
        {
            ApplyDefaultFields();
            Build();
        }

        private void ApplyDefaultFields()
        {
            platformCount = 42;
            verticalSpacing = 1.58f;
            towerHalfWidth = 4.6f;
            platformSize = new Vector2(2.65f, 0.34f);
        }

        private float GetPlatformX(int index, LevelZone zone)
        {
            float maxX = Mathf.Min(towerHalfWidth - 1.35f, zone.MaxX);
            float wave = Mathf.Sin(index * 1.28f + GetZoneNumber(index) * 0.55f) * maxX;
            float correction = Mathf.Sin(index * 0.43f) * 0.38f;
            return Mathf.Clamp(wave + correction, -maxX, maxX);
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

            CrashClimbPlayerController2D[] players = Object.FindObjectsByType<CrashClimbPlayerController2D>(FindObjectsSortMode.None);
            foreach (CrashClimbPlayerController2D player in players)
            {
                if (player != null && player.transform.parent == null && player.gameObject.name.StartsWith("Crash Player"))
                {
                    if (Application.isPlaying)
                    {
                        Destroy(player.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(player.gameObject);
                    }
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
                CrashClimbPlayerController2D prefabPlayer = Instantiate(playerPrefab, playerSpawn, Quaternion.identity);
                prefabPlayer.transform.SetParent(transform);
                return prefabPlayer;
            }

            GameObject player = new GameObject("Crash Player");
            player.transform.SetParent(transform);
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

        private void CreatePlatform(string objectName, Vector2 position, Vector2 size, CrashClimbSurfaceKind kind, string spriteOverridePath = null)
        {
            GameObject platform = new GameObject(objectName);
            platform.transform.SetParent(transform);
            platform.transform.position = position;

            SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
            bool isWall = objectName.Contains("Wall");
            Sprite platformSprite = useCraftpixSprites && !isWall ? GetPlatformSprite(kind, spriteOverridePath) : null;
            renderer.sprite = platformSprite != null ? platformSprite : CreateUnitSprite();
            renderer.color = platformSprite != null ? Color.white : GetSurfaceColor(kind);
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
            renderer.sprite = GetSprite("CrashClimb/Pads/New/Pad_2_2", 354f) ?? GetSprite("CrashClimb/Pads/Pad_2_2", 354f) ?? CreateUnitSprite();
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

        private void CreateGoalTrigger(Vector2 position)
        {
            GameObject goalTrigger = new GameObject("Goal Trigger");
            goalTrigger.transform.SetParent(transform);
            goalTrigger.transform.position = position;

            BoxCollider2D collider = goalTrigger.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(4.6f, 1.4f);
            collider.isTrigger = true;

            goalTrigger.AddComponent<CrashClimbGoal2D>();
        }

        private void CreateZoneMarker(string text, Vector2 position, Color color)
        {
            GameObject marker = new GameObject(text);
            marker.transform.SetParent(transform);
            marker.transform.position = new Vector3(position.x, position.y, -0.25f);

            TextMesh label = marker.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleLeft;
            label.alignment = TextAlignment.Left;
            label.characterSize = 0.18f;
            label.fontSize = 32;
            label.color = new Color(color.r, color.g, color.b, 0.82f);
        }

        private bool ShouldPlaceSpike(int index, LevelZone zone)
        {
            if (zone.SpikeEvery <= 0)
            {
                return false;
            }

            return index >= 8 && (index - zone.Start + 1) % zone.SpikeEvery == 0;
        }

        private CrashClimbSurfaceKind PickSurfaceKind(int index, LevelZone zone)
        {
            int zoneStep = index - zone.Start;
            if (index == platformCount || zoneStep % 6 == 5)
            {
                return CrashClimbSurfaceKind.Crystal;
            }

            if (zone.Primary == CrashClimbSurfaceKind.FragileRock && zoneStep % 3 != 0)
            {
                return CrashClimbSurfaceKind.FragileRock;
            }

            if (zoneStep % 4 == 2)
            {
                return zone.Secondary;
            }

            if (index % 9 == 0)
            {
                return CrashClimbSurfaceKind.Glue;
            }

            return zone.Primary;
        }

        private LevelZone GetLevelZone(int index)
        {
            for (int i = 0; i < LevelZones.Length; i++)
            {
                if (index >= LevelZones[i].Start && index <= LevelZones[i].End)
                {
                    return LevelZones[i];
                }
            }

            return LevelZones[LevelZones.Length - 1];
        }

        private int GetZoneNumber(int index)
        {
            for (int i = 0; i < LevelZones.Length; i++)
            {
                if (index >= LevelZones[i].Start && index <= LevelZones[i].End)
                {
                    return i + 1;
                }
            }

            return LevelZones.Length;
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

        private Sprite GetPlatformSprite(CrashClimbSurfaceKind kind, string overridePath = null)
        {
            if (!string.IsNullOrEmpty(overridePath))
            {
                return GetSprite(overridePath, 394f);
            }

            switch (kind)
            {
                case CrashClimbSurfaceKind.Ice:
                    return GetSprite("CrashClimb/Pads/New/Pad_2_1", 354f) ?? GetSprite("CrashClimb/Pads/Pad_2_1", 354f);
                case CrashClimbSurfaceKind.Glue:
                    return GetSprite("CrashClimb/Pads/New/Pad_3_2", 397f) ?? GetSprite("CrashClimb/Pads/Pad_3_2", 397f);
                case CrashClimbSurfaceKind.Crystal:
                    return GetSprite("CrashClimb/Pads/Pad_4_1", 395f);
                case CrashClimbSurfaceKind.FragileRock:
                    return GetSprite("CrashClimb/Pads/New/Pad_1_2", 394f) ?? GetSprite("CrashClimb/Pads/Pad_1_2", 394f);
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
