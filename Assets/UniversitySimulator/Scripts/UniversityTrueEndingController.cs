using UnityEngine;

public class UniversityTrueEndingController : MonoBehaviour
{
    [Header("Gate")]
    public int requiredLifetimeDays = UniversityTrueEndingProgress.DefaultRequiredLifetimeDays;
    public int requiredGameOvers = UniversityTrueEndingProgress.DefaultRequiredGameOvers;
    public string[] requiredPermanentFlags = UniversityTrueEndingProgress.DefaultRequiredFlags;

    [Header("Ending")]
    public GameObject trueEndingStartCard;
    public bool triggerOnlyOnce = true;

    [Header("Counters")]
    public bool countCardSwipesAsDays = true;
    public bool countGameOverEvents = true;

    [Header("Debug")]
    public bool debugLog;

    CardStack cardStack;
    GameStateManager gameStateManager;
    bool subscribed;

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

        cardStack.onCardSwipe.AddListener(RecordCardSwipeDay);
        cardStack.OnCardDestroy += EvaluateBeforeNextCard;
        gameStateManager.OnGameOver.AddListener(RecordGameOver);
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (cardStack != null)
        {
            cardStack.onCardSwipe.RemoveListener(RecordCardSwipeDay);
            cardStack.OnCardDestroy -= EvaluateBeforeNextCard;
        }

        if (gameStateManager != null)
        {
            gameStateManager.OnGameOver.RemoveListener(RecordGameOver);
        }

        subscribed = false;
    }

    void RecordCardSwipeDay()
    {
        if (!countCardSwipesAsDays || !IsGameActive())
        {
            return;
        }

        UniversityTrueEndingProgress.AddLifetimeDays(1);
    }

    void RecordGameOver()
    {
        if (!countGameOverEvents)
        {
            return;
        }

        UniversityTrueEndingProgress.AddGameOver(1);
    }

    void EvaluateBeforeNextCard()
    {
        if (!IsGameActive())
        {
            return;
        }

        if (cardStack == null || trueEndingStartCard == null)
        {
            return;
        }

        if (cardStack.followUpCard != null)
        {
            return;
        }

        if (triggerOnlyOnce && UniversityTrueEndingProgress.HasTriggeredTrueEnding)
        {
            return;
        }

        if (!UniversityTrueEndingProgress.IsMetaUnlocked(requiredLifetimeDays, requiredGameOvers))
        {
            return;
        }

        if (!UniversityTrueEndingProgress.AreAllPermanentFlagsSet(requiredPermanentFlags))
        {
            return;
        }

        cardStack.followUpCard = trueEndingStartCard;
        UniversityTrueEndingProgress.SetTrueEndingTriggered(true);

        if (debugLog)
        {
            Debug.Log("True ending chain queued: " + trueEndingStartCard.name);
        }
    }

    bool IsGameActive()
    {
        return GameStateManager.instance != null && GameStateManager.instance.gamestate == GameStateManager.Gamestate.gameActive;
    }
}
