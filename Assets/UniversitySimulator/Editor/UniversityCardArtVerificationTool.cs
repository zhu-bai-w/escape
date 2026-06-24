using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UniversityCardArtVerificationTool
{
    const string CardPrefabPath = "Assets/UniversitySimulator/Prefabs/Cards/Main/US_E001.prefab";
    const string ExpectedSpritePath = "Assets/UniversitySimulator/Art/cards/card-E001.png";
    const string ReportPath = "Assets/UniversitySimulator/Data/card_art_verification_E001.json";

    [MenuItem("University Simulator/Cards/Verify E001 Art")]
    public static void VerifyE001Art()
    {
        VerificationReport report = new VerificationReport
        {
            cardPrefabPath = CardPrefabPath,
            expectedSpritePath = ExpectedSpritePath,
            isPlaying = EditorApplication.isPlaying
        };

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
        Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ExpectedSpritePath);
        report.prefabLoaded = prefab != null;
        report.expectedSpriteLoaded = expectedSprite != null;

        if (!EditorApplication.isPlaying)
        {
            report.error = "Enter Play Mode before running this verifier.";
            WriteReport(report);
            Debug.LogWarning("E001 art verification requires Play Mode. Report: " + ReportPath);
            return;
        }

        CardStack cardStack = Object.FindObjectOfType<CardStack>();
        report.cardStackFound = cardStack != null;
        if (cardStack == null || prefab == null || expectedSprite == null)
        {
            report.error = "Missing CardStack, prefab, or expected sprite.";
            WriteReport(report);
            Debug.LogWarning("E001 art verification could not run. Report: " + ReportPath);
            return;
        }

        GameObject spawned = cardStack.DebugShowCard(prefab);
        report.spawnedCardName = spawned != null ? spawned.name : "";
        if (spawned == null)
        {
            report.error = "CardStack.DebugShowCard returned null.";
            WriteReport(report);
            Debug.LogWarning("E001 art verification failed to spawn the card. Report: " + ReportPath);
            return;
        }

        CardStyle cardStyle = spawned.GetComponent<CardStyle>();
        report.cardStyleFound = cardStyle != null;
        if (cardStyle != null)
        {
            report.usePrefabIconOverride = cardStyle.usePrefabIconOverride;
            report.iconImageFound = cardStyle.iconImage != null;
            if (cardStyle.iconImage != null)
            {
                report.iconSpriteName = cardStyle.iconImage.sprite != null ? cardStyle.iconImage.sprite.name : "";
                report.iconSpritePath = cardStyle.iconImage.sprite != null ? AssetDatabase.GetAssetPath(cardStyle.iconImage.sprite) : "";
            }
        }

        Image[] images = spawned.GetComponentsInChildren<Image>(true);
        report.imageCount = images.Length;
        StringBuilder imagesJson = new StringBuilder();
        imagesJson.Append("[");
        for (int i = 0; i < images.Length; i++)
        {
            if (i > 0)
            {
                imagesJson.Append(",");
            }

            Image image = images[i];
            string spriteName = image.sprite != null ? image.sprite.name : "";
            string spritePath = image.sprite != null ? AssetDatabase.GetAssetPath(image.sprite) : "";
            imagesJson.Append("{");
            AppendJsonField(imagesJson, "name", image.gameObject.name);
            imagesJson.Append(",");
            AppendJsonField(imagesJson, "path", GetTransformPath(image.transform));
            imagesJson.Append(",");
            AppendJsonField(imagesJson, "spriteName", spriteName);
            imagesJson.Append(",");
            AppendJsonField(imagesJson, "spritePath", spritePath);
            imagesJson.Append(",");
            imagesJson.Append("\"activeInHierarchy\":").Append(image.gameObject.activeInHierarchy ? "true" : "false");
            imagesJson.Append("}");
        }
        imagesJson.Append("]");

        report.imagesJson = imagesJson.ToString();
        report.matchesExpectedSprite = report.iconSpritePath == ExpectedSpritePath;
        WriteReport(report);

        if (report.matchesExpectedSprite)
        {
            Debug.Log("E001 art verification passed. Icon sprite: " + report.iconSpritePath + ". Report: " + ReportPath);
        }
        else
        {
            Debug.LogWarning("E001 art verification did not find the expected Icon sprite. Actual: " + report.iconSpritePath + ". Report: " + ReportPath);
        }
    }

    static string GetTransformPath(Transform transform)
    {
        if (transform == null)
        {
            return "";
        }

        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    static void WriteReport(VerificationReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));

        StringBuilder json = new StringBuilder();
        json.Append("{\n");
        AppendJsonLine(json, "cardPrefabPath", report.cardPrefabPath, true);
        AppendJsonLine(json, "expectedSpritePath", report.expectedSpritePath, true);
        AppendJsonLine(json, "isPlaying", report.isPlaying, true);
        AppendJsonLine(json, "prefabLoaded", report.prefabLoaded, true);
        AppendJsonLine(json, "expectedSpriteLoaded", report.expectedSpriteLoaded, true);
        AppendJsonLine(json, "cardStackFound", report.cardStackFound, true);
        AppendJsonLine(json, "spawnedCardName", report.spawnedCardName, true);
        AppendJsonLine(json, "cardStyleFound", report.cardStyleFound, true);
        AppendJsonLine(json, "usePrefabIconOverride", report.usePrefabIconOverride, true);
        AppendJsonLine(json, "iconImageFound", report.iconImageFound, true);
        AppendJsonLine(json, "iconSpriteName", report.iconSpriteName, true);
        AppendJsonLine(json, "iconSpritePath", report.iconSpritePath, true);
        AppendJsonLine(json, "matchesExpectedSprite", report.matchesExpectedSprite, true);
        AppendJsonLine(json, "imageCount", report.imageCount, true);
        AppendJsonLine(json, "error", report.error, true);
        json.Append("  \"images\": ").Append(report.imagesJson ?? "[]").Append("\n");
        json.Append("}\n");

        File.WriteAllText(ReportPath, json.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);
    }

    static void AppendJsonLine(StringBuilder builder, string name, string value, bool comma)
    {
        builder.Append("  ");
        AppendJsonField(builder, name, value ?? "");
        if (comma)
        {
            builder.Append(",");
        }
        builder.Append("\n");
    }

    static void AppendJsonLine(StringBuilder builder, string name, bool value, bool comma)
    {
        builder.Append("  \"").Append(name).Append("\": ").Append(value ? "true" : "false");
        if (comma)
        {
            builder.Append(",");
        }
        builder.Append("\n");
    }

    static void AppendJsonLine(StringBuilder builder, string name, int value, bool comma)
    {
        builder.Append("  \"").Append(name).Append("\": ").Append(value);
        if (comma)
        {
            builder.Append(",");
        }
        builder.Append("\n");
    }

    static void AppendJsonField(StringBuilder builder, string name, string value)
    {
        builder.Append("\"").Append(name).Append("\": ");
        AppendJsonString(builder, value);
    }

    static void AppendJsonString(StringBuilder builder, string value)
    {
        builder.Append("\"");
        foreach (char c in value ?? "")
        {
            switch (c)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }
        builder.Append("\"");
    }

    class VerificationReport
    {
        public string cardPrefabPath;
        public string expectedSpritePath;
        public bool isPlaying;
        public bool prefabLoaded;
        public bool expectedSpriteLoaded;
        public bool cardStackFound;
        public string spawnedCardName;
        public bool cardStyleFound;
        public bool usePrefabIconOverride;
        public bool iconImageFound;
        public string iconSpriteName;
        public string iconSpritePath;
        public bool matchesExpectedSprite;
        public int imageCount;
        public string error;
        public string imagesJson;
    }
}
