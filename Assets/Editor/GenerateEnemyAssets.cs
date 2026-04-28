#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// 编辑器工具：一键生成 Minion 和 Boss 的占位素材及 Prefab。
/// 菜单路径：工具 → 生成 Enemy 资源 (Generate Enemy Assets)
/// 
/// 生成内容：
///   Assets/Art/Sprites/
///     placeholder_minion_32.png   —— 青色小怪精灵（32×32）
///     placeholder_boss_48.png     —— 紫色 Boss 精灵（48×48，更大）
///   Assets/Art/Animations/
///     MinionAnimator.controller   —— 含 Idle/Walk/Attack/Hit 四个占位状态
///     BossAnimator.controller     —— 含 Idle/Walk/Attack/Hit/Charge 五个占位状态
///   Assets/Prefabs/
///     MinionEnemy.prefab          —— 小怪预制体
///     BossEnemy.prefab            —— Boss 预制体
/// </summary>
public static class GenerateEnemyAssets
{
    private const string SpritesDir    = "Assets/Art/Sprites";
    private const string AnimationsDir = "Assets/Art/Animations";
    private const string PrefabsDir    = "Assets/Prefabs";

    // ────────────────────────────────────────────────────────────────────────
    [MenuItem("工具/生成 Enemy 资源 (Generate Enemy Assets)")]
    public static void GenerateAll()
    {
        EnsureDirectories();

        // 1. 占位 Sprite
        string minionSpritePath = CreateEnemySprite("placeholder_minion_32",  32, new Color(0.2f, 0.9f, 0.8f));
        string bossSpritePath   = CreateEnemySprite("placeholder_boss_48",    48, new Color(0.7f, 0.2f, 0.9f));

        // 2. AnimatorController
        string minionCtrl = CreateAnimatorController("MinionAnimator", addCharge: false);
        string bossCtrl   = CreateAnimatorController("BossAnimator",   addCharge: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 3. Prefab
        Sprite minionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(minionSpritePath);
        Sprite bossSprite   = AssetDatabase.LoadAssetAtPath<Sprite>(bossSpritePath);
        AnimatorController minionAnimCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(minionCtrl);
        AnimatorController bossAnimCtrl   = AssetDatabase.LoadAssetAtPath<AnimatorController>(bossCtrl);

        CreateMinionPrefab(minionSprite, minionAnimCtrl);
        CreateBossPrefab(bossSprite, bossAnimCtrl);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "生成完成",
            "已生成：\n" +
            "• Assets/Art/Sprites/placeholder_minion_32.png\n" +
            "• Assets/Art/Sprites/placeholder_boss_48.png\n" +
            "• Assets/Art/Animations/MinionAnimator.controller\n" +
            "• Assets/Art/Animations/BossAnimator.controller\n" +
            "• Assets/Prefabs/NormalEnemy.prefab\n" +
            "• Assets/Prefabs/BossEnemy.prefab\n\n" +
            "请在 EnemySpawner 的 enemyPrefab 字段分别指定对应 Prefab。",
            "确定");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  辅助：目录创建
    // ════════════════════════════════════════════════════════════════════════
    static void EnsureDirectories()
    {
        foreach (var dir in new[] { SpritesDir, AnimationsDir, PrefabsDir })
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.ImportAsset(dir);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  辅助：生成占位 Sprite（带边框 + 动作标记）
    // ════════════════════════════════════════════════════════════════════════
    static string CreateEnemySprite(string name, int size, Color bodyColor)
    {
        string path = $"{SpritesDir}/{name}.png";
        if (File.Exists(path))
        {
            Debug.Log($"[GenerateEnemyAssets] Sprite already exists: {path}");
            return path;
        }

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 外边框黑色
                bool isBorder = (x == 0 || x == size - 1 || y == 0 || y == size - 1);
                // 眼睛（简单两个白点在上半部分）
                int eyeY = (int)(size * 0.65f);
                int eyeL = (int)(size * 0.30f);
                int eyeR = (int)(size * 0.70f);
                bool isEye = (Mathf.Abs(x - eyeL) <= 1 && Mathf.Abs(y - eyeY) <= 1) ||
                             (Mathf.Abs(x - eyeR) <= 1 && Mathf.Abs(y - eyeY) <= 1);
                // 嘴（水平线在中间偏下）
                int mouthY = (int)(size * 0.40f);
                bool isMouth = (y == mouthY && x >= (int)(size * 0.25f) && x <= (int)(size * 0.75f));

                Color c;
                if (isBorder)       c = Color.black;
                else if (isEye)     c = Color.white;
                else if (isMouth)   c = Color.black;
                else                c = bodyColor;
                tex.SetPixel(x, y, c);
            }
        }
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.filterMode          = FilterMode.Point;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.isReadable          = false;
            importer.SaveAndReimport();
        }
        Debug.Log($"[GenerateEnemyAssets] Created sprite: {path}");
        return path;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  辅助：生成 AnimatorController（占位状态机）
    // ════════════════════════════════════════════════════════════════════════
    static string CreateAnimatorController(string name, bool addCharge)
    {
        string path = $"{AnimationsDir}/{name}.controller";
        if (File.Exists(path))
        {
            Debug.Log($"[GenerateEnemyAssets] Controller already exists: {path}");
            return path;
        }

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // ─── 参数 ───
        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack",    AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit",       AnimatorControllerParameterType.Trigger);
        if (addCharge)
            controller.AddParameter("Charge", AnimatorControllerParameterType.Trigger);

