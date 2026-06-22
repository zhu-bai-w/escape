using UnityEngine;

public class UniversityMainlineCardHook : MonoBehaviour
{
    [Header("Mainline Gate")]
    public bool requiresMetaUnlock = true;
    public int requiredLifetimeDays = UniversityTrueEndingProgress.DefaultRequiredLifetimeDays;
    public int requiredGameOvers = UniversityTrueEndingProgress.DefaultRequiredGameOvers;

    [Header("Left Choice")]
    public string permanentFlagLeft;
    public GameObject chainStartLeft;

    [Header("Right Choice")]
    public string permanentFlagRight;
    public GameObject chainStartRight;

    [Header("Debug")]
    public string cardId;
    public bool debugLog;

    public void ApplyLeftChoice()
    {
        ApplyChoice(permanentFlagLeft, chainStartLeft);
    }

    public void ApplyRightChoice()
    {
        ApplyChoice(permanentFlagRight, chainStartRight);
    }

    void ApplyChoice(string permanentFlag, GameObject chainStart)
    {
        if (requiresMetaUnlock && !UniversityTrueEndingProgress.IsMetaUnlocked(requiredLifetimeDays, requiredGameOvers))
        {
            if (debugLog)
            {
                Debug.Log("Mainline choice ignored before true-ending gate unlock: " + cardId);
            }

            return;
        }

        if (!string.IsNullOrEmpty(permanentFlag))
        {
            UniversityTrueEndingProgress.SetPermanentFlag(permanentFlag, true);
            if (debugLog)
            {
                Debug.Log("Mainline permanent flag set: " + permanentFlag);
            }
        }

        if (chainStart != null && CardStack.instance != null)
        {
            CardStack.instance.followUpCard = chainStart;
            if (debugLog)
            {
                Debug.Log("Mainline chain started from card " + cardId + ": " + chainStart.name);
            }
        }
    }
}
