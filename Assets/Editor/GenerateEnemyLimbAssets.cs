#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// GenerateEnemyLimbAssets — Editor tool that rebuilds Minion and Boss prefabs
/// with a proper multi-sprite limb hierarchy.
/// 
/// Menu: 工具 → 生成带肢体的 Enemy Prefab (Generate Limbed Enemy Prefabs)
/// 
/// What it creates / updates:
///   Assets/Art/Sprites/
///     minion_body.png    — cyan   torso placeholder   20×20 px
///     minion_head.png    — cyan   head placeholder    10×10 px
///     minion_arm.png     — cyan   arm placeholder      8×4  px
///     minion_leg.png     — cyan   leg placeholder      6×8  px
///     minion_weapon.png  — orange dagger placeholder   4×12 px
///
///     boss_body.png      — purple torso placeholder   36×36 px
///     boss_head.png      — purple head placeholder    18×18 px
///     boss_arm.png       — purple arm placeholder     14×6  px
///     boss_leg.png       — purple leg placeholder     10×14 px
///     boss_weapon.png    — dark-orange axe placeholder  8×20 px
///     boss_shield.png    — steel-blue shield placeholder 8×16 px
///
///   Assets/Prefabs/
///     MinionEnemy.prefab  — rebuilt with Body/Head/ArmL/ArmR/LegL/LegR/Weapon hierarchy
///     BossEnemy.prefab    — rebuilt with Body/Head/ArmL/ArmR/LegL/LegR/Weapon/Shield
/// </summary>
public static class GenerateEnemyLimbAssets
{
    private const string SpritesDir = "Assets/Art/Sprites";
    private const string AnimDir    = "Assets/Art/Animations";
    private const string PrefabsDir = "Assets/Prefabs";