        var root = controller.layers[0].stateMachine;

        // ─── 状态 ───
        var idle   = root.AddState("Idle");
        var walk   = root.AddState("Walk");
        var attack = root.AddState("Attack");
        var hit    = root.AddState("Hit");

        idle.speed   = 1f;
        walk.speed   = 1f;
        attack.speed = 1.5f;    // 攻击动画略快
        hit.speed    = 2f;

        // 为每个状态生成对应的占位 AnimationClip
        idle.motion   = CreatePlaceholderClip($"{name}_Idle",   path, isLoop: true);
        walk.motion   = CreatePlaceholderClip($"{name}_Walk",   path, isLoop: true);
        attack.motion = CreatePlaceholderClip($"{name}_Attack", path, isLoop: false);
        hit.motion    = CreatePlaceholderClip($"{name}_Hit",    path, isLoop: false);

        // 默认入口
        root.defaultState = idle;

        // ─── 过渡 ───
        // Idle <-> Walk
        AddBoolTransition(idle,   walk,   "IsWalking", true,  0.05f);
        AddBoolTransition(walk,   idle,   "IsWalking", false, 0.05f);
        // Any -> Attack
        AddTriggerFromAny(root, attack, "Attack", 0.05f);
        AddExitTransition(attack, idle, 0.3f);
        // Any -> Hit
        AddTriggerFromAny(root, hit, "Hit", 0.05f);
        AddExitTransition(hit, idle, 0.25f);

