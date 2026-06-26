using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class UniversitySimulatorRegressionTests
{
    const string ProgramCsvPath = "Assets/UniversitySimulator/Data/cards_v1_program.csv";

    [SetUp]
    public void SetUp()
    {
        CardStack.instance = null;
        GameStateManager.instance = null;
        UniversityTrueEndingProgress.ResetForDebug(UniversityTrueEndingProgress.DefaultRequiredFlags);
    }

    [TearDown]
    public void TearDown()
    {
        CardStack.instance = null;
        GameStateManager.instance = null;
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
        string[] lines = File.ReadAllLines(ProgramCsvPath);
        string[] headers = SplitCsvLine(lines[0]);
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = SplitCsvLine(lines[i]);
            if (values.Length == 0 || values[0] != eventId)
            {
                continue;
            }

            Dictionary<string, string> row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++)
            {
                row[headers[j]] = j < values.Length ? values[j] : "";
            }

            return row;
        }

        Assert.Fail("CSV row not found: " + eventId);
        return null;
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
