using System;
using System.Collections.Generic;
using UnityEngine;

public static class UniversityManualSaveSystem
{
    public const string LegacySlotKey = "US.ManualSave.Slot0";
    public const string SlotKey = LegacySlotKey;
    public const int MinSlotNumber = 1;
    public const int MaxSlotNumber = 3;

    const int SaveVersion = 1;
    const string SlotKeyPrefix = "US.ManualSave.Slot";
    const string MainlineKeyPrefix = "US.MainlineEventScheduler.";
    const string CurrentRunDaysKey = "US.TrueEnding.CurrentRunDays";

    static bool legacyMigrationChecked;

    static readonly string[] PlainStringKeys =
    {
        "drawCnt",
        "blockCnt",
        "Cind",
        "followUpStack",
        MainlineKeyPrefix + "OfferedChains"
    };

    static readonly string[] PlainIntKeys =
    {
        "GameState",
        MainlineKeyPrefix + "NormalDrawCount",
        MainlineKeyPrefix + "MissedRollCount"
    };

    static readonly string[] SecureStringKeys =
    {
        "GameDictionary",
        "Inventory",
        "Quests"
    };

    public struct SlotInfo
    {
        public int slotNumber;
        public bool hasSave;
        public string slotKey;
        public string savedAtUtc;
    }

    [Serializable]
    class ManualSaveData
    {
        public int version;
        public int slotNumber;
        public string savedAtUtc;
        public List<StringEntry> plainStrings = new List<StringEntry>();
        public List<IntEntry> plainInts = new List<IntEntry>();
        public List<StringEntry> secureStrings = new List<StringEntry>();
        public List<FloatEntry> secureFloats = new List<FloatEntry>();
        public List<IntEntry> secureInts = new List<IntEntry>();
    }

    [Serializable]
    class StringEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    class IntEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    class FloatEntry
    {
        public string key;
        public float value;
    }

    public static bool HasManualSave()
    {
        return HasManualSave(MinSlotNumber);
    }

    public static bool HasManualSave(int slotNumber)
    {
        EnsureLegacySaveMigrated();

        string slotKey;
        if (!TryGetSlotKey(slotNumber, out slotKey))
        {
            return false;
        }

        return HasRawSlot(slotKey);
    }

    public static SlotInfo GetSlotInfo(int slotNumber)
    {
        EnsureLegacySaveMigrated();

        SlotInfo info = new SlotInfo
        {
            slotNumber = slotNumber,
            slotKey = GetSlotKey(slotNumber)
        };

        if (!IsValidSlotNumber(slotNumber) || !HasRawSlot(info.slotKey))
        {
            return info;
        }

        info.hasSave = true;
        ManualSaveData data = DeserializeSaveData(PlayerPrefs.GetString(info.slotKey));
        if (data != null)
        {
            info.savedAtUtc = data.savedAtUtc;
        }

        return info;
    }

    public static void SaveCurrentProgress()
    {
        SaveCurrentProgress(MinSlotNumber);
    }

