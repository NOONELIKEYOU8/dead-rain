using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class GeneratePlaceholders
{
    static GeneratePlaceholders()
    {
        // Delay the generation so Unity finishes compilation first
        EditorApplication.delayCall += GenerateIfNeeded;
    }

    public static void GenerateIfNeeded()
    {
        try
        {
            string artDir = "Assets/Art";
            string spritesDir = Path.Combine(artDir, "Sprites");
            if (!Directory.Exists(spritesDir)) Directory.CreateDirectory(spritesDir);

            CreateSpriteIfMissing(PathCombineUnity(spritesDir, "placeholder_player_32.png"), new Color(0.1f, 0.8f, 0.9f));
            CreateSpriteIfMissing(PathCombineUnity(spritesDir, "placeholder_enemy_32.png"), new Color(0.9f, 0.25f, 0.25f));
            CreateSpriteIfMissing(PathCombineUnity(spritesDir, "placeholder_tile_32.png"), new Color(0.6f, 0.6f, 0.6f));

            string audioDir = Path.Combine(artDir, "Audio");
            if (!Directory.Exists(audioDir)) Directory.CreateDirectory(audioDir);
            CreateRainIfMissing(PathCombineUnity(audioDir, "placeholder_rain.wav"));
            CreateHitIfMissing(PathCombineUnity(audioDir, "hit.wav"));

            AssetDatabase.Refresh();
            Debug.Log("GeneratePlaceholders: placeholder assets ensured.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("GeneratePlaceholders failed: " + e);
        }
    }

    [MenuItem("工具/生成占位素材 (Generate Placeholders)")]
    public static void Menu_GeneratePlaceholders()
    {
        GenerateIfNeeded();
        EditorUtility.DisplayDialog("生成占位素材", "已尝试生成并导入占位素材。请检查 Project 面板（Assets/Art/Sprites 和 Assets/Art/Audio）。若未显示，请在 Project 面板右键选择 '刷新'。", "确定");
    }

    static string PathCombineUnity(string a, string b)
    {
        return (a + "/" + b).Replace("\\\\", "/");
    }

    static void CreateSpriteIfMissing(string assetPath, Color fill)
    {
        if (File.Exists(assetPath)) return;
        int w = 32, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = fill;
                if (x == 0 || x == w - 1 || y == 0 || y == h - 1) c = Color.black;
                tex.SetPixel(x, y, c);
            }
        }
        tex.filterMode = FilterMode.Point;
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(assetPath, png);

        // Import and set as Sprite
        AssetDatabase.ImportAsset(assetPath);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
    }

    static void CreateRainIfMissing(string assetPath)
    {
        if (File.Exists(assetPath)) return;
        int sampleRate = 44100;
        int lengthSec = 2;
        int samples = sampleRate * lengthSec;
        float[] data = new float[samples];
        System.Random rnd = new System.Random(12345);
        for (int i = 0; i < samples; i++)
        {
            data[i] = (float)((rnd.NextDouble() * 2.0 - 1.0) * 0.12);
            // simple fade in/out
            float env = 1f;
            int fade = sampleRate / 10;
            if (i < fade) env = (float)i / fade;
            if (i > samples - fade) env = (float)(samples - i) / fade;
            data[i] *= env;
        }
        WriteWav(assetPath, data, sampleRate);
    }

    static void CreateHitIfMissing(string assetPath)
    {
        if (File.Exists(assetPath)) return;
        int sampleRate = 44100;
        float freq = 800f;
        int samples = (int)(sampleRate * 0.18f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-12f * t);
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.5f * env;
        }
        WriteWav(assetPath, data, sampleRate);
    }

    static void WriteWav(string assetPath, float[] samples, int sampleRate)
    {
        // write 16-bit PCM WAV
        using (var stream = new FileStream(assetPath, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            int channels = 1;
            int byteRate = sampleRate * channels * 2;

            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);

            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(samples.Length * 2);

            for (int i = 0; i < samples.Length; i++)
            {
                float f = Mathf.Clamp(samples[i], -1f, 1f);
                short s = (short)(f * short.MaxValue);
                writer.Write(s);
            }
        }

        AssetDatabase.ImportAsset(assetPath);
    }
}
#endif
