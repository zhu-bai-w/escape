using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class UniversitySimulatorRegressionTests
{
    const string ProgramCsvPath = "Assets/UniversitySimulator/Data/cards_v1_program.csv";
    const string CardsFolderPath = "Assets/UniversitySimulator/Art/cards";
    const string PrefabRootPath = "Assets/UniversitySimulator/Prefabs/Cards";
    const string GameScenePath = "Assets/UniversitySimulator/Scenes/Game.unity";
    const string ValueResetTestKeyPrefix = "US.Tests.NewGameValue.";

    [SetUp]
    public void SetUp()
    {
        CardStack.instance = null;
        GameStateManager.instance = null;
        valueManager.instance = null;
        UniversityTrueEndingProgress.ResetForDebug(UniversityTrueEndingProgress.DefaultRequiredFlags);
        UniversityAchievementSystem.ResetAllForDebug();
    }

    [TearDown]
    public void TearDown()
    {
        CardStack.instance = null;
        GameStateManager.instance = null;
        valueManager.instance = null;
        UniversityTrueEndingProgress.ResetForDebug(UniversityTrueEndingProgress.DefaultRequiredFlags);
        UniversityAchievementSystem.ResetAllForDebug();

        if (UniversityAchievementSystem.instance != null)
        {
            Object.DestroyImmediate(UniversityAchievementSystem.instance.gameObject);
        }
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
    public void ExternalFollowUpCardIsConsumedOnceBeforeReturningToDrawableCards()
    {
        GameObject stackObject = new GameObject("CardStack Test Host");
        GameObject parentObject = new GameObject("Card Parent");
        GameObject valueManagerObject = new GameObject("ValueManager Test Host");
        GameObject standardPrefab = new GameObject("Standard Test Card");
        GameObject followUpPrefab = new GameObject("Follow Up Test Card");
        CardStack stack = null;
        try
        {
            valueManager manager = valueManagerObject.AddComponent<valueManager>();
            valueManager.instance = manager;
            manager.values = new List<ValueScript>();

            ConfigureStackTestCard(standardPrefab, true);
            ConfigureStackTestCard(followUpPrefab, false);

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
                    groupCards = new[] { standardPrefab }
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
            stack.fallBackCard = standardPrefab;
            stack.followUpCard = followUpPrefab;

            typeof(CardStack)
                .GetMethod("newCard", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(stack, null);

            Assert.IsNotNull(stack.spawnedCard);
            Assert.IsTrue(stack.spawnedCard.name.StartsWith(followUpPrefab.name));
            Assert.IsNull(stack.followUpCard);
            Assert.AreEqual(0, stack.cardDrawCount.cnt[0].drawCnt[0]);

            typeof(CardStack)
                .GetMethod("newCard", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(stack, null);

            Assert.IsNotNull(stack.spawnedCard);
            Assert.IsTrue(stack.spawnedCard.name.StartsWith(standardPrefab.name));
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
            UnityEngine.Object.DestroyImmediate(followUpPrefab);
            UnityEngine.Object.DestroyImmediate(standardPrefab);
            UnityEngine.Object.DestroyImmediate(valueManagerObject);
            UnityEngine.Object.DestroyImmediate(parentObject);
            UnityEngine.Object.DestroyImmediate(stackObject);
        }
    }

    [Test]
    public void StartMenuSceneSkipsQuestCardAndReturnsToDrawableCards()
    {
        string previousScenePath = EditorSceneManager.GetActiveScene().path;
        try
        {
            ClearRuntimeCardStackPrefs();
            EditorSceneManager.OpenScene(GameScenePath);

            CardStack stack = Object.FindObjectOfType<CardStack>();
            valueManager.instance = Object.FindObjectOfType<valueManager>();
            GameStateManager gameStateManager = Object.FindObjectOfType<GameStateManager>();
            Quests quests = Object.FindObjectOfType<Quests>();
            Assert.IsNotNull(stack, "Game scene is missing CardStack.");
            Assert.IsNotNull(valueManager.instance, "Game scene is missing valueManager.");
            Assert.IsNotNull(gameStateManager, "Game scene is missing GameStateManager.");
            Assert.IsNotNull(quests, "Game scene is missing the retained Quests component.");
            Assert.IsFalse(quests.featureEnabled, "Quest feature should be isolated in the current scene.");
            AssertSceneNewGameDoesNotAutoFillQuests(gameStateManager);

            typeof(CardStack)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(stack, null);
            ConfigureSceneStackForEditMode(stack);

            Assert.IsNotNull(stack.spawnedCard);
            Assert.IsTrue(stack.spawnedCard.name.StartsWith("MainMenuCard"));

            EventScript mainMenuEvent = stack.spawnedCard.GetComponent<EventScript>();
            mainMenuEvent.onLeftSwipe();
            Assert.IsNull(stack.followUpCard, "MainMenuCard should not queue the deferred quest/start placeholder card.");

            InvokeNewCard(stack);
            Assert.IsNotNull(stack.spawnedCard);
            Assert.IsFalse(stack.spawnedCard.name.StartsWith("_StartCard"));
            Assert.IsFalse(stack.spawnedCard.name.StartsWith("MainMenuCard"));
            Assert.IsTrue(stack.getCardMoveEnabled());
        }
        finally
        {
            ClearRuntimeCardStackPrefs();
            if (!string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath);
            }
        }
    }

    static void AssertSceneNewGameDoesNotAutoFillQuests(GameStateManager gameStateManager)
    {
        Assert.IsNotNull(gameStateManager.OnNewGame);
        int count = gameStateManager.OnNewGame.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            Assert.AreNotEqual("FillActiveQuests", gameStateManager.OnNewGame.GetPersistentMethodName(i));
        }
    }

    [Test]
    public void ReturnToNewGameClearsCurrentRunDaysWithoutIncrementingGameOverCount()
    {
        GameObject managerObject = new GameObject("GameStateManager Test Host");
        GameObject returnObject = new GameObject("True Ending Return Test Host");
        try
        {
            UniversityTrueEndingProgress.ResetForDebug(UniversityTrueEndingProgress.DefaultRequiredFlags);
            UniversityTrueEndingProgress.AddGameOver(1);
            UniversityTrueEndingProgress.AddCurrentRunDays(7);
            int before = UniversityTrueEndingProgress.GameOverCount;

            GameStateManager manager = managerObject.AddComponent<GameStateManager>();
            GameStateManager.instance = manager;
            CardStack.instance = null;
            manager.gamestate = GameStateManager.Gamestate.gameActive;

            UniversityTrueEndingReturnToNewGame returnToNewGame = returnObject.AddComponent<UniversityTrueEndingReturnToNewGame>();
            returnToNewGame.reloadScene = false;
            returnToNewGame.ReturnToNewGame();

            Assert.AreEqual(before, UniversityTrueEndingProgress.GameOverCount);
            Assert.AreEqual(0, UniversityTrueEndingProgress.CurrentRunDays);
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
    public void AchievementSystemUnlocksSpecificChoiceByCardName()
    {
        GameObject cardObject = new GameObject("US_E001(Clone)");
        try
        {
            UniversityAchievementSystem.RecordChoice(cardObject, "left");
            Assert.IsTrue(UniversityAchievementSystem.IsUnlocked("ACH_CHOICE_E001_L"), "Left choice achievement should unlock.");
            Assert.IsFalse(UniversityAchievementSystem.IsUnlocked("ACH_CHOICE_E001_R"), "Right choice achievement should stay locked.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cardObject);
        }
    }

    [Test]
    public void AchievementSystemUnlocksValueEndingByEventId()
    {
        UniversityAchievementSystem.RecordValueEnding("END07", valueDefinitions.values.economy, true, 12f);

        Assert.IsTrue(UniversityAchievementSystem.IsUnlocked("ACH_END_ECON_HIGH"));
        Assert.IsFalse(UniversityAchievementSystem.IsUnlocked("ACH_END_ECON_LOW"));
    }

    [Test]
    public void AchievementPanelUsesLargeReadableRows()
    {
        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        try
        {
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(3000f, 1240f);

            UniversityAchievementSystem.EnsureInstance();
            UniversityAchievementSystem achievementSystem = UniversityAchievementSystem.instance;
            System.Type type = typeof(UniversityAchievementSystem);
            const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

            type.GetMethod("ConfigureContent", instanceFlags)
                .Invoke(achievementSystem, new object[] { contentObject.transform });
            type.GetMethod("CreateAchievementRow", instanceFlags)
                .Invoke(achievementSystem, new object[] { contentObject.transform, UniversityAchievementSystem.AllAchievements[0], Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") });

            Assert.AreEqual(1, contentObject.transform.childCount);

            VerticalLayoutGroup listLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(listLayout);
            Assert.IsTrue(listLayout.childControlHeight);
            Assert.AreEqual(24f, listLayout.spacing);
            Assert.AreEqual(32, listLayout.padding.top);

            Transform row = contentObject.transform.GetChild(0);
            Assert.IsNull(row.GetComponent<HorizontalLayoutGroup>());
            Assert.AreEqual(300f, row.GetComponent<LayoutElement>().preferredHeight);
            Assert.AreEqual(300f, row.GetComponent<RectTransform>().sizeDelta.y);

            RectTransform iconRect = row.Find("Icon").GetComponent<RectTransform>();
            Assert.AreEqual(188f, iconRect.sizeDelta.x);
            Assert.AreEqual(188f, iconRect.sizeDelta.y);
            Assert.AreEqual(0f, iconRect.anchorMin.x);
            Assert.AreEqual(0.5f, iconRect.anchorMin.y);
            Assert.AreEqual(0f, iconRect.anchorMax.x);
            Assert.AreEqual(0.5f, iconRect.anchorMax.y);

            RectTransform textsRect = row.Find("Texts").GetComponent<RectTransform>();
            Assert.AreEqual(0f, textsRect.anchorMin.x);
            Assert.AreEqual(0f, textsRect.anchorMin.y);
            Assert.AreEqual(1f, textsRect.anchorMax.x);
            Assert.AreEqual(1f, textsRect.anchorMax.y);
            Assert.AreEqual(256f, textsRect.offsetMin.x);
            Assert.AreEqual(28f, textsRect.offsetMin.y);
            Assert.AreEqual(-36f, textsRect.offsetMax.x);
            Assert.AreEqual(-28f, textsRect.offsetMax.y);

            Transform texts = row.Find("Texts");
            Text title = texts.Find("Title").GetComponent<Text>();
            Text description = texts.Find("Description").GetComponent<Text>();
            Text status = texts.Find("Status").GetComponent<Text>();

            Assert.AreEqual(52, title.fontSize);
            Assert.AreEqual(TextAnchor.UpperLeft, title.alignment);
            Assert.AreEqual(VerticalWrapMode.Truncate, title.verticalOverflow);
            Assert.AreEqual(36, description.fontSize);
            Assert.AreEqual(TextAnchor.UpperLeft, description.alignment);
            Assert.AreEqual(VerticalWrapMode.Truncate, description.verticalOverflow);
            Assert.AreEqual(30, status.fontSize);
            Assert.AreEqual(TextAnchor.LowerLeft, status.alignment);

            RectTransform titleRect = title.rectTransform;
            RectTransform descriptionRect = description.rectTransform;
            RectTransform statusRect = status.rectTransform;
            Assert.AreEqual(72f, titleRect.sizeDelta.y);
            Assert.AreEqual(0f, titleRect.anchoredPosition.y);
            Assert.AreEqual(108f, descriptionRect.sizeDelta.y);
            Assert.AreEqual(-84f, descriptionRect.anchoredPosition.y);
            Assert.AreEqual(44f, statusRect.sizeDelta.y);
            Assert.AreEqual(0f, statusRect.anchoredPosition.y);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(contentObject);
        }

        float visibleRows = 1240f / (300f + 24f);
        Assert.Greater(visibleRows, 3f);
        Assert.Less(visibleRows, 4f);
    }

    [Test]
    public void SettingsMenuFindsInactiveAchievementsButton()
    {
        GameObject hostObject = new GameObject("Settings Controller Host");
        GameObject menuPanel = new GameObject("MenuPanel");
        GameObject buttonObject = new GameObject("AchievementsButton");
        try
        {
            buttonObject.transform.SetParent(menuPanel.transform);
            Button button = buttonObject.AddComponent<Button>();
            menuPanel.SetActive(false);

            UniversitySettingsMenuController controller = hostObject.AddComponent<UniversitySettingsMenuController>();
            controller.menuPanel = menuPanel;

            Button foundButton = (Button)typeof(UniversitySettingsMenuController)
                .GetMethod("FindButtonInMenu", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, new object[] { "AchievementsButton" });

            Assert.AreSame(button, foundButton);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(buttonObject);
            UnityEngine.Object.DestroyImmediate(menuPanel);
            UnityEngine.Object.DestroyImmediate(hostObject);
        }
    }

    [Test]
    public void SettingsMenuCreatesReturnNavigationOnAchievementsPanel()
    {
        GameObject hostObject = new GameObject("Settings Controller Host");
        GameObject menuPanel = new GameObject("MenuPanel", typeof(RectTransform));
        GameObject settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform));
        GameObject achievementsPanel = new GameObject("AchievementsPanel", typeof(RectTransform));
        GameObject questsPanel = new GameObject("QuestsPanel", typeof(RectTransform));
        try
        {
            settingsPanel.transform.SetParent(menuPanel.transform, false);
            achievementsPanel.transform.SetParent(menuPanel.transform, false);
            questsPanel.transform.SetParent(menuPanel.transform, false);
            menuPanel.SetActive(false);
            settingsPanel.SetActive(false);
            achievementsPanel.SetActive(false);
            questsPanel.SetActive(false);

            UniversitySettingsMenuController controller = hostObject.AddComponent<UniversitySettingsMenuController>();
            controller.menuPanel = menuPanel;
            controller.settingsPanel = settingsPanel;
            controller.achievementsPanel = achievementsPanel;
            controller.questsPanel = questsPanel;

            controller.OpenAchievementsPanel();

            Transform navigation = menuPanel.transform.Find("BottomNavigationBar");
            Assert.IsNotNull(navigation, "The achievements panel should include the menu navigation bar.");
            Assert.AreEqual(menuPanel.transform.childCount - 1, navigation.GetSiblingIndex(), "Navigation should render above the active panel.");
            Assert.IsTrue(menuPanel.activeSelf);
            Assert.IsFalse(settingsPanel.activeSelf);
            Assert.IsTrue(achievementsPanel.activeSelf);
            Assert.IsFalse(questsPanel.activeSelf);

            Button settingsButton = navigation.Find("SettingsNavButton").GetComponent<Button>();
            Button achievementsButton = navigation.Find("AchievementsNavButton").GetComponent<Button>();
            Button returnGameButton = navigation.Find("ReturnGameNavButton").GetComponent<Button>();
            Assert.IsTrue(settingsButton.gameObject.activeSelf);
            Assert.IsFalse(achievementsButton.interactable);
            Assert.IsTrue(returnGameButton.gameObject.activeSelf);
            Assert.IsNull(navigation.Find("QuestsNavButton"));

            settingsButton.onClick.Invoke();

            Assert.IsTrue(settingsPanel.activeSelf);
            Assert.IsFalse(achievementsPanel.activeSelf);
            Assert.IsFalse(settingsButton.interactable);
            Assert.IsTrue(achievementsButton.interactable);

            returnGameButton.onClick.Invoke();

            Assert.IsFalse(menuPanel.activeSelf);
            Assert.IsFalse(settingsPanel.activeSelf);
            Assert.IsFalse(achievementsPanel.activeSelf);
            Assert.IsFalse(questsPanel.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(questsPanel);
            UnityEngine.Object.DestroyImmediate(achievementsPanel);
            UnityEngine.Object.DestroyImmediate(settingsPanel);
            UnityEngine.Object.DestroyImmediate(menuPanel);
            UnityEngine.Object.DestroyImmediate(hostObject);
        }
    }

    [Test]
    public void ValueScriptRecordsLimitDeviationBeforeClamping()
    {
        GameObject valueObject = new GameObject("Value Limit Test Host");
        try
        {
            ValueScript valueScript = valueObject.AddComponent<ValueScript>();
            valueScript.valueType = valueDefinitions.values.bodyMind;
            valueScript.limits = new ValueScript.valueLimits
            {
                min = 0f,
                max = 100f,
                randomMin = 0f,
                randomMax = 100f,
                roundToWholeNumbers = true
            };
            valueScript.events = BuildValueEvents();
            valueScript.value = 107f;

            valueScript.limitValue();

            float deviation;
            Assert.AreEqual(100f, valueScript.value);
            Assert.IsTrue(valueScript.TryGetLastLimitDeviation(Time.frameCount, true, out deviation));
            Assert.AreEqual(7f, deviation);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(valueObject);
        }
    }

    [Test]
    public void NewGameStartForcesUniversityCoreValuesToFifty()
    {
        GameObject valueManagerObject = new GameObject("ValueManager Reset Test Host");
        List<GameObject> valueObjects = new List<GameObject>();
        try
        {
            valueManager manager = valueManagerObject.AddComponent<valueManager>();
            valueManager.instance = manager;
            manager.values = new List<ValueScript>
            {
                CreateNewGameResetValue(valueObjects, valueDefinitions.values.bodyMind, 12f, 80f, 80f),
                CreateNewGameResetValue(valueObjects, valueDefinitions.values.academics, 25f, 80f, 80f),
                CreateNewGameResetValue(valueObjects, valueDefinitions.values.relationships, 70f, 80f, 80f),
                CreateNewGameResetValue(valueObjects, valueDefinitions.values.economy, 99f, 80f, 80f),
                CreateNewGameResetValue(valueObjects, valueDefinitions.values.health, 99f, 12f, 12f)
            };

            manager.setRandomValues();

            Assert.AreEqual(50f, manager.getFirstFittingValue(valueDefinitions.values.bodyMind).value);
            Assert.AreEqual(50f, manager.getFirstFittingValue(valueDefinitions.values.academics).value);
            Assert.AreEqual(50f, manager.getFirstFittingValue(valueDefinitions.values.relationships).value);
            Assert.AreEqual(50f, manager.getFirstFittingValue(valueDefinitions.values.economy).value);
            Assert.AreEqual(12f, manager.getFirstFittingValue(valueDefinitions.values.health).value);
        }
        finally
        {
            DeleteSecurePrefsKey(ValueResetTestKeyPrefix + valueDefinitions.values.bodyMind);
            DeleteSecurePrefsKey(ValueResetTestKeyPrefix + valueDefinitions.values.academics);
            DeleteSecurePrefsKey(ValueResetTestKeyPrefix + valueDefinitions.values.relationships);
            DeleteSecurePrefsKey(ValueResetTestKeyPrefix + valueDefinitions.values.economy);
            DeleteSecurePrefsKey(ValueResetTestKeyPrefix + valueDefinitions.values.health);

            for (int i = 0; i < valueObjects.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(valueObjects[i]);
            }

            UnityEngine.Object.DestroyImmediate(valueManagerObject);
        }
    }

    [Test]
    public void ValueEndingControllerChoosesLargestBoundaryDeviation()
    {
        GameObject stackObject = new GameObject("CardStack Test Host");
        GameObject managerObject = new GameObject("GameStateManager Test Host");
        GameObject valueManagerObject = new GameObject("ValueManager Test Host");
        GameObject bodyMindObject = new GameObject("BodyMind Value");
        GameObject economyObject = new GameObject("Economy Value");
        GameObject healthHighEnding = new GameObject("US_END01");
        GameObject economyLowEnding = new GameObject("US_END08");
        try
        {
            CardStack stack = stackObject.AddComponent<CardStack>();
            CardStack.instance = stack;
            stack.onCardSwipe = new CardStack.mEvent();
            stack.followUpStack = new CardStack.C_WrapFollowUpStack();

            GameStateManager manager = managerObject.AddComponent<GameStateManager>();
            GameStateManager.instance = manager;
            manager.OnNewGame = new GameStateManager.mEvent();
            manager.OnGameOver = new GameStateManager.mEvent();
            manager.gamestate = GameStateManager.Gamestate.gameActive;

            ValueScript bodyMind = bodyMindObject.AddComponent<ValueScript>();
            bodyMind.valueType = valueDefinitions.values.bodyMind;
            bodyMind.lastLimitFrame = Time.frameCount;
            bodyMind.lastLimitWasMax = true;
            bodyMind.lastLimitDeviation = 7f;

            ValueScript economy = economyObject.AddComponent<ValueScript>();
            economy.valueType = valueDefinitions.values.economy;
            economy.lastLimitFrame = Time.frameCount;
            economy.lastLimitWasMax = false;
            economy.lastLimitDeviation = 15f;

            valueManager valueManagerComponent = valueManagerObject.AddComponent<valueManager>();
            valueManager.instance = valueManagerComponent;
            valueManagerComponent.values = new List<ValueScript> { bodyMind, economy };

            UniversityValueEndingController controller = stackObject.AddComponent<UniversityValueEndingController>();
            controller.rules = new[]
            {
                new UniversityValueEndingController.ValueEndingRule
                {
                    eventId = "END01",
                    valueType = valueDefinitions.values.bodyMind,
                    triggerOnMaximum = true,
                    endingCard = healthHighEnding
                },
                new UniversityValueEndingController.ValueEndingRule
                {
                    eventId = "END08",
                    valueType = valueDefinitions.values.economy,
                    triggerOnMaximum = false,
                    endingCard = economyLowEnding
                }
            };

            typeof(UniversityValueEndingController)
                .GetMethod("Subscribe", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(controller, null);

            stack.onCardSwipe.Invoke();

            Assert.AreSame(economyLowEnding, stack.followUpCard);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(economyLowEnding);
            UnityEngine.Object.DestroyImmediate(healthHighEnding);
            UnityEngine.Object.DestroyImmediate(economyObject);
            UnityEngine.Object.DestroyImmediate(bodyMindObject);
            UnityEngine.Object.DestroyImmediate(valueManagerObject);
            UnityEngine.Object.DestroyImmediate(managerObject);
            UnityEngine.Object.DestroyImmediate(stackObject);
        }
    }

    [Test]
    public void ProgramCsvContainsConfiguredValueEndingCards()
    {
        string[] expectedIds = { "END01", "END02", "END03", "END04", "END05", "END06", "END07", "END08" };
        string[] expectedExpressions =
        {
            "bodyMind > 100",
            "bodyMind < 0",
            "academics > 100",
            "academics < 0",
            "relationships > 100",
            "relationships < 0",
            "economy > 100",
            "economy < 0"
        };

        for (int i = 0; i < expectedIds.Length; i++)
        {
            Dictionary<string, string> row = FindCsvRow(expectedIds[i]);
            Assert.AreEqual("US_" + expectedIds[i], row["cardName"]);
            Assert.AreEqual("ValueEnding", row["groupName"]);
            Assert.AreEqual("cs_GameOver", row["styleName"]);
            Assert.AreEqual("false", row["isDrawable"]);
            Assert.AreEqual("false", row["isEndingChain"]);
            Assert.AreEqual(expectedExpressions[i], row["conditionExpression"]);
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
            string eventId = Path.GetFileNameWithoutExtension(path).Substring("card-".Length);
            if (IsStandardCardArtEventId(eventId))
            {
                artIds.Add(eventId);
            }
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

    static ValueScript.valueEvents BuildValueEvents()
    {
        return new ValueScript.valueEvents
        {
            OnIncrease = new ValueScript.mEvent(),
            OnDecrease = new ValueScript.mEvent(),
            OnMax = new ValueScript.mEvent(),
            OnMin = new ValueScript.mEvent()
        };
    }

    static ValueScript CreateNewGameResetValue(List<GameObject> valueObjects, valueDefinitions.values valueType, float currentValue, float randomMin, float randomMax)
    {
        GameObject valueObject = new GameObject(valueType + " Reset Test Host");
        valueObjects.Add(valueObject);

        ValueScript valueScript = valueObject.AddComponent<ValueScript>();
        valueScript.valueType = valueType;
        valueScript.value = currentValue;
        valueScript.limits = new ValueScript.valueLimits
        {
            min = 0f,
            max = 100f,
            randomMin = randomMin,
            randomMax = randomMax,
            roundToWholeNumbers = true
        };
        valueScript.events = BuildValueEvents();
        typeof(ValueScript)
            .GetField("identifier", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(valueScript, ValueResetTestKeyPrefix + valueType);

        return valueScript;
    }

    static void DeleteSecurePrefsKey(string key)
    {
        MethodInfo generateMd5 = typeof(SecurePlayerPrefs).GetMethod("GenerateMD5", BindingFlags.Static | BindingFlags.NonPublic);
        if (generateMd5 == null)
        {
            PlayerPrefs.DeleteKey(key);
            return;
        }

        PlayerPrefs.DeleteKey((string)generateMd5.Invoke(null, new object[] { key }));
        PlayerPrefs.DeleteKey((string)generateMd5.Invoke(null, new object[] { key + "asdf" }));
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

    static void ConfigureStackTestCard(GameObject cardPrefab, bool isDrawable)
    {
        EventScript eventScript = cardPrefab.AddComponent<EventScript>();
        eventScript.isDrawable = isDrawable;
        eventScript.cardPropability = 1f;
        eventScript.conditions = new EventScript.condition[0];
        eventScript.changeExtrasOnCardDespawn = new EventScript.C_AdditionalModifiers[0];
        eventScript.changeValueOnCardDespawn = new EventScript.resultModifier[0];
        eventScript.OnCardSpawn = new EventScript.mEvent();
        eventScript.OnCardDespawn = new EventScript.mEvent();
    }

    static void ConfigureSceneStackForEditMode(CardStack stack)
    {
        stack.availableCards = new List<GameObject>();
        stack.highPriorityCards = new List<GameObject>();
        stack.followUpStack = new CardStack.C_WrapFollowUpStack();
        stack.cardDrawCount = new CardStack.drawCnts
        {
            cnt = new CardStack.cardCount[stack.allCards.Length]
        };
        stack.cardBlockCount = new CardStack.blockCount
        {
            cnt = new CardStack.cardCount[stack.allCards.Length]
        };

        for (int i = 0; i < stack.allCards.Length; i++)
        {
            int cardCount = stack.allCards[i].groupCards.Length;
            stack.cardDrawCount.cnt[i] = new CardStack.cardCount
            {
                drawCnt = new int[cardCount]
            };
            stack.cardBlockCount.cnt[i] = new CardStack.cardCount
            {
                drawCnt = new int[cardCount]
            };
        }
    }

    static void InvokeNewCard(CardStack stack)
    {
        typeof(CardStack)
            .GetMethod("newCard", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(stack, null);
    }

    static void ClearRuntimeCardStackPrefs()
    {
        PlayerPrefs.DeleteKey("GameState");
        PlayerPrefs.DeleteKey("drawCnt");
        PlayerPrefs.DeleteKey("blockCnt");
        PlayerPrefs.DeleteKey("Cind");
        PlayerPrefs.DeleteKey("followUpStack");
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
