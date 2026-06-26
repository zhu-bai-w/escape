using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniversityMainlineEventScheduler : MonoBehaviour
{
    const string KeyPrefix = "US.MainlineEventScheduler.";
    const string NormalDrawCountKey = KeyPrefix + "NormalDrawCount";
    const string MissedRollCountKey = KeyPrefix + "MissedRollCount";
    const string OfferedChainsKey = KeyPrefix + "OfferedChains";

    [Header("Timing")]
    public int protectedNormalDraws = 10;
    [Range(0f, 1f)] public float initialChance = 0.15f;
    [Range(0f, 1f)] public float chanceIncreasePerMiss = 0.15f;
    public int guaranteedAfterMisses = 6;

    [Header("Debug")]
    public bool debugLog;

    CardStack cardStack;
    GameStateManager gameStateManager;
    bool subscribed;
    bool stateLoaded;
    int normalDrawCount;
    int missedRollCount;
    readonly HashSet<string> offeredChains = new HashSet<string>();

    void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    IEnumerator SubscribeWhenReady()
    {
        while (!subscribed)
        {
            if (TrySubscribe())
            {
                LoadState();
                yield break;
            }

            yield return null;
        }
    }

    bool TrySubscribe()
    {
        if (subscribed)
        {
            return true;
        }

        cardStack = CardStack.instance;
        gameStateManager = GameStateManager.instance;
        if (cardStack == null || gameStateManager == null)
        {
            return false;
        }

        cardStack.onCardSwipe.AddListener(HandleCardSwipe);
        gameStateManager.OnNewGame.AddListener(ResetRunState);
        subscribed = true;
        return true;
    }

    void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (cardStack != null)
        {
            cardStack.onCardSwipe.RemoveListener(HandleCardSwipe);
        }

        if (gameStateManager != null)
        {
            gameStateManager.OnNewGame.RemoveListener(ResetRunState);
        }

        subscribed = false;
    }

    void HandleCardSwipe()
    {
        if (!IsGameActive())
        {
            return;
        }

        if (!stateLoaded)
        {
            LoadState();
        }

        UniversityMainlineEventCard currentCard = GetCurrentMainlineCard();
        if (currentCard != null)
        {
            if (currentCard.isEntryCard)
            {
                MarkOffered(currentCard.chainId);
            }

            ResetProbabilityWindow();
            SaveState();
            return;
        }

        normalDrawCount++;
        TryScheduleEntryCard();
        SaveState();
    }

    void TryScheduleEntryCard()
    {
        if (cardStack == null || cardStack.followUpCard != null)
        {
            return;
        }

        if (normalDrawCount < protectedNormalDraws)
        {
            return;
        }

        List<GameObject> eligibleEntries = GetEligibleEntryCards();
        if (eligibleEntries.Count == 0)
        {
            return;
        }

        float chance = Mathf.Clamp01(initialChance + missedRollCount * chanceIncreasePerMiss);
        bool guaranteed = missedRollCount >= guaranteedAfterMisses;
        if (!guaranteed && Random.value >= chance)
        {
            missedRollCount++;
            return;
        }

        GameObject selected = eligibleEntries[Random.Range(0, eligibleEntries.Count)];
        UniversityMainlineEventCard marker = selected.GetComponent<UniversityMainlineEventCard>();
        if (marker != null)
        {
            MarkOffered(marker.chainId);
        }

        cardStack.followUpCard = selected;
        ResetProbabilityWindow();

        if (debugLog)
        {
            Debug.Log("Mainline event scheduled: " + selected.name);
        }
    }

    List<GameObject> GetEligibleEntryCards()
    {
        List<GameObject> entries = new List<GameObject>();
        if (cardStack == null || cardStack.allCards == null)
        {
            return entries;
        }

        for (int i = 0; i < cardStack.allCards.Length; i++)
        {
            GameObject[] groupCards = cardStack.allCards[i].groupCards;
            if (groupCards == null)
            {
                continue;
            }

            for (int j = 0; j < groupCards.Length; j++)
            {
                GameObject candidate = groupCards[j];
                if (candidate == null)
                {
                    continue;
                }

                UniversityMainlineEventCard marker = candidate.GetComponent<UniversityMainlineEventCard>();
                if (marker == null || !marker.isEntryCard || string.IsNullOrEmpty(marker.chainId))
                {
                    continue;
                }

                if (offeredChains.Contains(marker.chainId))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(marker.permanentFlagId) && UniversityTrueEndingProgress.GetPermanentFlag(marker.permanentFlagId))
                {
                    continue;
                }

                entries.Add(candidate);
            }
        }

        return entries;
    }

    UniversityMainlineEventCard GetCurrentMainlineCard()
    {
        if (cardStack == null || cardStack.spawnedCard == null)
        {
            return null;
        }

        return cardStack.spawnedCard.GetComponent<UniversityMainlineEventCard>();
    }

    void ResetRunState()
    {
        normalDrawCount = 0;
        missedRollCount = 0;
        offeredChains.Clear();
        stateLoaded = true;
        SaveState();
    }

    void ResetProbabilityWindow()
    {
        normalDrawCount = 0;
        missedRollCount = 0;
    }

    void MarkOffered(string chainId)
    {
        if (!string.IsNullOrEmpty(chainId))
        {
            offeredChains.Add(chainId);
        }
    }

    void LoadState()
    {
        normalDrawCount = PlayerPrefs.GetInt(NormalDrawCountKey, 0);
        missedRollCount = PlayerPrefs.GetInt(MissedRollCountKey, 0);
        offeredChains.Clear();

        string serializedChains = PlayerPrefs.GetString(OfferedChainsKey, "");
        if (!string.IsNullOrEmpty(serializedChains))
        {
            string[] chainIds = serializedChains.Split(',');
            for (int i = 0; i < chainIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(chainIds[i]))
                {
                    offeredChains.Add(chainIds[i]);
                }
            }
        }

        stateLoaded = true;
    }

    void SaveState()
    {
        PlayerPrefs.SetInt(NormalDrawCountKey, normalDrawCount);
        PlayerPrefs.SetInt(MissedRollCountKey, missedRollCount);
        PlayerPrefs.SetString(OfferedChainsKey, string.Join(",", offeredChains));
        PlayerPrefs.Save();
    }

    bool IsGameActive()
    {
        return GameStateManager.instance != null && GameStateManager.instance.gamestate == GameStateManager.Gamestate.gameActive;
    }
}
