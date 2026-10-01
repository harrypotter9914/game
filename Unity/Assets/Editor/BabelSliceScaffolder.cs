using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using Babel.Runtime.Runes;
using Babel.Runtime.Shop;
using Babel.Runtime.UI;
using Babel.Runtime.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Babel.EditorTools
{
    public static partial class BabelSliceScaffolder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string DataFolder = "Assets/Data";
        private const string GeneratedFolder = "Assets/Art/Generated";
        private const float LegacyUnitScale = 0.01f;

        private sealed class LegacyEnemySpawn
        {
            public string Id;
            public string BindingType;
            public Vector2 Position;
        }

        private sealed class LegacyCollectibleSpawn
        {
            public string Tag;
            public Vector2 Position;
        }

        private sealed class LegacySceneData
        {
            public Vector2 PlayerStart = Vector2.zero;
            public Vector2 RecommendedSpawn = Vector2.zero;
            public readonly HashSet<Vector3Int> GroundTiles = new HashSet<Vector3Int>();
            public readonly List<LegacyEnemySpawn> EnemySpawns = new List<LegacyEnemySpawn>();
            public readonly List<LegacyCollectibleSpawn> CollectibleSpawns = new List<LegacyCollectibleSpawn>();
        }

        [MenuItem("Babel/Scaffold First Playable Slice")]
        public static void ScaffoldFirstPlayableSlice()
        {
            EnsureFolders();

            var playerDefinition = CreatePlayerDefinition();
            var enemyDefinitions = CreateEnemyDefinitions();
            var runeDefinitions = CreateRuneDefinitions();
            var whiteTile = CreateWhiteTileAsset();

            var playerPrefab = CreatePlayerPrefab(playerDefinition);
            var coinPickupPrefab = CreateCoinPickupPrefab();
            var enemyPrefabs = CreateEnemyPrefabs(enemyDefinitions, coinPickupPrefab);
            var checkpointPrefab = CreateCheckpointPrefab();
            var runePickupPrefab = CreateRunePickupPrefab(runeDefinitions[0]);
            var shopItems = CreateShopItems();
            var shopkeeperPrefab = CreateShopkeeperPrefab(shopItems);
            var sceneData = LoadLegacySceneData();

            CreateBootstrapScene();
            CreateGameplayScene(sceneData, whiteTile, playerPrefab, enemyPrefabs, checkpointPrefab, runePickupPrefab, runeDefinitions, coinPickupPrefab, shopkeeperPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Babel", "Legacy whitebox scene rebuilt.", "OK");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Editor");
            EnsureFolder(ScenesFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder("Assets/Prefabs/Core");
            EnsureFolder("Assets/Prefabs/Characters");
            EnsureFolder("Assets/Prefabs/Characters/Player");
            EnsureFolder("Assets/Prefabs/Characters/Enemies");
            EnsureFolder("Assets/Prefabs/World");
            EnsureFolder("Assets/Prefabs/UI");
            EnsureFolder(DataFolder);
            EnsureFolder("Assets/Data/Characters");
            EnsureFolder("Assets/Data/Characters/Player");
            EnsureFolder("Assets/Data/Characters/Enemies");
            EnsureFolder("Assets/Data/Runes");
            EnsureFolder("Assets/Data/Shop");
            EnsureFolder(GeneratedFolder);
            EnsureFolder("Assets/Data/Generated");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static PlayerDefinition CreatePlayerDefinition()
        {
            var assetPath = "Assets/Data/Characters/Player/BabelPlayer.asset";
            var asset = AssetDatabase.LoadAssetAtPath<PlayerDefinition>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<PlayerDefinition>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("baseMaxHealth").intValue = 6;
            so.FindProperty("baseMaxMana").intValue = 3;
            so.FindProperty("moveSpeed").floatValue = 7f;
            so.FindProperty("groundAcceleration").floatValue = 60f;
            so.FindProperty("airAcceleration").floatValue = 35f;
            so.FindProperty("jumpImpulse").floatValue = 16.4f;
            so.FindProperty("extraAirJumps").intValue = 1;
            so.FindProperty("wallSlideSpeed").floatValue = 2f;
            so.FindProperty("wallCheckDistance").floatValue = 0.1f;
            so.FindProperty("wallJumpHorizontalImpulse").floatValue = 8f;
            so.FindProperty("wallJumpVerticalImpulse").floatValue = 16.9f;
            so.FindProperty("meleeDamage").intValue = 1;
            so.FindProperty("meleeRange").floatValue = 1.35f;
            so.FindProperty("meleeCooldown").floatValue = 0.3f;
            so.FindProperty("shockwaveDamage").intValue = 2;
            so.FindProperty("shockwaveManaCost").intValue = 1;
            so.FindProperty("shockwaveRange").floatValue = 10f;
            so.FindProperty("shockwaveDuration").floatValue = 2.5f;
            so.FindProperty("shockwaveTickInterval").floatValue = .3f;
            so.FindProperty("shockwaveCooldown").floatValue = 1.6f;
            so.FindProperty("crystalDashDamage").intValue = 2;
            so.FindProperty("crystalDashSpeed").floatValue = 14f;
            so.FindProperty("crystalDashDuration").floatValue = 0.18f;
            so.FindProperty("crystalDashRange").floatValue = 4.2f;
            so.FindProperty("crystalDashCooldown").floatValue = 1.2f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Dictionary<string, EnemyDefinition> CreateEnemyDefinitions()
        {
            return new Dictionary<string, EnemyDefinition>
            {
                { "EnemyPrefabBinding", CreateEnemyDefinitionAsset("MeleeGatekeeper", 3, 1, 1.45f, 2.2f, 5.8f, 1.15f, 1.15f, false, false, false) },
                { "Enemy1PrefabBinding", CreateEnemyDefinitionAsset("ShieldSentinel", 6, 1, 0.85f, 1.35f, 5f, 1.2f, 1.45f, true, false, false) },
                { "Enemy2PrefabBinding", CreateEnemyDefinitionAsset("RangedBoneslinger", 3, 1, 0.85f, 1.55f, 7.2f, 1.2f, 1.85f, false, true, false) },
                { "Enemy3PrefabBinding", CreateEnemyDefinitionAsset("GiantPenitent", 10, 2, 0.72f, 1.3f, 6.8f, 1.9f, 2.15f, false, false, false) },
                { "Enemy4PrefabBinding", CreateEnemyDefinitionAsset("ShockwaveBoss", 16, 2, 0.5f, 1.15f, 8.2f, 2.5f, 2.4f, false, false, true) },
                { "Enemy5PrefabBinding", CreateEnemyDefinitionAsset("BurrowBoss", 14, 2, 0.5f, 1.15f, 8.4f, 2.1f, 2.3f, false, false, true) },
                { "Enemy6PrefabBinding", CreateEnemyDefinitionAsset("FinalNero", 22, 3, 0.7f, 1.65f, 9.5f, 2.6f, 2.2f, false, false, true) },
                { "Enemy7PrefabBinding", CreateEnemyDefinitionAsset("AerialJudgeBoss", 15, 2, 0.95f, 1.7f, 8.8f, 1.8f, 1.9f, false, false, true) },
            };
        }

        private static EnemyDefinition CreateEnemyDefinitionAsset(string assetName, int maxHealth, int damage, float patrolSpeed, float chaseSpeed, float chaseRange, float attackRange, float attackCooldown, bool blocksFrontAttacks, bool usesProjectileAttack, bool isBoss)
        {
            var assetPath = $"Assets/Data/Characters/Enemies/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EnemyDefinition>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("maxHealth").intValue = maxHealth;
            so.FindProperty("contactDamage").intValue = damage;
            so.FindProperty("patrolSpeed").floatValue = patrolSpeed;
            so.FindProperty("chaseSpeed").floatValue = chaseSpeed;
            so.FindProperty("chaseRange").floatValue = chaseRange;
            so.FindProperty("attackRange").floatValue = attackRange;
            so.FindProperty("verticalAggroTolerance").floatValue = 2f;
            so.FindProperty("attackCooldown").floatValue = attackCooldown;
            so.FindProperty("blocksFrontAttacks").boolValue = blocksFrontAttacks;
            so.FindProperty("usesProjectileAttack").boolValue = usesProjectileAttack;
            so.FindProperty("isBoss").boolValue = isBoss;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static List<RuneDefinition> CreateRuneDefinitions()
        {
            var result = new List<RuneDefinition>();
            var runeData = new[]
            {
                new object[] { RuneId.Romance, RuneEffectType.BonusMaxHealth, "Romance", 1f, "Wait upon the Lord and rise renewed. Increases max HP, but lowers damage." },
                new object[] { RuneId.Rings, RuneEffectType.LowHealthDamageBoost, "Rings", 1.5f, "In the dark forest, the Lord is my only light. Boosts damage at low HP." },
                new object[] { RuneId.Star, RuneEffectType.AttackSpeedBoost, "Star", 1.4f, "He trains my hands for war. Increases attack speed." },
                new object[] { RuneId.Sun, RuneEffectType.AttackRangeBoost, "Sun", 1.4f, "A sigil of judgment and power. Extends attack reach." },
                new object[] { RuneId.Flight, RuneEffectType.CoinMagnet, "Flight", 4f, "Trust in the Lord, not your own understanding. Pulls nearby gold toward you." },
                new object[] { RuneId.Woman, RuneEffectType.OneTimeRevive, "Woman", 1f, "I am the resurrection and the life. Revives you once on the spot." },
                new object[] { RuneId.Harvest, RuneEffectType.ShopDiscount, "Harvest", 0.8f, "Give freely and ask for little in return. Grants shop discounts." },
                new object[] { RuneId.Moon, RuneEffectType.DamageReflect, "Moon", 1f, "Above these stars there is only the glory of God. Reflects part of incoming damage." },
            };

            foreach (var runeEntry in runeData)
            {
                var id = (RuneId)runeEntry[0];
                var effect = (RuneEffectType)runeEntry[1];
                var displayName = (string)runeEntry[2];
                var magnitude = (float)runeEntry[3];
                var description = (string)runeEntry[4];
                var assetPath = $"Assets/Data/Runes/{displayName}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<RuneDefinition>(assetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<RuneDefinition>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                }

                var so = new SerializedObject(asset);
                so.FindProperty("runeId").enumValueIndex = (int)id;
                so.FindProperty("effectType").enumValueIndex = (int)effect;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("description").stringValue = description;
                so.FindProperty("magnitude").floatValue = magnitude;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                result.Add(asset);
            }

            return result;
        }

        private static TileBase CreateWhiteTileAsset()
        {
            var texturePath = $"{GeneratedFolder}/WhiteTile.png";
            var tilePath = "Assets/Data/Generated/WhiteTile.asset";

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 1;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static GameObject CreatePlayerPrefab(PlayerDefinition definition)
        {
            var prefabPath = "Assets/Prefabs/Characters/Player/Player.prefab";
            var root = new GameObject("Player");
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.white;
            spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedFolder}/WhiteTile.png");
            root.transform.localScale = new Vector3(0.8f, 1.6f, 1f);

            var rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.freezeRotation = true;
            rigidbody.gravityScale = 3f;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            root.AddComponent<BoxCollider2D>().size = Vector2.one;

            var groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(root.transform);
            groundCheck.localPosition = new Vector3(0f, -0.55f, 0f);

            var attackOrigin = new GameObject("AttackOrigin").transform;
            attackOrigin.SetParent(root.transform);
            attackOrigin.localPosition = new Vector3(0.6f, 0f, 0f);

            var controller = root.AddComponent<PlayerController2D>();
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("definition").objectReferenceValue = definition;
            controllerSo.FindProperty("groundCheck").objectReferenceValue = groundCheck;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            var combat = root.AddComponent<PlayerCombatController>();
            var combatSo = new SerializedObject(combat);
            combatSo.FindProperty("definition").objectReferenceValue = definition;
            combatSo.FindProperty("attackOrigin").objectReferenceValue = attackOrigin;
            combatSo.ApplyModifiedPropertiesWithoutUndo();

            var health = root.AddComponent<HealthComponent>();
            var healthSo = new SerializedObject(health);
            healthSo.FindProperty("alignment").enumValueIndex = (int)TeamAlignment.Player;
            healthSo.FindProperty("invulnerabilityDuration").floatValue = 0.35f;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<ManaComponent>();
            root.AddComponent<PlayerRuntimeState>();
            root.AddComponent<PlayerRespawnController>();
            root.AddComponent<PlayerHealChannelController>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Dictionary<string, GameObject> CreateEnemyPrefabs(Dictionary<string, EnemyDefinition> definitions, GameObject coinPickupPrefab)
        {
            var projectilePrefab = CreateEnemyProjectilePrefab();
            var result = new Dictionary<string, GameObject>();
            result["EnemyPrefabBinding"] = CreateEnemyPrefab(typeof(MeleeEnemyController), "MeleeEnemy", definitions["EnemyPrefabBinding"], Color.white, new Vector3(0.8f, 1.6f, 1f), coinPickupPrefab, null, 2.5f);
            result["Enemy1PrefabBinding"] = CreateEnemyPrefab(typeof(ShieldSentinelController), "ShieldEnemy", definitions["Enemy1PrefabBinding"], new Color(0.85f, 0.85f, 0.85f, 1f), new Vector3(0.95f, 1.7f, 1f), coinPickupPrefab, null, 2.5f);
            result["Enemy2PrefabBinding"] = CreateEnemyPrefab(typeof(RangedEnemyController), "RangedEnemy", definitions["Enemy2PrefabBinding"], new Color(0.8f, 0.9f, 1f, 1f), new Vector3(0.85f, 1.5f, 1f), coinPickupPrefab, projectilePrefab.GetComponent<EnemyProjectile>(), 3.2f);
            result["Enemy3PrefabBinding"] = CreateEnemyPrefab(typeof(GiantEnemyController), "GiantEnemy", definitions["Enemy3PrefabBinding"], new Color(1f, 0.9f, 0.9f, 1f), new Vector3(1.8f, 2.4f, 1f), coinPickupPrefab, null, 2.8f);
            result["Enemy4PrefabBinding"] = CreateEnemyPrefab(typeof(ShockwaveBossController), "ShockwaveBoss", definitions["Enemy4PrefabBinding"], new Color(1f, 0.8f, 0.8f, 1f), new Vector3(2.2f, 2.8f, 1f), coinPickupPrefab, null, 1.2f);
            result["Enemy5PrefabBinding"] = CreateEnemyPrefab(typeof(BurrowBossController), "BurrowBoss", definitions["Enemy5PrefabBinding"], new Color(0.95f, 0.85f, 0.75f, 1f), new Vector3(2.2f, 2.8f, 1f), coinPickupPrefab, null, 1.2f);
            result["Enemy6PrefabBinding"] = CreateEnemyPrefab(typeof(FinalBossController), "FinalBoss", definitions["Enemy6PrefabBinding"], new Color(1f, 0.7f, 0.7f, 1f), new Vector3(2.5f, 3f, 1f), coinPickupPrefab, null, 1.4f);
            result["Enemy7PrefabBinding"] = CreateEnemyPrefab(typeof(AerialJudgeBossController), "AerialJudgeBoss", definitions["Enemy7PrefabBinding"], new Color(0.82f, 0.92f, 1f, 1f), new Vector3(1.25f, 1.9f, 1f), coinPickupPrefab, null, 2f);
            return result;
        }

        private static GameObject CreateEnemyProjectilePrefab()
        {
            var prefabPath = "Assets/Prefabs/Characters/Enemies/EnemyProjectile.prefab";
            var root = new GameObject("EnemyProjectile");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0.75f, 0.9f, 1f, 1f);
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedFolder}/WhiteTile.png");
            root.transform.localScale = new Vector3(0.18f, 0.18f, 1f);
            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            root.AddComponent<EnemyProjectile>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateEnemyPrefab(Type controllerType, string prefabName, EnemyDefinition definition, Color color, Vector3 scale, GameObject coinPickupPrefab, EnemyProjectile projectilePrefab, float patrolDistanceValue)
        {
            var prefabPath = $"Assets/Prefabs/Characters/Enemies/{prefabName}.prefab";
            var root = new GameObject(prefabName);
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.color = color;
            spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedFolder}/WhiteTile.png");
            root.transform.localScale = scale;

            var rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.freezeRotation = true;
            rigidbody.gravityScale = 3f;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            root.AddComponent<BoxCollider2D>().size = Vector2.one;

            var attackOrigin = new GameObject("AttackOrigin").transform;
            attackOrigin.SetParent(root.transform);
            attackOrigin.localPosition = new Vector3(0.6f, 0f, 0f);

            var health = root.AddComponent<HealthComponent>();
            var healthSo = new SerializedObject(health);
            healthSo.FindProperty("alignment").enumValueIndex = (int)TeamAlignment.Enemy;
            healthSo.FindProperty("destroyOnDeath").boolValue = true;
            healthSo.FindProperty("invulnerabilityDuration").floatValue = 0.1f;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            var controller = (EnemyControllerBase)root.AddComponent(controllerType);
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("definition").objectReferenceValue = definition;
            controllerSo.FindProperty("attackOrigin").objectReferenceValue = attackOrigin;
            controllerSo.FindProperty("patrolDistance").floatValue = patrolDistanceValue;
            var projectileProperty = controllerSo.FindProperty("projectilePrefab");
            if (projectileProperty != null)
            {
                projectileProperty.objectReferenceValue = projectilePrefab;
            }
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            var reward = root.AddComponent<EnemyDeathReward>();
            var rewardSo = new SerializedObject(reward);
            rewardSo.FindProperty("coinPickupPrefab").objectReferenceValue = coinPickupPrefab;
            rewardSo.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }
        private static GameObject CreateCheckpointPrefab()
        {
            var prefabPath = "Assets/Prefabs/World/Checkpoint.prefab";
            var root = new GameObject("Checkpoint");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.color = new Color(1f, 1f, 0.35f, 1f);
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedFolder}/WhiteTile.png");
            root.transform.localScale = new Vector3(0.4f, 1.4f, 1f);

            var collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            root.AddComponent<CheckpointMarker>();

            var pickup = root.AddComponent<CollectiblePickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("pickupKind").enumValueIndex = 4;
            so.FindProperty("destroyOnCollect").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateRunePickupPrefab(RuneDefinition rune)
        {
            var prefabPath = "Assets/Prefabs/World/RunePickup_Romance.prefab";
            var root = new GameObject("RunePickup_Romance");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0.85f, 0.55f, 1f, 1f);
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedFolder}/WhiteTile.png");
            root.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;

            var pickup = root.AddComponent<CollectiblePickup>();
            var so = new SerializedObject(pickup);
            so.FindProperty("pickupKind").enumValueIndex = 2;
            so.FindProperty("rune").objectReferenceValue = rune;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }


        private static GameObject CreateCoinPickupPrefab()
        {
            var prefabPath = "Assets/Prefabs/World/CoinPickup.prefab";
            var root = new GameObject("CoinPickup");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.color = new Color(1f, 0.85f, 0.2f, 1f);
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedFolder}/WhiteTile.png");
            root.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;

            var pickup = root.AddComponent<CollectiblePickup>();
            pickup.ConfigureGoldAmount(1);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static List<ShopItemDefinition> CreateShopItems()
        {
            var items = new List<ShopItemDefinition>();
            items.Add(CreateShopItemAsset("PilgrimBread", "Pilgrim Bread", "Warm bread from a roadside shrine. Increases max health by 1.", ShopItemEffectType.MaxHealth, 6, 1f));
            items.Add(CreateShopItemAsset("LanternOil", "Lantern Oil", "Blessed oil for the lantern. Increases max mana by 1.", ShopItemEffectType.MaxMana, 7, 1f));
            items.Add(CreateShopItemAsset("PenitentNeedle", "Penitent Needle", "A relic needle etched with vows. Increases damage by 20%.", ShopItemEffectType.DamageBoost, 10, 0.2f));
            return items;
        }

        private static ShopItemDefinition CreateShopItemAsset(string assetName, string displayName, string description, ShopItemEffectType effectType, int price, float magnitude)
        {
            var assetPath = $"Assets/Data/Shop/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ShopItemDefinition>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            var so = new SerializedObject(asset);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("effectType").enumValueIndex = (int)effectType;
            so.FindProperty("price").intValue = price;
            so.FindProperty("magnitude").floatValue = magnitude;
            so.FindProperty("oneTimePurchase").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static GameObject CreateShopkeeperPrefab(List<ShopItemDefinition> stock)
        {
            var prefabPath = "Assets/Prefabs/World/Shopkeeper.prefab";
            var root = new GameObject("Shopkeeper");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0.9f, 0.75f, 0.45f, 1f);
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{GeneratedFolder}/WhiteTile.png");
            root.transform.localScale = new Vector3(1f, 1.8f, 1f);

            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.8f, 2.2f);
            collider.isTrigger = true;

            var shopkeeper = root.AddComponent<ShopkeeperController>();
            var so = new SerializedObject(shopkeeper);
            var stockProperty = so.FindProperty("stock");
            stockProperty.arraySize = stock.Count;
            for (var i = 0; i < stock.Count; i++)
            {
                stockProperty.GetArrayElementAtIndex(i).objectReferenceValue = stock[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }
        private static LegacySceneData LoadLegacySceneData()
        {
            var sceneData = new LegacySceneData();

            var wallText = File.ReadAllText(Path.Combine(Application.dataPath, "Art/SourceData/LegacyPrefabs/wall.yaml"));
            foreach (Match match in Regex.Matches(wallText, @"centerX:\s*(-?\d+(?:\.\d+)?)\s*\r?\n\s*centerY:\s*(-?\d+(?:\.\d+)?)"))
            {
                var x = ParseFloat(match.Groups[1].Value);
                var y = ParseFloat(match.Groups[2].Value);
                sceneData.GroundTiles.Add(ToTileCell(new Vector2(x, y)));
            }

            var sceneText = File.ReadAllText(Path.Combine(Application.dataPath, "Art/SourceData/LegacyScene/main.yaml"));
            var playerMatch = Regex.Match(sceneText, @"id:\s*mainRole\s*\r?\n\s*prefab:\s*\r?\n\s*type:\s*MainRolePrefabBinding\s*\r?\n\s*properties:\s*\r?\n\s*action:[^\r\n]*\r?\n\s*x:\s*(-?\d+(?:\.\d+)?)\s*\r?\n\s*'y':\s*(-?\d+(?:\.\d+)?)", RegexOptions.Singleline);
            if (playerMatch.Success)
            {
                sceneData.PlayerStart = ToWorldPosition(new Vector2(ParseFloat(playerMatch.Groups[1].Value), ParseFloat(playerMatch.Groups[2].Value)));
            }

            var enemyPattern = new Regex(@"id:\s*(?<id>[^\r\n]+)\s*\r?\n\s*prefab:\s*\r?\n\s*type:\s*(?<type>Enemy\d*PrefabBinding)\s*\r?\n\s*properties:\s*\r?\n\s*action:[^\r\n]*\r?\n\s*x:\s*(?<x>-?\d+(?:\.\d+)?)\s*\r?\n\s*'y':\s*(?<y>-?\d+(?:\.\d+)?)", RegexOptions.Singleline);
            foreach (Match match in enemyPattern.Matches(sceneText))
            {
                sceneData.EnemySpawns.Add(new LegacyEnemySpawn
                {
                    Id = match.Groups["id"].Value.Trim(),
                    BindingType = match.Groups["type"].Value.Trim(),
                    Position = ToWorldPosition(new Vector2(ParseFloat(match.Groups["x"].Value), ParseFloat(match.Groups["y"].Value)))
                });
            }

            var collectibleBlocks = Regex.Matches(sceneText, @"-\s*behaviours:\s*(?<block>.*?)(?=(?:\r?\n\s*-\s*behaviours:)|\z)", RegexOptions.Singleline);
            foreach (Match blockMatch in collectibleBlocks)
            {
                var block = blockMatch.Groups["block"].Value;
                if (!Regex.IsMatch(block, @"id:\s*hoxi", RegexOptions.Singleline))
                {
                    continue;
                }

                var tagMatch = Regex.Match(block, @"tag:\s*(?<tag>hoxi[^\r\n]+)");
                var positionMatch = Regex.Match(block, @"-\s*type:\s*Transform\s*\r?\n\s*properties:\s*\r?\n\s*x:\s*(?<x>-?\d+(?:\.\d+)?)\s*\r?\n\s*'y':\s*(?<y>-?\d+(?:\.\d+)?)", RegexOptions.Singleline);
                if (!tagMatch.Success || !positionMatch.Success)
                {
                    continue;
                }

                sceneData.CollectibleSpawns.Add(new LegacyCollectibleSpawn
                {
                    Tag = tagMatch.Groups["tag"].Value.Trim(),
                    Position = ToWorldPosition(new Vector2(ParseFloat(positionMatch.Groups["x"].Value), ParseFloat(positionMatch.Groups["y"].Value)))
                });
            }

            ScaleSceneGeometry(sceneData, 2);
            sceneData.RecommendedSpawn = FindRecommendedSpawn(sceneData.PlayerStart, sceneData.GroundTiles);
            EnsureRequiredBossSpawns(sceneData);
            return sceneData;
        }

        private static Vector2 FindRecommendedSpawn(Vector2 preferredStart, HashSet<Vector3Int> tiles)
        {
            if (tiles.Count == 0)
            {
                return preferredStart == Vector2.zero ? new Vector2(0f, 2f) : preferredStart;
            }

            var preferredX = Mathf.RoundToInt(preferredStart.x);
            for (var radius = 0; radius <= 12; radius++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    var x = preferredX + dx;
                    var column = tiles.Where(cell => cell.x == x).OrderByDescending(cell => cell.y).ToList();
                    if (column.Count == 0)
                    {
                        continue;
                    }

                    var top = column[0];
                    if (!tiles.Contains(new Vector3Int(top.x, top.y + 1, 0)))
                    {
                        return new Vector2(top.x, top.y + 2f);
                    }
                }
            }

            var fallback = tiles.OrderByDescending(cell => cell.y).ThenBy(cell => cell.x).First();
            return new Vector2(fallback.x, fallback.y + 2f);
        }

        private static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var gameRoot = new GameObject("GameRoot");
            gameRoot.AddComponent<RuneInventory>();
            gameRoot.AddComponent<GameSession>();
            gameRoot.AddComponent<GameFlowController>();
            gameRoot.AddComponent<GameBootstrap>();
            gameRoot.AddComponent<BootstrapSceneLoader>();

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
            cameraObject.tag = "MainCamera";

            EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/Bootstrap.unity");
        }

        private static void CreateGameplayScene(LegacySceneData sceneData, TileBase whiteTile, GameObject playerPrefab, Dictionary<string, GameObject> enemyPrefabs, GameObject checkpointPrefab, GameObject runePickupPrefab, List<RuneDefinition> runeDefinitions, GameObject coinPickupPrefab, GameObject shopkeeperPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var gameRoot = new GameObject("GameRoot");
            gameRoot.AddComponent<RuneInventory>();
            gameRoot.AddComponent<GameSession>();
            gameRoot.AddComponent<GameFlowController>();
            gameRoot.AddComponent<GameBootstrap>();

            var services = new GameObject("SceneServices");
            services.AddComponent<CheckpointService>();

            var gridRoot = new GameObject("Grid");
            gridRoot.AddComponent<Grid>();
            var tilemapObject = new GameObject("GroundTilemap");
            tilemapObject.transform.SetParent(gridRoot.transform, false);
            var tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();
            var tileCollider = tilemapObject.AddComponent<TilemapCollider2D>();
            tileCollider.extrusionFactor = 0.01f;
            var rb = tilemapObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var composite = tilemapObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            tileCollider.usedByComposite = true;
            tilemap.color = Color.white;

            foreach (var cell in sceneData.GroundTiles)
            {
                tilemap.SetTile(cell, whiteTile);
            }
            tilemap.CompressBounds();

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
            cameraObject.tag = "MainCamera";
            var cameraFollow = cameraObject.AddComponent<CameraFollow2D>();
            var cameraSo = new SerializedObject(cameraFollow);
            cameraSo.FindProperty("offset").vector3Value = new Vector3(0f, 0f, -10f);
            cameraSo.ApplyModifiedPropertiesWithoutUndo();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.position = sceneData.RecommendedSpawn;
            cameraFollow.SetTarget(player.transform);

            foreach (var spawn in sceneData.EnemySpawns)
            {
                if (!enemyPrefabs.TryGetValue(spawn.BindingType, out var prefab))
                {
                    prefab = enemyPrefabs["EnemyPrefabBinding"];
                }

                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                enemy.name = spawn.Id;
                enemy.transform.position = FindEnemySpawnPosition(sceneData.GroundTiles, spawn.Position, spawn.BindingType);
                AttachWorldNameplate(enemy.transform, GetEnemyDisplayName(spawn.Id, spawn.BindingType), GetEnemyNameplateHeight(spawn.BindingType), Color.white);
                ConfigureBossReward(enemy, spawn.Id);
            }

            var checkpoint = (GameObject)PrefabUtility.InstantiatePrefab(checkpointPrefab);
            checkpoint.name = "Checkpoint";
            checkpoint.transform.position = FindGroundAnchorPosition(sceneData.GroundTiles, sceneData.RecommendedSpawn + new Vector2(-1f, 0f), 0.6f);

            foreach (var collectible in sceneData.CollectibleSpawns)
            {
                var pickupGo = (GameObject)PrefabUtility.InstantiatePrefab(runePickupPrefab);
                pickupGo.name = collectible.Tag;
                pickupGo.transform.position = FindCollectibleSpawnPosition(sceneData.GroundTiles, collectible.Position);
                ConfigureHoxiPickup(pickupGo, collectible.Tag, runeDefinitions);
            }

            for (var i = 0; i < 6; i++)
            {
                var coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPickupPrefab);
                coin.name = $"Coin_{i + 1}";
                coin.transform.position = FindCollectibleSpawnPosition(sceneData.GroundTiles, sceneData.RecommendedSpawn + new Vector2(-3f + i * 0.8f, 1f + (i % 2) * 0.35f));
                var pickup = coin.GetComponent<CollectiblePickup>();
                if (pickup != null)
                {
                    pickup.ConfigureGoldAmount(1 + (i % 3));
                }
            }

            var shopkeeper = (GameObject)PrefabUtility.InstantiatePrefab(shopkeeperPrefab);
            shopkeeper.name = "PilgrimMerchant";
            shopkeeper.transform.position = FindGroundAnchorPosition(sceneData.GroundTiles, sceneData.RecommendedSpawn + new Vector2(12f, 1f), 0.95f);

            CreateHudCanvas(player);
        }

        private static void CreateHudCanvas(GameObject player)
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            CreatePanel(canvasGo.transform, "HealthPanel", new Vector2(28f, -24f), new Vector2(230f, 42f), new Color(0f, 0f, 0f, 0.45f));
            CreatePanel(canvasGo.transform, "ManaPanel", new Vector2(28f, -78f), new Vector2(230f, 42f), new Color(0f, 0f, 0f, 0.45f));
            CreatePanel(canvasGo.transform, "GoldPanel", new Vector2(28f, -132f), new Vector2(230f, 42f), new Color(0f, 0f, 0f, 0.45f));

            var presenter = canvasGo.AddComponent<PlayerHudPresenter>();
            var presenterSo = new SerializedObject(presenter);
            presenterSo.FindProperty("playerState").objectReferenceValue = player.GetComponent<PlayerRuntimeState>();

            var healthFill = CreateBar(canvasGo.transform, "HealthFill", new Vector2(40f, -36f), new Color(0.95f, 0.2f, 0.2f, 1f));
            var manaFill = CreateBar(canvasGo.transform, "ManaFill", new Vector2(40f, -90f), new Color(0.2f, 0.55f, 0.95f, 1f));
            var healthLabel = CreateLabel(canvasGo.transform, "HealthLabel", new Vector2(190f, -34f), "HP");
            var manaLabel = CreateLabel(canvasGo.transform, "ManaLabel", new Vector2(190f, -88f), "MP");
            var goldLabel = CreateLabel(canvasGo.transform, "GoldLabel", new Vector2(40f, -142f), "Gold 0");
            var centerMessage = CreateCenterLabel(canvasGo.transform, "CenterMessage", new Vector2(0f, 120f), string.Empty, 30f);
            var worldMessagePrefab = CreateCenterLabel(canvasGo.transform, "WorldMessagePrefab", new Vector2(-10000f, -10000f), string.Empty, 22f);
            worldMessagePrefab.gameObject.SetActive(false);

            presenterSo.FindProperty("healthFill").objectReferenceValue = healthFill;
            presenterSo.FindProperty("manaFill").objectReferenceValue = manaFill;
            presenterSo.FindProperty("healthLabel").objectReferenceValue = healthLabel;
            presenterSo.FindProperty("manaLabel").objectReferenceValue = manaLabel;
            presenterSo.FindProperty("goldLabel").objectReferenceValue = goldLabel;
            presenterSo.ApplyModifiedPropertiesWithoutUndo();

            var screenPresenter = canvasGo.AddComponent<ScreenMessagePresenter>();
            var screenSo = new SerializedObject(screenPresenter);
            screenSo.FindProperty("centerMessage").objectReferenceValue = centerMessage;
            screenSo.FindProperty("worldMessagePrefab").objectReferenceValue = worldMessagePrefab;
            screenSo.ApplyModifiedPropertiesWithoutUndo();

            var runePanel = CreatePanel(canvasGo.transform, "RunePanel", new Vector2(24f, -140f), new Vector2(520f, 250f), new Color(0f, 0f, 0f, 0.78f));
            runePanel.gameObject.SetActive(false);
            var runeTitle = CreateLabel(runePanel.transform, "RuneTitle", new Vector2(18f, -16.9f), "Runes");
            runeTitle.fontSize = 24f;
            var inventoryText = CreateLabel(runePanel.transform, "RuneInventoryText", new Vector2(18f, -56f), string.Empty);
            inventoryText.rectTransform.sizeDelta = new Vector2(280f, 170f);
            inventoryText.fontSize = 18f;
            var slotsText = CreateLabel(runePanel.transform, "RuneSlotsText", new Vector2(290f, -56f), string.Empty);
            slotsText.rectTransform.sizeDelta = new Vector2(200f, 170f);
            slotsText.fontSize = 18f;
            var runeFooter = CreateLabel(runePanel.transform, "RuneFooter", new Vector2(18f, -214f), string.Empty);
            runeFooter.rectTransform.sizeDelta = new Vector2(480f, 28f);
            runeFooter.fontSize = 16.9f;

            var runeMenu = canvasGo.AddComponent<RuneMenuController>();
            var runeSo = new SerializedObject(runeMenu);
            runeSo.FindProperty("panelRoot").objectReferenceValue = runePanel.gameObject;
            runeSo.FindProperty("inventoryText").objectReferenceValue = inventoryText;
            runeSo.FindProperty("slotsText").objectReferenceValue = slotsText;
            runeSo.FindProperty("footerText").objectReferenceValue = runeFooter;
            runeSo.ApplyModifiedPropertiesWithoutUndo();

            var pausePanel = CreateCenteredPanel(canvasGo.transform, "PausePanel", new Vector2(360f, 220f), new Color(0f, 0f, 0f, 0.82f));
            pausePanel.gameObject.SetActive(false);
            var pauseTitle = CreateCenterLabel(pausePanel.transform, "PauseTitle", new Vector2(0f, 70f), "Paused", 32f);
            var pauseOptions = CreateCenterLabel(pausePanel.transform, "PauseOptions", new Vector2(0f, 0f), string.Empty, 24f);
            var pauseFooter = CreateCenterLabel(pausePanel.transform, "PauseFooter", new Vector2(0f, -72f), string.Empty, 18f);
            pauseFooter.rectTransform.sizeDelta = new Vector2(320f, 60f);

            var pauseMenu = canvasGo.AddComponent<PauseMenuController>();
            var pauseSo = new SerializedObject(pauseMenu);
            pauseSo.FindProperty("panelRoot").objectReferenceValue = pausePanel.gameObject;
            pauseSo.FindProperty("optionsText").objectReferenceValue = pauseOptions;
            pauseSo.FindProperty("footerText").objectReferenceValue = pauseFooter;
            pauseSo.ApplyModifiedPropertiesWithoutUndo();

            var shopPanel = CreateCenteredPanel(canvasGo.transform, "ShopPanel", new Vector2(520f, 260f), new Color(0f, 0f, 0f, 0.86f));
            shopPanel.gameObject.SetActive(false);
            var shopList = CreateLabel(shopPanel.transform, "ShopList", new Vector2(20f, -24f), string.Empty);
            shopList.rectTransform.sizeDelta = new Vector2(220f, 190f);
            shopList.fontSize = 20f;
            var shopDetail = CreateLabel(shopPanel.transform, "ShopDetail", new Vector2(260f, -24f), string.Empty);
            shopDetail.rectTransform.sizeDelta = new Vector2(220f, 190f);
            shopDetail.fontSize = 18f;
            var shopFooter = CreateCenterLabel(shopPanel.transform, "ShopFooter", new Vector2(0f, -94f), string.Empty, 18f);
            shopFooter.rectTransform.sizeDelta = new Vector2(420f, 50f);

            var shopMenu = canvasGo.AddComponent<ShopMenuController>();
            var shopSo = new SerializedObject(shopMenu);
            shopSo.FindProperty("panelRoot").objectReferenceValue = shopPanel.gameObject;
            shopSo.FindProperty("listText").objectReferenceValue = shopList;
            shopSo.FindProperty("detailText").objectReferenceValue = shopDetail;
            shopSo.FindProperty("footerText").objectReferenceValue = shopFooter;
            shopSo.ApplyModifiedPropertiesWithoutUndo();

            foreach (var keeper in Object.FindObjectsOfType<ShopkeeperController>())
            {
                var keeperSo = new SerializedObject(keeper);
                keeperSo.FindProperty("shopMenu").objectReferenceValue = shopMenu;
                keeperSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static Image CreateCenteredPanel(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }
        private static Image CreateBar(Transform parent, string name, Vector2 anchoredPosition, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(130f, 18f);

            var image = go.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = 0;
            image.fillAmount = 1f;
            return image;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 anchoredPosition, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(220f, 26f);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 18f;
            label.color = Color.white;
            return label;
        }

        private static TextMeshProUGUI CreateCenterLabel(Transform parent, string name, Vector2 anchoredPosition, string text, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(600f, 80f);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            return label;
        }

        private static void ConfigureHoxiPickup(GameObject pickupGo, string rawTag, List<RuneDefinition> runeDefinitions)
        {
            var normalizedTag = NormalizeHoxiTag(rawTag);
            var pickup = pickupGo.GetComponent<CollectiblePickup>();
            var renderer = pickupGo.GetComponent<SpriteRenderer>();
            if (pickup == null || renderer == null)
            {
                return;
            }

            var rune = runeDefinitions.FirstOrDefault(definition => string.Equals(definition.DisplayName, normalizedTag, StringComparison.OrdinalIgnoreCase));
            if (rune != null)
            {
                pickup.ConfigureRune(rune);
                renderer.color = GetRuneColor(rune.RuneId);
                return;
            }

            var story = GetStoryEntry(normalizedTag);
            pickup.ConfigureStory(normalizedTag, story.title, story.message, story.duration);
            renderer.color = story.color;
        }

        private static void ConfigureBossReward(GameObject enemy, string spawnId)
        {
            if (enemy == null)
            {
                return;
            }

            AbilityId? rewardAbility = null;
            string rewardTitle = null;
            switch (spawnId)
            {
                case "shockwave_boss":
                    rewardAbility = AbilityId.Shockwave;
                    rewardTitle = "The first seal breaks.";
                    break;
                case "burrow_boss":
                    rewardAbility = AbilityId.WallJump;
                    rewardTitle = "Stone yields to your feet.";
                    break;
                case "sky_judicator":
                    rewardAbility = AbilityId.DoubleJump;
                    rewardTitle = "The tower grants a second ascent.";
                    break;
                case "giant_penitent":
                    rewardAbility = AbilityId.CrystalDash;
                    rewardTitle = "Crystal force answers your charge.";
                    break;
            }

            if (!rewardAbility.HasValue)
            {
                return;
            }

            var reward = enemy.GetComponent<BossAbilityReward>();
            if (reward == null)
            {
                reward = enemy.AddComponent<BossAbilityReward>();
            }

            reward.Configure(rewardAbility.Value, rewardTitle);
        }

        private static string NormalizeHoxiTag(string rawTag)
        {
            return rawTag.Replace("hoxi", string.Empty).Trim();
        }

        private static (string title, string message, float duration, Color color) GetStoryEntry(string tag)
        {
            switch (tag.ToLowerInvariant())
            {
                case "4.23 travellers":
                    return ("Traveller's Diary I", "My homeland has burned. I have not eaten in three days. There are rats and corpses everywhere. God, protect me.", 5.5f, new Color(0.95f, 0.88f, 0.6f, 1f));
                case "4.24 travellers":
                    return ("Traveller's Diary II", "At the edge of the forest I found a tall tower. It looks abandoned. If no one lives inside, it may become my shelter.", 5.5f, new Color(0.95f, 0.88f, 0.6f, 1f));
                case "4.25 travellers":
                    return ("Traveller's Diary III", "I settled inside the tower. Its name is carved into the wall: Babel... the last letters are ruined. I finally had time to pray and repent.", 5.8f, new Color(0.95f, 0.88f, 0.6f, 1f));
                case "4.28 travellers":
                    return ("Traveller's Diary IV", "This tower is a maze. I cannot find the exit, and my food is almost gone. I have to keep climbing upward.", 5.2f, new Color(0.95f, 0.88f, 0.6f, 1f));
                case "4.30 travellers":
                    return ("Traveller's Diary V", "It is Easter, and I fear I may die here. I hear a voice from the top of the tower. Perhaps it is only hunger... I want to go home.", 6f, new Color(1f, 0.92f, 0.7f, 1f));
                case "6.14 builders":
                    return ("Builder's Diary I", "The first day of Babel's construction. We build so high that the flood will never touch us again. They call us hollow, but only we know this is survival.", 5.8f, new Color(0.75f, 0.95f, 1f, 1f));
                case "8.30 builders":
                    return ("Builder's Diary II", "The foundation is done. Mother, forgive the long silence. Work has become endless, and Nero has stretched our labor from dawn to night. I am so tired.", 5.8f, new Color(0.75f, 0.95f, 1f, 1f));
                case "10.1 builders":
                    return ("Builder's Diary III", "This tower is already high enough to escape the flood, yet Nero says it is not enough. He wants to reach the heavens themselves. I am afraid.", 5.8f, new Color(0.75f, 0.95f, 1f, 1f));
                case "12.12 builders":
                    return ("Builder's Diary IV", "Mother, I found faith again. I am no longer hollow. Today our great god was born: Nero.", 5.4f, new Color(0.75f, 0.95f, 1f, 1f));
                case "sword":
                    return ("Forgotten Relic", "A blade hums inside the stone. Its memory points toward a shockwave yet to be claimed from a boss.", 4.8f, new Color(0.9f, 0.9f, 1f, 1f));
                default:
                    return ("Memory Fragment", tag, 4f, new Color(0.9f, 0.9f, 0.9f, 1f));
            }
        }

        private static Color GetRuneColor(RuneId runeId)
        {
            switch (runeId)
            {
                case RuneId.Sun:
                    return new Color(1f, 0.78f, 0.2f, 1f);
                case RuneId.Flight:
                    return new Color(0.65f, 0.95f, 1f, 1f);
                case RuneId.Rings:
                    return new Color(1f, 0.45f, 0.45f, 1f);
                case RuneId.Moon:
                    return new Color(0.72f, 0.72f, 1f, 1f);
                case RuneId.Star:
                    return new Color(1f, 1f, 0.65f, 1f);
                case RuneId.Romance:
                    return new Color(0.9f, 0.7f, 1f, 1f);
                case RuneId.Woman:
                    return new Color(1f, 0.75f, 0.85f, 1f);
                case RuneId.Harvest:
                    return new Color(0.72f, 1f, 0.72f, 1f);
                default:
                    return Color.white;
            }
        }

        private static string GetEnemyDisplayName(string spawnId, string bindingType)
        {
            switch (spawnId)
            {
                case "melee_gatekeeper":
                    return "Melee Gatekeeper";
                case "shield_sentinel":
                    return "Shield Sentinel";
                case "ranged_boneslinger":
                    return "Ranged Boneslinger";
                case "shockwave_boss":
                    return "Shockwave Boss";
                case "giant_penitent":
                    return "Giant Penitent";
                case "burrow_boss":
                    return "Burrow Boss";
                case "late_ranged_guard":
                    return "Late Ranged Guard";
                case "final_nero":
                    return "Final Nero";
                case "sky_judicator":
                    return "Aerial Judicator";
                default:
                    return bindingType.Replace("PrefabBinding", string.Empty);
            }
        }

        private static float GetEnemyNameplateHeight(string bindingType)
        {
            switch (bindingType)
            {
                case "Enemy3PrefabBinding":
                    return 1.9f;
                case "Enemy4PrefabBinding":
                case "Enemy5PrefabBinding":
                    return 2.2f;
                case "Enemy6PrefabBinding":
                    return 2.45f;
                case "Enemy7PrefabBinding":
                    return 1.95f;
                default:
                    return 1.35f;
            }
        }

        private static void AttachWorldNameplate(Transform target, string labelText, float yOffset, Color color)
        {
            var labelObject = new GameObject("Nameplate");
            labelObject.transform.SetParent(target, false);
            labelObject.transform.localPosition = new Vector3(0f, yOffset, 0f);
            var text = labelObject.AddComponent<TextMeshPro>();
            text.text = labelText;
            text.fontSize = 4f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.enableWordWrapping = false;
            text.fontStyle = FontStyles.Bold;
            text.sortingOrder = 20;
        }
        private static void ScaleSceneGeometry(LegacySceneData sceneData, int scale)
        {
            if (scale <= 1 || sceneData.GroundTiles.Count == 0)
            {
                return;
            }

            var scaledTiles = new HashSet<Vector3Int>();
            foreach (var tile in sceneData.GroundTiles)
            {
                var baseX = tile.x * scale;
                var baseY = tile.y * scale;
                for (var offsetX = 0; offsetX < scale; offsetX++)
                {
                    for (var offsetY = 0; offsetY < scale; offsetY++)
                    {
                        scaledTiles.Add(new Vector3Int(baseX + offsetX, baseY + offsetY, 0));
                    }
                }
            }

            sceneData.GroundTiles.Clear();
            foreach (var tile in scaledTiles)
            {
                sceneData.GroundTiles.Add(tile);
            }

            sceneData.PlayerStart *= scale;
            for (var index = 0; index < sceneData.EnemySpawns.Count; index++)
            {
                sceneData.EnemySpawns[index].Position *= scale;
            }

            for (var index = 0; index < sceneData.CollectibleSpawns.Count; index++)
            {
                sceneData.CollectibleSpawns[index].Position *= scale;
            }
        }

        private static void EnsureRequiredBossSpawns(LegacySceneData sceneData)
        {
            if (sceneData.EnemySpawns.Any(spawn => spawn.BindingType == "Enemy7PrefabBinding"))
            {
                return;
            }

            var lateGuard = sceneData.EnemySpawns.FirstOrDefault(spawn => spawn.Id == "late_ranged_guard");
            var burrowBoss = sceneData.EnemySpawns.FirstOrDefault(spawn => spawn.Id == "burrow_boss");
            var preferredX = lateGuard != null ? lateGuard.Position.x - 14f : (burrowBoss != null ? burrowBoss.Position.x + 20f : sceneData.RecommendedSpawn.x + 70f);
            var preferredY = lateGuard != null ? lateGuard.Position.y + 10f : sceneData.RecommendedSpawn.y + 10f;
            sceneData.EnemySpawns.Add(new LegacyEnemySpawn
            {
                Id = "sky_judicator",
                BindingType = "Enemy7PrefabBinding",
                Position = new Vector2(preferredX, preferredY)
            });
        }

        private static Vector2 FindGroundAnchorPosition(HashSet<Vector3Int> tiles, Vector2 preferredPosition, float lift)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return preferredPosition;
            }

            var preferredCell = new Vector3Int(Mathf.RoundToInt(preferredPosition.x), Mathf.RoundToInt(preferredPosition.y), 0);
            var bestTile = default(Vector3Int);
            var bestScore = float.MaxValue;
            foreach (var tile in tiles)
            {
                if (tiles.Contains(new Vector3Int(tile.x, tile.y + 1, 0)))
                {
                    continue;
                }

                var xCost = Mathf.Abs(tile.x - preferredCell.x);
                if (xCost > 8f)
                {
                    continue;
                }

                var yCost = Mathf.Abs((tile.y + 1f) - preferredCell.y) * 0.35f;
                var score = xCost + yCost;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTile = tile;
                }
            }

            if (bestScore == float.MaxValue)
            {
                return preferredPosition;
            }

            return new Vector2(bestTile.x, bestTile.y + lift);
        }

        private static Vector2 FindCollectibleSpawnPosition(HashSet<Vector3Int> tiles, Vector2 preferredPosition)
        {
            var grounded = FindGroundAnchorPosition(tiles, preferredPosition, 1.35f);
            return new Vector2(grounded.x, grounded.y);
        }

        private static float ParseFloat(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        private static Vector2 FindEnemySpawnPosition(HashSet<Vector3Int> tiles, Vector2 preferredPosition, string bindingType)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return preferredPosition;
            }

            var preferredCell = new Vector3Int(Mathf.RoundToInt(preferredPosition.x), Mathf.RoundToInt(preferredPosition.y), 0);
            var bestColumnTop = default(Vector3Int);
            var bestScore = float.MaxValue;

            foreach (var tile in tiles)
            {
                if (tiles.Contains(new Vector3Int(tile.x, tile.y + 1, 0)))
                {
                    continue;
                }

                var xCost = Mathf.Abs(tile.x - preferredCell.x);
                if (xCost > 4f)
                {
                    continue;
                }

                var yCost = Mathf.Abs((tile.y + 1f) - preferredCell.y) * 0.35f;
                var score = xCost + yCost;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestColumnTop = tile;
                }
            }

            if (bestScore == float.MaxValue)
            {
                return preferredPosition;
            }

            return new Vector2(bestColumnTop.x, bestColumnTop.y + GetEnemySpawnHeight(bindingType));
        }

        private static float GetEnemySpawnHeight(string bindingType)
        {
            switch (bindingType)
            {
                case "Enemy3PrefabBinding":
                    return 1.65f;
                case "Enemy4PrefabBinding":
                case "Enemy5PrefabBinding":
                    return 1.85f;
                case "Enemy6PrefabBinding":
                    return 2f;
                case "Enemy7PrefabBinding":
                    return 1.25f;
                default:
                    return 1.1f;
            }
        }

        private static Vector2 ToWorldPosition(Vector2 legacyPosition)
        {
            return legacyPosition * LegacyUnitScale;
        }

        private static Vector3Int ToTileCell(Vector2 legacyPosition)
        {
            var world = ToWorldPosition(legacyPosition);
            return new Vector3Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y), 0);
        }
    }
}

































