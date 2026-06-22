using UnityEditor;
using UnityEngine;

public static class UniversityTrueEndingDebugMenu
{
    [MenuItem("University Simulator/True Ending/Reset Progress")]
    public static void ResetProgress()
    {
        UniversityTrueEndingProgress.ResetForDebug(UniversityTrueEndingProgress.DefaultRequiredFlags);
        Debug.Log("University true-ending progress reset.");
    }

    [MenuItem("University Simulator/True Ending/Unlock Gate For Debug")]
    public static void UnlockGateForDebug()
    {
        UniversityTrueEndingProgress.UnlockGateForDebug();
        Debug.Log("University true-ending gate unlocked for debug.");
    }

    [MenuItem("University Simulator/True Ending/Complete Required Flags For Debug")]
    public static void CompleteRequiredFlagsForDebug()
    {
        UniversityTrueEndingProgress.CompleteDefaultFlagsForDebug();
        Debug.Log("University true-ending required flags completed for debug.");
    }
}
