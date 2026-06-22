using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class UniversityCardImportTool
{
    const string SourceCsvPath = "Assets/UniversitySimulator/Data/cards_v1_kings_text_import.csv";
    const string StyleListPath = "Assets/Kings/cards/_templates/CardStyle_List.asset";
    const string OutputFolderPath = "Assets/UniversitySimulator/Prefabs/Cards";
    const string ReportPath = "Assets/UniversitySimulator/Data/cards_v1_kings_import_report.json";

    [MenuItem("University Simulator/Cards/Import Kings Text Cards")]
    public static void ImportKingsTextCards()
    {
        ImportReport report = RunImport();
        WriteReport(report);

        if (report.success)
        {
            Debug.Log("University card import finished. Prefabs: " + report.generatedPrefabs + "/" + report.csvRows + ". Report: " + ReportPath);
        }
        else
        {
            Debug.LogError("University card import failed. See report: " + ReportPath);
        }
    }

    static ImportReport RunImport()
    {
        ImportReport report = new ImportReport
        {
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            sourceCsv = SourceCsvPath,
            styleList = StyleListPath,
            outputFolder = OutputFolderPath
        };

        AssetDatabase.Refresh();
        EnsureAssetFolder(OutputFolderPath);

        TextAsset csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(SourceCsvPath);
        if (csvAsset == null)
        {
            report.errors.Add("CSV asset not found: " + SourceCsvPath);
            return report;
        }

        KingsCardStyleList styleList = AssetDatabase.LoadAssetAtPath<KingsCardStyleList>(StyleListPath);
        if (styleList == null)
        {
            report.errors.Add("Style list asset not found: " + StyleListPath);
            return report;
        }

        CsvTable table = CsvTable.Read(Encoding.UTF8.GetString(csvAsset.bytes));
        report.csvRows = table.rows.Count;
        string tableError = table.ValidateRequiredColumns();
        if (!string.IsNullOrEmpty(tableError))
        {
            report.errors.Add(tableError);
            return report;
        }

        string styleErrors = styleList.GetCardStyleDefinitionErrors();
        if (!string.IsNullOrEmpty(styleErrors))
        {
            report.errors.Add("Kings CardStyle_List has invalid entries: " + styleErrors);
            return report;
        }

        string outputAbsolutePath = ToAbsoluteAssetFolder(OutputFolderPath);
        Directory.CreateDirectory(outputAbsolutePath);

        KingsImEx importer = EditorWindow.GetWindow<KingsImEx>("Kings ImEx");
        importer.mInit();
        importer.fieldSeparatorIndex = 0;
        importer.importFile = csvAsset;
        importer.styleDefinitions = styleList;
        importer.importFolder = outputAbsolutePath.Replace("\\", "/");
        importer.AnalyzeImportData();

        if (importer.importState != KingsImEx.E_ImportState.Analyzed_OK)
        {
            report.errors.Add("KingsImEx analyze failed: " + importer.importInfo);
            return report;
        }

        importer.ExecuteImport();
        if (importer.importState != KingsImEx.E_ImportState.Imported_OK)
        {
            report.errors.Add("KingsImEx import failed: " + importer.importInfo);
            return report;
        }

        ApplyStyleReferences(table, styleList, report);
        AssetDatabase.Refresh();
        VerifyImportedPrefabs(table, styleList, report);
        report.success = report.errors.Count == 0 && report.missingPrefabs == 0 && report.prefabErrors == 0;
        return report;
    }

    static void ApplyStyleReferences(CsvTable table, KingsCardStyleList styleList, ImportReport report)
    {
        int fixedCount = 0;
        foreach (Dictionary<string, string> row in table.rows)
        {
            string styleName = row["StyleName"];
            KingsCardStyle style = styleList.GetStyle(styleName);
            if (style == null)
            {
                report.errors.Add("Style does not exist in CardStyle_List: " + styleName);
                continue;
            }

            string prefabPath = OutputFolderPath + "/" + row["GroupName"] + "/" + row["CardName"] + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                CardStyle cardStyle = root.GetComponent<CardStyle>();
                if (cardStyle == null)
                {
                    report.errors.Add("Generated prefab is missing CardStyle: " + prefabPath);
                    continue;
                }

                if (cardStyle.GetStyleName() != styleName)
                {
                    cardStyle.SetStyle(style);
                    cardStyle.Refresh();
                    EditorUtility.SetDirty(cardStyle);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    fixedCount++;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        if (fixedCount > 0)
        {
            report.warnings.Add("Applied CardStyle references after KingsImEx import: " + fixedCount + " prefab(s).");
        }
    }

    static void VerifyImportedPrefabs(CsvTable table, KingsCardStyleList styleList, ImportReport report)
    {
        foreach (Dictionary<string, string> row in table.rows)
        {
            string group = row["GroupName"];
            string cardName = row["CardName"];
            string styleName = row["StyleName"];
            string prefabPath = OutputFolderPath + "/" + group + "/" + cardName + ".prefab";
            CardCheck check = new CardCheck
            {
                cardName = cardName,
                groupName = group,
                styleName = styleName,
                prefabPath = prefabPath
            };

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            check.exists = prefab != null;
            if (prefab == null)
            {
                report.missingPrefabs++;
                check.issues.Add("Prefab was not created.");
                report.cards.Add(check);
                continue;
            }

            EventScript eventScript = prefab.GetComponent<EventScript>();
            CardStyle cardStyle = prefab.GetComponent<CardStyle>();
            check.hasEventScript = eventScript != null;
            check.hasCardStyle = cardStyle != null;

            if (eventScript == null)
            {
                check.issues.Add("Missing EventScript.");
            }
            else
            {
                CompareText(row, "EventScript.titleText", eventScript.textFields.titleText.textContent, check);
                CompareText(row, "EventScript.questionText", eventScript.textFields.questionText.textContent, check);
                CompareText(row, "EventScript.answerLeft", eventScript.textFields.answerLeft.textContent, check);
                CompareText(row, "EventScript.answerRight", eventScript.textFields.answerRight.textContent, check);
                CompareText(row, "EventScript.answerUp", eventScript.textFields.answerUp.textContent, check);
                CompareText(row, "EventScript.answerDown", eventScript.textFields.answerDown.textContent, check);
            }

            if (cardStyle == null)
            {
                check.issues.Add("Missing CardStyle.");
            }
            else if (cardStyle.GetStyleName() != styleName)
            {
                check.issues.Add("Style mismatch. Expected '" + styleName + "', got '" + cardStyle.GetStyleName() + "'.");
            }

            if (!styleList.HasStyle(styleName))
            {
                check.issues.Add("Style does not exist in CardStyle_List: " + styleName);
            }

            check.textMatches = check.issues.Count == 0;
            if (check.issues.Count > 0)
            {
                report.prefabErrors++;
            }

            report.cards.Add(check);
        }

        report.generatedPrefabs = report.cards.FindAll(card => card.exists).Count;
    }

    static void CompareText(Dictionary<string, string> row, string key, string actual, CardCheck check)
    {
        string expected = row.ContainsKey(key) ? row[key] : "";
        if ((actual ?? "") != expected)
        {
            check.issues.Add(key + " mismatch.");
        }
    }

    static void WriteReport(ImportReport report)
    {
        string absolutePath = ToAbsoluteAssetPath(ReportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        File.WriteAllText(absolutePath, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        AssetDatabase.Refresh();
    }

    static string ToAbsoluteAssetFolder(string assetPath)
    {
        string relative = assetPath.StartsWith("Assets/") ? assetPath.Substring("Assets/".Length) : assetPath;
        return Path.Combine(Application.dataPath, relative).Replace("\\", "/");
    }

    static string ToAbsoluteAssetPath(string assetPath)
    {
        string relative = assetPath.StartsWith("Assets/") ? assetPath.Substring("Assets/".Length) : assetPath;
        return Path.Combine(Application.dataPath, relative).Replace("\\", "/");
    }

    static void EnsureAssetFolder(string assetFolder)
    {
        string[] parts = assetFolder.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
        {
            throw new ArgumentException("Folder must be under Assets: " + assetFolder);
        }

        string current = "Assets";
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    [Serializable]
    public class ImportReport
    {
        public bool success;
        public string generatedAt;
        public string sourceCsv;
        public string styleList;
        public string outputFolder;
        public int csvRows;
        public int generatedPrefabs;
        public int missingPrefabs;
        public int prefabErrors;
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
        public List<CardCheck> cards = new List<CardCheck>();
    }

    [Serializable]
    public class CardCheck
    {
        public string cardName;
        public string groupName;
        public string styleName;
        public string prefabPath;
        public bool exists;
        public bool hasEventScript;
        public bool hasCardStyle;
        public bool textMatches;
        public List<string> issues = new List<string>();
    }

    class CsvTable
    {
        public readonly string[] headers;
        public readonly List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

        CsvTable(string[] headers)
        {
            this.headers = headers;
        }

        public static CsvTable Read(string csv)
        {
            string[] lines = csv.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            CsvTable table = new CsvTable(SplitLine(lines[0]));
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                string[] values = SplitLine(lines[i]);
                Dictionary<string, string> row = new Dictionary<string, string>();
                for (int h = 0; h < table.headers.Length; h++)
                {
                    row[table.headers[h]] = h < values.Length ? values[h] : "";
                }

                table.rows.Add(row);
            }

            return table;
        }

        public string ValidateRequiredColumns()
        {
            string[] required =
            {
                "GroupName",
                "CardName",
                "StyleName",
                "EventScript.titleText",
                "EventScript.questionText",
                "EventScript.answerLeft",
                "EventScript.answerRight",
                "EventScript.answerUp",
                "EventScript.answerDown"
            };

            List<string> missing = new List<string>();
            foreach (string requiredColumn in required)
            {
                if (Array.IndexOf(headers, requiredColumn) < 0)
                {
                    missing.Add(requiredColumn);
                }
            }

            return missing.Count == 0 ? "" : "Missing required columns: " + string.Join(", ", missing.ToArray());
        }

        static string[] SplitLine(string line)
        {
            string[] values = Regex.Split(line, ";(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].Length >= 2 && values[i].StartsWith("\"") && values[i].EndsWith("\""))
                {
                    values[i] = values[i].Substring(1, values[i].Length - 2);
                }

                values[i] = values[i].Replace("\"\"", "\"");
            }

            return values;
        }
    }
}
