using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class UniversityCardArtBatchVerificationTool
{
    const string ProgramCsvPath = "Assets/UniversitySimulator/Data/cards_v1_program.csv";
    const string CardsFolder = "Assets/UniversitySimulator/Art/cards";
    const string PrefabRoot = "Assets/UniversitySimulator/Prefabs/Cards";
    const string ReportPath = "Assets/UniversitySimulator/Data/card_art_verification_program.json";

    [MenuItem("University Simulator/Cards/Validate Program Card Art Coverage")]
    public static void ValidateProgramCardArtCoverage()
    {
        BatchReport report = BuildReport();
        WriteReport(report);

        if (report.success)
        {
            Debug.Log("Program card art coverage passed. Bound: " + report.boundCount + "/" + report.expectedCount + ". Report: " + ReportPath);
        }
        else
        {
            Debug.LogWarning("Program card art coverage found issue(s). Missing: " + report.missingExpectedArtCount
                + ", mismatched: " + report.mismatchCount
                + ", extra: " + report.extraArtCount
                + ". Report: " + ReportPath);
        }
    }

    [MenuItem("University Simulator/Cards/Verify Available Card Art")]
    public static void VerifyAvailableCardArt()
    {
        ValidateProgramCardArtCoverage();
    }

    static BatchReport BuildReport()
    {
        BatchReport report = new BatchReport
        {
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            sourceCsv = ProgramCsvPath,
            cardsFolder = CardsFolder,
            prefabRoot = PrefabRoot,
            reportPath = ReportPath
        };

        TextAsset csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(ProgramCsvPath);
        if (csvAsset == null)
        {
            report.errors.Add("Program CSV asset not found: " + ProgramCsvPath);
            report.success = false;
            return report;
        }

        List<CardRow> rows = ReadRows(Encoding.UTF8.GetString(csvAsset.bytes));
        HashSet<string> expectedArtPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (CardRow row in rows)
        {
            string expectedSpritePath = GetExpectedSpritePath(row);
            if (string.IsNullOrWhiteSpace(expectedSpritePath))
            {
                continue;
            }

            expectedArtPaths.Add(expectedSpritePath);
            CardArtEntry entry = BuildEntry(row, expectedSpritePath);
            report.cards.Add(entry);
        }

        AddExtraArtEntries(expectedArtPaths, report);
        Summarize(report);
        return report;
    }

    static CardArtEntry BuildEntry(CardRow row, string expectedSpritePath)
    {
        string prefabPath = GetPrefabPath(row);
        Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(expectedSpritePath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        CardArtEntry entry = new CardArtEntry
        {
            eventId = row.EventId,
            cardName = row.CardName,
            groupName = row.GroupName,
            expectedSpritePath = expectedSpritePath,
            prefabPath = prefabPath,
            expectedSpriteLoaded = expectedSprite != null,
            prefabLoaded = prefab != null,
            lowVisibilitySource = LooksLikeLowVisibilitySource(expectedSpritePath)
        };

        if (expectedSprite == null)
        {
            entry.status = "missing";
            entry.error = "Missing expected sprite.";
            return entry;
        }

        if (prefab == null)
        {
            entry.status = "missing_prefab";
            entry.error = "Missing expected prefab.";
            return entry;
        }

        CardStyle cardStyle = prefab.GetComponent<CardStyle>();
        entry.cardStyleFound = cardStyle != null;
        if (cardStyle == null)
        {
            entry.status = "mismatch";
            entry.error = "Prefab has no CardStyle component.";
            return entry;
        }

        entry.usePrefabIconOverride = cardStyle.usePrefabIconOverride;
        entry.iconImageFound = cardStyle.iconImage != null;
        if (cardStyle.iconImage != null)
        {
            entry.actualDisplayColor = "#" + ColorUtility.ToHtmlStringRGBA(cardStyle.iconImage.color);
            entry.displayColorUntinted = IsUntintedDisplayColor(cardStyle.iconImage.color);
            if (cardStyle.iconImage.sprite != null)
            {
                entry.actualSpriteName = cardStyle.iconImage.sprite.name;
                entry.actualSpritePath = AssetDatabase.GetAssetPath(cardStyle.iconImage.sprite);
            }
        }

        entry.matchesExpectedSprite = entry.usePrefabIconOverride && entry.actualSpritePath == expectedSpritePath;
        if (entry.matchesExpectedSprite && entry.displayColorUntinted)
        {
            entry.status = "bound";
            return entry;
        }

        entry.status = "mismatch";
        if (!entry.matchesExpectedSprite)
        {
            entry.error = "Icon sprite did not match expected card art.";
        }

        if (!entry.displayColorUntinted)
        {
            entry.error = string.IsNullOrEmpty(entry.error)
                ? "Icon display color is not untinted white."
                : entry.error + " Icon display color is not untinted white.";
        }

        return entry;
    }

    static void AddExtraArtEntries(HashSet<string> expectedArtPaths, BatchReport report)
    {
        if (!Directory.Exists(CardsFolder))
        {
            report.errors.Add("Cards folder not found: " + CardsFolder);
            return;
        }

        string[] spritePaths = Directory.GetFiles(CardsFolder, "card-E*.png", SearchOption.TopDirectoryOnly);
        Array.Sort(spritePaths, StringComparer.OrdinalIgnoreCase);
        foreach (string rawPath in spritePaths)
        {
            string spritePath = rawPath.Replace('\\', '/');
            if (expectedArtPaths.Contains(spritePath))
            {
                continue;
            }

            report.extraArt.Add(new ExtraArtEntry
            {
                eventId = Path.GetFileNameWithoutExtension(spritePath).Substring("card-".Length),
                spritePath = spritePath,
                lowVisibilitySource = LooksLikeLowVisibilitySource(spritePath)
            });
        }
    }

    static void Summarize(BatchReport report)
    {
        report.expectedCount = report.cards.Count;
        report.boundCount = report.cards.Count(card => card.status == "bound");
        report.missingExpectedArtCount = report.cards.Count(card => card.status == "missing");
        report.missingPrefabCount = report.cards.Count(card => card.status == "missing_prefab");
        report.mismatchCount = report.cards.Count(card => card.status == "mismatch");
        report.lowVisibilitySourceCount = report.cards.Count(card => card.lowVisibilitySource);
        report.extraArtCount = report.extraArt.Count;
        report.success = report.errors.Count == 0
            && report.missingExpectedArtCount == 0
            && report.missingPrefabCount == 0
            && report.mismatchCount == 0
            && report.extraArtCount == 0;
    }

    static string GetExpectedSpritePath(CardRow row)
    {
        string artPath = row.Get("artPath").Trim().Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(artPath))
        {
            return artPath;
        }

        return IsStandardCardArtEventId(row.EventId) ? CardsFolder + "/card-" + row.EventId + ".png" : "";
    }

    static string GetPrefabPath(CardRow row)
    {
        return PrefabRoot + "/" + row.GroupName + "/" + row.CardName + ".prefab";
    }

    static bool IsStandardCardArtEventId(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId) || eventId.Length != 4 || eventId[0] != 'E')
        {
            return false;
        }

        for (int i = 1; i < eventId.Length; i++)
        {
            if (!char.IsDigit(eventId[i]))
            {
                return false;
            }
        }

        return true;
    }

    static bool IsUntintedDisplayColor(Color color)
    {
        return color.a > 0.99f && color.r > 0.99f && color.g > 0.99f && color.b > 0.99f;
    }

    static bool LooksLikeLowVisibilitySource(string assetPath)
    {
        string absolutePath = ToAbsoluteAssetPath(assetPath);
        if (!File.Exists(absolutePath))
        {
            return false;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolutePath)))
            {
                return false;
            }

            Color32[] pixels = texture.GetPixels32();
            int visibleCount = 0;
            int nonWhiteCount = 0;
            double lumaSum = 0d;
            foreach (Color32 pixel in pixels)
            {
                if (pixel.a == 0)
                {
                    continue;
                }

                visibleCount++;
                double luma = 0.2126d * pixel.r + 0.7152d * pixel.g + 0.0722d * pixel.b;
                lumaSum += luma;
                if (luma < 245d)
                {
                    nonWhiteCount++;
                }
            }

            if (visibleCount == 0)
            {
                return false;
            }

            double averageLuma = lumaSum / visibleCount;
            double nonWhitePercent = (double)nonWhiteCount / visibleCount * 100d;
            return averageLuma >= 240d && nonWhitePercent < 20d;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    static string ToAbsoluteAssetPath(string assetPath)
    {
        string normalized = assetPath.Replace('\\', '/');
        string relative = normalized.StartsWith("Assets/") ? normalized.Substring("Assets/".Length) : normalized;
        return Path.Combine(Application.dataPath, relative).Replace("\\", "/");
    }

    static List<CardRow> ReadRows(string csv)
    {
        List<string[]> rawRows = ParseRows(csv);
        List<CardRow> rows = new List<CardRow>();
        if (rawRows.Count == 0)
        {
            return rows;
        }

        string[] headers = rawRows[0];
        for (int i = 1; i < rawRows.Count; i++)
        {
            if (rawRows[i].All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            Dictionary<string, string> values = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++)
            {
                values[headers[j]] = j < rawRows[i].Length ? rawRows[i][j] : "";
            }

            rows.Add(new CardRow(values));
        }

        return rows;
    }

    static List<string[]> ParseRows(string csv)
    {
        List<string[]> rows = new List<string[]>();
        List<string> row = new List<string>();
        StringBuilder cell = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < csv.Length; i++)
        {
            char ch = csv[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                row.Add(cell.ToString());
                cell.Length = 0;
            }
            else if ((ch == '\n' || ch == '\r') && !inQuotes)
            {
                if (ch == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(cell.ToString());
                rows.Add(row.ToArray());
                row = new List<string>();
                cell.Length = 0;
            }
            else
            {
                cell.Append(ch);
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row.ToArray());
        }

        return rows;
    }

    static void WriteReport(BatchReport report)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);
    }

    [Serializable]
    public class BatchReport
    {
        public bool success;
        public string generatedAt;
        public string sourceCsv;
        public string cardsFolder;
        public string prefabRoot;
        public string reportPath;
        public int expectedCount;
        public int boundCount;
        public int missingExpectedArtCount;
        public int missingPrefabCount;
        public int mismatchCount;
        public int lowVisibilitySourceCount;
        public int extraArtCount;
        public List<string> errors = new List<string>();
        public List<CardArtEntry> cards = new List<CardArtEntry>();
        public List<ExtraArtEntry> extraArt = new List<ExtraArtEntry>();
    }

    [Serializable]
    public class CardArtEntry
    {
        public string eventId;
        public string cardName;
        public string groupName;
        public string prefabPath;
        public string expectedSpritePath;
        public bool expectedSpriteLoaded;
        public bool prefabLoaded;
        public bool cardStyleFound;
        public bool usePrefabIconOverride;
        public bool iconImageFound;
        public string actualSpriteName;
        public string actualSpritePath;
        public string actualDisplayColor;
        public bool displayColorUntinted;
        public bool lowVisibilitySource;
        public bool matchesExpectedSprite;
        public string status;
        public string error;
    }

    [Serializable]
    public class ExtraArtEntry
    {
        public string eventId;
        public string spritePath;
        public bool lowVisibilitySource;
    }

    class CardRow
    {
        readonly Dictionary<string, string> values;

        public CardRow(Dictionary<string, string> values)
        {
            this.values = values;
        }

        public string EventId { get { return Get("eventId"); } }
        public string CardName { get { return Get("cardName"); } }
        public string GroupName { get { return string.IsNullOrWhiteSpace(Get("groupName")) ? "Main" : Get("groupName"); } }

        public string Get(string key)
        {
            string value;
            return values.TryGetValue(key, out value) ? value ?? "" : "";
        }
    }
}
