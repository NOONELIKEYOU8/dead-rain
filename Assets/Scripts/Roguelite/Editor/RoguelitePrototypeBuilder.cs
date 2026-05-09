#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RoguelitePrototypeBuilder
{
    [MenuItem("Tools/Dead Rain/Build Bronze Roguelite Prototype")]
    public static void Build()
    {
        EnsureFolder("Assets/Scripts/Roguelite/GeneratedData");
        EnsureFolder("Assets/Scripts/Roguelite/GeneratedData/Eras");
        EnsureFolder("Assets/Scripts/Roguelite/GeneratedData/Enemies");
        EnsureFolder("Assets/Scripts/Roguelite/GeneratedData/Bosses");
        EnsureFolder("Assets/Scripts/Roguelite/GeneratedData/Items");
        EnsureFolder("Assets/Scripts/Roguelite/GeneratedData/Runtime");
        EnsureFolder("Assets/Prefabs/Roguelite");
        EnsureFolder("Assets/Prefabs/Roguelite/Enemies");
        EnsureFolder("Assets/Prefabs/Roguelite/Bosses");
        EnsureFolder("Assets/Prefabs/Roguelite/Stage");

        ImportBronzeActionSprites();
        Sprite bronzeSprite = ImportBronzeSprite();
        DifficultyScalingData difficulty = CreateDifficulty();
        ItemData[] items = CreateItems(bronzeSprite);
        EnemyData[] enemies = CreateEnemyData(bronzeSprite);
        EnemyActionSpriteSet spearSprites = CreateSpriteSet("BronzeSpearSoldierSprites", "bronze_spear_soldier_sprites", "spear", false);
        EnemyActionSpriteSet priestSprites = CreateSpriteSet("OraclePriestSprites", "oracle_priest_sprites", "priest", true);
        EnemyActionSpriteSet bruteSprites = CreateSpriteSet("BeastMaskAutomatonSprites", "beast_mask_automaton_sprites", "brute", false);
        EnemyActionSpriteSet bossSprites = CreateSpriteSet("BronzeKingShadowSprites", "bronze_king_shadow_sprites", "boss", true);
        RunRewardTableData rewards = CreateRewards(items);

        GameObject spearPrefab = CreateRuntimeEnemyPrefab("BronzeSpearSoldier", enemies[0], new Color(0.72f, 0.48f, 0.22f, 1f), spearSprites);
        CreateRuntimeEnemyPrefab("OraclePriest", enemies[1], new Color(0.78f, 0.78f, 0.58f, 1f), priestSprites);
        CreateRuntimeEnemyPrefab("BeastMaskAutomaton", enemies[2], new Color(0.35f, 0.75f, 0.68f, 1f), bruteSprites);

        BossData bronzeBoss = CreateBronzeBossData(bronzeSprite);
        GameObject warningPrefab = CreateWarningPrefab(bronzeSprite);
        CreateBossPrefab(bronzeBoss, enemies[0], spearPrefab, warningPrefab, bronzeSprite, bossSprites);

        EraStageData[] eras = CreateEras(bronzeSprite, enemies, bronzeBoss, items);
        CreateBronzeScene(difficulty, eras, rewards, bronzeSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built Bronze Roguelite prototype scene and data.");
    }

    private static Sprite ImportBronzeSprite()
    {
        string spritePath = "Assets/Sprites/Generated/Bronze/bronze_sprite_sheet.png";
        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            if (texture != null)
            {
                float cellWidth = texture.width / 2f;
                float cellHeight = texture.height / 3f;
                string[] names =
                {
                    "bronze_spear",
                    "bronze_priest",
                    "bronze_brute",
                    "bronze_boss",
                    "bronze_warning",
                    "bronze_ornament"
                };
                SpriteMetaData[] meta = new SpriteMetaData[names.Length];
                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 2; col++)
                    {
                        int index = row * 2 + col;
                        meta[index] = new SpriteMetaData
                        {
                            name = names[index],
                            rect = new Rect(col * cellWidth, texture.height - (row + 1) * cellHeight, cellWidth, cellHeight),
                            alignment = (int)SpriteAlignment.BottomCenter,
                            pivot = new Vector2(0.5f, 0f)
                        };
                    }
                }
#pragma warning disable 0618
                importer.spritesheet = meta;
#pragma warning restore 0618
            }
            importer.SaveAndReimport();
        }

        return LoadBronzeSprite("bronze_ornament");
    }

    private static void ImportBronzeActionSprites()
    {
        string spritePath = "Assets/Sprites/Generated/Bronze/bronze_action_sheet.png";
        if (!File.Exists(spritePath))
        {
            return;
        }

        AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
        if (texture != null)
        {
            float cellWidth = texture.width / 4f;
            float cellHeight = texture.height / 4f;
            string[] names =
            {
                "spear_idle", "spear_move", "spear_attack", "spear_hurt",
                "priest_idle", "priest_cast", "priest_attack", "priest_hurt",
                "brute_idle", "brute_windup", "brute_charge", "brute_hurt",
                "boss_idle", "boss_attack", "boss_charge", "boss_cast"
            };
            SpriteMetaData[] meta = new SpriteMetaData[names.Length];
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    int index = row * 4 + col;
                    meta[index] = new SpriteMetaData
                    {
                        name = names[index],
                        rect = new Rect(col * cellWidth, texture.height - (row + 1) * cellHeight, cellWidth, cellHeight),
                        alignment = (int)SpriteAlignment.BottomCenter,
                        pivot = new Vector2(0.5f, 0f)
                    };
                }
            }
