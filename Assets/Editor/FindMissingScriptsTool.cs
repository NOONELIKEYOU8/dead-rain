using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class FindMissingScriptsTool
{
    [MenuItem("工具/检查/查找场景中缺失脚本")]
    public static void FindMissingInScene()
    {
        int totalMissing = 0;
        var roots = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (var go in roots)
        {
            totalMissing += CountMissingInGameObject(go);
        }
        EditorUtility.DisplayDialog("查找完成", $"场景扫描完成，发现 {totalMissing} 个缺失脚本（详情请查看 Console）。", "确定");
    }

    [MenuItem("工具/检查/查找项目中 Prefab 的缺失脚本")]
    public static void FindMissingInProjectPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int totalMissing = 0;
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) continue;
            int missing = CountMissingInGameObject(root);
            if (missing > 0)
            {
                Debug.Log($"Prefab '{path}' 存在 {missing} 个缺失脚本", root);
                totalMissing += missing;
            }
        }
        EditorUtility.DisplayDialog("查找完成", $"项目 Prefab 扫描完成，发现 {totalMissing} 个缺失脚本（详情请查看 Console）。", "确定");
    }

    [MenuItem("工具/操作/移除场景中所有缺失脚本（慎用）")]
    public static void RemoveMissingInScene()
    {
        int removed = 0;
        var roots = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (var go in roots)
        {
            removed += RemoveMissingFromGameObject(go);
        }
        if (removed > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
        }
        EditorUtility.DisplayDialog("清理完成", $"已尝试从场景中移除 {removed} 个缺失脚本条目。请检查 Console 并保存场景。", "确定");
    }

    [MenuItem("工具/操作/清理项目中 Prefab 的缺失脚本（慎用）")]
    public static void RemoveMissingInPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int totalRemoved = 0;
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                int before = CountMissingInGameObject(root);
                if (before > 0)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    totalRemoved += before;
                    Debug.Log($"已从 Prefab '{path}' 移除 {before} 个缺失脚本条目。", root);
                }
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }
        EditorUtility.DisplayDialog("清理完成", $"已尝试从项目所有 Prefab 中移除 {totalRemoved} 个缺失脚本条目。请检查 Console。", "确定");
    }

    static int CountMissingInGameObject(GameObject go)
    {
        int missing = 0;
        // 只统计根对象及其子对象
        missing += CountMissingOnThis(go);
        foreach (Transform t in go.transform)
        {
            missing += CountMissingInChildren(t);
        }
        return missing;
    }

    static int CountMissingInChildren(Transform t)
    {
        int count = 0;
        var go = t.gameObject;
        count += CountMissingOnThis(go);
        foreach (Transform c in t)
        {
            count += CountMissingInChildren(c);
        }
        return count;
    }

    static int CountMissingOnThis(GameObject go)
    {
        int missing = 0;
        Component[] comps = go.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null)
            {
                missing++;
                Debug.LogWarning($"缺失脚本: GameObject='{GetFullPath(go)}'", go);
            }
        }
        return missing;
    }

    static int RemoveMissingFromGameObject(GameObject go)
    {
        int removed = 0;
        Component[] comps = go.GetComponents<Component>();
        for (int i = comps.Length - 1; i >= 0; i--)
        {
            if (comps[i] == null)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                removed++;
                Debug.Log($"已从 GameObject '{GetFullPath(go)}' 移除缺失脚本条目。", go);
                break; // RemoveMonoBehavioursWithMissingScript 会移除所有缺失脚本，跳出即可
            }
        }
        return removed;
    }

    static string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform cur = go.transform.parent;
        while (cur != null)
        {
            path = cur.name + "/" + path;
            cur = cur.parent;
        }
        return path;
    }
}
