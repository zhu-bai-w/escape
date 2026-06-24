using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UniversityCardArtBatchVerificationTool
{
    const string CardsFolder = "Assets/UniversitySimulator/Art/cards";
    const string PrefabRoot = "Assets/UniversitySimulator/Prefabs/Cards";
    const string ReportPath = "Assets/UniversitySimulator/Data/card_art_verification_available.json";

    [MenuItem("University Simulator/Cards/Verify Available Card Art")]
    public static void VerifyAvailableCardArt()
    {
        BatchReport report = new BatchReport
        {
            isPlaying = EditorApplication.isPlaying,
            reportPath = ReportPath,
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        string[] spritePaths = Directory.GetFiles(CardsFolder, "card-E*.png", SearchOption.TopDirectoryOnly);
        Array.Sort(spritePaths, StringComparer.OrdinalIgnoreCase);

        CardStack cardStack = EditorApplication.isPlaying ? UnityEngine.Object.FindObjectOfType<CardStack>() : null;
        report.cardStackFound = cardStack != null;

        foreach (string rawPath in spritePaths)
        {
            string spritePath = rawPath.Replace('\\', '/');
            string eventId = Path.GetFileNameWithoutExtension(spritePath).Substring("card-".Length);
            string prefabPath = FindCardPrefabPath(eventId);

            CardArtEntry entry = new CardArtEntry
            {
                eventId = eventId,
                expectedSpritePath = spritePath,
                prefabPath = prefabPath
            };

            Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            GameObject prefab = string.IsNullOrEmpty(prefabPath) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            entry.expectedSpriteLoaded = expectedSprite != null;
            entry.prefabLoaded = prefab != null;

            if (!EditorApplication.isPlaying)
            {
                entry.error = "Verifier requires Play Mode.";
                report.cards.Add(entry);
                continue;
            }

            if (cardStack == null)
            {
                entry.error = "CardStack was not found in the active scene.";
                report.cards.Add(entry);
                continue;
            }

            if (expectedSprite == null || prefab == null)
            {
                entry.error = "Missing expected sprite or prefab.";
                report.cards.Add(entry);
                continue;
            }

            GameObject spawned = cardStack.DebugShowCard(prefab);
            entry.spawnedCardName = spawned != null ? spawned.name : "";
            if (spawned == null)
            {
                entry.error = "CardStack.DebugShowCard returned null.";
                report.cards.Add(entry);
                continue;
            }

            CardStyle cardStyle = spawned.GetComponent<CardStyle>();
            entry.cardStyleFound = cardStyle != null;
            if (cardStyle == null)
            {
                entry.error = "Spawned card has no CardStyle component.";
                report.cards.Add(entry);
                continue;
            }

            entry.usePrefabIconOverride = cardStyle.usePrefabIconOverride;
            entry.iconImageFound = cardStyle.iconImage != null;
            if (cardStyle.iconImage != null && cardStyle.iconImage.sprite != null)
            {
                entry.actualSpriteName = cardStyle.iconImage.sprite.name;
                entry.actualSpritePath = AssetDatabase.GetAssetPath(cardStyle.iconImage.sprite);
            }

            entry.matchesExpectedSprite = entry.usePrefabIconOverride && entry.actualSpritePath == spritePath;
            if (!entry.matchesExpectedSprite)
            {
                entry.error = "Icon sprite did not match the expected card art.";
            }

            report.cards.Add(entry);
        }

        report.totalCount = report.cards.Count;
        foreach (CardArtEntry entry in report.cards)
        {
            if (entry.matchesExpectedSprite)
            {
                report.successCount++;
            }
            else
            {
                report.failCount++;
            }
        }

        WriteReport(report);

        if (report.failCount == 0)
        {
            Debug.Log("Available card art verification passed. Cards: " + report.successCount + ". Report: " + ReportPath);
        }
        else
        {
            Debug.LogWarning("Available card art verification found " + report.failCount + " issue(s). Report: " + ReportPath);
        }
    }

    static string FindCardPrefabPath(string eventId)
    {
        string prefabName = "US_" + eventId;
        string[] guids = AssetDatabase.FindAssets(prefabName + " t:Prefab", new[] { PrefabRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == prefabName)
            {
                return path;
            }
        }

        return "";
    }

    static void WriteReport(BatchReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);
    }

    [Serializable]
    public class BatchReport
    {
        public string generatedAt;
        public string reportPath;
        public bool isPlaying;
        public bool cardStackFound;
        public int totalCount;
        public int successCount;
        public int failCount;
        public List<CardArtEntry> cards = new List<CardArtEntry>();
    }

    [Serializable]
    public class CardArtEntry
    {
        public string eventId;
        public string prefabPath;
        public string expectedSpritePath;
        public bool expectedSpriteLoaded;
        public bool prefabLoaded;
        public string spawnedCardName;
        public bool cardStyleFound;
        public bool usePrefabIconOverride;
        public bool iconImageFound;
        public string actualSpriteName;
        public string actualSpritePath;
        public bool matchesExpectedSprite;
        public string error;
    }
}
