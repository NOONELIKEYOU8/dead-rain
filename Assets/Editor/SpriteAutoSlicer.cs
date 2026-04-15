using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SpriteAutoSlicer : EditorWindow
{
    private int columns = 5; // 默认列数
    private int rows = 1;    // 默认行数
    private int frameRate = 12; // 默认动画帧率

    [MenuItem("Tools/Sprite Auto Slicer (像素横版切图精灵)")]
    public static void ShowWindow()
    {
        GetWindow<SpriteAutoSlicer>("Sprite 切图与动画生成");
    }

    void OnGUI()
    {
        GUILayout.Label("基于固定网格的大图切分与动画生成", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        GUILayout.Label("1. 参数设置", EditorStyles.boldLabel);
        columns = EditorGUILayout.IntField("每图包含的列数 (Columns)", columns);
        rows = EditorGUILayout.IntField("每图包含的行数 (Rows)", rows);
        frameRate = EditorGUILayout.IntField("生成动画帧率 (FPS)", frameRate);

        EditorGUILayout.Space();
        GUILayout.Label("2. 操作说明", EditorStyles.boldLabel);
        GUILayout.Label("请在 Project 视图中选中一张或多张角色大图 (如 xxx_run.png),\n然后点击下方按钮处理。");

        EditorGUILayout.Space();
        if (GUILayout.Button("一键切图并生成动画 (处理选中项)", GUILayout.Height(40)))
        {
            ProcessSelectedTextures();
        }
    }

    private void ProcessSelectedTextures()
    {
        Object[] selectedObjects = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
        
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请先在 Project 面板中选中至少一张需要处理的图片！", "确定");
            return;
        }

        foreach (Object obj in selectedObjects)
        {
            Texture2D texture = obj as Texture2D;
            string assetPath = AssetDatabase.GetAssetPath(texture);

            // 1. 切图处理
            SliceTexture(texture, assetPath);

            // 2. 生成动画片段 (AnimationClip)
            GenerateAnimationClip(texture.name, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", "切图并生成动画处理完毕！", "确定");
    }

    private void SliceTexture(Texture2D texture, string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;

        // 设置纹理为多图模式及不压缩等像素风必备设置
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        int cellWidth = texture.width / columns;
        int cellHeight = texture.height / rows;

        List<SpriteMetaData> metaDataList = new List<SpriteMetaData>();
        
        int spriteIndex = 0;
        // 注意：图片的左下角为(0,0)，所以行需要从下往上或者计算好y坐标
        for (int r = rows - 1; r >= 0; r--)
        {
            for (int c = 0; c < columns; c++)
            {
                SpriteMetaData metaData = new SpriteMetaData();
                metaData.name = $"{texture.name}_{spriteIndex}";
                metaData.rect = new Rect(c * cellWidth, r * cellHeight, cellWidth, cellHeight);
                metaData.alignment = (int)SpriteAlignment.BottomCenter;
                // 设置锚点在底部中间，适合横版平台跳跃角色
                metaData.pivot = new Vector2(0.5f, 0f); 
                
                metaDataList.Add(metaData);
                spriteIndex++;
            }
        }

        importer.spritesheet = metaDataList.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private void GenerateAnimationClip(string textureName, string assetPath)
    {
        // 加载所有的切片Sprite
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        List<Sprite> sprites = new List<Sprite>();
        
        foreach (Object asset in assets)
        {
            if (asset is Sprite)
            {
                sprites.Add(asset as Sprite);
            }
        }

        if (sprites.Count == 0) return;

        // 对Sprite根据名字后缀进行排序，确保帧顺序正确
        sprites.Sort((a, b) => EditorUtility.NaturalCompare(a.name, b.name));

        // 识别动作名称
        string actionName = GetActionName(textureName);
        string directoryInfo = Path.GetDirectoryName(assetPath);
        string animPath = Path.Combine(directoryInfo, $"{actionName}.anim");

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
        bool isNew = false;
        if (clip == null)
        {
            clip = new AnimationClip();
            isNew = true;
        }

        clip.frameRate = frameRate;

        // 用于绑定Sprite的曲线设置
        EditorCurveBinding curveBinding = new EditorCurveBinding();
        curveBinding.type = typeof(SpriteRenderer);
        curveBinding.path = "";
        curveBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[sprites.Count + 1];
        
        // 赋予每一帧Sprite
        for (int i = 0; i < sprites.Count; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe();
            keyFrames[i].time = i / (float)frameRate;
            keyFrames[i].value = sprites[i];
        }

        // 添加最后一帧用来控制时长 (复制倒数第一张的时间+1帧)
        keyFrames[sprites.Count] = new ObjectReferenceKeyframe();
        keyFrames[sprites.Count].time = sprites.Count / (float)frameRate;
        keyFrames[sprites.Count].value = sprites[sprites.Count - 1];

        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyFrames);

        // 设置动作是否循环
        AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        if (actionName.Contains("Idle") || actionName.Contains("Run")) {
            clip.wrapMode = WrapMode.Loop;
            clipSettings.loopTime = true;
        } else {
            clip.wrapMode = WrapMode.Default;
            clipSettings.loopTime = false;
        }
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        if (isNew)
        {
            AssetDatabase.CreateAsset(clip, animPath);
        }
        else
        {
            EditorUtility.SetDirty(clip);
        }
    }

    private string GetActionName(string fileName)
    {
        string lowerName = fileName.ToLower();
        string action = "DefaultAnim";

        if (lowerName.Contains("daiji") || lowerName.Contains("stay") || lowerName.Contains("idle"))
        {
            action = "Idle";
        }
        else if (lowerName.Contains("run") || lowerName.Contains("zou") || lowerName.Contains("move"))
        {
            action = "Run";
        }
        else if (lowerName.Contains("att") || lowerName.Contains("gongji") || lowerName.Contains("attack"))
        {
            action = "Attack";
        }
        // 如果想要以角色区分，可以加上前缀，这里暂把解析出来的标准词作为动画名
        return action;
    }
}
