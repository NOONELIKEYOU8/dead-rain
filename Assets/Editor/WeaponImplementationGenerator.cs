using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WeaponImplementationGenerator
{
    private const string BaseSpriteRoot = "Assets/Sprites/Player/Combat/Base";
    private const string WeaponSpriteRoot = "Assets/Sprites/Player/Combat/Weapon1";
    private const string DataRoot = "Assets/Scripts/Data/Weapons";
    private const string BaseAnimationRoot = "Assets/Animation/Animations/Combat/Base";
    private const string WeaponAnimationRoot = "Assets/Animation/Animations/Combat/Weapon";
    private const string BaseControllerRoot = "Assets/Animation/Animation Controllers/Combat/Base";
    private const string WeaponControllerRoot = "Assets/Animation/Animation Controllers/Combat/Weapon";
    private const string WeaponPrefabRoot = "Assets/Prefabs/Weapons";
    private const string EmptyClipPath = "Assets/Animation/Animations/Empty.anim";

    private sealed class WeaponSpec
    {
        public string Name;
        public string[] BaseSprites;
        public string[] WeaponSprites;
        public WeaponAttackDetails[] Details;
        public Vector2 ColliderOffset = new Vector2(0.7f, -0.2f);
        public Vector2 ColliderSize = new Vector2(0.9f, 0.8f);
        public bool IsRanged;
        public bool IsShield;
    }

    [MenuItem("Tools/Dead Rain/Generate Weapon Implementations")]
    public static void Generate()
    {
        EnsureFolder(WeaponPrefabRoot);

        WeaponSpec[] specs =
        {
            new WeaponSpec
            {
                Name = "Fist",
                BaseSprites = Names("Base_Fist_", 1, 3),
                WeaponSprites = Names("Weapon1_Fist_", 1, 3),
                Details = Details(
                    Attack("Jab", 2.5f, 6f, 6f, new Vector2(1f, 0.5f)),
                    Attack("Cross", 3f, 8f, 8f, new Vector2(1f, 0.75f)),
                    Attack("Uppercut", 2f, 12f, 12f, new Vector2(1f, 1.4f))),
                ColliderOffset = new Vector2(0.45f, -0.2f),
                ColliderSize = new Vector2(0.65f, 0.65f)
            },
            new WeaponSpec
            {
                Name = "Knife",
                BaseSprites = Names("Base_Knife_", 1, 4),
                WeaponSprites = Names("Weapon1_Knife_", 1, 4),
                Details = Details(
                    Attack("Quick Stab", 4.5f, 8f, 8f, new Vector2(1f, 0.5f)),
                    Attack("Backhand Slash", 4f, 10f, 10f, new Vector2(1f, 0.8f)),
                    Attack("Rising Cut", 3f, 12f, 12f, new Vector2(1f, 1.3f)),
                    Attack("Finisher", 5f, 15f, 14f, new Vector2(1f, 1f))),
                ColliderOffset = new Vector2(0.65f, -0.2f),
                ColliderSize = new Vector2(0.75f, 0.65f)
            },
            new WeaponSpec
            {
                Name = "Shield",
                BaseSprites = new[] { "Base_Shield_Enter", "Base_Shield_Parry", "Base_Shield_Break" },
                WeaponSprites = new[] { "Weapon1_Shield_Enter", "Weapon1_Shield_Parry", "Weapon1_Shield_Break" },
                Details = Details(
                    Attack("Shield Bash", 2f, 7f, 18f, new Vector2(1f, 0.5f)),
                    Attack("Parry Slam", 1.5f, 9f, 24f, new Vector2(1f, 0.8f)),
                    Attack("Guard Break", 2.5f, 14f, 28f, new Vector2(1f, 1f))),
                ColliderOffset = new Vector2(0.55f, -0.1f),
                ColliderSize = new Vector2(0.8f, 1.05f),
                IsShield = true
            },
            new WeaponSpec
            {
                Name = "Bow",
                BaseSprites = new[] { "Base_Bow_1" },
                WeaponSprites = new[] { "Weapon1_Bow_1" },
                Details = Details(Attack("Bow Strike", 1f, 9f, 12f, new Vector2(1f, 0.6f))),
                ColliderOffset = new Vector2(0.8f, -0.15f),
                ColliderSize = new Vector2(1.1f, 0.65f),
                IsRanged = true
            },
            new WeaponSpec
            {
                Name = "Book",
                BaseSprites = new[] { "Base_Book_Throw" },
                WeaponSprites = new[] { "Weapon1_Book_Throw" },
                Details = Details(Attack("Arcane Toss", 1f, 11f, 14f, new Vector2(1f, 0.8f))),
                ColliderOffset = new Vector2(0.75f, -0.1f),
                ColliderSize = new Vector2(0.85f, 0.85f)
            }
        };

        var createdWeapons = new List<Weapon>();
        ShieldWeapon shield = null;
        foreach (WeaponSpec spec in specs)
        {
            SO_AggressiveWeaponData data = CreateWeaponData(spec);
            AnimationClip[] baseClips = CreateClips(spec, true);
            AnimationClip[] weaponClips = CreateClips(spec, false);
            AnimatorController baseController = CreateController(spec.Name, baseClips, BaseControllerRoot, "Base");
            AnimatorController weaponController = CreateController(spec.Name, weaponClips, WeaponControllerRoot, "Weapon");
            Weapon weapon = CreateSceneWeapon(spec, data, baseController, weaponController);
            if (weapon is ShieldWeapon shieldWeapon)
            {
                shield = shieldWeapon;
            }
            else
            {
                createdWeapons.Add(weapon);
            }
        }

        UpdatePlayerInventory(createdWeapons, shield);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"Generated {createdWeapons.Count} additional weapon implementations.");
    }

    private static WeaponAttackDetails Attack(string name, float speed, float damage, float knockback, Vector2 angle)
    {
        return new WeaponAttackDetails
        {
            attackName = name,
            movementSpeed = speed,
            damageAmount = damage,
            knockbackStrength = knockback,
            knockbackAngle = angle
        };
    }

    private static WeaponAttackDetails[] Details(params WeaponAttackDetails[] details) => details;

    private static string[] Names(string prefix, int first, int last)
    {
        return Enumerable.Range(first, last - first + 1).Select(i => $"{prefix}{i}").ToArray();
    }

    private static SO_AggressiveWeaponData CreateWeaponData(WeaponSpec spec)
    {
        EnsureFolder(DataRoot);
        string path = $"{DataRoot}/{spec.Name}.asset";

        if (AssetDatabase.LoadAssetAtPath<SO_AggressiveWeaponData>(path) == null)
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<SO_AggressiveWeaponData>(), path);
        }

        var data = AssetDatabase.LoadAssetAtPath<SO_AggressiveWeaponData>(path);
        var serialized = new SerializedObject(data);
        SerializedProperty attackDetails = serialized.FindProperty("attackDetails");
        attackDetails.arraySize = spec.Details.Length;

        for (int i = 0; i < spec.Details.Length; i++)
        {
            SerializedProperty entry = attackDetails.GetArrayElementAtIndex(i);
            WeaponAttackDetails details = spec.Details[i];
            entry.FindPropertyRelative("attackName").stringValue = details.attackName;
            entry.FindPropertyRelative("movementSpeed").floatValue = details.movementSpeed;
            entry.FindPropertyRelative("damageAmount").floatValue = details.damageAmount;
            entry.FindPropertyRelative("knockbackStrength").floatValue = details.knockbackStrength;
            entry.FindPropertyRelative("knockbackAngle").vector2Value = details.knockbackAngle;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static AnimationClip[] CreateClips(WeaponSpec spec, bool isBase)
    {
        string clipRoot = isBase ? BaseAnimationRoot : WeaponAnimationRoot;
        string spriteRoot = isBase ? BaseSpriteRoot : WeaponSpriteRoot;
        string folder = $"{clipRoot}/{spec.Name}";
        EnsureFolder(folder);

        string[] spriteNames = isBase ? spec.BaseSprites : spec.WeaponSprites;
        var clips = new AnimationClip[spriteNames.Length];

        for (int i = 0; i < spriteNames.Length; i++)
        {
            string clipPath = $"{folder}/{(isBase ? "Base" : "Weapon")}_{spec.Name}_{i + 1}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, clipPath);
            }

            clip.name = $"{(isBase ? "Base" : "Weapon")}_{spec.Name}_{i + 1}";
            clip.frameRate = 15f;
            float duration = SetSpriteCurve(clip, LoadSprites($"{spriteRoot}/{spriteNames[i]}.png"));

            if (isBase)
            {
                SetWeaponEvents(clip, duration);
            }

            EditorUtility.SetDirty(clip);
            clips[i] = clip;
        }

        return clips;
    }

    private static float SetSpriteCurve(AnimationClip clip, Sprite[] sprites)
    {
        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        var keyframes = sprites.Select((sprite, i) => new ObjectReferenceKeyframe
        {
            time = i / 15f,
            value = sprite
        }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        return keyframes[keyframes.Length - 1].time + 1f / 15f;
    }

    private static void SetWeaponEvents(AnimationClip clip, float duration)
    {
        float length = Mathf.Max(duration, 0.33333334f);
        float actionTime = Mathf.Min(0.13333334f, length * 0.35f);
        float stopTime = Mathf.Min(0.26666668f, length * 0.75f);
        float flipOnTime = Mathf.Min(0.3f, length * 0.9f);
        float finishTime = length;

        AnimationUtility.SetAnimationEvents(clip, new[]
        {
            Event(actionTime, "AnimationStartMovementTrigger"),
            Event(actionTime, "AnimationTurnOffFlipTrigger"),
            Event(actionTime, "AnimationActionTrigger"),
            Event(stopTime, "AnimationStopMovementTrigger"),
            Event(flipOnTime, "AnimationTurnOnFlipTrigger"),
            Event(finishTime, "AnimationFinishTrigger")
        });
    }

    private static AnimationEvent Event(float time, string functionName)
    {
        return new AnimationEvent { time = time, functionName = functionName };
    }

    private static AnimatorController CreateController(string weaponName, AnimationClip[] clips, string root, string prefix)
    {
        EnsureFolder(root);
        string path = $"{root}/{prefix}_{weaponName}_AC.controller";
        AssetDatabase.DeleteAsset(path);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("attack", AnimatorControllerParameterType.Bool);
        controller.AddParameter("attackCounter", AnimatorControllerParameterType.Int);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimationClip emptyClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(EmptyClipPath);
        AnimatorState empty = stateMachine.AddState("Empty");
        empty.motion = emptyClip;
        stateMachine.defaultState = empty;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimatorState attackState = stateMachine.AddState(clips[i].name);
            attackState.motion = clips[i];

            AnimatorStateTransition enter = stateMachine.AddAnyStateTransition(attackState);
            enter.hasExitTime = false;
            enter.duration = 0f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0f, "attack");
            enter.AddCondition(AnimatorConditionMode.Equals, i, "attackCounter");

            AnimatorStateTransition exit = attackState.AddTransition(empty);
            exit.hasExitTime = false;
            exit.duration = 0f;
            exit.AddCondition(AnimatorConditionMode.IfNot, 0f, "attack");
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static Weapon CreateSceneWeapon(
        WeaponSpec spec,
        SO_AggressiveWeaponData data,
        RuntimeAnimatorController baseController,
        RuntimeAnimatorController weaponController)
    {
        Player player = Object.FindObjectOfType<Player>();
        if (player == null)
        {
            throw new System.InvalidOperationException("No Player component found in the active scene.");
        }

        Transform weaponsRoot = player.transform.Find("Weapons");
        if (weaponsRoot == null)
        {
            var root = new GameObject("Weapons");
            root.layer = 9;
            root.transform.SetParent(player.transform, false);
            weaponsRoot = root.transform;
        }

        Transform old = weaponsRoot.Find(spec.Name);
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        GameObject rootObject = new GameObject(spec.Name);
        rootObject.layer = 9;
        rootObject.transform.SetParent(weaponsRoot, false);

        Weapon weapon;
        if (spec.IsShield)
        {
            var shieldWeapon = rootObject.AddComponent<ShieldWeapon>();
            SetObjectReference(shieldWeapon, "weaponData", data);
            ConfigureShieldWeapon(shieldWeapon);
            weapon = shieldWeapon;
        }
        else if (spec.IsRanged)
        {
            var rangedWeapon = rootObject.AddComponent<RangedWeapon>();
            SetObjectReference(rangedWeapon, "weaponData", data);
            ConfigureRangedWeapon(rangedWeapon);
            weapon = rangedWeapon;
        }
        else
        {
            var aggressiveWeapon = rootObject.AddComponent<AggressiveWeapon>();
            SetObjectReference(aggressiveWeapon, "weaponData", data);
            weapon = aggressiveWeapon;
        }

        CreateVisualChild(rootObject.transform, "Weapon", 2, weaponController, true, spec.ColliderOffset, spec.ColliderSize);
        CreateVisualChild(rootObject.transform, "Base", 1, baseController, false, Vector2.zero, Vector2.zero);

        string prefabPath = $"{WeaponPrefabRoot}/{spec.Name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(rootObject, prefabPath);
        return weapon;
    }

    private static void ConfigureRangedWeapon(RangedWeapon rangedWeapon)
    {
        Projectile arrow = AssetDatabase.LoadAssetAtPath<Projectile>("Assets/Prefabs/Arrow.prefab");
        var serialized = new SerializedObject(rangedWeapon);
        serialized.FindProperty("projectilePrefab").objectReferenceValue = arrow;
        serialized.FindProperty("projectileSpeed").floatValue = 14f;
        serialized.FindProperty("projectileTravelDistance").floatValue = 8f;
        serialized.FindProperty("projectileDamage").floatValue = 10f;
        serialized.FindProperty("projectileSpawnOffset").vector2Value = new Vector2(0.85f, -0.05f);
        serialized.FindProperty("targetMask").intValue = 1 << LayerMask.NameToLayer("Damageable");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rangedWeapon);
    }

    private static void ConfigureShieldWeapon(ShieldWeapon shieldWeapon)
    {
        var serialized = new SerializedObject(shieldWeapon);
        serialized.FindProperty("baseHoldSprite").objectReferenceValue = LoadSprites($"{BaseSpriteRoot}/Base_Shield_Hold.png").First();
        serialized.FindProperty("weaponHoldSprite").objectReferenceValue = LoadSprites($"{WeaponSpriteRoot}/Weapon1_Shield_Hold.png").First();
        serialized.FindProperty("firstParryAttackIndex").intValue = 1;
        serialized.FindProperty("parryAttackCount").intValue = 2;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(shieldWeapon);
    }

    private static void CreateVisualChild(
        Transform parent,
        string name,
        int sortingOrder,
        RuntimeAnimatorController controller,
        bool addHitbox,
        Vector2 colliderOffset,
        Vector2 colliderSize)
    {
        GameObject child = new GameObject(name);
        child.layer = 9;
        child.transform.SetParent(parent, false);

        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sortingLayerID = 687696237;
        renderer.sortingOrder = sortingOrder;

        var animator = child.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        if (addHitbox)
        {
            var collider = child.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.offset = colliderOffset;
            collider.size = colliderSize;
            child.AddComponent<WeaponHitboxToWeapon>();
        }
        else
        {
            child.AddComponent<WeaponAnimationToWeapon>();
        }
    }

    private static void UpdatePlayerInventory(List<Weapon> generatedWeapons, ShieldWeapon shield)
    {
        Player player = Object.FindObjectOfType<Player>();
        if (player == null)
        {
            return;
        }

        var inventory = player.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = player.gameObject.AddComponent<PlayerInventory>();
        }

        var weapons = new List<Weapon>();
        if (inventory.weapons != null)
        {
            weapons.AddRange(inventory.weapons.Where(weapon => weapon != null && weapon.name.Contains("Sword")));
        }

        if (weapons.Count == 0)
        {
            Transform sword = player.transform.Find("Weapons/Sword1");
            if (sword != null && sword.TryGetComponent(out Weapon swordWeapon))
            {
                weapons.Add(swordWeapon);
            }
        }

        weapons.AddRange(generatedWeapons.Where(weapon => weapon != null && !(weapon is ShieldWeapon)));
        inventory.weapons = weapons.Distinct().ToArray();
        inventory.shield = shield;
        EditorUtility.SetDirty(inventory);
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static Sprite[] LoadSprites(string path)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, System.StringComparer.Ordinal)
            .ToArray();

        if (sprites.Length == 0)
        {
            throw new FileNotFoundException($"No sprites found at {path}");
        }

        return sprites;
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