    [MenuItem("工具/生成带肢体的 Enemy Prefab (Generate Limbed Enemy Prefabs)")]
    public static void Generate()
    {
        EnsureDirectories();

        // ── Generate sprites ──
        string mBodyPath   = MakeSprite("minion_body",   20, 20, new Color(0.2f, 0.9f, 0.8f));
        string mHeadPath   = MakeSprite("minion_head",   10, 10, new Color(0.2f, 0.9f, 0.8f));
        string mArmPath    = MakeSprite("minion_arm",     8,  4, new Color(0.2f, 0.9f, 0.8f));
        string mLegPath    = MakeSprite("minion_leg",     6,  8, new Color(0.2f, 0.9f, 0.8f));
        string mWeaponPath = MakeSprite("minion_weapon",  4, 12, new Color(0.9f, 0.55f, 0.1f)); // rusty dagger

        string bBodyPath   = MakeSprite("boss_body",     36, 36, new Color(0.6f, 0.15f, 0.85f));
        string bHeadPath   = MakeSprite("boss_head",     18, 18, new Color(0.6f, 0.15f, 0.85f));
        string bArmPath    = MakeSprite("boss_arm",      14,  6, new Color(0.6f, 0.15f, 0.85f));
        string bLegPath    = MakeSprite("boss_leg",      10, 14, new Color(0.6f, 0.15f, 0.85f));
        string bWeaponPath = MakeSprite("boss_weapon",    8, 20, new Color(0.85f, 0.4f, 0.1f)); // dark axe
        string bShieldPath = MakeSprite("boss_shield",    8, 16, new Color(0.4f, 0.55f, 0.8f)); // steel blue

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── Load sprites ──
        Sprite mBody   = LoadSprite(mBodyPath);
        Sprite mHead   = LoadSprite(mHeadPath);
        Sprite mArm    = LoadSprite(mArmPath);
        Sprite mLeg    = LoadSprite(mLegPath);
        Sprite mWeapon = LoadSprite(mWeaponPath);

        Sprite bBody   = LoadSprite(bBodyPath);
        Sprite bHead   = LoadSprite(bHeadPath);
        Sprite bArm    = LoadSprite(bArmPath);
        Sprite bLeg    = LoadSprite(bLegPath);
        Sprite bWeapon = LoadSprite(bWeaponPath);
        Sprite bShield = LoadSprite(bShieldPath);

        // Load existing animator controllers
        AnimatorController minionCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            $"{AnimDir}/MinionAnimator.controller");
        AnimatorController bossCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            $"{AnimDir}/BossAnimator.controller");

        // ── Rebuild prefabs ──
        BuildMinionPrefab(mBody, mHead, mArm, mLeg, mWeapon, minionCtrl);
        BuildBossPrefab(bBody, bHead, bArm, bLeg, bWeapon, bShield, bossCtrl);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成",
            "已重建带肢体层级的 MinionEnemy.prefab 和 BossEnemy.prefab。\n\n" +
            "请在 EnemySpawner 中重新引用新 Prefab。",
            "确定");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Prefab builders
    // ════════════════════════════════════════════════════════════════════════

    static void BuildMinionPrefab(Sprite body, Sprite head, Sprite arm,
                                   Sprite leg, Sprite weapon,
                                   AnimatorController ctrl)
    {
        string path = $"{PrefabsDir}/MinionEnemy.prefab";
        // Force re-create
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

        var root = new GameObject("MinionEnemy");

        // ── Root components ──
        var rootSr = root.AddComponent<SpriteRenderer>();
        rootSr.enabled = false; // root has no visible sprite; children do

        var rb = root.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = root.AddComponent<BoxCollider2D>();
        col.size   = new Vector2(0.5f, 0.7f);
        col.offset = new Vector2(0f, 0.1f);

        if (ctrl != null)
        {
            var an = root.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
        }

        var enemy = root.AddComponent<MinionEnemy>();
        enemy.maxHealth     = 3;
        enemy.contactDamage = 1;
        enemy.chaseRange    = 4f;
        enemy.attackInterval = 1f;

        root.AddComponent<EnemyLimbs>();
        root.AddComponent<EnemyEquipment>();
        root.AddComponent<EnemyEffectSystem>();

        root.layer = SafeLayer("Enemy");

        // ── Leg L ──
        var legL = MakeLimbGO(root.transform, "LegL", leg, new Vector3(-0.12f, -0.32f, 0f), 10);
        // ── Leg R ──
        var legR = MakeLimbGO(root.transform, "LegR", leg, new Vector3( 0.12f, -0.32f, 0f), 10);
        // ── Body ──
        var bodyGO = MakeLimbGO(root.transform, "Body", body, new Vector3(0f, 0f, 0f), 11);
        // ── Head ──
        MakeLimbGO(bodyGO.transform, "Head", head, new Vector3(0f, 0.35f, 0f), 12);
        // ── ArmL ──
        MakeLimbGO(bodyGO.transform, "ArmL", arm, new Vector3(-0.28f, 0.05f, 0f), 11);
        // ── ArmR + Weapon ──
        var armRGO = MakeLimbGO(bodyGO.transform, "ArmR", arm, new Vector3( 0.28f, 0.05f, 0f), 11);
        MakeLimbGO(armRGO.transform, "Weapon", weapon, new Vector3(0f, -0.28f, 0f), 12);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[GenerateEnemyLimbAssets] Built: {path}");
    }

    static void BuildBossPrefab(Sprite body, Sprite head, Sprite arm,
                                 Sprite leg, Sprite weapon, Sprite shield,
                                 AnimatorController ctrl)
    {
        string path = $"{PrefabsDir}/BossEnemy.prefab";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

        var root = new GameObject("BossEnemy");

        var rootSr = root.AddComponent<SpriteRenderer>();
        rootSr.enabled = false;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.mass = 4f;

        var col = root.AddComponent<BoxCollider2D>();
        col.size   = new Vector2(0.9f, 1.2f);
        col.offset = new Vector2(0f, 0.1f);

        if (ctrl != null)
        {
            var an = root.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
        }

        var boss = root.AddComponent<BossEnemy>();
        boss.maxHealth     = 25;
        boss.contactDamage = 3;
        boss.chaseRange    = 5f;
        boss.attackInterval = 1.5f;
        boss.sizeMultiplier = 1.8f;

        root.AddComponent<EnemyLimbs>();

        // Boss equipment: heavy sword + armor
        var eq = root.AddComponent<EnemyEquipment>();
        eq.weapon = new EnemyEquipment.WeaponData
        {
            weaponName      = "War Axe",
            damageBonus     = 2,
            attackRange     = 1.0f,
            cooldownOverride = 1.8f,
            weaponTint      = new Color(0.85f, 0.4f, 0.1f)
        };
        eq.armor = new EnemyEquipment.ArmorData
        {
            armorName       = "Heavy Plate",
            damageReduction = 1,
            blockChance     = 0.15f,
            armorTint       = new Color(0.4f, 0.55f, 0.8f)
        };

        root.AddComponent<EnemyEffectSystem>();

        root.layer = SafeLayer("Enemy");

        // ── Legs ──
        MakeLimbGO(root.transform, "LegL", leg, new Vector3(-0.22f, -0.55f, 0f), 10);
        MakeLimbGO(root.transform, "LegR", leg, new Vector3( 0.22f, -0.55f, 0f), 10);

        // ── Body ──
        var bodyGO = MakeLimbGO(root.transform, "Body", body, Vector3.zero, 11);

        // ── Head ──
        MakeLimbGO(bodyGO.transform, "Head", head, new Vector3(0f, 0.62f, 0f), 12);

        // ── ArmL + Shield ──
        var armLGO = MakeLimbGO(bodyGO.transform, "ArmL", arm, new Vector3(-0.5f, 0.08f, 0f), 11);
        var shieldGO = MakeLimbGO(armLGO.transform, "Shield", shield, new Vector3(0f, -0.4f, 0f), 12);

        // ── ArmR + Weapon ──
        var armRGO = MakeLimbGO(bodyGO.transform, "ArmR", arm, new Vector3( 0.5f, 0.08f, 0f), 11);
        var weapGO = MakeLimbGO(armRGO.transform, "Weapon", weapon, new Vector3(0f, -0.5f, 0f), 12);

        // Wire up equipment visual overrides
        eq.weaponTransform = weapGO.transform;
        eq.shieldTransform = shieldGO.transform;

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[GenerateEnemyLimbAssets] Built: {path}");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    static GameObject MakeLimbGO(Transform parent, string name, Sprite sprite,
                                   Vector3 localPos, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        if (sprite != null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprite;
            sr.sortingOrder = sortingOrder;
        }
        return go;
    }

    static string MakeSprite(string name, int w, int h, Color fill)
    {
        string path = $"{SpritesDir}/{name}.png";
        if (File.Exists(path)) return path;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            bool border = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
            tex.SetPixel(x, y, border ? Color.black : fill);
        }
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType         = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = 32;
            imp.filterMode          = FilterMode.Point;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.isReadable          = false;
            imp.SaveAndReimport();
        }
        return path;
    }

    static Sprite LoadSprite(string path) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(path);

    static int SafeLayer(string layerName)
    {
        int idx = LayerMask.NameToLayer(layerName);
        return idx >= 0 ? idx : 0;
    }

    static void EnsureDirectories()
    {
        foreach (var d in new[] { SpritesDir, AnimDir, PrefabsDir })
            if (!Directory.Exists(d)) Directory.CreateDirectory(d);
    }
}
#endif