#pragma warning disable 0618
            importer.spritesheet = meta;
#pragma warning restore 0618
        }

        importer.SaveAndReimport();
    }

    private static Sprite LoadBronzeSprite(string spriteName)
    {
        Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/Sprites/Generated/Bronze/bronze_sprite_sheet.png");
        foreach (Object sprite in sprites)
        {
            if (sprite.name == spriteName)
            {
                return sprite as Sprite;
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Generated/Bronze/bronze_sprite_sheet.png");
    }

    private static Sprite LoadActionSprite(string spriteName)
    {
        Object[] sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath("Assets/Sprites/Generated/Bronze/bronze_action_sheet.png");
        foreach (Object sprite in sprites)
        {
            if (sprite.name == spriteName)
            {
                return sprite as Sprite;
            }
        }

        return null;
    }

    private static EnemyActionSpriteSet CreateSpriteSet(string file, string id, string prefix, bool caster)
    {
        EnemyActionSpriteSet set = CreateAsset<EnemyActionSpriteSet>("Assets/Scripts/Roguelite/GeneratedData/Enemies/" + file + ".asset");
        set.id = id;
        set.idle = LoadActionSprite(prefix + "_idle");
        set.move = LoadActionSprite(prefix == "brute" ? "brute_windup" : prefix + "_move");
        set.attack = LoadActionSprite(prefix + "_attack");
        set.cast = caster ? LoadActionSprite(prefix + "_cast") : set.attack;
        set.charge = LoadActionSprite(prefix == "brute" ? "brute_charge" : prefix + "_charge");
        set.hurt = LoadActionSprite(prefix + "_hurt");
        EditorUtility.SetDirty(set);
        return set;
    }

    private static DifficultyScalingData CreateDifficulty()
    {
        DifficultyScalingData difficulty = CreateAsset<DifficultyScalingData>("Assets/Scripts/Roguelite/GeneratedData/Runtime/DefaultDifficulty.asset");
        difficulty.id = "default_risk_scaling";
        difficulty.displayName = "时间裂隙难度";
        difficulty.description = "随时间和已通关时代推进的局内难度曲线。";
        difficulty.baseDifficulty = 1f;
        difficulty.timeScale = 0.018f;
        difficulty.eraScale = 0.65f;
        difficulty.multiplierPerDifficulty = 0.16f;
        difficulty.baseSpawnInterval = 4.5f;
        difficulty.minSpawnInterval = 1.5f;
        difficulty.maxAliveEnemies = 8;
        difficulty.baseEliteChance = 0.02f;
        difficulty.eliteChancePerMinute = 0.018f;
        difficulty.maxEliteChance = 0.35f;
        difficulty.baseDropChance = 0.12f;
        difficulty.dropChanceDifficultyBonus = 0.01f;
        difficulty.maxDropChance = 0.45f;
        difficulty.specialMechanics = "difficultyLevel = baseDifficulty + elapsedTime * timeScale + clearedEraCount * eraScale";
        EditorUtility.SetDirty(difficulty);
        return difficulty;
    }

    private static ItemData[] CreateItems(Sprite icon)
    {
        ItemData bronzeCharm = CreateItem("BronzeBeastCharm", "bronze_beast_charm", "青铜兽纹符", "近战伤害提高，叠层继续提高。", EraId.Bronze, ItemEffectType.MeleeDamagePercent, 0.15f, 0.08f, 0f, 0f, ContentTier.Common, icon);
        ItemData warScroll = CreateItem("WarBambooScroll", "war_bamboo_scroll", "兵家竹简", "降低翻滚冷却。", EraId.QinHanThreeKingdoms, ItemEffectType.DashCooldownPercent, 0.12f, 0.06f, 0.65f, 0f, ContentTier.Common, icon);
        ItemData jade = CreateItem("TangJadePendant", "tang_jade_pendant", "唐风玉佩", "击杀敌人后小幅回血，有触发间隔。", EraId.TangSong, ItemEffectType.HealOnKill, 3f, 1.5f, 0f, 1.2f, ContentTier.Uncommon, icon);
        ItemData firearm = CreateItem("FirearmParts", "firearm_parts", "火铳零件", "攻击时概率追加远程弹丸。", EraId.MingQing, ItemEffectType.BonusProjectileChance, 0.12f, 0.07f, 0.55f, 0f, ContentTier.Uncommon, icon);
        ItemData echo = CreateItem("EraEcho", "era_echo", "时代残响", "每进入新朝代副本时获得额外成长。", EraId.Bronze, ItemEffectType.EraAdvanceGrowth, 0.05f, 0.03f, 0f, 0f, ContentTier.Rare, icon);
        return new[] { bronzeCharm, warScroll, jade, firearm, echo };
    }

    private static ItemData CreateItem(string file, string id, string displayName, string description, EraId era, ItemEffectType effect, float baseValue, float stackValue, float maxValue, float cooldown, ContentTier tier, Sprite icon)
    {
        ItemData item = CreateAsset<ItemData>("Assets/Scripts/Roguelite/GeneratedData/Items/" + file + ".asset");
        item.id = id;
        item.displayName = displayName;
        item.description = description;
        item.era = era;
        item.effectType = effect;
        item.baseValue = baseValue;
        item.stackValue = stackValue;
        item.maxValue = maxValue;
        item.triggerCooldown = cooldown;
        item.iconPlaceholder = icon;
        item.tier = tier;
        item.specialMechanics = description;
        EditorUtility.SetDirty(item);
        return item;
    }

    private static EnemyData[] CreateEnemyData(Sprite icon)
    {
        EnemyData spear = CreateEnemy("BronzeSpearSoldier", "bronze_spear_soldier", "青铜戈兵", "商周裂隙中的近战突刺敌人。", 38f, 8f, 2.1f, EnemyAttackPattern.PatrolMelee, RuntimeEnemyRole.Melee, ContentTier.Common, icon);
        EnemyData priest = CreateEnemy("OraclePriest", "oracle_priest", "甲骨祭司", "远程释放甲骨咒术弹。", 28f, 7f, 1.6f, EnemyAttackPattern.RangedCaster, RuntimeEnemyRole.Ranged, ContentTier.Common, icon);
        EnemyData brute = CreateEnemy("BeastMaskAutomaton", "beast_mask_automaton", "兽面傀儡", "慢速高血量冲锋敌人。", 80f, 13f, 1.35f, EnemyAttackPattern.ChargeBruiser, RuntimeEnemyRole.Charger, ContentTier.Uncommon, icon);
        return new[] { spear, priest, brute };
    }

    private static EnemyData CreateEnemy(string file, string id, string displayName, string description, float hp, float damage, float speed, EnemyAttackPattern pattern, RuntimeEnemyRole role, ContentTier tier, Sprite icon)
    {
        EnemyData enemy = CreateAsset<EnemyData>("Assets/Scripts/Roguelite/GeneratedData/Enemies/" + file + ".asset");
        enemy.id = id;
        enemy.displayName = displayName;
        enemy.description = description;
        enemy.era = EraId.Bronze;
        enemy.hp = hp;
        enemy.damage = damage;
        enemy.moveSpeed = speed;
        enemy.attackPattern = pattern;
        enemy.runtimeRole = role;
        enemy.tier = tier;
        enemy.iconPlaceholder = icon;
        enemy.specialMechanics = description;
        EditorUtility.SetDirty(enemy);
        return enemy;
    }

    private static RunRewardTableData CreateRewards(ItemData[] items)
    {
        RunRewardTableData rewards = CreateAsset<RunRewardTableData>("Assets/Scripts/Roguelite/GeneratedData/Runtime/BronzeRewardTable.asset");
        rewards.id = "bronze_reward_table";
        rewards.displayName = "商周遗物奖励";
        rewards.description = "青铜器时期 Boss 后的三选一奖励池。";
        rewards.era = EraId.Bronze;
        rewards.rewards = items;
        rewards.rewardChoiceCount = 3;
        EditorUtility.SetDirty(rewards);
        return rewards;
    }

    private static BossData CreateBronzeBossData(Sprite icon)
    {
        BossData boss = CreateAsset<BossData>("Assets/Scripts/Roguelite/GeneratedData/Bosses/BronzeKingShadow.asset");
        boss.id = "bronze_king_shadow";
        boss.displayName = "青铜王影";
        boss.description = "时间裂隙中的商王武丁意象化青铜王影。";
        boss.era = EraId.Bronze;
        boss.hp = 320f;
        boss.damage = 16f;
        boss.moveSpeed = 2.2f;
        boss.baseSkillCooldown = 2.4f;
        boss.phaseTwoCooldownMultiplier = 0.62f;
        boss.iconPlaceholder = icon;
        boss.specialMechanics = "青铜横扫、兽面冲锋、祭器震荡、召唤青铜戈兵；半血后加速。";
        EditorUtility.SetDirty(boss);
        return boss;
    }

    private static GameObject CreateRuntimeEnemyPrefab(string name, EnemyData enemy, Color tint, EnemyActionSpriteSet spriteSet)
    {
        string path = "Assets/Prefabs/Roguelite/Enemies/" + name + ".prefab";
        Sprite bronzeSprite = LoadBronzeSprite(name == "BronzeSpearSoldier" ? "bronze_spear" : name == "OraclePriest" ? "bronze_priest" : "bronze_brute");
        GameObject root = new GameObject(name);
        root.tag = "Enemy";
        SetLayerRecursive(root, LayerMask.NameToLayer("Damageable"));

        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = spriteSet != null && spriteSet.idle != null ? spriteSet.idle : bronzeSprite;
        sr.color = tint;
        sr.sortingLayerName = "Enemy";
        sr.sortingOrder = 2;
        RuntimeSpriteAnimator spriteAnimator = root.AddComponent<RuntimeSpriteAnimator>();
        SetRef(spriteAnimator, "spriteSet", spriteSet);

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 5f;

        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(enemy.runtimeRole == RuntimeEnemyRole.Charger ? 1.3f : 0.8f, enemy.runtimeRole == RuntimeEnemyRole.Charger ? 1.5f : 1.35f);
        col.offset = new Vector2(0f, -0.1f);

        EnemyRuntime runtime = root.AddComponent<EnemyRuntime>();
        root.AddComponent<EnemyDeathReporter>();

        GameObject attack = new GameObject("AttackPoint");
        attack.transform.SetParent(root.transform);
        attack.transform.localPosition = new Vector3(0.9f, 0f, 0f);

        GameObject projectile = new GameObject("ProjectileSpawn");
        projectile.transform.SetParent(root.transform);
        projectile.transform.localPosition = new Vector3(0.75f, 0.25f, 0f);

        GameObject core = new GameObject("Core", typeof(Core));
        core.transform.SetParent(root.transform);
        core.transform.localPosition = Vector3.zero;

        GameObject statsGo = new GameObject("Stats", typeof(Stats));
        statsGo.transform.SetParent(core.transform);
        statsGo.transform.localPosition = Vector3.zero;
        SetFloat(statsGo.GetComponent<Stats>(), "maxHealth", enemy.hp);

        SetRef(runtime, "data", enemy);
        SetRef(runtime, "attackPoint", attack.transform);
        SetRef(runtime, "projectileSpawn", projectile.transform);
        SetRef(runtime, "projectilePrefab", AssetDatabase.LoadAssetAtPath<Projectile>("Assets/Prefabs/Arrow.prefab"));
        SetPrivate(runtime, "playerMask", LayerMask.GetMask("Player"));
        SetPrivate(runtime, "groundMask", LayerMask.GetMask("Ground"));
        SetFloat(runtime, "attackRange", enemy.runtimeRole == RuntimeEnemyRole.Charger ? 1.25f : 1.05f);
        SetFloat(runtime, "rangedRange", 7f);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        enemy.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        enemy.prefabPathPlaceholder = path;
        EditorUtility.SetDirty(enemy);
        return enemy.prefab;
    }

    private static GameObject CreateWarningPrefab(Sprite sprite)
    {
        string path = "Assets/Prefabs/Roguelite/Stage/BronzeShockwaveWarning.prefab";
        GameObject warning = new GameObject("BronzeShockwaveWarning");
        SpriteRenderer sr = warning.AddComponent<SpriteRenderer>();
        sr.sprite = LoadBronzeSprite("bronze_warning") != null ? LoadBronzeSprite("bronze_warning") : sprite;
        sr.color = new Color(1f, 0.35f, 0.05f, 0.38f);
        sr.sortingLayerName = "Enemy";
        sr.sortingOrder = 1;
        PrefabUtility.SaveAsPrefabAsset(warning, path);
        Object.DestroyImmediate(warning);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void CreateBossPrefab(BossData bossData, EnemyData summonEnemy, GameObject summonPrefab, GameObject warningPrefab, Sprite sprite, EnemyActionSpriteSet spriteSet)
    {
        string path = "Assets/Prefabs/Roguelite/Bosses/BronzeKingShadow.prefab";
        GameObject root = new GameObject("BronzeKingShadow");
        root.tag = "Enemy";
        SetLayerRecursive(root, LayerMask.NameToLayer("Damageable"));

        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = spriteSet != null && spriteSet.idle != null ? spriteSet.idle : LoadBronzeSprite("bronze_boss") != null ? LoadBronzeSprite("bronze_boss") : sprite;
        sr.color = new Color(0.42f, 0.25f, 0.12f, 1f);
        sr.sortingLayerName = "Enemy";
        sr.sortingOrder = 3;
        RuntimeSpriteAnimator spriteAnimator = root.AddComponent<RuntimeSpriteAnimator>();
        SetRef(spriteAnimator, "spriteSet", spriteSet);

        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 5f;
        rb.mass = 4f;

        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.9f, 2.4f);
        col.offset = new Vector2(0f, 0.15f);

        BronzeKingShadowBoss boss = root.AddComponent<BronzeKingShadowBoss>();
        root.AddComponent<EnemyDeathReporter>();

        GameObject attack = new GameObject("AttackPoint");
        attack.transform.SetParent(root.transform);
        attack.transform.localPosition = new Vector3(1.25f, 0.15f, 0f);

        GameObject summon = new GameObject("SummonPoint");
        summon.transform.SetParent(root.transform);
        summon.transform.localPosition = Vector3.zero;

        GameObject core = new GameObject("Core", typeof(Core));
        core.transform.SetParent(root.transform);
        core.transform.localPosition = Vector3.zero;

        GameObject stats = new GameObject("Stats", typeof(Stats));
        stats.transform.SetParent(core.transform);
        stats.transform.localPosition = Vector3.zero;
        SetFloat(stats.GetComponent<Stats>(), "maxHealth", bossData.hp);

        SetRef(boss, "bossData", bossData);
        SetRef(boss, "attackPoint", attack.transform);
        SetRef(boss, "summonPoint", summon.transform);
        SetRef(boss, "summonEnemyData", summonEnemy);
        SetRef(boss, "summonEnemyPrefab", summonPrefab);
        SetRef(boss, "warningAreaPrefab", warningPrefab);
        SetPrivate(boss, "playerMask", LayerMask.GetMask("Player"));

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        bossData.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        bossData.prefabPathPlaceholder = path;
        EditorUtility.SetDirty(bossData);
    }

    private static EraStageData[] CreateEras(Sprite icon, EnemyData[] enemies, BossData bronzeBoss, ItemData[] items)
    {
        EraStageData bronze = CreateAsset<EraStageData>("Assets/Scripts/Roguelite/GeneratedData/Eras/BronzeEra.asset");
        bronze.id = "era_bronze";
        bronze.displayName = "商周 / 青铜器时期";
        bronze.description = "青铜纹样、祭祀台、甲骨文和地下遗迹构成的第一个裂隙副本。";
        bronze.era = EraId.Bronze;
        bronze.visualTheme = "青铜纹样、祭祀台、甲骨文、古战车、火光、地下遗迹";
        bronze.enemyPool = enemies;
        bronze.finalBoss = bronzeBoss;
        bronze.itemDropPool = items;
        bronze.prefabPathPlaceholder = "Assets/Scenes/Era_Bronze.unity";
        bronze.tilemapPrefabPathPlaceholder = "TODO: Bronze tilemap prefab";
        bronze.backgroundMusicPlaceholder = "TODO: Bronze ritual drums loop";
        bronze.iconPlaceholder = icon;
        bronze.unlockCondition = "Run start";
        bronze.specialMechanics = "召唤青铜器图腾，周期性释放范围攻击。";
        EditorUtility.SetDirty(bronze);

        string[] files = { "QinHanThreeKingdoms", "TangSong", "MingQing", "ModernFounding" };
        string[] display = { "秦汉 / 三国时期", "唐宋时期", "明清时期", "近现代至中华人民共和国成立时期" };
        string[] themes = { "秦俑、长城、烽火台、汉军营、三国战场", "长安街市、飞檐楼阁、山水、机关、火药雏形", "城墙、锦衣卫、火铳、宫殿、码头、风雪边关", "旧城街巷、铁路、工厂、硝烟、红旗、时代交替" };

        EraStageData[] eras = new EraStageData[5];
        eras[0] = bronze;
        for (int i = 0; i < files.Length; i++)
        {
            EraStageData era = CreateAsset<EraStageData>("Assets/Scripts/Roguelite/GeneratedData/Eras/" + files[i] + "Era.asset");
            era.id = "era_" + files[i].ToLowerInvariant();
            era.displayName = display[i];
            era.description = "后续版本扩展的时代副本占位数据。";
            era.era = (EraId)(i + 1);
            era.visualTheme = themes[i];
            era.enemyPool = new EnemyData[0];
            era.itemDropPool = items;
            era.prefabPathPlaceholder = "TODO: stage scene/prefab";
            era.iconPlaceholder = icon;
            era.unlockCondition = "Beat previous era boss";
            era.specialMechanics = i == 3 ? "使用架空象征敌人，避免真实近现代政治人物作为可击杀 Boss。" : "TODO: era-specific mechanics";
            EditorUtility.SetDirty(era);
            eras[i + 1] = era;
        }

        return eras;
    }

    private static void CreateBronzeScene(DifficultyScalingData difficulty, EraStageData[] eras, RunRewardTableData rewards, Sprite bronzeSprite)
    {
        GameObject playerTemplate = GameObject.FindGameObjectWithTag("Player");
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Roguelite/Player_RunPrototype.prefab");
        if (playerTemplate != null)
        {
            playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerTemplate, "Assets/Prefabs/Roguelite/Player_RunPrototype.prefab");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Era_Bronze";

        GameObject camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(-2f, 3f, -10f);
        Camera cam = camera.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = new Color(0.05f, 0.07f, 0.07f, 1f);

        GameObject player = playerPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab) : new GameObject("Player");
        player.name = "Player";
        player.transform.position = new Vector3(-6f, 0.5f, 0f);
        player.SetActive(true);
        CameraFollow2D follow = camera.AddComponent<CameraFollow2D>();
        SetRef(follow, "target", player.transform);
        if (player.GetComponent<RunInventorySystem>() == null) player.AddComponent<RunInventorySystem>();
        PlayerRunStats prs = player.GetComponent<PlayerRunStats>();
        if (prs == null) prs = player.AddComponent<PlayerRunStats>();
        SetRef(prs, "bonusProjectilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Arrow.prefab"));
        SetPrivate(prs, "bonusProjectileTargetMask", LayerMask.GetMask("Damageable"));

        GameObject managers = new GameObject("Run Managers");
        GameRunManager run = managers.AddComponent<GameRunManager>();
        SetRef(run, "difficultyScaling", difficulty);
        SetRef(run, "startingEra", eras[0]);

        EraStageSystem eraSystem = managers.AddComponent<EraStageSystem>();
        SerializedObject eraSo = new SerializedObject(eraSystem);
        SerializedProperty erasProp = eraSo.FindProperty("eras");
        erasProp.arraySize = eras.Length;
        for (int i = 0; i < eras.Length; i++) erasProp.GetArrayElementAtIndex(i).objectReferenceValue = eras[i];
        eraSo.ApplyModifiedPropertiesWithoutUndo();

        EnemySpawnDirector director = managers.AddComponent<EnemySpawnDirector>();
        SetRef(director, "eraStageSystem", eraSystem);
        SetPrivate(director, "initialSpawnCount", 3);

        RunFlowController flow = managers.AddComponent<RunFlowController>();
        SetRef(flow, "spawnDirector", director);
        SetRef(flow, "eraStageSystem", eraSystem);
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Roguelite/Bosses/BronzeKingShadow.prefab");
        SetRef(flow, "bronzeBossPrefab", bossPrefab != null ? bossPrefab.GetComponent<BossBase>() : null);
        SetRef(flow, "rewardTable", rewards);
        SetPrivate(flow, "killsBeforeBoss", 6);

        GameObject portal = new GameObject("Next Era Portal", typeof(SpriteRenderer), typeof(BoxCollider2D));
        portal.transform.position = new Vector3(95f, 1.2f, 0f);
        portal.transform.localScale = new Vector3(1.2f, 2.2f, 1f);
        SpriteRenderer psr = portal.GetComponent<SpriteRenderer>();
        psr.sprite = bronzeSprite;
        psr.color = new Color(0.2f, 0.9f, 1f, 0.75f);
        psr.sortingOrder = 1;
        portal.GetComponent<BoxCollider2D>().isTrigger = true;
        portal.SetActive(false);
        SetRef(flow, "nextEraPortal", portal);

        GameObject map = new GameObject("Bronze Procedural Map", typeof(BronzeMapGenerator));
        BronzeMapGenerator generator = map.GetComponent<BronzeMapGenerator>();
        SetPrivate(generator, "mainRoomCount", 8);
        SetPrivate(generator, "branchRoomCount", 3);
        SetRef(generator, "platformSprite", bronzeSprite);
        SetRef(generator, "backgroundSprite", bronzeSprite);
        SetRef(generator, "player", player.transform);
        SetRef(generator, "targetCamera", cam);
        SetRef(director, "mapGenerator", generator);
        SetRef(flow, "mapGenerator", generator);

        GameObject backdrop = new GameObject("Bronze Taotie Backdrop", typeof(SpriteRenderer));
        backdrop.transform.position = new Vector3(0f, 2.5f, 1f);
        backdrop.transform.localScale = new Vector3(13f, 7f, 1f);
        SpriteRenderer bgsr = backdrop.GetComponent<SpriteRenderer>();
        bgsr.sprite = bronzeSprite;
        bgsr.color = new Color(0.08f, 0.15f, 0.14f, 0.55f);
        bgsr.sortingOrder = -5;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Era_Bronze.unity");
    }

    private static Transform[] CreateSpawnPoints()
    {
        GameObject root = new GameObject("Enemy Spawn Points");
        Transform[] spawnPoints = new Transform[5];
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            GameObject sp = new GameObject("Spawn_" + (i + 1));
            sp.transform.SetParent(root.transform);
            sp.transform.position = new Vector3(-2.2f + i * 2.7f, 0.6f, 0f);
            spawnPoints[i] = sp.transform;
        }
        return spawnPoints;
    }

    private static void MakePlatform(string name, Vector3 pos, Vector2 size, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(BoxCollider2D));
        go.layer = LayerMask.NameToLayer("Ground");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one;
        GameObject visual = new GameObject("Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(go.transform);
        visual.transform.localPosition = Vector3.zero;
        SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = -1;
        if (sprite != null && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f)
        {
            visual.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
        }
        BoxCollider2D collider = go.GetComponent<BoxCollider2D>();
        collider.size = size;
        collider.offset = Vector2.zero;
    }

    private static void EnsureFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string name = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static T CreateAsset<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
    }

    private static void SetRef(Object target, string fieldName, Object value)
    {
        SetPrivate(target, fieldName, value);
    }

    private static void SetFloat(Object target, string fieldName, float value)
    {
        SetPrivate(target, fieldName, value);
    }

    private static void SetPrivate(Object target, string fieldName, object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null) return;
        if (value is float) prop.floatValue = (float)value;
        else if (value is int) prop.intValue = (int)value;
        else if (value is bool) prop.boolValue = (bool)value;
        else if (value is string) prop.stringValue = (string)value;
        else if (value is Object) prop.objectReferenceValue = (Object)value;
        else if (value is Color) prop.colorValue = (Color)value;
        else if (value is Vector2) prop.vector2Value = (Vector2)value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