        if (addCharge)
        {
            var charge   = root.AddState("Charge");
            charge.speed = 1.2f;
            charge.motion = CreatePlaceholderClip($"{name}_Charge", path, isLoop: false);
            AddTriggerFromAny(root, charge, "Charge", 0.05f);
            AddExitTransition(charge, idle, 0.4f);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[GenerateEnemyAssets] Created controller: {path}");
        return path;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  辅助：生成占位 AnimationClip（通过颜色闪烁模拟动画帧）
    // ════════════════════════════════════════════════════════════════════════
    static AnimationClip CreatePlaceholderClip(string clipName, string controllerPath, bool isLoop)
    {
        var clip = new AnimationClip();
        clip.name = clipName;

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = isLoop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // 占位关键帧：用 SpriteRenderer.enabled 做简单闪烁代替真实精灵序列
        // 此处用 color.a 做 0→1 的简单动画（可见变化）
        var curve = new AnimationCurve(
            new Keyframe(0f,   1f),
            new Keyframe(0.1f, 0.5f),
            new Keyframe(0.2f, 1f)
        );
        clip.SetCurve("", typeof(UnityEngine.SpriteRenderer), "m_Color.a", curve);
        clip.frameRate = 12f;

        // 将 Clip 保存为 Controller 的子资源
        AssetDatabase.AddObjectToAsset(clip, controllerPath);
        return clip;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  辅助：Transition 快捷方法
    // ════════════════════════════════════════════════════════════════════════
    static void AddBoolTransition(AnimatorState from, AnimatorState to, string param, bool value, float duration)
    {
        var t = from.AddTransition(to);
        t.hasExitTime       = false;
        t.duration          = duration;
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
    }

    static void AddTriggerFromAny(AnimatorStateMachine sm, AnimatorState to, string trigger, float duration)
    {
        var t = sm.AddAnyStateTransition(to);
        t.hasExitTime       = false;
        t.duration          = duration;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
    }

    static void AddExitTransition(AnimatorState from, AnimatorState to, float exitTime)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime    = exitTime;
        t.duration    = 0.05f;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  辅助：创建 NormalEnemy Prefab
    // ════════════════════════════════════════════════════════════════════════
    static void CreateMinionPrefab(Sprite sprite, AnimatorController ctrl)
    {
        string prefabPath = $"{PrefabsDir}/NormalEnemy.prefab";
        if (File.Exists(prefabPath))
        {
            Debug.Log($"[GenerateEnemyAssets] Prefab already exists: {prefabPath}");
            return;
        }

        var go = new GameObject("NormalEnemy");

        // ── 组件 ──
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 0.9f);

        if (ctrl != null)
        {
            var an = go.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
        }

        // 新版系统：NormalEnemy 组件会自动挂载 EnemyStateMachine、
        // EnemyAIController、StanceBar 等（通过 RequireComponent）
        var enemy = go.AddComponent<NormalEnemy>();
        // 注意：数值配置通过 ScriptableObject（EnemyDataSO）设置，
        // 此处仅创建预制体框架，具体数值请在 Inspector 中配置 SO 实例。

        go.layer = LayerMask.NameToLayer("Enemy") >= 0 ? LayerMask.NameToLayer("Enemy") : 0;

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);
        Debug.Log($"[GenerateEnemyAssets] Created prefab: {prefabPath}");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  辅助：创建 BossEnemy Prefab
    // ════════════════════════════════════════════════════════════════════════
    static void CreateBossPrefab(Sprite sprite, AnimatorController ctrl)
    {
        string prefabPath = $"{PrefabsDir}/BossEnemy.prefab";
        if (File.Exists(prefabPath))
        {
            Debug.Log($"[GenerateEnemyAssets] Prefab already exists: {prefabPath}");
            return;
        }

        var go = new GameObject("BossEnemy");

        // ── 组件 ──
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.mass = 3f;    // Boss 质量更大，不易被推开

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.4f, 1.4f);   // 碰撞体也更大

        if (ctrl != null)
        {
            var an = go.AddComponent<Animator>();
            an.runtimeAnimatorController = ctrl;
        }

        // 新版系统：BossEnemy 组件会自动挂载依赖组件
        var boss = go.AddComponent<BossEnemy>();
        // 注意：数值配置通过 ScriptableObject（EnemyDataSO）设置，
        // 此处仅创建预制体框架，具体数值请在 Inspector 中配置 SO 实例。

        go.layer = LayerMask.NameToLayer("Enemy") >= 0 ? LayerMask.NameToLayer("Enemy") : 0;

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        UnityEngine.Object.DestroyImmediate(go);
        Debug.Log($"[GenerateEnemyAssets] Created prefab: {prefabPath}");
    }
}
#endif
