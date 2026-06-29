using System;
using UnityEngine;

public class UniversityValueEndingController : MonoBehaviour
{
    [Serializable]
    public class ValueEndingRule
    {
        public string eventId;
        public valueDefinitions.values valueType;
        public bool triggerOnMaximum;
        public GameObject endingCard;
    }

    public ValueEndingRule[] rules = new ValueEndingRule[0];
    public bool triggerOnlyOncePerRun = true;
    public bool debugLog;

    CardStack cardStack;
    GameStateManager gameStateManager;
    bool subscribed;
    bool hasQueuedEnding;

    void Start()
    {
        Subscribe();
    }

    void OnEnable()
    {
        Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        cardStack = CardStack.instance;
        gameStateManager = GameStateManager.instance;
        if (cardStack == null || gameStateManager == null)
        {
            return;
        }

        if (cardStack.onCardSwipe == null)
        {
            cardStack.onCardSwipe = new CardStack.mEvent();
        }

        if (gameStateManager.OnNewGame == null)
        {
            gameStateManager.OnNewGame = new GameStateManager.mEvent();
        }

        cardStack.onCardSwipe.AddListener(EvaluateAfterCardSwipe);
        gameStateManager.OnNewGame.AddListener(ResetForNewRun);
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (cardStack != null && cardStack.onCardSwipe != null)
        {
            cardStack.onCardSwipe.RemoveListener(EvaluateAfterCardSwipe);
        }

        if (gameStateManager != null && gameStateManager.OnNewGame != null)
        {
            gameStateManager.OnNewGame.RemoveListener(ResetForNewRun);
        }

        subscribed = false;
    }

    void ResetForNewRun()
    {
        hasQueuedEnding = false;
        ClearTrackedLimitDeviations();
    }

    void EvaluateAfterCardSwipe()
    {
        if (!IsGameActive() || cardStack == null || valueManager.instance == null)
        {
            ClearTrackedLimitDeviations();
            return;
        }

        if (triggerOnlyOncePerRun && hasQueuedEnding)
        {
            ClearTrackedLimitDeviations();
            return;
        }

        int frame = Time.frameCount;
        ValueEndingRule bestRule = null;
        float bestDeviation = 0f;

        for (int i = 0; i < rules.Length; i++)
        {
            ValueEndingRule rule = rules[i];
            if (rule == null || rule.endingCard == null)
            {
                continue;
            }

            ValueScript valueScript = valueManager.instance.getFirstFittingValue(rule.valueType);
            if (valueScript == null)
            {
                continue;
            }

            float deviation;
            if (!valueScript.TryGetLastLimitDeviation(frame, rule.triggerOnMaximum, out deviation))
            {
                continue;
            }

            if (deviation > bestDeviation)
            {
                bestDeviation = deviation;
                bestRule = rule;
            }
        }

        ClearTrackedLimitDeviations();

        if (bestRule == null)
        {
            return;
        }

        cardStack.followUpCard = bestRule.endingCard;
        hasQueuedEnding = true;
        UniversityAchievementSystem.RecordValueEnding(bestRule.eventId, bestRule.valueType, bestRule.triggerOnMaximum, bestDeviation);

        if (debugLog)
        {
            string side = bestRule.triggerOnMaximum ? "maximum" : "minimum";
            Debug.Log("Value ending queued: " + bestRule.eventId + " from " + bestRule.valueType + " " + side + " deviation " + bestDeviation);
        }
    }

    void ClearTrackedLimitDeviations()
    {
        if (valueManager.instance == null || valueManager.instance.values == null)
        {
            return;
        }

        for (int i = 0; i < valueManager.instance.values.Count; i++)
        {
            ValueScript valueScript = valueManager.instance.values[i];
            if (valueScript != null)
            {
                valueScript.ClearLastLimitDeviation();
            }
        }
    }

    bool IsGameActive()
    {
        return GameStateManager.instance != null && GameStateManager.instance.gamestate == GameStateManager.Gamestate.gameActive;
    }
}