    public static void SaveCurrentProgress(int slotNumber)
    {
        EnsureLegacySaveMigrated();

        string slotKey;
        if (!TryGetSlotKey(slotNumber, out slotKey))
        {
            Debug.LogWarning("Manual save slot is out of range: " + slotNumber);
            return;
        }

        ManualSaveData data = CaptureCurrentProgress(slotNumber);
        PlayerPrefs.SetString(slotKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static bool TryLoadCurrentProgress()
    {
        return TryLoadCurrentProgress(MinSlotNumber);
    }

    public static bool TryLoadCurrentProgress(int slotNumber)
    {
        EnsureLegacySaveMigrated();

        string slotKey;
        if (!TryGetSlotKey(slotNumber, out slotKey) || !HasRawSlot(slotKey))
        {
            return false;
        }

        ManualSaveData data = DeserializeSaveData(PlayerPrefs.GetString(slotKey));
        if (data == null || data.version <= 0)
        {
            return false;
        }

        ApplyCurrentProgress(data);
        PlayerPrefs.Save();
        return true;
    }

    static void EnsureLegacySaveMigrated()
    {
        if (legacyMigrationChecked)
        {
            return;
        }

        legacyMigrationChecked = true;

        string slotOneKey = GetSlotKey(MinSlotNumber);
        if (!HasRawSlot(slotOneKey) && HasRawSlot(LegacySlotKey))
        {
            PlayerPrefs.SetString(slotOneKey, PlayerPrefs.GetString(LegacySlotKey));
            PlayerPrefs.Save();
        }
    }

    static bool TryGetSlotKey(int slotNumber, out string slotKey)
    {
        slotKey = null;
        if (!IsValidSlotNumber(slotNumber))
        {
            return false;
        }

        slotKey = GetSlotKey(slotNumber);
        return true;
    }

    static bool IsValidSlotNumber(int slotNumber)
    {
        return slotNumber >= MinSlotNumber && slotNumber <= MaxSlotNumber;
    }

    static string GetSlotKey(int slotNumber)
    {
        return SlotKeyPrefix + slotNumber;
    }

    static bool HasRawSlot(string slotKey)
    {
        return PlayerPrefs.HasKey(slotKey) && !string.IsNullOrEmpty(PlayerPrefs.GetString(slotKey));
    }

    static ManualSaveData DeserializeSaveData(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<ManualSaveData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Manual save could not be parsed: " + ex.Message);
            return null;
        }
    }

    static ManualSaveData CaptureCurrentProgress(int slotNumber)
    {
        ManualSaveData data = new ManualSaveData
        {
            version = SaveVersion,
            slotNumber = slotNumber,
            savedAtUtc = DateTime.UtcNow.ToString("o")
        };

        AddPlainStrings(data);
        AddPlainInts(data);
        AddSecureStrings(data);
        AddValueScriptSnapshot(data);
        AddSecureIntIfPresent(data, CurrentRunDaysKey);
        AddPersistentImageSnapshot(data);

        return data;
    }

    static void AddPlainStrings(ManualSaveData data)
    {
        for (int i = 0; i < PlainStringKeys.Length; i++)
        {
            string key = PlainStringKeys[i];
            if (PlayerPrefs.HasKey(key))
            {
                data.plainStrings.Add(new StringEntry { key = key, value = PlayerPrefs.GetString(key) });
            }
        }
    }

    static void AddPlainInts(ManualSaveData data)
    {
        for (int i = 0; i < PlainIntKeys.Length; i++)
        {
            string key = PlainIntKeys[i];
            if (PlayerPrefs.HasKey(key))
            {
                data.plainInts.Add(new IntEntry { key = key, value = PlayerPrefs.GetInt(key) });
            }
        }
    }

    static void AddSecureStrings(ManualSaveData data)
    {
        for (int i = 0; i < SecureStringKeys.Length; i++)
        {
            string key = SecureStringKeys[i];
            if (SecurePlayerPrefs.HasKey(key))
            {
                data.secureStrings.Add(new StringEntry { key = key, value = SecurePlayerPrefs.GetString(key) });
            }
        }
    }

    static void AddValueScriptSnapshot(ManualSaveData data)
    {
        HashSet<string> savedKeys = new HashSet<string>();

        if (valueManager.instance != null && valueManager.instance.values != null)
        {
            for (int i = 0; i < valueManager.instance.values.Count; i++)
            {
                ValueScript valueScript = valueManager.instance.values[i];
                if (valueScript == null)
                {
                    continue;
                }

                string key = valueScript.valueType.ToString();
                data.secureFloats.Add(new FloatEntry { key = key, value = valueScript.value });
                savedKeys.Add(key);
            }
        }

        Array valueTypes = Enum.GetValues(typeof(valueDefinitions.values));
        for (int i = 0; i < valueTypes.Length; i++)
        {
            string key = valueTypes.GetValue(i).ToString();
            if (!savedKeys.Contains(key) && SecurePlayerPrefs.HasKey(key))
            {
                data.secureFloats.Add(new FloatEntry { key = key, value = SecurePlayerPrefs.GetFloat(key) });
            }
        }
    }

    static void AddPersistentImageSnapshot(ManualSaveData data)
    {
        PersistentImages[] images = Resources.FindObjectsOfTypeAll<PersistentImages>();
        for (int i = 0; i < images.Length; i++)
        {
            PersistentImages image = images[i];
            if (image == null || image.gameObject.scene.IsValid() == false || string.IsNullOrEmpty(image.saveKey))
            {
                continue;
            }

            data.secureInts.Add(new IntEntry { key = image.saveKey, value = image.spriteIndex });
        }
    }

    static void AddSecureIntIfPresent(ManualSaveData data, string key)
    {
        if (SecurePlayerPrefs.HasKey(key))
        {
            data.secureInts.Add(new IntEntry { key = key, value = SecurePlayerPrefs.GetInt(key) });
        }
    }

    static void ApplyCurrentProgress(ManualSaveData data)
    {
        if (data.plainStrings != null)
        {
            for (int i = 0; i < data.plainStrings.Count; i++)
            {
                StringEntry entry = data.plainStrings[i];
                if (!string.IsNullOrEmpty(entry.key))
                {
                    PlayerPrefs.SetString(entry.key, entry.value);
                }
            }
        }

        if (data.plainInts != null)
        {
            for (int i = 0; i < data.plainInts.Count; i++)
            {
                IntEntry entry = data.plainInts[i];
                if (!string.IsNullOrEmpty(entry.key))
                {
                    PlayerPrefs.SetInt(entry.key, entry.value);
                    ApplyRuntimePlainInt(entry);
                }
            }
        }

        if (data.secureStrings != null)
        {
            for (int i = 0; i < data.secureStrings.Count; i++)
            {
                StringEntry entry = data.secureStrings[i];
                if (!string.IsNullOrEmpty(entry.key))
                {
                    SecurePlayerPrefs.SetString(entry.key, entry.value);
                }
            }
        }

        if (data.secureFloats != null)
        {
            for (int i = 0; i < data.secureFloats.Count; i++)
            {
                FloatEntry entry = data.secureFloats[i];
                if (!string.IsNullOrEmpty(entry.key))
                {
                    SecurePlayerPrefs.SetFloat(entry.key, entry.value);
                    ApplyRuntimeValue(entry);
                }
            }
        }

        if (data.secureInts != null)
        {
            for (int i = 0; i < data.secureInts.Count; i++)
            {
                IntEntry entry = data.secureInts[i];
                if (!string.IsNullOrEmpty(entry.key))
                {
                    SecurePlayerPrefs.SetInt(entry.key, entry.value);
                    ApplyRuntimePersistentImage(entry);
                }
            }
        }
    }

    static void ApplyRuntimePlainInt(IntEntry entry)
    {
        if (entry.key != "GameState" || GameStateManager.instance == null)
        {
            return;
        }

        GameStateManager.instance.gamestate = (GameStateManager.Gamestate)entry.value;
    }

    static void ApplyRuntimeValue(FloatEntry entry)
    {
        valueDefinitions.values valueType;
        if (!Enum.TryParse(entry.key, out valueType))
        {
            return;
        }

        if (valueManager.instance != null && valueManager.instance.values != null)
        {
            for (int i = 0; i < valueManager.instance.values.Count; i++)
            {
                ApplyRuntimeValue(valueManager.instance.values[i], valueType, entry.value);
            }
        }

        ValueScript[] sceneValues = Resources.FindObjectsOfTypeAll<ValueScript>();
        for (int i = 0; i < sceneValues.Length; i++)
        {
            ValueScript valueScript = sceneValues[i];
            if (valueScript != null && valueScript.gameObject.scene.IsValid())
            {
                ApplyRuntimeValue(valueScript, valueType, entry.value);
            }
        }
    }

    static void ApplyRuntimeValue(ValueScript valueScript, valueDefinitions.values valueType, float value)
    {
        if (valueScript == null || valueScript.valueType != valueType)
        {
            return;
        }

        valueScript.value = value;
        if (valueScript.UserInterface != null)
        {
            valueScript.UserInterface.lerpedValue = value;
        }
    }

    static void ApplyRuntimePersistentImage(IntEntry entry)
    {
        PersistentImages[] images = Resources.FindObjectsOfTypeAll<PersistentImages>();
        for (int i = 0; i < images.Length; i++)
        {
            PersistentImages image = images[i];
            if (image != null && image.gameObject.scene.IsValid() && image.saveKey == entry.key)
            {
                image.spriteIndex = entry.value;
            }
        }
    }
}
