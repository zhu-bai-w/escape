using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class UniversitySimulatorRegressionTests
{
    const string ProgramCsvPath = "Assets/UniversitySimulator/Data/cards_v1_program.csv";
    const string CardsFolderPath = "Assets/UniversitySimulator/Art/cards";
    const string PrefabRootPath = "Assets/UniversitySimulator/Prefabs/Cards";

    [SetUp]
    public void SetUp()
    {
        CardStack.instance = null;
        GameStateManager.instance = null;
        valueManager.instance = null;
        UniversityTrueEndingProgress.ResetForDebug(UniversityTrueEndingProgress.DefaultRequiredFlags);
    }

    [TearDown]
    public void TearDown()
    {
        CardStack.instance = null;
        GameStateManager.instance = null;
        valueManager.instance = null;
        UniversityTrueEndingProgress.ResetForDebug(UniversityTrueEndingProgress.DefaultRequiredFlags);
    }

    [Test]
    public void FourDirectionUpAndDownInvokeTheirOwnEvents()
    {
        GameObject stackObject = new GameObject("CardStack Test Host");
        GameObject cardObject = new GameObject("Four Direction Card");
        try
        {
            CardStack stack = stackObject.AddComponent<CardStack>();
            CardStack.instance = stack;
            stack.followUpStack = new CardStack.C_WrapFollowUpStack();

            EventScript eventScript = cardObject.AddComponent<EventScript>();
            eventScript.swipeType = EventScript.E_SwipeType.FourDirection;
            eventScript.Results = new EventScript.resultGroup
            {
                resultLeft = EmptyResult(),
                resultRight = EmptyResult(),
                resultUp = EmptyResult(),
                resultDown = EmptyResult(),
                additional_choice_0 = EmptyResult(),
                additional_choice_1 = EmptyResult()
            };
            eventScript.changeExtrasOnCardDespawn = new EventScript.C_AdditionalModifiers[0];
            eventScript.changeValueOnCardDespawn = new EventScript.resultModifier[0];
            eventScript.OnCardDespawn = new EventScript.mEvent();
            eventScript.OnSwipeRight = new EventScript.mEvent();
            eventScript.OnSwipeUp = new EventScript.mEvent();
            eventScript.OnSwipeDown = new EventScript.mEvent();

            int rightCount = 0;
            int upCount = 0;
            int downCount = 0;
            eventScript.OnSwipeRight.AddListener(() => rightCount++);
            eventScript.OnSwipeUp.AddListener(() => upCount++);
            eventScript.OnSwipeDown.AddListener(() => downCount++);

            eventScript.onUpSwipe();
            eventScript.onDownSwipe();

            Assert.AreEqual(0, rightCount);
            Assert.AreEqual(1, upCount);
            Assert.AreEqual(1, downCount);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cardObject);
            UnityEngine.Object.DestroyImmediate(stackObject);
        }
    }

    [Test]
    public void ProgramCsvContainsConfiguredTrueEndingStartCard()
    {
        Dictionary<string, string> row = FindCsvRow("TE001");

        Assert.AreEqual("US_TE001", row["cardName"]);
        Assert.AreEqual("TrueEnding", row["groupName"]);
        Assert.AreEqual("路在何方", row["titleText"]);
        Assert.AreEqual("原来，人生是旷野，而非孤岛", row["questionText"]);
        Assert.AreEqual("找到自己的星星", row["answerLeft"]);
        Assert.AreEqual("找到自己的星星", row["answerRight"]);
        Assert.AreEqual("false", row["isDrawable"]);
        Assert.AreEqual("TRUE_ENDING", row["chainId"]);
        Assert.AreEqual("1", row["chainOrder"]);
        Assert.AreEqual("true", row["isEndingChain"]);
    }

    [Test]
    public void TrueEndingControllerQueuesStartCardWhenAllFlagsAreSet()
    {
        GameObject stackObject = new GameObject("CardStack Test Host");
        GameObject managerObject = new GameObject("GameStateManager Test Host");
        GameObject trueEndingCard = new GameObject("US_TE001");
        UniversityTrueEndingController controller = null;
        try
        {
            UniversityTrueEndingProgress.ResetForDebug(UniversityTrueEndingProgress.DefaultRequiredFlags);
            UniversityTrueEndingProgress.CompleteDefaultFlagsForDebug();

            CardStack stack = stackObject.AddComponent<CardStack>();
            CardStack.instance = stack;
            stack.onCardSwipe = new CardStack.mEvent();
            stack.followUpStack = new CardStack.C_WrapFollowUpStack();

            GameStateManager manager = managerObject.AddComponent<GameStateManager>();
            GameStateManager.instance = manager;
            manager.OnNewGame = new GameStateManager.mEvent();
            manager.OnGameOver = new GameStateManager.mEvent();
            manager.gamestate = GameStateManager.Gamestate.gameActive;

            controller = stackObject.AddComponent<UniversityTrueEndingController>();
            controller.trueEndingStartCard = trueEndingCard;
            controller.requiredLifetimeDays = 0;
            controller.requiredGameOvers = 0;
            controller.requiredPermanentFlags = UniversityTrueEndingProgress.DefaultRequiredFlags;

            typeof(UniversityTrueEndingController)
                .GetMethod("Subscribe", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);

            typeof(UniversityTrueEndingController)
                .GetMethod("EvaluateBeforeNextCard", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);

            Assert.AreSame(trueEndingCard, stack.followUpCard);
            Assert.IsTrue(UniversityTrueEndingProgress.HasTriggeredTrueEnding);
        }
        finally
        {
            UniversityTrueEndingProgress.ResetForDebug(UniversityTrueEndingProgress.DefaultRequiredFlags);
            if (controller != null)
            {
                UnityEngine.Object.DestroyImmediate(controller);
            }
            UnityEngine.Object.DestroyImmediate(trueEndingCard);
            UnityEngine.Object.DestroyImmediate(managerObject);
            UnityEngine.Object.DestroyImmediate(stackObject);
        }
    }

    [Test]
    public void NextCardSpawnsInitialCardWhenNoCardIsLoaded()
    {
        GameObject stackObject = new GameObject("CardStack Test Host");
        GameObject parentObject = new GameObject("Card Parent");
        GameObject valueManagerObject = new GameObject("ValueManager Test Host");
        GameObject cardPrefab = new GameObject("Initial Test Card");
        CardStack stack = null;
        try
        {
            valueManager manager = valueManagerObject.AddComponent<valueManager>();
            valueManager.instance = manager;
            manager.values = new List<ValueScript>();

            EventScript eventScript = cardPrefab.AddComponent<EventScript>();
            eventScript.conditions = new EventScript.condition[0];
            eventScript.changeExtrasOnCardDespawn = new EventScript.C_AdditionalModifiers[0];
            eventScript.changeValueOnCardDespawn = new EventScript.resultModifier[0];
            eventScript.OnCardSpawn = new EventScript.mEvent();
            eventScript.OnCardDespawn = new EventScript.mEvent();

            stack = stackObject.AddComponent<CardStack>();
            typeof(CardStack)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(stack, null);

            stack.CardParent = parentObject.transform;
            stack.availableCards = new List<GameObject>();
            stack.highPriorityCards = new List<GameObject>();
            stack.followUpStack = new CardStack.C_WrapFollowUpStack();
            stack.allCards = new[]
            {
                new CardStack.cardCategory
                {
                    groupName = "Test",
                    subStackCondition = new EventScript.condition[0],
                    groupCards = new[] { cardPrefab }
                }
            };
            stack.cardDrawCount = new CardStack.drawCnts
            {
                cnt = new[] { new CardStack.cardCount { drawCnt = new int[1] } }
            };
            stack.cardBlockCount = new CardStack.blockCount
            {
                cnt = new[] { new CardStack.cardCount { drawCnt = new int[1] } }
            };
            stack.fallBackCard = cardPrefab;

            stack.nextCard();

            Assert.IsNotNull(stack.spawnedCard);
            Assert.AreSame(parentObject.transform, stack.spawnedCard.transform.parent);
            Assert.AreEqual(1, stack.cardDrawCount.cnt[0].drawCnt[0]);
        }
        finally
        {
            PlayerPrefs.DeleteKey("drawCnt");
            PlayerPrefs.DeleteKey("blockCnt");
            PlayerPrefs.DeleteKey("Cind");
            if (stack != null && stack.spawnedCard != null)
            {
                UnityEngine.Object.DestroyImmediate(stack.spawnedCard);
            }
            UnityEngine.Object.DestroyImmediate(cardPrefab);
            UnityEngine.Object.DestroyImmediate(valueManagerObject);
            UnityEngine.Object.DestroyImmediate(parentObject);
            UnityEngine.Object.DestroyImmediate(stackObject);
        }
    }

    [Test]
    public void ReturnToNewGameDoesNotIncrementGameOverCount()
    {
        GameObject managerObject = new GameObject("GameStateManager Test Host");
        GameObject returnObject = new GameObject("True Ending Return Test Host");
        try
        {
            UniversityTrueEndingProgress.ResetForDebug(UniversityTrueEndingProgress.DefaultRequiredFlags);
            UniversityTrueEndingProgress.AddGameOver(1);
            int before = UniversityTrueEndingProgress.GameOverCount;

            GameStateManager manager = managerObject.AddComponent<GameStateManager>();
            GameStateManager.instance = manager;
            CardStack.instance = null;
            manager.gamestate = GameStateManager.Gamestate.gameActive;

            UniversityTrueEndingReturnToNewGame returnToNewGame = returnObject.AddComponent<UniversityTrueEndingReturnToNewGame>();
            returnToNewGame.reloadScene = false;
            returnToNewGame.ReturnToNewGame();

            Assert.AreEqual(before, UniversityTrueEndingProgress.GameOverCount);
            Assert.AreEqual(GameStateManager.Gamestate.idle, manager.gamestate);
        }
        finally
        {
            UniversityTrueEndingProgress.ResetForDebug(UniversityTrueEndingProgress.DefaultRequiredFlags);
            UnityEngine.Object.DestroyImmediate(returnObject);
            UnityEngine.Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void ExistingProgramCardArtIsBoundToMainPrefabs()
    {
        foreach (Dictionary<string, string> row in ReadCsvRows())
        {
            string eventId = row["eventId"];
            if (!IsStandardCardArtEventId(eventId))
            {
                continue;
            }

            string expectedArtPath = CardsFolderPath + "/card-" + eventId + ".png";
            if (!File.Exists(ToAbsoluteAssetPath(expectedArtPath)))
            {
                continue;
            }

            Sprite expectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(expectedArtPath);
            Assert.IsNotNull(expectedSprite, "Expected art is not importable as a sprite: " + expectedArtPath);

            string prefabPath = PrefabRootPath + "/" + row["groupName"] + "/" + row["cardName"] + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, "Missing prefab for card art binding: " + prefabPath);

            CardStyle cardStyle = prefab.GetComponent<CardStyle>();
            Assert.IsNotNull(cardStyle, "Missing CardStyle on prefab: " + prefabPath);
            Assert.IsTrue(cardStyle.usePrefabIconOverride, "Card art override is disabled: " + eventId);
            Assert.IsNotNull(cardStyle.iconImage, "Missing icon image on prefab: " + eventId);
            Assert.IsNotNull(cardStyle.iconImage.sprite, "Missing bound icon sprite: " + eventId);
            Assert.AreEqual(expectedArtPath, AssetDatabase.GetAssetPath(cardStyle.iconImage.sprite), "Card art sprite mismatch: " + eventId);
            Assert.IsTrue(IsUntintedIconColor(cardStyle.iconImage.color), "Card art icon tint is not removed: " + eventId);
        }
    }

    [Test]
    public void ProgramCardArtCoverageOnlyHasKnownOpenItems()
    {
        HashSet<string> expectedIds = new HashSet<string>();
        foreach (Dictionary<string, string> row in ReadCsvRows())
        {
            if (IsStandardCardArtEventId(row["eventId"]))
            {
                expectedIds.Add(row["eventId"]);
            }
        }

        HashSet<string> artIds = new HashSet<string>();
        foreach (string path in Directory.GetFiles(CardsFolderPath, "card-E*.png", SearchOption.TopDirectoryOnly))
        {
            artIds.Add(Path.GetFileNameWithoutExtension(path).Substring("card-".Length));
        }

        HashSet<string> missing = new HashSet<string>(expectedIds);
        missing.ExceptWith(artIds);
        HashSet<string> extra = new HashSet<string>(artIds);
        extra.ExceptWith(expectedIds);

        CollectionAssert.IsSubsetOf(missing, new[] { "E008", "E012" }, "Unexpected missing card art IDs.");
        CollectionAssert.IsSubsetOf(extra, new[] { "E036", "E053" }, "Unexpected extra card art IDs.");
    }

    [Test]
    public void UniversityCardPrefabsDoNotContainCollapsedYamlModificationEntries()
    {
        foreach (string path in Directory.GetFiles(PrefabRootPath, "*.prefab", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(path);
            Assert.IsFalse(text.Contains("objectReference: {fileID: 0}    - target"), "Collapsed YAML modification entry found in " + path);
        }
    }

    static EventScript.result EmptyResult()
    {
        return new EventScript.result
        {
            resultType = EventScript.resultTypes.simple,
            modifiers = EmptyModifierGroup(),
            conditions = new EventScript.condition[0],
            modifiersTrue = EmptyModifierGroup(),
            modifiersFalse = EmptyModifierGroup(),
            randomModifiers = new EventScript.modifierGroup[0]
        };
    }

    static EventScript.modifierGroup EmptyModifierGroup()
    {
        return new EventScript.modifierGroup
        {
            valueChanges = new EventScript.resultModifier[0],
            extras = new EventScript.C_AdditionalModifiers[0],
            followUpCard = null,
            followUpDelay = new EventScript.C_intRange { min = 0, max = 0 }
        };
    }

    static Dictionary<string, string> FindCsvRow(string eventId)
    {
        foreach (Dictionary<string, string> row in ReadCsvRows())
        {
            if (row["eventId"] == eventId)
            {
                return row;
            }
        }

        Assert.Fail("CSV row not found: " + eventId);
        return null;
    }

    static List<Dictionary<string, string>> ReadCsvRows()
    {
        string[] lines = File.ReadAllLines(ProgramCsvPath);
        string[] headers = SplitCsvLine(lines[0]);
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = SplitCsvLine(lines[i]);
            if (values.Length == 0 || string.IsNullOrWhiteSpace(values[0]))
            {
                continue;
            }

            Dictionary<string, string> row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++)
            {
                row[headers[j]] = j < values.Length ? values[j] : "";
            }

            rows.Add(row);
        }

        return rows;
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

    static bool IsUntintedIconColor(Color color)
    {
        return color.a > 0.99f && color.r > 0.99f && color.g > 0.99f && color.b > 0.99f;
    }

    static string ToAbsoluteAssetPath(string assetPath)
    {
        string relative = assetPath.StartsWith("Assets/") ? assetPath.Substring("Assets/".Length) : assetPath;
        return Path.Combine(Application.dataPath, relative);
    }

    static string[] SplitCsvLine(string line)
    {
        List<string> values = new List<string>();
        bool inQuotes = false;
        string current = "";
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        values.Add(current);
        return values.ToArray();
    }
}
