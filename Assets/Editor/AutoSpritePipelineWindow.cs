using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AutoSpritePipelineWindow : EditorWindow
{
    private struct GridSpec
    {
        public int rows;
        public int cols;
        public int cellWidth;
        public int cellHeight;
        public bool valid;
        public string reason;
    }

    private class RowSpriteEntry
    {
        public int frame;
        public Sprite sprite;
    }

    private class GeneratedClipInfo
    {
        public string character;
        public string action;
        public string direction;
        public AnimationClip clip;
        public string texturePath;
    }

    private readonly Dictionary<string, string[]> actionKeywordMap = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        { "Idle", new[] { "idle", "stay", "daiji", "dai", "qimadaiji" } },
        { "Run", new[] { "run", "walk", "move", "zou", "marun", "mawalk" } },
        { "Attack", new[] { "attack", "atk", "att", "gongji", "kongshou" } },
        { "Hurt", new[] { "hurt", "shoushang", "jifei" } },
        { "Dead", new[] { "dead", "death", "siwang" } },
        { "Jump", new[] { "jump", "tiao", "tiyunzong" } },
        { "Fall", new[] { "fall", "xiazhui" } },
        { "Interact", new[] { "shiqu", "caiyao", "dazuo", "baoquan", "qima", "give", "hold", "ketou", "tanqin", "tideng" } },
    };

    private static readonly Regex SliceNamePattern = new Regex(@"_r(\d+)_f(\d+)$", RegexOptions.Compiled);
    private static readonly string[] CommonCellSizes = { "16", "24", "32", "48", "64", "96", "128", "256" };

    private bool autoDetectGrid = true;
    private int manualRows = 1;
    private int manualCols = 6;
    private int manualCellWidth;
    private int manualCellHeight;

    private int pixelsPerUnit = 32;
    private int frameRate = 12;
    private bool createAnimationClips = true;
    private bool createAnimatorControllers = true;
    private bool splitRowsAsDirections = true;
    private bool overwriteAssets = true;
    private bool loopInteract = false;

    private string directionOrder = "Up,Left,Right,Down";
    private string preferredControllerDirection = "Right";

    [MenuItem("Tools/DeadRain/Auto Sprite Pipeline")]
    [MenuItem("工具/DeadRain/自动切图流水线")]
    [MenuItem("Assets/DeadRain/Open Auto Sprite Pipeline", false, 2000)]
    public static void ShowWindow()
    {
        GetWindow<AutoSpritePipelineWindow>("Auto Sprite Pipeline");
    }

    [MenuItem("Tools/DeadRain/Recompile Editor Scripts")]
    [MenuItem("工具/DeadRain/刷新并重编译脚本")]
    public static void RecompileScripts()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        Debug.Log("Auto Sprite Pipeline: requested AssetDatabase refresh.");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("横版像素角色：自动切图 + 动画 + 状态机", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("切片模式", EditorStyles.boldLabel);
        autoDetectGrid = EditorGUILayout.Toggle("自动推断 Grid", autoDetectGrid);
        using (new EditorGUI.DisabledScope(autoDetectGrid))
        {
            manualRows = Mathf.Max(1, EditorGUILayout.IntField("Rows", manualRows));
            manualCols = Mathf.Max(1, EditorGUILayout.IntField("Cols", manualCols));
        }
        manualCellWidth = Mathf.Max(0, EditorGUILayout.IntField("手动 CellWidth (可选)", manualCellWidth));
        manualCellHeight = Mathf.Max(0, EditorGUILayout.IntField("手动 CellHeight (可选)", manualCellHeight));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("输出参数", EditorStyles.boldLabel);
        pixelsPerUnit = Mathf.Max(1, EditorGUILayout.IntField("Pixels Per Unit", pixelsPerUnit));
        frameRate = Mathf.Max(1, EditorGUILayout.IntField("动画 FPS", frameRate));
        splitRowsAsDirections = EditorGUILayout.Toggle("多行按方向拆分", splitRowsAsDirections);
        directionOrder = EditorGUILayout.TextField("方向顺序 (逗号分隔)", directionOrder);
        preferredControllerDirection = EditorGUILayout.TextField("控制器优选方向", preferredControllerDirection);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("生成项", EditorStyles.boldLabel);
        createAnimationClips = EditorGUILayout.Toggle("生成 AnimationClip", createAnimationClips);
        createAnimatorControllers = EditorGUILayout.Toggle("生成 AnimatorController", createAnimatorControllers);
        overwriteAssets = EditorGUILayout.Toggle("覆盖已存在资源", overwriteAssets);
        loopInteract = EditorGUILayout.Toggle("Interact 动作循环", loopInteract);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("在 Project 面板选中一批 Texture2D 后执行。\n工具会基于文件名推断动作（Idle/Run/Attack/Hurt/Dead/Jump/Fall/Interact），并对未知动作归类为 Custom。", MessageType.Info);

        if (GUILayout.Button("处理选中贴图", GUILayout.Height(40)))
        {
            ProcessSelection();
        }
    }

    private void ProcessSelection()
    {
        var selectedTextures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
        if (selectedTextures == null || selectedTextures.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 面板中选中至少一张 Texture2D。", "确定");
            return;
        }

        var generatedClips = new List<GeneratedClipInfo>();
        var warnings = new List<string>();
        var log = new StringBuilder();

        for (int i = 0; i < selectedTextures.Length; i++)
        {
            var texture = selectedTextures[i];
            var assetPath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
            {
                warnings.Add("跳过无效资源路径。\n");
                continue;
            }

            EditorUtility.DisplayProgressBar("Auto Sprite Pipeline", $"处理中: {texture.name}", (float)i / Mathf.Max(1, selectedTextures.Length));

            if (!TryResolveGrid(texture, out var grid))
            {
                warnings.Add($"[{texture.name}] Grid 推断失败: {grid.reason}");
                continue;
            }

            SliceTexture(assetPath, texture, grid);
            log.AppendLine($"[Slice] {texture.name} -> {grid.rows}x{grid.cols} (Cell: {grid.cellWidth}x{grid.cellHeight})");

            if (!createAnimationClips)
            {
                continue;
            }

            var rowSprites = LoadRowSprites(assetPath);
            if (rowSprites.Count == 0)
            {
                warnings.Add($"[{texture.name}] 切片后未读取到 Sprite。请检查导入设置。");
                continue;
            }

            var character = InferCharacterName(assetPath, texture.name);
            var action = InferActionName(texture.name);
            var animFolder = EnsureAssetFolder(Path.Combine(Path.GetDirectoryName(assetPath) ?? "Assets", "Animations").Replace("\\", "/"));

            if (splitRowsAsDirections && grid.rows > 1)
            {
                foreach (var rowPair in rowSprites.OrderBy(k => k.Key))
                {
                    var direction = ResolveDirectionName(rowPair.Key, grid.rows);
                    var clipName = $"{character}_{action}_{direction}";
                    var clip = CreateOrUpdateClip(animFolder, clipName, rowPair.Value.OrderBy(v => v.frame).Select(v => v.sprite).ToList(), action);
                    generatedClips.Add(new GeneratedClipInfo
                    {
                        character = character,
                        action = action,
                        direction = direction,
                        clip = clip,
                        texturePath = assetPath,
                    });
                    log.AppendLine($"[Clip] {clipName}.anim");
                }
            }
            else
            {
                var merged = rowSprites
                    .OrderBy(k => k.Key)
                    .SelectMany(k => k.Value.OrderBy(v => v.frame))
                    .Select(v => v.sprite)
                    .ToList();
                var clipName = $"{character}_{action}";
                var clip = CreateOrUpdateClip(animFolder, clipName, merged, action);
                generatedClips.Add(new GeneratedClipInfo
                {
                    character = character,
                    action = action,
                    direction = string.Empty,
                    clip = clip,
                    texturePath = assetPath,
                });
                log.AppendLine($"[Clip] {clipName}.anim");
            }
        }

        if (createAnimatorControllers)
        {
            GenerateControllers(generatedClips, log, warnings);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (warnings.Count > 0)
        {
            log.AppendLine("\n=== Warnings ===");
            foreach (var w in warnings)
            {
                log.AppendLine("- " + w);
            }
        }

        Debug.Log(log.ToString());
        var tip = warnings.Count == 0 ? "处理完成。详细日志见 Console。" : $"处理完成，包含 {warnings.Count} 条警告。详细日志见 Console。";
        EditorUtility.DisplayDialog("Auto Sprite Pipeline", tip, "确定");
    }

    private bool TryResolveGrid(Texture2D texture, out GridSpec spec)
    {
        spec = new GridSpec { valid = false, reason = "unknown" };
        int width = texture.width;
        int height = texture.height;

        if (manualCellWidth > 0 && manualCellHeight > 0)
        {
            if (width % manualCellWidth == 0 && height % manualCellHeight == 0)
            {
                spec = new GridSpec
                {
                    cellWidth = manualCellWidth,
                    cellHeight = manualCellHeight,
                    cols = width / manualCellWidth,
                    rows = height / manualCellHeight,
                    valid = true,
                    reason = "manual-cell",
                };
                return true;
            }

            spec.reason = "手动 cell 尺寸无法整除图片宽高";
            return false;
        }

        if (!autoDetectGrid)
        {
            if (manualRows <= 0 || manualCols <= 0)
            {
                spec.reason = "Rows/Cols 必须大于 0";
                return false;
            }

            if (width % manualCols != 0 || height % manualRows != 0)
            {
                spec.reason = "Rows/Cols 无法整除图片宽高";
                return false;
            }

            spec = new GridSpec
            {
                rows = manualRows,
                cols = manualCols,
                cellWidth = width / manualCols,
                cellHeight = height / manualRows,
                valid = true,
                reason = "manual-grid",
            };
            return true;
        }

        var candidates = new List<GridSpec>();

        if (width > height && width % height == 0)
        {
            candidates.Add(new GridSpec
            {
                rows = 1,
                cols = width / height,
                cellWidth = height,
                cellHeight = height,
                valid = true,
                reason = "auto-1row-square",
            });
        }

        if (height > width && height % width == 0)
        {
            candidates.Add(new GridSpec
            {
                rows = height / width,
                cols = 1,
                cellWidth = width,
                cellHeight = width,
                valid = true,
                reason = "auto-1col-square",
            });
        }

        foreach (var cellSizeText in CommonCellSizes)
        {
            int cellSize = int.Parse(cellSizeText);
            if (cellSize <= 0) continue;
            if (width % cellSize != 0 || height % cellSize != 0) continue;

            candidates.Add(new GridSpec
            {
                rows = height / cellSize,
                cols = width / cellSize,
                cellWidth = cellSize,
                cellHeight = cellSize,
                valid = true,
                reason = "auto-common-cell",
            });
        }

        int gcd = Gcd(width, height);
        if (gcd > 0)
        {
            int limitedGcd = gcd;
            while (limitedGcd > 256)
            {
                limitedGcd /= 2;
            }

            if (limitedGcd > 0 && width % limitedGcd == 0 && height % limitedGcd == 0)
            {
                candidates.Add(new GridSpec
                {
                    rows = height / limitedGcd,
                    cols = width / limitedGcd,
                    cellWidth = limitedGcd,
                    cellHeight = limitedGcd,
                    valid = true,
                    reason = "auto-gcd",
                });
            }
        }

        if (candidates.Count == 0)
        {
            spec.reason = "未找到可用 grid，建议手动输入 Rows/Cols 或 Cell 尺寸";
            return false;
        }

        spec = candidates
            .OrderByDescending(ScoreCandidate)
            .ThenBy(c => c.rows * c.cols)
            .First();
        spec.valid = true;
        return true;
    }

    private int ScoreCandidate(GridSpec c)
    {
        int score = 0;
        if (c.rows == 1) score += 60;
        if (c.rows == 4) score += 35;
        if (c.rows > 1 && c.rows <= 8) score += 10;
        if (c.cols >= 2 && c.cols <= 12) score += 20;
        if (c.cols <= 20) score += 8;
        if (c.cellWidth == c.cellHeight) score += 20;
        if (c.rows * c.cols <= 64) score += 8;
        return score;
    }

    private void SliceTexture(string assetPath, Texture2D texture, GridSpec grid)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = pixelsPerUnit;

        var metaDataList = new List<SpriteMetaData>();
        for (int visualRow = 0; visualRow < grid.rows; visualRow++)
        {
            int unityRow = grid.rows - 1 - visualRow;
            for (int col = 0; col < grid.cols; col++)
            {
                var metaData = new SpriteMetaData
                {
                    name = $"{texture.name}_r{visualRow:00}_f{col:00}",
                    rect = new Rect(col * grid.cellWidth, unityRow * grid.cellHeight, grid.cellWidth, grid.cellHeight),
                    alignment = (int)SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                };

                metaDataList.Add(metaData);
            }
        }

        importer.spritesheet = metaDataList.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private Dictionary<int, List<RowSpriteEntry>> LoadRowSprites(string assetPath)
    {
        var rowMap = new Dictionary<int, List<RowSpriteEntry>>();
        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (var asset in assets)
        {
            if (!(asset is Sprite sprite)) continue;

            var match = SliceNamePattern.Match(sprite.name);
            int row = 0;
            int frame = 0;
            if (match.Success)
            {
                row = int.Parse(match.Groups[1].Value);
                frame = int.Parse(match.Groups[2].Value);
            }

            if (!rowMap.TryGetValue(row, out var list))
            {
                list = new List<RowSpriteEntry>();
                rowMap[row] = list;
            }

            list.Add(new RowSpriteEntry
            {
                frame = frame,
                sprite = sprite,
            });
        }

        foreach (var key in rowMap.Keys.ToList())
        {
            rowMap[key] = rowMap[key].OrderBy(v => v.frame).ToList();
        }

        return rowMap;
    }

    private AnimationClip CreateOrUpdateClip(string animFolder, string clipName, List<Sprite> sprites, string action)
    {
        if (sprites == null || sprites.Count == 0)
        {
            return null;
        }

        string clipPath = Path.Combine(animFolder, clipName + ".anim").Replace("\\", "/");
        if (!overwriteAssets && AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) != null)
        {
            clipPath = AssetDatabase.GenerateUniqueAssetPath(clipPath);
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        bool isNew = clip == null;
        if (isNew)
        {
            clip = new AnimationClip();
        }

        clip.frameRate = frameRate;

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite",
        };

        var keyFrames = new ObjectReferenceKeyframe[sprites.Count + 1];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe
            {
                time = i / (float)frameRate,
                value = sprites[i],
            };
        }

        keyFrames[sprites.Count] = new ObjectReferenceKeyframe
        {
            time = sprites.Count / (float)frameRate,
            value = sprites[sprites.Count - 1],
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyFrames);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        bool shouldLoop = ShouldLoopAction(action);
        settings.loopTime = shouldLoop;
        clip.wrapMode = shouldLoop ? WrapMode.Loop : WrapMode.Default;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew)
        {
            AssetDatabase.CreateAsset(clip, clipPath);
        }
        else
        {
            EditorUtility.SetDirty(clip);
        }

        return clip;
    }

    private bool ShouldLoopAction(string action)
    {
        if (string.Equals(action, "Idle", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(action, "Run", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(action, "Interact", StringComparison.OrdinalIgnoreCase)) return loopInteract;
        return false;
    }

    private void GenerateControllers(List<GeneratedClipInfo> generatedClips, StringBuilder log, List<string> warnings)
    {
        var grouped = generatedClips
            .Where(c => c != null && c.clip != null)
            .GroupBy(c => c.character);

        foreach (var characterGroup in grouped)
        {
            string character = characterGroup.Key;
            var clips = characterGroup.ToList();
            if (clips.Count == 0) continue;

            string sourceDir = Path.GetDirectoryName(clips[0].texturePath) ?? "Assets";
            string controllerFolder = EnsureAssetFolder(Path.Combine(sourceDir, "Controllers").Replace("\\", "/"));
            string controllerPath = Path.Combine(controllerFolder, character + "_Auto.controller").Replace("\\", "/");

            AnimatorController controller;
            bool isNew = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) == null;
            if (isNew)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }
            else
            {
                controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            }

            if (controller == null)
            {
                warnings.Add($"[{character}] AnimatorController 创建失败。路径: {controllerPath}");
                continue;
            }

            ResetController(controller);
            BuildBaseControllerGraph(controller, clips, warnings);

            EditorUtility.SetDirty(controller);
            log.AppendLine($"[Controller] {character}_Auto.controller");
        }
    }

    private void ResetController(AnimatorController controller)
    {
        while (controller.parameters.Length > 0)
        {
            controller.RemoveParameter(controller.parameters[0]);
        }

        var sm = controller.layers[0].stateMachine;
        var childStates = sm.states;
        for (int i = childStates.Length - 1; i >= 0; i--)
        {
            sm.RemoveState(childStates[i].state);
        }

        var anyStateTransitions = sm.anyStateTransitions;
        for (int i = anyStateTransitions.Length - 1; i >= 0; i--)
        {
            sm.RemoveAnyStateTransition(anyStateTransitions[i]);
        }
    }

    private void BuildBaseControllerGraph(AnimatorController controller, List<GeneratedClipInfo> clips, List<string> warnings)
    {
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AttackTrigger", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        var idleClip = PickClip(clips, "Idle");
        var runClip = PickClip(clips, "Run");
        var jumpClip = PickClip(clips, "Jump");
        var fallClip = PickClip(clips, "Fall");
        var attackClip = PickClip(clips, "Attack");
        var hurtClip = PickClip(clips, "Hurt");
        var deadClip = PickClip(clips, "Dead");
        var interactClip = PickClip(clips, "Interact");
        var customClip = PickClip(clips, "Custom");

        if (idleClip == null && runClip == null && customClip == null)
        {
            warnings.Add("控制器构建警告: 缺少 Idle/Run/Custom 动画，默认状态机会不完整。");
        }

        var idle = AddState(sm, "Idle", idleClip, new Vector3(180, 80, 0));
        var run = AddState(sm, "Run", runClip, new Vector3(440, 80, 0));
        var jump = AddState(sm, "Jump", jumpClip, new Vector3(440, -120, 0));
        var fall = AddState(sm, "Fall", fallClip, new Vector3(700, -120, 0));
        var attack = AddState(sm, "Attack", attackClip, new Vector3(440, 280, 0));
        var hurt = AddState(sm, "Hurt", hurtClip, new Vector3(700, 280, 0));
        var dead = AddState(sm, "Dead", deadClip, new Vector3(960, 280, 0));
        var interact = AddState(sm, "Interact", interactClip, new Vector3(960, 80, 0));
        var custom = AddState(sm, "Custom", customClip, new Vector3(180, 280, 0));

        sm.defaultState = idle ?? run ?? custom ?? attack ?? jump ?? fall ?? hurt ?? dead ?? interact;

        if (idle != null && run != null)
        {
            var t1 = idle.AddTransition(run);
            t1.hasExitTime = false;
            t1.duration = 0.06f;
            t1.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var t2 = run.AddTransition(idle);
            t2.hasExitTime = false;
            t2.duration = 0.06f;
            t2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        }

        if (jump != null)
        {
            if (idle != null)
            {
                var t = idle.AddTransition(jump);
                t.hasExitTime = false;
                t.duration = 0.05f;
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.01f, "VerticalSpeed");
            }

            if (run != null)
            {
                var t = run.AddTransition(jump);
                t.hasExitTime = false;
                t.duration = 0.05f;
                t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.01f, "VerticalSpeed");
            }
        }

        if (jump != null && fall != null)
        {
            var t = jump.AddTransition(fall);
            t.hasExitTime = false;
            t.duration = 0.05f;
            t.AddCondition(AnimatorConditionMode.Less, -0.01f, "VerticalSpeed");
        }

        if (fall != null)
        {
            if (idle != null)
            {
                var t = fall.AddTransition(idle);
                t.hasExitTime = false;
                t.duration = 0.06f;
                t.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
                t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            }

            if (run != null)
            {
                var t = fall.AddTransition(run);
                t.hasExitTime = false;
                t.duration = 0.06f;
                t.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            }
        }

        if (attack != null)
        {
            var anyAttack = sm.AddAnyStateTransition(attack);
            anyAttack.hasExitTime = false;
            anyAttack.duration = 0.03f;
            anyAttack.AddCondition(AnimatorConditionMode.If, 0, "AttackTrigger");

            if (idle != null)
            {
                var backToIdle = attack.AddTransition(idle);
                backToIdle.hasExitTime = true;
                backToIdle.exitTime = 0.92f;
                backToIdle.duration = 0.05f;
            }
            else if (run != null)
            {
                var backToRun = attack.AddTransition(run);
                backToRun.hasExitTime = true;
                backToRun.exitTime = 0.92f;
                backToRun.duration = 0.05f;
            }
        }

        if (hurt != null && idle != null)
        {
            var back = hurt.AddTransition(idle);
            back.hasExitTime = true;
            back.exitTime = 0.96f;
            back.duration = 0.05f;
        }

        if (interact != null && idle != null)
        {
            var back = interact.AddTransition(idle);
            back.hasExitTime = true;
            back.exitTime = 0.96f;
            back.duration = 0.05f;
        }

        if (dead != null)
        {
            dead.speed = 1f;
        }
    }

    private AnimatorState AddState(AnimatorStateMachine sm, string stateName, AnimationClip clip, Vector3 pos)
    {
        if (clip == null) return null;
        var state = sm.AddState(stateName, pos);
        state.motion = clip;
        return state;
    }

    private AnimationClip PickClip(List<GeneratedClipInfo> clips, string action)
    {
        var candidates = clips.Where(c => string.Equals(c.action, action, StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(preferredControllerDirection))
        {
            var preferred = candidates.FirstOrDefault(c => string.Equals(c.direction, preferredControllerDirection, StringComparison.OrdinalIgnoreCase));
            if (preferred != null)
            {
                return preferred.clip;
            }
        }

        return candidates[0].clip;
    }

    private string InferActionName(string textureName)
    {
        string lower = textureName.ToLowerInvariant();
        foreach (var pair in actionKeywordMap)
        {
            foreach (var token in pair.Value)
            {
                if (lower.Contains(token))
                {
                    return pair.Key;
                }
            }
        }

        return "Custom";
    }

    private string InferCharacterName(string assetPath, string textureName)
    {
        string fileNoExt = textureName;
        int underscore = fileNoExt.IndexOf('_');
        if (underscore > 0)
        {
            string prefix = fileNoExt.Substring(0, underscore);
            if (!LooksLikeActionToken(prefix))
            {
                return SanitizeName(prefix);
            }
        }

        string dir = Path.GetDirectoryName(assetPath) ?? "Assets";
        string folder = Path.GetFileName(dir);
        if (IsUtilityFolder(folder))
        {
            string parent = Path.GetFileName(Path.GetDirectoryName(dir));
            if (!string.IsNullOrEmpty(parent))
            {
                folder = parent;
            }
        }

        if (string.IsNullOrEmpty(folder) || folder.Equals("Assets", StringComparison.OrdinalIgnoreCase))
        {
            folder = "Character";
        }

        return SanitizeName(folder);
    }

    private string ResolveDirectionName(int rowIndex, int rowCount)
    {
        var parts = directionOrder
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        if (rowIndex >= 0 && rowIndex < parts.Length)
        {
            return SanitizeName(parts[rowIndex]);
        }

        if (rowCount == 8)
        {
            string[] defaults = { "Up", "UpLeft", "Left", "DownLeft", "Down", "DownRight", "Right", "UpRight" };
            if (rowIndex >= 0 && rowIndex < defaults.Length)
            {
                return defaults[rowIndex];
            }
        }

        return "Row" + rowIndex;
    }

    private bool LooksLikeActionToken(string token)
    {
        string lower = token.ToLowerInvariant();
        foreach (var kv in actionKeywordMap)
        {
            if (kv.Key.Equals(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var t in kv.Value)
            {
                if (lower.Contains(t))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsUtilityFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder)) return false;
        return folder.Equals("Textures", StringComparison.OrdinalIgnoreCase)
               || folder.Equals("Texture", StringComparison.OrdinalIgnoreCase)
               || folder.Equals("Char", StringComparison.OrdinalIgnoreCase)
               || folder.Equals("Sprite", StringComparison.OrdinalIgnoreCase)
               || folder.Equals("Sword", StringComparison.OrdinalIgnoreCase)
               || folder.Equals("Spear", StringComparison.OrdinalIgnoreCase)
               || folder.Equals("Fist", StringComparison.OrdinalIgnoreCase)
               || folder.Equals("Sabre", StringComparison.OrdinalIgnoreCase);
    }

    private string EnsureAssetFolder(string assetFolderPath)
    {
        assetFolderPath = assetFolderPath.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(assetFolderPath))
        {
            return assetFolderPath;
        }

        var split = assetFolderPath.Split('/');
        if (split.Length == 0 || !split[0].Equals("Assets", StringComparison.OrdinalIgnoreCase))
        {
            return "Assets";
        }

        string current = "Assets";
        for (int i = 1; i < split.Length; i++)
        {
            if (string.IsNullOrEmpty(split[i])) continue;
            string next = current + "/" + split[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, split[i]);
            }
            current = next;
        }

        return current;
    }

    private string SanitizeName(string text)
    {
        if (string.IsNullOrEmpty(text)) return "Unknown";

        var chars = text.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray();
        string cleaned = new string(chars);
        if (string.IsNullOrEmpty(cleaned)) return "Unknown";

        return cleaned;
    }

    private int Gcd(int a, int b)
    {
        a = Mathf.Abs(a);
        b = Mathf.Abs(b);
        while (b != 0)
        {
            int t = b;
            b = a % b;
            a = t;
        }
        return a;
    }
}