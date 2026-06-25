using System.Collections.Generic;
using UnityEngine;

public static class UniversityTrueEndingProgress
{
    public const int DefaultRequiredLifetimeDays = 365;
    public const int DefaultRequiredGameOvers = 51;

    public static event System.Action<int> OnCurrentRunDaysChanged;

    const string KeyPrefix = "US.TrueEnding.";
    const string LifetimeDaysKey = KeyPrefix + "LifetimeDays";
    const string CurrentRunDaysKey = KeyPrefix + "CurrentRunDays";
    const string GameOverCountKey = KeyPrefix + "GameOverCount";
    const string TriggeredKey = KeyPrefix + "Triggered";
    const string FlagPrefix = KeyPrefix + "Flag.";

    public static readonly string[] DefaultRequiredFlags =
    {
        "MAIN_01",
        "MAIN_02",
        "MAIN_03",
        "MAIN_04",
        "MAIN_05",
        "MAIN_06",
        "MAIN_07",
        "MAIN_08"
    };

    public static int LifetimeDays
    {
        get { return SecurePlayerPrefs.GetInt(LifetimeDaysKey); }
    }

    public static int CurrentRunDays
    {
        get { return SecurePlayerPrefs.GetInt(CurrentRunDaysKey); }
    }

    public static int GameOverCount
    {
        get { return SecurePlayerPrefs.GetInt(GameOverCountKey); }
    }

    public static bool HasTriggeredTrueEnding
    {
        get { return SecurePlayerPrefs.GetBool(TriggeredKey); }
    }

    public static void AddLifetimeDays(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SecurePlayerPrefs.SetInt(LifetimeDaysKey, LifetimeDays + amount);
    }

    public static void AddCurrentRunDays(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int updatedDays = CurrentRunDays + amount;
        SecurePlayerPrefs.SetInt(CurrentRunDaysKey, updatedDays);
        OnCurrentRunDaysChanged?.Invoke(updatedDays);
    }

    public static void ResetCurrentRunDays()
    {
        SecurePlayerPrefs.SetInt(CurrentRunDaysKey, 0);
        OnCurrentRunDaysChanged?.Invoke(0);
    }

    public static void AddGameOver(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SecurePlayerPrefs.SetInt(GameOverCountKey, GameOverCount + amount);
    }

    public static bool IsMetaUnlocked(int requiredLifetimeDays, int requiredGameOvers)
    {
        return LifetimeDays >= requiredLifetimeDays || GameOverCount >= requiredGameOvers;
    }

    public static bool IsDefaultMetaUnlocked()
    {
        return IsMetaUnlocked(DefaultRequiredLifetimeDays, DefaultRequiredGameOvers);
    }

    public static bool TrySetPermanentFlag(string flagId, bool requiresMetaUnlock, int requiredLifetimeDays, int requiredGameOvers)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            return false;
        }

        if (requiresMetaUnlock && !IsMetaUnlocked(requiredLifetimeDays, requiredGameOvers))
        {
            return false;
        }

        SetPermanentFlag(flagId, true);
        return true;
    }

    public static void SetPermanentFlag(string flagId, bool value)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            return;
        }

        SecurePlayerPrefs.SetBool(FlagPrefix + flagId, value);
    }

    public static bool GetPermanentFlag(string flagId)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            return false;
        }

        return SecurePlayerPrefs.GetBool(FlagPrefix + flagId);
    }

    public static bool AreAllPermanentFlagsSet(IEnumerable<string> flagIds)
    {
        if (flagIds == null)
        {
            return false;
        }

        bool hasAnyFlag = false;
        foreach (string flagId in flagIds)
        {
            if (string.IsNullOrEmpty(flagId))
            {
                continue;
            }

            hasAnyFlag = true;
            if (!GetPermanentFlag(flagId))
            {
                return false;
            }
        }

        return hasAnyFlag;
    }

    public static void SetTrueEndingTriggered(bool value)
    {
        SecurePlayerPrefs.SetBool(TriggeredKey, value);
    }

    public static void ResetForDebug(IEnumerable<string> flagIds)
    {
        SecurePlayerPrefs.SetInt(LifetimeDaysKey, 0);
        SecurePlayerPrefs.SetInt(CurrentRunDaysKey, 0);
        SecurePlayerPrefs.SetInt(GameOverCountKey, 0);
        SecurePlayerPrefs.SetBool(TriggeredKey, false);
        OnCurrentRunDaysChanged?.Invoke(0);

        IEnumerable<string> flagsToReset = flagIds ?? DefaultRequiredFlags;
        foreach (string flagId in flagsToReset)
        {
            SetPermanentFlag(flagId, false);
        }

        PlayerPrefs.Save();
    }

    public static void UnlockGateForDebug()
    {
        SecurePlayerPrefs.SetInt(LifetimeDaysKey, DefaultRequiredLifetimeDays);
        PlayerPrefs.Save();
    }

    public static void CompleteDefaultFlagsForDebug()
    {
        foreach (string flagId in DefaultRequiredFlags)
        {
            SetPermanentFlag(flagId, true);
        }

        PlayerPrefs.Save();
    }
}
