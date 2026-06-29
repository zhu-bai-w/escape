using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UniversityCardImportTool
{
    const string ProgramCsvPath = "Assets/UniversitySimulator/Data/cards_v1_program.csv";
    const string KingsTextCsvPath = "Assets/UniversitySimulator/Data/cards_v1_kings_text_import.csv";
    const string StyleListPath = "Assets/UniversitySimulator/Data/CardStyles/CardStyle_List.asset";
    const string OutputFolderPath = "Assets/UniversitySimulator/Prefabs/Cards";
    const string CardArtFolderPath = "Assets/UniversitySimulator/Art/cards";
    const string ReportPath = "Assets/UniversitySimulator/Data/cards_v1_kings_import_report.json";
    const string ValidationReportPath = "Assets/UniversitySimulator/Data/cards_v1_program_validation.json";
    const string TargetScenePath = "Assets/UniversitySimulator/Scenes/Game.unity";
    const string ValueEndingGroupName = "ValueEnding";
    static readonly Color CardArtDisplayColor = Color.white;

    static readonly Dictionary<string, valueDefinitions.values> ValueMap = new Dictionary<string, valueDefinitions.values>
    {
        { "bodyMind", valueDefinitions.values.bodyMind },
        { "academics", valueDefinitions.values.academics },
        { "relationships", valueDefinitions.values.relationships },
        { "economy", valueDefinitions.values.economy }
    };

    [MenuItem("University Simulator/Cards/Import Program Cards And Wire Scene")]
    public static void ImportProgramCardsAndWireScene()
    {
        ImportReport report = RunImport();
        WriteReport(report);

        if (report.success)
        {
            Debug.Log("University program card import finished. Prefabs: " + report.generatedPrefabs + "/" + report.csvRows + ". Scene cards: " + report.sceneCards + ". Report: " + ReportPath);
        }
        else
        {
            Debug.LogError("University program card import failed. See report: " + ReportPath);
        }
    }

    [MenuItem("University Simulator/Cards/Validate Program Card CSV")]
    public static void ValidateProgramCardCsv()
    {
        ProgramValidationReport report = RunProgramValidation();
        WriteValidationReport(report);

        if (report.success)
        {
            Debug.Log("University program card validation passed. Rows: " + report.rowCount + ". Event-chain cards: " + report.eventChainCards + ". Report: " + ValidationReportPath);
        }
        else
        {
            Debug.LogError("University program card validation failed. See report: " + ValidationReportPath);
        }
    }

    [MenuItem("University Simulator/Cards/Import Kings Text Cards")]
    public static void ImportKingsTextCards()
    {
        ImportProgramCardsAndWireScene();
    }

    static ImportReport RunImport()
    {
        ImportReport report = new ImportReport
        {
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            sourceCsv = ProgramCsvPath,
            textCsv = KingsTextCsvPath,
            styleList = StyleListPath,
            outputFolder = OutputFolderPath,
            targetScene = TargetScenePath
        };

        AssetDatabase.Refresh();
        EnsureAssetFolder(OutputFolderPath);

        TextAsset programCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(ProgramCsvPath);
        if (programCsv == null)
        {
            report.errors.Add("Program CSV asset not found: " + ProgramCsvPath);
            return report;
        }

        KingsCardStyleList styleList = AssetDatabase.LoadAssetAtPath<KingsCardStyleList>(StyleListPath);
        if (styleList == null)
        {
            report.errors.Add("Style list asset not found: " + StyleListPath);
            return report;
        }

        CsvTable table = CsvTable.Read(Encoding.UTF8.GetString(programCsv.bytes), ',');
        report.csvRows = table.rows.Count;
        string tableError = table.ValidateRequiredColumns(ProgramRequiredColumns());
        if (!string.IsNullOrEmpty(tableError))
        {
            report.errors.Add(tableError);
            return report;
        }

        report.tableValidation = ValidateProgramTable(table);
        if (report.tableValidation.errors.Count > 0)
        {
            report.errors.AddRange(report.tableValidation.errors);
            return report;
        }

        report.warnings.AddRange(report.tableValidation.warnings);

        string styleErrors = styleList.GetCardStyleDefinitionErrors();
        if (!string.IsNullOrEmpty(styleErrors))
        {
            report.errors.Add("Kings CardStyle_List has invalid entries: " + styleErrors);
            return report;
        }

        WriteKingsTextCsv(table, report);
        AssetDatabase.ImportAsset(KingsTextCsvPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        TextAsset textCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(KingsTextCsvPath);
        if (textCsv == null)
        {
            report.errors.Add("Generated Kings text CSV could not be loaded: " + KingsTextCsvPath);
            return report;
        }

        string outputAbsolutePath = ToAbsoluteAssetFolder(OutputFolderPath);
        Directory.CreateDirectory(outputAbsolutePath);

        KingsImEx importer = EditorWindow.GetWindow<KingsImEx>("Kings ImEx");
        importer.mInit();
        importer.fieldSeparatorIndex = 0;
        importer.importFile = textCsv;
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

        AssetDatabase.Refresh();
        Dictionary<string, GameObject> prefabsByEventId = LoadImportedPrefabs(table, report);
        ApplyStyleReferences(table, styleList, report);
        ConfigurePrefabs(table, prefabsByEventId, report);
        WireScene(table, prefabsByEventId, report);
        AssetDatabase.Refresh();
        VerifyImportedPrefabs(table, styleList, report);

        report.success = report.errors.Count == 0 && report.missingPrefabs == 0 && report.prefabErrors == 0;
        return report;
    }

    static string[] ProgramRequiredColumns()
    {
        return new[]
        {
            "eventId",
            "cardName",
            "groupName",
            "styleName",
            "titleText",
            "questionText",
            "answerLeft",
            "answerRight",
            "left_bodyMind",
            "left_academics",
            "left_relationships",
            "left_economy",
            "right_bodyMind",
            "right_academics",
            "right_relationships",
            "right_economy",
            "cardProbability",
            "cooldown",
            "maxDraws",
            "isDrawable",
            "isHighPriority",
            "poolId",
            "nextLeftCardId",
            "nextRightCardId",
            "requiresMetaUnlock",
            "permanentFlagLeft",
            "permanentFlagRight",
            "chainId",
            "chainOrder",
            "isEndingChain"
        };
    }

    static ProgramValidationReport RunProgramValidation()
    {
        ProgramValidationReport report = new ProgramValidationReport
        {
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            sourceCsv = ProgramCsvPath
        };

        AssetDatabase.Refresh();
        TextAsset programCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(ProgramCsvPath);
        if (programCsv == null)
        {
            report.errors.Add("Program CSV asset not found: " + ProgramCsvPath);
            report.success = false;
            return report;
        }

        CsvTable table = CsvTable.Read(Encoding.UTF8.GetString(programCsv.bytes), ',');
        string tableError = table.ValidateRequiredColumns(ProgramRequiredColumns());
        if (!string.IsNullOrEmpty(tableError))
        {
            report.errors.Add(tableError);
            report.success = false;
            return report;
        }

        report = ValidateProgramTable(table);
        report.generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        report.sourceCsv = ProgramCsvPath;
        return report;
    }

    static ProgramValidationReport ValidateProgramTable(CsvTable table)
    {
        ProgramValidationReport report = new ProgramValidationReport
        {
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            sourceCsv = ProgramCsvPath,
            rowCount = table.rows.Count
        };

        Dictionary<string, ProgramCard> rowsByEventId = new Dictionary<string, ProgramCard>();
        HashSet<string> duplicateEventIds = new HashSet<string>();
        foreach (ProgramCard row in table.rows)
        {
            string eventId = row.EventId.Trim();
            if (string.IsNullOrEmpty(eventId))
            {
                report.errors.Add("A row has an empty eventId.");
                continue;
            }

            if (rowsByEventId.ContainsKey(eventId))
            {
                duplicateEventIds.Add(eventId);
            }
            else
            {
                rowsByEventId[eventId] = row;
            }

            if (string.IsNullOrWhiteSpace(row.CardName))
            {
                report.errors.Add("Card " + eventId + " has an empty cardName.");
            }
            else if (row.CardName != "US_" + eventId)
            {
                report.warnings.Add("Card " + eventId + " uses cardName '" + row.CardName + "'. Standard workflow expects 'US_" + eventId + "'.");
            }
        }

        foreach (string duplicateEventId in duplicateEventIds.OrderBy(id => id))
        {
            report.errors.Add("Duplicate eventId: " + duplicateEventId);
        }

        foreach (ProgramCard row in table.rows)
        {
            ValidateFollowUpReference(row, "nextLeftCardId", rowsByEventId, report);
            ValidateFollowUpReference(row, "nextRightCardId", rowsByEventId, report);
            ValidateFollowUpReference(row, "chainStartLeft", rowsByEventId, report);
            ValidateFollowUpReference(row, "chainStartRight", rowsByEventId, report);
        }

        ValidateEventChains(table, rowsByEventId, report);
        report.success = report.errors.Count == 0;
        return report;
    }

    static void ValidateFollowUpReference(ProgramCard row, string fieldName, Dictionary<string, ProgramCard> rowsByEventId, ProgramValidationReport report)
    {
        string targetId = row.Get(fieldName).Trim();
        if (string.IsNullOrEmpty(targetId))
        {
            return;
        }

        if (!rowsByEventId.ContainsKey(targetId))
        {
            report.errors.Add("Card " + row.EventId + " references missing " + fieldName + ": " + targetId);
        }
    }

    static void ValidateEventChains(CsvTable table, Dictionary<string, ProgramCard> rowsByEventId, ProgramValidationReport report)
    {
        Dictionary<string, List<ProgramCard>> chains = new Dictionary<string, List<ProgramCard>>();
        foreach (ProgramCard row in table.rows)
        {
            string chainId = row.Get("chainId").Trim();
            if (string.IsNullOrEmpty(chainId))
            {
                continue;
            }

            if (!chains.ContainsKey(chainId))
            {
                chains[chainId] = new List<ProgramCard>();
            }

            chains[chainId].Add(row);
        }

        report.eventChainCount = chains.Count;
        report.eventChainCards = chains.Values.Sum(rows => rows.Count);

        foreach (KeyValuePair<string, List<ProgramCard>> pair in chains.OrderBy(item => item.Key))
        {
            ChainCheck check = new ChainCheck
            {
                chainId = pair.Key,
                cardCount = pair.Value.Count
            };

            bool isMainlineChain = pair.Key.StartsWith("MAIN_", StringComparison.OrdinalIgnoreCase)
                || pair.Value.Any(row => row.GroupName == "Mainline");

            Dictionary<int, ProgramCard> rowsByOrder = new Dictionary<int, ProgramCard>();
            HashSet<int> duplicateOrders = new HashSet<int>();
            foreach (ProgramCard row in pair.Value)
            {
                int order = ParseInt(row.Get("chainOrder"), 0);
                if (order <= 0)
                {
                    AddChainError(report, check, "Card " + row.EventId + " in chain " + pair.Key + " must have chainOrder >= 1.");
                    continue;
                }

                if (rowsByOrder.ContainsKey(order))
                {
                    duplicateOrders.Add(order);
                }
                else
                {
                    rowsByOrder[order] = row;
                }

                if (ParseBool(row.Get("isDrawable"), true))
                {
                    AddChainError(report, check, "Card " + row.EventId + " in chain " + pair.Key + " must set isDrawable=false.");
                }

                if (isMainlineChain && row.GroupName != "Mainline")
                {
                    AddChainWarning(report, check, "Card " + row.EventId + " is in mainline chain " + pair.Key + " but groupName is '" + row.GroupName + "'. Standard workflow uses Mainline.");
                }
            }

            foreach (int duplicateOrder in duplicateOrders.OrderBy(order => order))
            {
                AddChainError(report, check, "Chain " + pair.Key + " has duplicate chainOrder " + duplicateOrder + ".");
            }

            if (!rowsByOrder.ContainsKey(1))
            {
                AddChainError(report, check, "Chain " + pair.Key + " has no entry card with chainOrder=1.");
            }
            else
            {
                check.entryEventId = rowsByOrder[1].EventId;
                report.eventChainEntryCards++;
            }

            int maxOrder = rowsByOrder.Count == 0 ? 0 : rowsByOrder.Keys.Max();
            check.orderRange = maxOrder == 0 ? "" : "1-" + maxOrder;
            for (int order = 1; order <= maxOrder; order++)
            {
                ProgramCard row;
                if (!rowsByOrder.TryGetValue(order, out row))
                {
                    AddChainError(report, check, "Chain " + pair.Key + " is missing chainOrder " + order + ".");
                    continue;
                }

                bool isFinal = order == maxOrder;
                ValidateChainCard(row, order, isFinal, pair.Key, rowsByOrder, rowsByEventId, isMainlineChain, report, check);
            }

            report.chainChecks.Add(check);
        }
    }

    static void ValidateChainCard(
        ProgramCard row,
        int order,
        bool isFinal,
        string chainId,
        Dictionary<int, ProgramCard> rowsByOrder,
        Dictionary<string, ProgramCard> rowsByEventId,
        bool isMainlineChain,
        ProgramValidationReport report,
        ChainCheck check)
    {
        if (order == 1)
        {
            if (!string.IsNullOrWhiteSpace(row.Get("nextLeftCardId")) && isMainlineChain)
            {
                AddChainError(report, check, "Entry card " + row.EventId + " should leave nextLeftCardId empty so the wrong choice returns to the normal pool.");
            }

            if (rowsByOrder.Count > 1)
            {
                ValidateExpectedChainNext(row, "nextRightCardId", 2, chainId, rowsByOrder, rowsByEventId, report, check);
            }

            if (isMainlineChain && string.IsNullOrWhiteSpace(row.Get("permanentFlagRight")))
            {
                AddChainError(report, check, "Entry card " + row.EventId + " in mainline chain " + chainId + " must set permanentFlagRight.");
            }

            if (isMainlineChain && !string.IsNullOrWhiteSpace(row.Get("permanentFlagLeft")))
            {
                AddChainError(report, check, "Entry card " + row.EventId + " should leave permanentFlagLeft empty.");
            }

            return;
        }

        if (!ChainCardValuesAreZero(row))
        {
            AddChainError(report, check, "Narrative chain card " + row.EventId + " must keep all value deltas at 0.");
        }

        if (!string.IsNullOrWhiteSpace(row.Get("permanentFlagLeft")) || !string.IsNullOrWhiteSpace(row.Get("permanentFlagRight")))
        {
            AddChainError(report, check, "Narrative chain card " + row.EventId + " should not set permanent flags.");
        }

        if (isFinal)
        {
            if (!string.IsNullOrWhiteSpace(row.Get("nextLeftCardId")) || !string.IsNullOrWhiteSpace(row.Get("nextRightCardId")))
            {
                AddChainError(report, check, "Final chain card " + row.EventId + " should leave both next card fields empty.");
            }

            return;
        }

        ValidateExpectedChainNext(row, "nextLeftCardId", order + 1, chainId, rowsByOrder, rowsByEventId, report, check);
        ValidateExpectedChainNext(row, "nextRightCardId", order + 1, chainId, rowsByOrder, rowsByEventId, report, check);
    }

    static void ValidateExpectedChainNext(
        ProgramCard row,
        string fieldName,
        int expectedOrder,
        string chainId,
        Dictionary<int, ProgramCard> rowsByOrder,
        Dictionary<string, ProgramCard> rowsByEventId,
        ProgramValidationReport report,
        ChainCheck check)
    {
        ProgramCard expectedRow;
        rowsByOrder.TryGetValue(expectedOrder, out expectedRow);
        string targetId = row.Get(fieldName).Trim();
        if (expectedRow == null)
        {
            AddChainError(report, check, "Card " + row.EventId + " expects chainOrder " + expectedOrder + " in chain " + chainId + ", but that card is missing.");
            return;
        }

        if (targetId != expectedRow.EventId)
        {
            AddChainError(report, check, "Card " + row.EventId + " " + fieldName + " should point to " + expectedRow.EventId + ".");
            return;
        }

        ProgramCard targetRow;
        if (rowsByEventId.TryGetValue(targetId, out targetRow) && targetRow.Get("chainId").Trim() != chainId)
        {
            AddChainError(report, check, "Card " + row.EventId + " " + fieldName + " points outside chain " + chainId + ".");
        }
    }

    static bool ChainCardValuesAreZero(ProgramCard row)
    {
        return Mathf.Approximately(ParseFloat(row.Get("left_bodyMind"), 0f), 0f)
            && Mathf.Approximately(ParseFloat(row.Get("left_academics"), 0f), 0f)
            && Mathf.Approximately(ParseFloat(row.Get("left_relationships"), 0f), 0f)
            && Mathf.Approximately(ParseFloat(row.Get("left_economy"), 0f), 0f)
            && Mathf.Approximately(ParseFloat(row.Get("right_bodyMind"), 0f), 0f)
            && Mathf.Approximately(ParseFloat(row.Get("right_academics"), 0f), 0f)
            && Mathf.Approximately(ParseFloat(row.Get("right_relationships"), 0f), 0f)
            && Mathf.Approximately(ParseFloat(row.Get("right_economy"), 0f), 0f);
    }

    static void AddChainError(ProgramValidationReport report, ChainCheck check, string message)
    {
        report.errors.Add(message);
        check.issues.Add(message);
    }

    static void AddChainWarning(ProgramValidationReport report, ChainCheck check, string message)
    {
        report.warnings.Add(message);
        check.warnings.Add(message);
    }

    static void WriteKingsTextCsv(CsvTable table, ImportReport report)
    {
        string[] headers =
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

        StringBuilder csv = new StringBuilder();
        csv.AppendLine(string.Join(";", headers));
        foreach (ProgramCard row in table.rows)
        {
            string[] values =
            {
                row.GroupName,
                row.CardName,
                row.StyleName,
                row.Get("titleText"),
                row.Get("questionText"),
                row.Get("answerLeft"),
                row.Get("answerRight"),
                "",
                ""
            };
            csv.AppendLine(string.Join(";", values.Select(QuoteSemicolonCsv)));
        }

        File.WriteAllText(ToAbsoluteAssetPath(KingsTextCsvPath), csv.ToString(), new UTF8Encoding(false));
        report.generatedTextCsvRows = table.rows.Count;
    }

    static Dictionary<string, GameObject> LoadImportedPrefabs(CsvTable table, ImportReport report)
    {
        Dictionary<string, GameObject> prefabsByEventId = new Dictionary<string, GameObject>();
        foreach (ProgramCard row in table.rows)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(row));
            if (prefab == null)
            {
                report.missingPrefabs++;
                report.errors.Add("Prefab was not created: " + GetPrefabPath(row));
                continue;
            }

            if (!string.IsNullOrEmpty(row.EventId))
            {
                prefabsByEventId[row.EventId] = prefab;
            }
        }

        return prefabsByEventId;
    }

    static void ApplyStyleReferences(CsvTable table, KingsCardStyleList styleList, ImportReport report)
    {
        int fixedCount = 0;
        foreach (ProgramCard row in table.rows)
        {
            KingsCardStyle style = styleList.GetStyle(row.StyleName);
            if (style == null)
            {
                report.errors.Add("Style does not exist in CardStyle_List: " + row.StyleName);
                continue;
            }

            string prefabPath = GetPrefabPath(row);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                CardStyle cardStyle = root.GetComponent<CardStyle>();
                if (cardStyle == null)
                {
                    report.errors.Add("Generated prefab is missing CardStyle: " + prefabPath);
                    continue;
                }

                if (cardStyle.GetStyleName() != row.StyleName)
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

    static void ConfigurePrefabs(CsvTable table, Dictionary<string, GameObject> prefabsByEventId, ImportReport report)
    {
        foreach (ProgramCard row in table.rows)
        {
            string prefabPath = GetPrefabPath(row);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                EventScript eventScript = root.GetComponent<EventScript>();
                if (eventScript == null)
                {
                    report.errors.Add("Generated prefab is missing EventScript: " + prefabPath);
                    continue;
                }

                eventScript.isDrawable = ParseBool(row.Get("isDrawable"), true);
                eventScript.isHighPriorityCard = ParseBool(row.Get("isHighPriority"), false);
                eventScript.cardPropability = Mathf.Clamp01(ParseFloat(row.Get("cardProbability"), Mathf.Min(1f, ParseFloat(row.Get("weight"), 1f))));
                eventScript.redrawBlockCnt = Mathf.Max(0, ParseInt(row.Get("cooldown"), 0));
                eventScript.maxDraws = Mathf.Max(1, ParseInt(row.Get("maxDraws"), ParseBool(row.Get("unique"), false) ? 1 : 100));
                eventScript.swipeType = EventScript.E_SwipeType.LeftRight;
                ConfigureConditionExpression(root, row, report);
                eventScript.conditions = string.IsNullOrWhiteSpace(row.Get("conditionExpression"))
                    ? BuildConditions(row, report)
                    : new EventScript.condition[0];
                eventScript.Results.resultLeft = BuildResult(row, "left");
                eventScript.Results.resultRight = BuildResult(row, "right");
                eventScript.Results.resultUp = BuildEmptyResult();
                eventScript.Results.resultDown = BuildEmptyResult();
                eventScript.Results.additional_choice_0 = BuildEmptyResult();
                eventScript.Results.additional_choice_1 = BuildEmptyResult();

                ApplyFollowUp(eventScript.Results.resultLeft, row.Get("nextLeftCardId"), prefabsByEventId, row, report);
                ApplyFollowUp(eventScript.Results.resultRight, row.Get("nextRightCardId"), prefabsByEventId, row, report);
                ConfigureMainlineHook(root, eventScript, row, prefabsByEventId, report);
                ConfigureMainlineEventCard(root, row);
                ConfigureReturnToNewGame(root, eventScript, row, report);
                ConfigureCardArt(root, row, report);

                EditorUtility.SetDirty(eventScript);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                report.configuredPrefabs++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    static void ConfigureCardArt(GameObject root, ProgramCard row, ImportReport report)
    {
        string expectedArtPath = GetExpectedCardArtPath(row);
        if (string.IsNullOrWhiteSpace(expectedArtPath))
        {
            return;
        }

        report.cardArtExpected++;
        CardStyle cardStyle = root.GetComponent<CardStyle>();
        if (cardStyle == null)
        {
            report.errors.Add("Generated prefab is missing CardStyle for card art binding: " + GetPrefabPath(row));
            return;
        }

        cardStyle.usePrefabIconOverride = true;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(expectedArtPath);
        if (cardStyle.iconImage == null)
        {
            report.errors.Add("Generated prefab is missing iconImage for card art binding: " + GetPrefabPath(row));
            EditorUtility.SetDirty(cardStyle);
            return;
        }

        cardStyle.iconImage.color = CardArtDisplayColor;
        cardStyle.iconImage.preserveAspect = true;
        if (sprite == null)
        {
            cardStyle.iconImage.sprite = null;
            report.cardArtMissing++;
            report.warnings.Add("Card " + row.EventId + " is missing expected card art: " + expectedArtPath);
        }
        else
        {
            cardStyle.iconImage.sprite = sprite;
            report.cardArtBound++;
        }

        EditorUtility.SetDirty(cardStyle.iconImage);
        EditorUtility.SetDirty(cardStyle);
    }

    static void ConfigureConditionExpression(GameObject root, ProgramCard row, ImportReport report)
    {
        string expression = row.Get("conditionExpression").Trim();
        UniversityConditionExpression gate = root.GetComponent<UniversityConditionExpression>();
        if (string.IsNullOrWhiteSpace(expression))
        {
            if (gate != null)
            {
                UnityEngine.Object.DestroyImmediate(gate, true);
            }

            return;
        }

        if (gate == null)
        {
            gate = root.AddComponent<UniversityConditionExpression>();
        }

        gate.conditionExpression = expression;
        gate.emptyExpressionPasses = true;
        gate.dangerLowThreshold = ParseFloat(row.Get("dangerLowThreshold"), 30f);
        gate.dangerHighThreshold = ParseFloat(row.Get("dangerHighThreshold"), 70f);
        gate.debugLogInvalidExpression = false;
        EditorUtility.SetDirty(gate);
        report.conditionExpressionGates++;
    }

    static EventScript.condition[] BuildConditions(ProgramCard row, ImportReport report)
    {
        List<EventScript.condition> conditions = new List<EventScript.condition>();
        AddRangeCondition(row, conditions, "bodyMind", "condition_bodyMind_min", "condition_bodyMind_max");
        AddRangeCondition(row, conditions, "academics", "condition_academics_min", "condition_academics_max");
        AddRangeCondition(row, conditions, "relationships", "condition_relationships_min", "condition_relationships_max");
        AddRangeCondition(row, conditions, "economy", "condition_economy_min", "condition_economy_max");

        if (!string.IsNullOrWhiteSpace(row.Get("condition_round_min")))
        {
            report.warnings.Add("Card " + row.EventId + " uses condition_round_min. Runtime day gating is handled by UniversityTrueEndingProgress, not Kings value conditions.");
        }

        return conditions.ToArray();
    }

    static void AddRangeCondition(ProgramCard row, List<EventScript.condition> conditions, string valueName, string minColumn, string maxColumn)
    {
        string minText = row.Get(minColumn);
        string maxText = row.Get(maxColumn);
        if (string.IsNullOrWhiteSpace(minText) && string.IsNullOrWhiteSpace(maxText))
        {
            return;
        }

        float min = string.IsNullOrWhiteSpace(minText) ? 0f : ParseFloat(minText, 0f);
        float max = string.IsNullOrWhiteSpace(maxText) ? 100f : ParseFloat(maxText, 100f);
        conditions.Add(new EventScript.condition
        {
            type = EventScript.E_ConditionType.standard,
            value = ValueMap[valueName],
            valueMin = min,
            valueMax = max,
            compareType = EventScript.E_ConditionCompareType.greaterThan,
            rValue = valueDefinitions.values.name,
            itemCompareType = EventScript.E_ItemCompareType.greaterThan,
            item = null,
            itemCmpValue = 1,
            gamedictionary_key = "",
            gamedictionary_comparer = ""
        });
    }

    static EventScript.result BuildResult(ProgramCard row, string side)
    {
        List<EventScript.resultModifier> modifiers = new List<EventScript.resultModifier>();
        AddValueModifier(row, modifiers, side, "bodyMind");
        AddValueModifier(row, modifiers, side, "academics");
        AddValueModifier(row, modifiers, side, "relationships");
        AddValueModifier(row, modifiers, side, "economy");
        return BuildResult(modifiers.ToArray());
    }

    static void AddValueModifier(ProgramCard row, List<EventScript.resultModifier> modifiers, string side, string valueName)
    {
        float delta = ParseFloat(row.Get(side + "_" + valueName), 0f);
        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        modifiers.Add(new EventScript.resultModifier
        {
            modificationType = EventScript.E_ModificationType.add,
            modifier = ValueMap[valueName],
            valueAdd = delta,
            valueSet = 0f,
            rndRangeAdd = new EventScript.C_RndRange(),
            rndRangeSet = new EventScript.C_RndRange()
        });
    }

    static EventScript.result BuildEmptyResult()
    {
        return BuildResult(new EventScript.resultModifier[0]);
    }

    static EventScript.result BuildResult(EventScript.resultModifier[] valueChanges)
    {
        return new EventScript.result
        {
            resultType = EventScript.resultTypes.simple,
            modifiers = BuildModifierGroup(valueChanges),
            conditions = new EventScript.condition[0],
            modifiersTrue = BuildModifierGroup(new EventScript.resultModifier[0]),
            modifiersFalse = BuildModifierGroup(new EventScript.resultModifier[0]),
            randomModifiers = new EventScript.modifierGroup[0]
        };
    }

    static EventScript.modifierGroup BuildModifierGroup(EventScript.resultModifier[] valueChanges)
    {
        return new EventScript.modifierGroup
        {
            valueChanges = valueChanges,
            extras = new EventScript.C_AdditionalModifiers[0],
            followUpCard = null,
            followUpDelay = new EventScript.C_intRange { min = 0, max = 0 }
        };
    }

    static void ApplyFollowUp(EventScript.result result, string nextCardId, Dictionary<string, GameObject> prefabsByEventId, ProgramCard row, ImportReport report)
    {
        if (string.IsNullOrWhiteSpace(nextCardId))
        {
            return;
        }

        GameObject nextCard;
        if (!prefabsByEventId.TryGetValue(nextCardId.Trim(), out nextCard) || nextCard == null)
        {
            report.errors.Add("Card " + row.EventId + " references missing follow-up card: " + nextCardId);
            return;
        }

        result.modifiers.followUpCard = nextCard;
        result.modifiers.followUpDelay = new EventScript.C_intRange { min = 0, max = 0 };
    }

    static void ConfigureMainlineEventCard(GameObject root, ProgramCard row)
    {
        string chainId = row.Get("chainId").Trim();
        UniversityMainlineEventCard marker = root.GetComponent<UniversityMainlineEventCard>();
        if (string.IsNullOrWhiteSpace(chainId))
        {
            if (marker != null)
            {
                UnityEngine.Object.DestroyImmediate(marker, true);
            }

            return;
        }

        if (marker == null)
        {
            marker = root.AddComponent<UniversityMainlineEventCard>();
        }

        int chainOrder = ParseInt(row.Get("chainOrder"), 0);
        marker.chainId = chainId;
        marker.chainOrder = chainOrder;
        marker.isEntryCard = chainOrder == 1;
        marker.permanentFlagId = row.Get("permanentFlagRight").Trim();
        EditorUtility.SetDirty(marker);
    }

    static void ConfigureMainlineHook(GameObject root, EventScript eventScript, ProgramCard row, Dictionary<string, GameObject> prefabsByEventId, ImportReport report)
    {
        string permanentFlagLeft = row.Get("permanentFlagLeft");
        string permanentFlagRight = row.Get("permanentFlagRight");
        string chainStartLeftId = row.Get("chainStartLeft");
        string chainStartRightId = row.Get("chainStartRight");
        bool hasHookData = !string.IsNullOrWhiteSpace(permanentFlagLeft)
            || !string.IsNullOrWhiteSpace(permanentFlagRight)
            || !string.IsNullOrWhiteSpace(chainStartLeftId)
            || !string.IsNullOrWhiteSpace(chainStartRightId);

        UniversityMainlineCardHook hook = root.GetComponent<UniversityMainlineCardHook>();
        if (!hasHookData)
        {
            if (hook != null)
            {
                UnityEngine.Object.DestroyImmediate(hook, true);
            }

            return;
        }

        if (hook == null)
        {
            hook = root.AddComponent<UniversityMainlineCardHook>();
        }

        hook.cardId = row.EventId;
        hook.requiresMetaUnlock = ParseBool(row.Get("requiresMetaUnlock"), true);
        hook.requiredLifetimeDays = ParseInt(row.Get("requiredLifetimeDays"), UniversityTrueEndingProgress.DefaultRequiredLifetimeDays);
        hook.requiredGameOvers = ParseInt(row.Get("requiredGameOvers"), UniversityTrueEndingProgress.DefaultRequiredGameOvers);
        hook.permanentFlagLeft = permanentFlagLeft.Trim();
        hook.permanentFlagRight = permanentFlagRight.Trim();
        hook.chainStartLeft = ResolveOptionalCard(chainStartLeftId, prefabsByEventId, row, report, "chainStartLeft");
        hook.chainStartRight = ResolveOptionalCard(chainStartRightId, prefabsByEventId, row, report, "chainStartRight");

        RemovePersistentListenersForTarget(eventScript.OnSwipeLeft, hook);
        RemovePersistentListenersForTarget(eventScript.OnSwipeRight, hook);
        UnityEventTools.AddPersistentListener(eventScript.OnSwipeLeft, hook.ApplyLeftChoice);
        UnityEventTools.AddPersistentListener(eventScript.OnSwipeRight, hook.ApplyRightChoice);
        EditorUtility.SetDirty(hook);
        report.mainlineHooks++;
    }

    static void ConfigureReturnToNewGame(GameObject root, EventScript eventScript, ProgramCard row, ImportReport report)
    {
        bool isTrueEndingCard = ParseBool(row.Get("isEndingChain"), false);
        bool isValueEndingCard = IsValueEndingRow(row);
        bool shouldReturnToNewGame = isTrueEndingCard || isValueEndingCard;
        UniversityTrueEndingReturnToNewGame returnToNewGame = root.GetComponent<UniversityTrueEndingReturnToNewGame>();
        if (!shouldReturnToNewGame)
        {
            if (returnToNewGame != null)
            {
                RemovePersistentListenersForTarget(eventScript.OnSwipeLeft, returnToNewGame);
                RemovePersistentListenersForTarget(eventScript.OnSwipeRight, returnToNewGame);
                UnityEngine.Object.DestroyImmediate(returnToNewGame, true);
            }

            return;
        }

        if (returnToNewGame == null)
        {
            returnToNewGame = root.AddComponent<UniversityTrueEndingReturnToNewGame>();
        }

        returnToNewGame.reloadScene = true;
        RemovePersistentListenersForTarget(eventScript.OnSwipeLeft, returnToNewGame);
        RemovePersistentListenersForTarget(eventScript.OnSwipeRight, returnToNewGame);
        UnityEventTools.AddPersistentListener(eventScript.OnSwipeLeft, returnToNewGame.ReturnToNewGame);
        UnityEventTools.AddPersistentListener(eventScript.OnSwipeRight, returnToNewGame.ReturnToNewGame);
        EditorUtility.SetDirty(returnToNewGame);
        if (isTrueEndingCard)
        {
            report.trueEndingReturnCards++;
        }
        else if (isValueEndingCard)
        {
            report.valueEndingReturnCards++;
        }
    }

    static GameObject ResolveOptionalCard(string eventId, Dictionary<string, GameObject> prefabsByEventId, ProgramCard row, ImportReport report, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        GameObject card;
        if (!prefabsByEventId.TryGetValue(eventId.Trim(), out card) || card == null)
        {
            report.errors.Add("Card " + row.EventId + " references missing " + fieldName + ": " + eventId);
            return null;
        }

        return card;
    }

    static void RemovePersistentListenersForTarget(EventScript.mEvent unityEvent, UnityEngine.Object target)
    {
        for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (unityEvent.GetPersistentTarget(i) == target)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, i);
            }
        }
    }

    static void WireScene(CsvTable table, Dictionary<string, GameObject> prefabsByEventId, ImportReport report)
    {
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != TargetScenePath)
        {
            if (activeScene.IsValid() && activeScene.isDirty && !string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        }

        CardStack cardStack = FindSceneComponent<CardStack>();
        if (cardStack == null)
        {
            report.errors.Add("CardStack not found in scene: " + TargetScenePath);
            return;
        }

        Dictionary<string, List<GameObject>> groupedCards = new Dictionary<string, List<GameObject>>();
        foreach (ProgramCard row in table.rows)
        {
            GameObject prefab;
            if (!prefabsByEventId.TryGetValue(row.EventId, out prefab) || prefab == null)
            {
                continue;
            }

            if (!groupedCards.ContainsKey(row.GroupName))
            {
                groupedCards[row.GroupName] = new List<GameObject>();
            }

            groupedCards[row.GroupName].Add(prefab);
        }

        cardStack.allCards = groupedCards
            .OrderBy(pair => GetGroupSortOrder(pair.Key))
            .ThenBy(pair => pair.Key)
            .Select(pair => new CardStack.cardCategory
            {
                groupName = pair.Key,
                subStackCondition = new EventScript.condition[0],
                groupCards = pair.Value.ToArray()
            })
            .ToArray();

        GameObject fallback;
        if (prefabsByEventId.TryGetValue("E001", out fallback) && fallback != null)
        {
            cardStack.fallBackCard = fallback;
        }

        UniversityTrueEndingController controller = cardStack.GetComponent<UniversityTrueEndingController>();
        if (controller == null)
        {
            controller = cardStack.gameObject.AddComponent<UniversityTrueEndingController>();
        }

        controller.requiredLifetimeDays = UniversityTrueEndingProgress.DefaultRequiredLifetimeDays;
        controller.requiredGameOvers = UniversityTrueEndingProgress.DefaultRequiredGameOvers;
        controller.requiredPermanentFlags = UniversityTrueEndingProgress.DefaultRequiredFlags.ToArray();
        controller.triggerOnlyOnce = true;
        controller.countCardSwipesAsDays = true;
        controller.countGameOverEvents = true;
        controller.trueEndingStartCard = ResolveTrueEndingStartCard(table, prefabsByEventId, report);

        UniversityMainlineEventScheduler scheduler = cardStack.GetComponent<UniversityMainlineEventScheduler>();
        if (scheduler == null)
        {
            scheduler = cardStack.gameObject.AddComponent<UniversityMainlineEventScheduler>();
        }

        scheduler.protectedNormalDraws = 10;
        scheduler.initialChance = 0.15f;
        scheduler.chanceIncreasePerMiss = 0.15f;
        scheduler.guaranteedAfterMisses = 6;
        scheduler.debugLog = false;

        UniversityValueEndingController valueEndingController = cardStack.GetComponent<UniversityValueEndingController>();
        if (valueEndingController == null)
        {
            valueEndingController = cardStack.gameObject.AddComponent<UniversityValueEndingController>();
        }

        ConfigureValueEndingController(valueEndingController, table, prefabsByEventId, report);

        EditorUtility.SetDirty(cardStack);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(scheduler);
        EditorUtility.SetDirty(valueEndingController);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        report.sceneGroups = cardStack.allCards.Length;
        report.sceneCards = cardStack.allCards.Sum(group => group.groupCards.Length);
        report.trueEndingStartCard = controller.trueEndingStartCard != null ? controller.trueEndingStartCard.name : "";
    }

    static int GetGroupSortOrder(string groupName)
    {
        if (groupName == "Main")
        {
            return 0;
        }

        if (groupName == "TrueEnding")
        {
            return 90;
        }

        if (groupName == ValueEndingGroupName)
        {
            return 80;
        }

        return 10;
    }

    static void ConfigureValueEndingController(
        UniversityValueEndingController controller,
        CsvTable table,
        Dictionary<string, GameObject> prefabsByEventId,
        ImportReport report)
    {
        List<UniversityValueEndingController.ValueEndingRule> rules = new List<UniversityValueEndingController.ValueEndingRule>();
        foreach (ProgramCard row in table.rows.Where(IsValueEndingRow).OrderBy(row => row.EventId))
        {
            valueDefinitions.values valueType;
            bool triggerOnMaximum;
            if (!TryParseValueEndingRule(row, out valueType, out triggerOnMaximum))
            {
                report.errors.Add("Value ending card " + row.EventId + " must set conditionExpression like 'bodyMind > 100' or 'economy < 0'.");
                continue;
            }

            GameObject endingCard;
            if (!prefabsByEventId.TryGetValue(row.EventId, out endingCard) || endingCard == null)
            {
                report.errors.Add("Value ending prefab missing: " + row.EventId);
                continue;
            }

            rules.Add(new UniversityValueEndingController.ValueEndingRule
            {
                eventId = row.EventId,
                valueType = valueType,
                triggerOnMaximum = triggerOnMaximum,
                endingCard = endingCard
            });
        }

        controller.rules = rules.ToArray();
        controller.triggerOnlyOncePerRun = true;
        controller.debugLog = false;
        report.valueEndingRules = rules.Count;
    }

    static bool TryParseValueEndingRule(ProgramCard row, out valueDefinitions.values valueType, out bool triggerOnMaximum)
    {
        valueType = valueDefinitions.values.bodyMind;
        triggerOnMaximum = false;
        string expression = row.Get("conditionExpression").Trim();
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        foreach (KeyValuePair<string, valueDefinitions.values> pair in ValueMap)
        {
            if (!expression.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            valueType = pair.Value;
            if (expression.Contains(">"))
            {
                triggerOnMaximum = true;
                return true;
            }

            if (expression.Contains("<"))
            {
                triggerOnMaximum = false;
                return true;
            }
        }

        return false;
    }

    static bool IsValueEndingRow(ProgramCard row)
    {
        return string.Equals(row.GroupName, ValueEndingGroupName, StringComparison.OrdinalIgnoreCase);
    }

    static bool ShouldReturnToNewGame(ProgramCard row)
    {
        return ParseBool(row.Get("isEndingChain"), false) || IsValueEndingRow(row);
    }

    static GameObject ResolveTrueEndingStartCard(CsvTable table, Dictionary<string, GameObject> prefabsByEventId, ImportReport report)
    {
        ProgramCard configuredStart = table.rows
            .Where(row => ParseBool(row.Get("isEndingChain"), false))
            .OrderBy(row => ParseInt(row.Get("chainOrder"), 999))
            .FirstOrDefault();

        if (configuredStart == null)
        {
            report.warnings.Add("No true-ending chain start card configured; true ending trigger is disabled for this card set.");
            return null;
        }

        GameObject prefab;
        if (!prefabsByEventId.TryGetValue(configuredStart.EventId, out prefab) || prefab == null)
        {
            report.errors.Add("True-ending chain start prefab missing: " + configuredStart.EventId);
            return null;
        }

        return prefab;
    }

    static T FindSceneComponent<T>() where T : Component
    {
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (component != null && component.gameObject.scene.IsValid())
            {
                return component;
            }
        }

        return null;
    }

    static void VerifyImportedPrefabs(CsvTable table, KingsCardStyleList styleList, ImportReport report)
    {
        foreach (ProgramCard row in table.rows)
        {
            string prefabPath = GetPrefabPath(row);
            CardCheck check = new CardCheck
            {
                eventId = row.EventId,
                cardName = row.CardName,
                groupName = row.GroupName,
                styleName = row.StyleName,
                prefabPath = prefabPath
            };

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            check.exists = prefab != null;
            if (prefab == null)
            {
                check.issues.Add("Prefab was not created.");
                report.cards.Add(check);
                continue;
            }

            EventScript eventScript = prefab.GetComponent<EventScript>();
            CardStyle cardStyle = prefab.GetComponent<CardStyle>();
            check.hasEventScript = eventScript != null;
            check.hasCardStyle = cardStyle != null;
            check.hasMainlineHook = prefab.GetComponent<UniversityMainlineCardHook>() != null;
            check.hasConditionExpression = prefab.GetComponent<UniversityConditionExpression>() != null;
            check.hasTrueEndingReturn = prefab.GetComponent<UniversityTrueEndingReturnToNewGame>() != null;
            check.cardArtExpectedPath = GetExpectedCardArtPath(row);

            if (eventScript == null)
            {
                check.issues.Add("Missing EventScript.");
            }
            else
            {
                CompareText(row.Get("titleText"), eventScript.textFields.titleText.textContent, "titleText", check);
                CompareText(row.Get("questionText"), eventScript.textFields.questionText.textContent, "questionText", check);
                CompareText(row.Get("answerLeft"), eventScript.textFields.answerLeft.textContent, "answerLeft", check);
                CompareText(row.Get("answerRight"), eventScript.textFields.answerRight.textContent, "answerRight", check);
                if (eventScript.isDrawable != ParseBool(row.Get("isDrawable"), true))
                {
                    check.issues.Add("isDrawable mismatch.");
                }

                if (eventScript.isHighPriorityCard != ParseBool(row.Get("isHighPriority"), false))
                {
                    check.issues.Add("isHighPriority mismatch.");
                }
            }

            if (cardStyle == null)
            {
                check.issues.Add("Missing CardStyle.");
            }
            else
            {
                if (cardStyle.GetStyleName() != row.StyleName)
                {
                    check.issues.Add("Style mismatch. Expected '" + row.StyleName + "', got '" + cardStyle.GetStyleName() + "'.");
                }

                VerifyCardArtBinding(row, cardStyle, check);
            }

            if (!styleList.HasStyle(row.StyleName))
            {
                check.issues.Add("Style does not exist in CardStyle_List: " + row.StyleName);
            }

            if (!string.IsNullOrWhiteSpace(row.Get("conditionExpression")) && !check.hasConditionExpression)
            {
                check.issues.Add("Missing UniversityConditionExpression.");
            }

            if (ShouldReturnToNewGame(row) && !check.hasTrueEndingReturn)
            {
                check.issues.Add("Missing UniversityTrueEndingReturnToNewGame.");
            }

            check.textMatches = check.issues.Count == 0;
            if (check.issues.Count > 0)
            {
                report.prefabErrors++;
            }

            report.cards.Add(check);
        }

        report.generatedPrefabs = report.cards.FindAll(card => card.exists).Count;
        VerifyExtraCardArt(table, report);
    }

    static void VerifyCardArtBinding(ProgramCard row, CardStyle cardStyle, CardCheck check)
    {
        if (string.IsNullOrWhiteSpace(check.cardArtExpectedPath))
        {
            return;
        }

        Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(check.cardArtExpectedPath);
        check.cardArtLoaded = expectedSprite != null;
        check.cardArtUsesPrefabOverride = cardStyle.usePrefabIconOverride;
        check.cardArtIconImageFound = cardStyle.iconImage != null;
        if (cardStyle.iconImage != null)
        {
            check.cardArtActualPath = cardStyle.iconImage.sprite != null ? AssetDatabase.GetAssetPath(cardStyle.iconImage.sprite) : "";
            check.cardArtDisplayColor = "#" + ColorUtility.ToHtmlStringRGBA(cardStyle.iconImage.color);
            check.cardArtDisplayColorUntinted = IsUntintedCardArtColor(cardStyle.iconImage.color);
        }

        if (expectedSprite == null)
        {
            check.cardArtIssue = "Missing expected card art.";
            return;
        }

        check.cardArtMatches = cardStyle.usePrefabIconOverride && check.cardArtActualPath == check.cardArtExpectedPath;
        if (!check.cardArtMatches)
        {
            check.cardArtIssue = "Card art sprite mismatch.";
            check.issues.Add(check.cardArtIssue);
        }

        if (!check.cardArtDisplayColorUntinted)
        {
            string message = "Card art display color is not untinted white.";
            check.cardArtIssue = string.IsNullOrEmpty(check.cardArtIssue) ? message : check.cardArtIssue + " " + message;
            check.issues.Add(message);
        }
    }

    static void VerifyExtraCardArt(CsvTable table, ImportReport report)
    {
        HashSet<string> expectedArtPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ProgramCard row in table.rows)
        {
            string expectedArtPath = GetExpectedCardArtPath(row);
            if (!string.IsNullOrWhiteSpace(expectedArtPath))
            {
                expectedArtPaths.Add(expectedArtPath);
            }
        }

        if (!Directory.Exists(CardArtFolderPath))
        {
            return;
        }

        string[] cardArtFiles = Directory.GetFiles(CardArtFolderPath, "card-E*.png", SearchOption.TopDirectoryOnly);
        Array.Sort(cardArtFiles, StringComparer.OrdinalIgnoreCase);
        foreach (string rawPath in cardArtFiles)
        {
            string artPath = rawPath.Replace('\\', '/');
            if (!expectedArtPaths.Contains(artPath))
            {
                report.cardArtExtra++;
                report.extraCardArtPaths.Add(artPath);
            }
        }
    }

    static void CompareText(string expected, string actual, string field, CardCheck check)
    {
        if ((actual ?? "") != (expected ?? ""))
        {
            check.issues.Add(field + " mismatch.");
        }
    }

    static string GetExpectedCardArtPath(ProgramCard row)
    {
        string explicitArtPath = row.Get("artPath").Trim().Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(explicitArtPath))
        {
            return explicitArtPath;
        }

        return IsStandardCardArtEventId(row.EventId) ? CardArtFolderPath + "/card-" + row.EventId + ".png" : "";
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

    static bool IsUntintedCardArtColor(Color color)
    {
        return color.a > 0.99f && color.r > 0.99f && color.g > 0.99f && color.b > 0.99f;
    }

    static void WriteReport(ImportReport report)
    {
        string absolutePath = ToAbsoluteAssetPath(ReportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        File.WriteAllText(absolutePath, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        AssetDatabase.Refresh();
    }

    static void WriteValidationReport(ProgramValidationReport report)
    {
        string absolutePath = ToAbsoluteAssetPath(ValidationReportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        File.WriteAllText(absolutePath, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        AssetDatabase.Refresh();
    }

    static string GetPrefabPath(ProgramCard row)
    {
        return OutputFolderPath + "/" + row.GroupName + "/" + row.CardName + ".prefab";
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

    static int ParseInt(string value, int fallback)
    {
        int parsed;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
    }

    static float ParseFloat(string value, float fallback)
    {
        float parsed;
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
    }

    static bool ParseBool(string value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string normalized = value.Trim().ToLowerInvariant();
        if (normalized == "true" || normalized == "1" || normalized == "yes" || normalized == "y")
        {
            return true;
        }

        if (normalized == "false" || normalized == "0" || normalized == "no" || normalized == "n")
        {
            return false;
        }

        return fallback;
    }

    static string QuoteSemicolonCsv(string value)
    {
        value = value ?? "";
        bool mustQuote = value.IndexOfAny(new[] { ';', '"', '\r', '\n' }) >= 0;
        value = value.Replace("\"", "\"\"");
        return mustQuote ? "\"" + value + "\"" : value;
    }

    [Serializable]
    public class ImportReport
    {
        public bool success;
        public string generatedAt;
        public string sourceCsv;
        public string textCsv;
        public string styleList;
        public string outputFolder;
        public string targetScene;
        public int csvRows;
        public int generatedTextCsvRows;
        public int generatedPrefabs;
        public int configuredPrefabs;
        public int missingPrefabs;
        public int prefabErrors;
        public int mainlineHooks;
        public int conditionExpressionGates;
        public int trueEndingReturnCards;
        public int valueEndingReturnCards;
        public int valueEndingRules;
        public int cardArtExpected;
        public int cardArtBound;
        public int cardArtMissing;
        public int cardArtExtra;
        public int sceneGroups;
        public int sceneCards;
        public string trueEndingStartCard;
        public ProgramValidationReport tableValidation;
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
        public List<string> extraCardArtPaths = new List<string>();
        public List<CardCheck> cards = new List<CardCheck>();
    }

    [Serializable]
    public class ProgramValidationReport
    {
        public bool success;
        public string generatedAt;
        public string sourceCsv;
        public int rowCount;
        public int eventChainCount;
        public int eventChainCards;
        public int eventChainEntryCards;
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
        public List<ChainCheck> chainChecks = new List<ChainCheck>();
    }

    [Serializable]
    public class ChainCheck
    {
        public string chainId;
        public int cardCount;
        public string entryEventId;
        public string orderRange;
        public List<string> issues = new List<string>();
        public List<string> warnings = new List<string>();
    }

    [Serializable]
    public class CardCheck
    {
        public string eventId;
        public string cardName;
        public string groupName;
        public string styleName;
        public string prefabPath;
        public bool exists;
        public bool hasEventScript;
        public bool hasCardStyle;
        public bool hasMainlineHook;
        public bool hasConditionExpression;
        public bool hasTrueEndingReturn;
        public string cardArtExpectedPath;
        public string cardArtActualPath;
        public string cardArtDisplayColor;
        public string cardArtIssue;
        public bool cardArtLoaded;
        public bool cardArtUsesPrefabOverride;
        public bool cardArtIconImageFound;
        public bool cardArtMatches;
        public bool cardArtDisplayColorUntinted;
        public bool textMatches;
        public List<string> issues = new List<string>();
    }

    class ProgramCard
    {
        readonly Dictionary<string, string> values;

        public ProgramCard(Dictionary<string, string> values)
        {
            this.values = values;
        }

        public string EventId { get { return Get("eventId"); } }
        public string CardName { get { return Get("cardName"); } }
        public string GroupName { get { return string.IsNullOrWhiteSpace(Get("groupName")) ? "Main" : Get("groupName"); } }
        public string StyleName { get { return string.IsNullOrWhiteSpace(Get("styleName")) ? "cs_None" : Get("styleName"); } }

        public string Get(string key)
        {
            string value;
            return values.TryGetValue(key, out value) ? value ?? "" : "";
        }
    }

    class CsvTable
    {
        public readonly string[] headers;
        public readonly List<ProgramCard> rows = new List<ProgramCard>();

        CsvTable(string[] headers)
        {
            this.headers = headers;
        }

        public static CsvTable Read(string csv, char delimiter)
        {
            List<string[]> rawRows = ParseRows(csv, delimiter);
            CsvTable table = new CsvTable(rawRows.Count > 0 ? rawRows[0] : new string[0]);
            for (int i = 1; i < rawRows.Count; i++)
            {
                if (rawRows[i].All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                Dictionary<string, string> row = new Dictionary<string, string>();
                for (int h = 0; h < table.headers.Length; h++)
                {
                    row[table.headers[h]] = h < rawRows[i].Length ? rawRows[i][h] : "";
                }

                table.rows.Add(new ProgramCard(row));
            }

            return table;
        }

        public string ValidateRequiredColumns(string[] required)
        {
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

        static List<string[]> ParseRows(string text, char delimiter)
        {
            List<string[]> rows = new List<string[]>();
            List<string> row = new List<string>();
            StringBuilder cell = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                char next = i + 1 < text.Length ? text[i + 1] : '\0';

                if (ch == '"')
                {
                    if (inQuotes && next == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == delimiter && !inQuotes)
                {
                    row.Add(cell.ToString());
                    cell.Length = 0;
                }
                else if ((ch == '\n' || ch == '\r') && !inQuotes)
                {
                    if (ch == '\r' && next == '\n')
                    {
                        i++;
                    }

                    row.Add(cell.ToString());
                    rows.Add(row.ToArray());
                    row.Clear();
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
    }
}
