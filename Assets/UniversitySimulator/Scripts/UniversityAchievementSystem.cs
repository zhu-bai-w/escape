using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum UniversityAchievementCategory
{
    Ending,
    Mainline,
    Survival,
    Choice
}

public enum UniversityAchievementTrigger
{
    ValueEnding,
    MainlineFlag,
    AllMainlineFlags,
    TrueEnding,
    CurrentRunDays,
    GameOverCount,
    SpecificChoice
}

public class UniversityAchievementDefinition
{
    public string id;
    public string title;
    public string description;
    public UniversityAchievementCategory category;
    public UniversityAchievementTrigger trigger;
    public string iconAssetPath;
    public string targetEventId;
    public string targetDirection;
    public string targetFlagId;
    public int targetCount;
    public valueDefinitions.values targetValue;
    public bool triggerOnMaximum;

    public UniversityAchievementDefinition(
        string id,
        string title,
        string description,
        UniversityAchievementCategory category,
        UniversityAchievementTrigger trigger,
        string iconAssetPath = "")
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.category = category;
        this.trigger = trigger;
        this.iconAssetPath = iconAssetPath;
    }
}

public class UniversityAchievementSystem : MonoBehaviour
{
    const string UnlockKeyPrefix = "US.Achievement.Unlocked.";
    const string UnlockTimeKeyPrefix = "US.Achievement.UnlockedAt.";

    static readonly string DefaultIconPath = "Assets/UniversitySimulator/Art/AchievementIcons/";
    const string DefaultIconResourcePath = "UniversityAchievementIcons/";
    const int AchievementTitleFontSize = 52;
    const int AchievementDescriptionFontSize = 36;
    const int AchievementStatusFontSize = 30;
    const float AchievementRowHeight = 300f;
    const float AchievementIconSize = 188f;
    const float AchievementListPadding = 32f;
    const float AchievementListSpacing = 24f;
    const float AchievementRowPaddingHorizontal = 36f;
    const float AchievementRowPaddingVertical = 28f;
    const float AchievementRowSpacing = 32f;
    const float AchievementTitleHeight = 72f;
    const float AchievementDescriptionHeight = 108f;
    const float AchievementStatusHeight = 44f;
    const float AchievementTextSpacing = 12f;

    static readonly List<UniversityAchievementDefinition> Definitions = new List<UniversityAchievementDefinition>
    {
        new UniversityAchievementDefinition("ACH_END_ECON_HIGH", "金钱的诅咒", "金钱溢出的情况下结束一局游戏。", UniversityAchievementCategory.Ending, UniversityAchievementTrigger.ValueEnding, DefaultIconPath)
        {
            targetEventId = "END07",
            targetValue = valueDefinitions.values.economy,
            triggerOnMaximum = true
        },
        new UniversityAchievementDefinition("ACH_END_ECON_LOW", "月底黑洞", "经济跌破底线的情况下结束一局游戏。", UniversityAchievementCategory.Ending, UniversityAchievementTrigger.ValueEnding, DefaultIconPath)
        {
            targetEventId = "END08",
            targetValue = valueDefinitions.values.economy
        },
        new UniversityAchievementDefinition("ACH_END_BODY_HIGH", "精神过载", "身心状态过高的情况下结束一局游戏。", UniversityAchievementCategory.Ending, UniversityAchievementTrigger.ValueEnding, DefaultIconPath)
        {
            targetEventId = "END01",
            targetValue = valueDefinitions.values.bodyMind,
            triggerOnMaximum = true
        },
        new UniversityAchievementDefinition("ACH_END_BODY_LOW", "电量耗尽", "身心状态跌破底线的情况下结束一局游戏。", UniversityAchievementCategory.Ending, UniversityAchievementTrigger.ValueEnding, DefaultIconPath)
        {
            targetEventId = "END02",
            targetValue = valueDefinitions.values.bodyMind
        },
        new UniversityAchievementDefinition("ACH_END_ACAD_HIGH", "卷到天花板", "学业压力过高的情况下结束一局游戏。", UniversityAchievementCategory.Ending, UniversityAchievementTrigger.ValueEnding, DefaultIconPath)
        {
            targetEventId = "END03",
            targetValue = valueDefinitions.values.academics,
            triggerOnMaximum = true
        },
        new UniversityAchievementDefinition("ACH_END_ACAD_LOW", "知识真空", "学业跌破底线的情况下结束一局游戏。", UniversityAchievementCategory.Ending, UniversityAchievementTrigger.ValueEnding, DefaultIconPath)
        {
            targetEventId = "END04",
            targetValue = valueDefinitions.values.academics
        },
        new UniversityAchievementDefinition("ACH_END_REL_HIGH", "社交恒星", "人际关系过高的情况下结束一局游戏。", UniversityAchievementCategory.Ending, UniversityAchievementTrigger.ValueEnding, DefaultIconPath)
        {
            targetEventId = "END05",
            targetValue = valueDefinitions.values.relationships,
            triggerOnMaximum = true
        },
        new UniversityAchievementDefinition("ACH_END_REL_LOW", "透明人", "人际关系跌破底线的情况下结束一局游戏。", UniversityAchievementCategory.Ending, UniversityAchievementTrigger.ValueEnding, DefaultIconPath)
        {
            targetEventId = "END06",
            targetValue = valueDefinitions.values.relationships
        },
        new UniversityAchievementDefinition("ACH_MAIN_SOCIAL", "社交线索", "完成一次社交主线选择。", UniversityAchievementCategory.Mainline, UniversityAchievementTrigger.MainlineFlag, DefaultIconPath)
        {
            targetFlagId = "MAIN_01"
        },
        new UniversityAchievementDefinition("ACH_MAIN_ACADEMICS", "课堂线索", "完成一次学业主线选择。", UniversityAchievementCategory.Mainline, UniversityAchievementTrigger.MainlineFlag, DefaultIconPath)
        {
            targetFlagId = "MAIN_02"
        },
        new UniversityAchievementDefinition("ACH_MAIN_BODY", "身体线索", "完成一次身心主线选择。", UniversityAchievementCategory.Mainline, UniversityAchievementTrigger.MainlineFlag, DefaultIconPath)
        {
            targetFlagId = "MAIN_03"
        },
        new UniversityAchievementDefinition("ACH_MAIN_ECON", "账本线索", "完成一次经济主线选择。", UniversityAchievementCategory.Mainline, UniversityAchievementTrigger.MainlineFlag, DefaultIconPath)
        {
            targetFlagId = "MAIN_04"
        },
        new UniversityAchievementDefinition("ACH_MAIN_ALL", "四条线索", "集齐四个主线永久标记。", UniversityAchievementCategory.Mainline, UniversityAchievementTrigger.AllMainlineFlags, DefaultIconPath),
        new UniversityAchievementDefinition("ACH_TRUE_END", "逃出日常", "触发一次真结局入口。", UniversityAchievementCategory.Mainline, UniversityAchievementTrigger.TrueEnding, DefaultIconPath),
        new UniversityAchievementDefinition("ACH_DAYS_05", "先撑五天", "单局坚持 5 天。", UniversityAchievementCategory.Survival, UniversityAchievementTrigger.CurrentRunDays, DefaultIconPath)
        {
            targetCount = 5
        },
        new UniversityAchievementDefinition("ACH_DAYS_10", "十天同学", "单局坚持 10 天。", UniversityAchievementCategory.Survival, UniversityAchievementTrigger.CurrentRunDays, DefaultIconPath)
        {
            targetCount = 10
        },
        new UniversityAchievementDefinition("ACH_DAYS_20", "二十天循环", "单局坚持 20 天。", UniversityAchievementCategory.Survival, UniversityAchievementTrigger.CurrentRunDays, DefaultIconPath)
        {
            targetCount = 20
        },
        new UniversityAchievementDefinition("ACH_DAYS_30", "月末幸存者", "单局坚持 30 天。", UniversityAchievementCategory.Survival, UniversityAchievementTrigger.CurrentRunDays, DefaultIconPath)
        {
            targetCount = 30
        },
        new UniversityAchievementDefinition("ACH_OVER_01", "第一次重开", "经历 1 次普通结局或失败。", UniversityAchievementCategory.Survival, UniversityAchievementTrigger.GameOverCount, DefaultIconPath)
        {
            targetCount = 1
        },
        new UniversityAchievementDefinition("ACH_OVER_03", "读档人生", "累计经历 3 次普通结局或失败。", UniversityAchievementCategory.Survival, UniversityAchievementTrigger.GameOverCount, DefaultIconPath)
        {
            targetCount = 3
        },
        new UniversityAchievementDefinition("ACH_CHOICE_E001_L", "我想静静", "在宿舍邀约事件里选择留在宿舍。", UniversityAchievementCategory.Choice, UniversityAchievementTrigger.SpecificChoice, DefaultIconPath)
        {
            targetEventId = "E001",
            targetDirection = "left"
        },
        new UniversityAchievementDefinition("ACH_CHOICE_E001_R", "先出门再说", "在宿舍邀约事件里选择出门见朋友。", UniversityAchievementCategory.Choice, UniversityAchievementTrigger.SpecificChoice, DefaultIconPath)
        {
            targetEventId = "E001",
            targetDirection = "right"
        },
        new UniversityAchievementDefinition("ACH_CHOICE_E010_L", "认真听讲", "在课堂点名事件里选择认真听讲。", UniversityAchievementCategory.Choice, UniversityAchievementTrigger.SpecificChoice, DefaultIconPath)
        {
            targetEventId = "E010",
            targetDirection = "left"
        },
        new UniversityAchievementDefinition("ACH_CHOICE_E010_R", "低头摸鱼", "在课堂点名事件里选择摸鱼。", UniversityAchievementCategory.Choice, UniversityAchievementTrigger.SpecificChoice, DefaultIconPath)
        {
            targetEventId = "E010",
            targetDirection = "right"
        },
        new UniversityAchievementDefinition("ACH_CHOICE_E018_L", "健身卡真用", "在健身房路过事件里选择进去运动。", UniversityAchievementCategory.Choice, UniversityAchievementTrigger.SpecificChoice, DefaultIconPath)
        {
            targetEventId = "E018",
            targetDirection = "left"
        },
        new UniversityAchievementDefinition("ACH_CHOICE_E018_R", "下次一定", "在健身房路过事件里选择直接路过。", UniversityAchievementCategory.Choice, UniversityAchievementTrigger.SpecificChoice, DefaultIconPath)
        {
            targetEventId = "E018",
            targetDirection = "right"
        },
        new UniversityAchievementDefinition("ACH_CHOICE_E031_L", "兼职启动", "在兼职机会事件里选择去打工。", UniversityAchievementCategory.Choice, UniversityAchievementTrigger.SpecificChoice, DefaultIconPath)
        {
            targetEventId = "E031",
            targetDirection = "left"
        },
        new UniversityAchievementDefinition("ACH_CHOICE_E031_R", "时间更贵", "在兼职机会事件里选择拒绝兼职。", UniversityAchievementCategory.Choice, UniversityAchievementTrigger.SpecificChoice, DefaultIconPath)
        {
            targetEventId = "E031",
            targetDirection = "right"
        },
        new UniversityAchievementDefinition("ACH_CHOICE_E041_L", "社团营业", "在社团招新事件里选择参与活动。", UniversityAchievementCategory.Choice, UniversityAchievementTrigger.SpecificChoice, DefaultIconPath)
        {
            targetEventId = "E041",
            targetDirection = "left"
        },
        new UniversityAchievementDefinition("ACH_CHOICE_E041_R", "路过社团", "在社团招新事件里选择不加入。", UniversityAchievementCategory.Choice, UniversityAchievementTrigger.SpecificChoice, DefaultIconPath)
        {
            targetEventId = "E041",
            targetDirection = "right"
        }
    };

    readonly Queue<UniversityAchievementDefinition> pendingPopups = new Queue<UniversityAchievementDefinition>();
    static readonly Dictionary<string, Sprite> IconCache = new Dictionary<string, Sprite>();
    bool popupRoutineRunning;

    public static UniversityAchievementSystem instance { get; private set; }

    public static IReadOnlyList<UniversityAchievementDefinition> AllAchievements
    {
        get { return Definitions; }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void Start()
    {
        EvaluateStoredProgress();
    }

    public static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        UniversityAchievementSystem found = FindObjectOfType<UniversityAchievementSystem>();
        if (found != null)
        {
            instance = found;
            return;
        }

        GameObject host = new GameObject("UniversityAchievementSystem");
        instance = host.AddComponent<UniversityAchievementSystem>();
    }

    public static void RecordChoice(GameObject card, string direction)
    {
        EnsureInstance();
        instance.RecordChoiceInternal(card, direction);
    }

    public static void RecordValueEnding(string eventId, valueDefinitions.values valueType, bool triggerOnMaximum, float deviation)
    {
        EnsureInstance();
        instance.RecordValueEndingInternal(eventId, valueType, triggerOnMaximum);
    }

    public static void RecordMainlineFlag(string flagId)
    {
        EnsureInstance();
        instance.RecordMainlineFlagInternal(flagId);
    }

    public static void RecordTrueEndingQueued()
    {
        EnsureInstance();
        instance.UnlockByTrigger(UniversityAchievementTrigger.TrueEnding);
    }

    public static void RecordCurrentRunDays(int currentRunDays)
    {
        EnsureInstance();
        instance.RecordCountInternal(UniversityAchievementTrigger.CurrentRunDays, currentRunDays);
    }

    public static void RecordGameOverCount(int gameOverCount)
    {
        EnsureInstance();
        instance.RecordCountInternal(UniversityAchievementTrigger.GameOverCount, gameOverCount);
    }

    public static bool IsUnlocked(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId))
        {
            return false;
        }

        return SecurePlayerPrefs.GetBool(UnlockKeyPrefix + achievementId);
    }

    public static int GetUnlockedCount()
    {
        int count = 0;
        foreach (UniversityAchievementDefinition definition in Definitions)
        {
            if (IsUnlocked(definition.id))
            {
                count++;
            }
        }

        return count;
    }

    public static void ResetAllForDebug()
    {
        foreach (UniversityAchievementDefinition definition in Definitions)
        {
            SecurePlayerPrefs.SetBool(UnlockKeyPrefix + definition.id, false);
            SecurePlayerPrefs.SetString(UnlockTimeKeyPrefix + definition.id, "");
        }

        if (instance != null)
        {
            instance.pendingPopups.Clear();
        }

        PlayerPrefs.Save();
    }

    public static void RefreshAchievementPanel(GameObject panel)
    {
        EnsureInstance();
        instance.EvaluateStoredProgress();
        instance.RefreshAchievementPanelInternal(panel);
    }

    void EvaluateStoredProgress()
    {
        RecordCountInternal(UniversityAchievementTrigger.CurrentRunDays, UniversityTrueEndingProgress.CurrentRunDays);
        RecordCountInternal(UniversityAchievementTrigger.GameOverCount, UniversityTrueEndingProgress.GameOverCount);

        foreach (string flagId in UniversityTrueEndingProgress.DefaultRequiredFlags)
        {
            if (UniversityTrueEndingProgress.GetPermanentFlag(flagId))
            {
                RecordMainlineFlagInternal(flagId);
            }
        }

        if (UniversityTrueEndingProgress.HasTriggeredTrueEnding)
        {
            UnlockByTrigger(UniversityAchievementTrigger.TrueEnding);
        }
    }

    void RecordChoiceInternal(GameObject card, string direction)
    {
        string eventId = ResolveEventId(card);
        if (string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(direction))
        {
            return;
        }

        foreach (UniversityAchievementDefinition definition in Definitions)
        {
            if (definition.trigger != UniversityAchievementTrigger.SpecificChoice)
            {
                continue;
            }

            if (Matches(definition.targetEventId, eventId) && Matches(definition.targetDirection, direction))
            {
                Unlock(definition);
            }
        }
    }

    void RecordValueEndingInternal(string eventId, valueDefinitions.values valueType, bool triggerOnMaximum)
    {
        foreach (UniversityAchievementDefinition definition in Definitions)
        {
            if (definition.trigger != UniversityAchievementTrigger.ValueEnding)
            {
                continue;
            }

            bool eventMatches = !string.IsNullOrEmpty(definition.targetEventId) && Matches(definition.targetEventId, eventId);
            bool valueMatches = definition.targetValue == valueType && definition.triggerOnMaximum == triggerOnMaximum;
            if (eventMatches || valueMatches)
            {
                Unlock(definition);
            }
        }
    }

    void RecordMainlineFlagInternal(string flagId)
    {
        if (string.IsNullOrEmpty(flagId))
        {
            return;
        }

        foreach (UniversityAchievementDefinition definition in Definitions)
        {
            if (definition.trigger == UniversityAchievementTrigger.MainlineFlag && Matches(definition.targetFlagId, flagId))
            {
                Unlock(definition);
            }
        }

        if (UniversityTrueEndingProgress.AreAllPermanentFlagsSet(UniversityTrueEndingProgress.DefaultRequiredFlags))
        {
            UnlockByTrigger(UniversityAchievementTrigger.AllMainlineFlags);
        }
    }

    void RecordCountInternal(UniversityAchievementTrigger trigger, int count)
    {
        foreach (UniversityAchievementDefinition definition in Definitions)
        {
            if (definition.trigger == trigger && count >= definition.targetCount)
            {
                Unlock(definition);
            }
        }
    }

    void UnlockByTrigger(UniversityAchievementTrigger trigger)
    {
        foreach (UniversityAchievementDefinition definition in Definitions)
        {
            if (definition.trigger == trigger)
            {
                Unlock(definition);
            }
        }
    }

    bool Unlock(UniversityAchievementDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.id) || IsUnlocked(definition.id))
        {
            return false;
        }

        SecurePlayerPrefs.SetBool(UnlockKeyPrefix + definition.id, true);
        SecurePlayerPrefs.SetString(UnlockTimeKeyPrefix + definition.id, System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        PlayerPrefs.Save();

        if (Application.isPlaying)
        {
            pendingPopups.Enqueue(definition);
            if (!popupRoutineRunning)
            {
                StartCoroutine(PlayPopupQueue());
            }
        }

        return true;
    }

    IEnumerator PlayPopupQueue()
    {
        popupRoutineRunning = true;
        while (pendingPopups.Count > 0)
        {
            UniversityAchievementDefinition definition = pendingPopups.Dequeue();
            AchievementsScript popup = AchievementsScript.instance;
            if (popup != null)
            {
                popup.ShowCustomAchievement(definition.title, definition.description, ResolveIcon(definition));
            }

            yield return new WaitForSeconds(2.8f);
        }

        popupRoutineRunning = false;
    }

    void RefreshAchievementPanelInternal(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        Font font = ResolveFont(panel);
        Transform content = ResolveContent(panel.transform);
        if (content == null)
        {
            return;
        }

        ConfigureContent(content);
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        foreach (UniversityAchievementDefinition definition in Definitions)
        {
            CreateAchievementRow(content, definition, font);
        }

        Text progressText = ResolveProgressText(panel);
        if (progressText != null)
        {
            progressText.text = GetUnlockedCount() + "/" + Definitions.Count;
        }
    }

    Transform ResolveContent(Transform panel)
    {
        Transform content = panel.Find("Scroll View/Viewport/Content");
        if (content != null)
        {
            return content;
        }

        ScrollRect scrollRect = panel.GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null && scrollRect.content != null)
        {
            return scrollRect.content;
        }

        return null;
    }

    void ConfigureContent(Transform content)
    {
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.padding = new RectOffset(
            Mathf.RoundToInt(AchievementListPadding),
            Mathf.RoundToInt(AchievementListPadding),
            Mathf.RoundToInt(AchievementListPadding),
            Mathf.RoundToInt(AchievementListPadding));
        layout.spacing = AchievementListSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    void CreateAchievementRow(Transform content, UniversityAchievementDefinition definition, Font font)
    {
        bool unlocked = IsUnlocked(definition.id);
        Color textColor = unlocked ? new Color(0.12f, 0.1f, 0.08f, 1f) : new Color(0.42f, 0.39f, 0.34f, 1f);
        Color rowColor = unlocked ? new Color(1f, 0.96f, 0.86f, 0.95f) : new Color(0.86f, 0.84f, 0.78f, 0.7f);

        GameObject row = new GameObject(definition.id, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        row.transform.SetParent(content, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, AchievementRowHeight);
        row.GetComponent<Image>().color = rowColor;

        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.minHeight = AchievementRowHeight;
        rowLayout.preferredHeight = AchievementRowHeight;
        rowLayout.flexibleWidth = 1f;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconObject.transform.SetParent(row.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        SetAnchoredRect(
            iconRect,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(AchievementRowPaddingHorizontal + AchievementIconSize * 0.5f, 0f),
            new Vector2(AchievementIconSize, AchievementIconSize));
        LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
        iconLayout.minWidth = AchievementIconSize;
        iconLayout.preferredWidth = AchievementIconSize;
        iconLayout.minHeight = AchievementIconSize;
        iconLayout.preferredHeight = AchievementIconSize;
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = ResolveIcon(definition);
        icon.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.32f);

        GameObject textRoot = new GameObject("Texts", typeof(RectTransform));
        textRoot.transform.SetParent(row.transform, false);
        RectTransform textRootRect = textRoot.GetComponent<RectTransform>();
        float textLeft = AchievementRowPaddingHorizontal + AchievementIconSize + AchievementRowSpacing;
        SetStretchRect(textRootRect, textLeft, AchievementRowPaddingHorizontal, AchievementRowPaddingVertical, AchievementRowPaddingVertical);

        Text titleText = CreateText(textRoot.transform, "Title", font, definition.title, AchievementTitleFontSize, FontStyle.Bold, textColor, TextAnchor.UpperLeft);
        SetTopTextRect(titleText.rectTransform, 0f, AchievementTitleHeight);

        Text descriptionText = CreateText(textRoot.transform, "Description", font, definition.description, AchievementDescriptionFontSize, FontStyle.Normal, new Color(textColor.r, textColor.g, textColor.b, unlocked ? 0.92f : 0.68f), TextAnchor.UpperLeft);
        SetTopTextRect(descriptionText.rectTransform, -(AchievementTitleHeight + AchievementTextSpacing), AchievementDescriptionHeight);

        Text statusText = CreateText(textRoot.transform, "Status", font, unlocked ? "已获得" : "未获得", AchievementStatusFontSize, FontStyle.Normal, unlocked ? new Color(0.31f, 0.46f, 0.22f, 1f) : new Color(0.5f, 0.46f, 0.4f, 1f), TextAnchor.LowerLeft);
        SetBottomTextRect(statusText.rectTransform, AchievementStatusHeight);
    }

    void SetAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    void SetStretchRect(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    void SetTopTextRect(RectTransform rect, float y, float height)
    {
        SetAnchoredRect(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(0f, height));
    }

    void SetBottomTextRect(RectTransform rect, float height)
    {
        SetAnchoredRect(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(0f, height));
    }

    Text CreateText(Transform parent, string name, Font font, string value, int size, FontStyle style, Color color, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.text = value;
        return text;
    }

    Text ResolveProgressText(GameObject panel)
    {
        if (AchievementsScript.instance != null && AchievementsScript.instance.achievementProgressText != null)
        {
            return AchievementsScript.instance.achievementProgressText;
        }

        Text[] texts = panel.GetComponentsInChildren<Text>(true);
        foreach (Text text in texts)
        {
            if (text.name.ToLowerInvariant().Contains("progress"))
            {
                return text;
            }
        }

        return null;
    }

    Font ResolveFont(GameObject panel)
    {
        Text text = panel.GetComponentInChildren<Text>(true);
        if (text != null && text.font != null)
        {
            return text.font;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    Sprite ResolveIcon(UniversityAchievementDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.id))
        {
            return null;
        }

        Sprite resourceIcon = ResolveResourceIcon(definition);
        if (resourceIcon != null)
        {
            return resourceIcon;
        }

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(definition.iconAssetPath))
        {
            string iconAssetPath = definition.iconAssetPath;
            if (!iconAssetPath.EndsWith(".png"))
            {
                iconAssetPath = iconAssetPath.TrimEnd('/', '\\') + "/" + definition.id + ".png";
            }

            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconAssetPath);
            if (icon != null)
            {
                return icon;
            }
        }
#endif

        return null;
    }

    Sprite ResolveResourceIcon(UniversityAchievementDefinition definition)
    {
        string resourcePath = DefaultIconResourcePath + definition.id;
        if (IconCache.TryGetValue(resourcePath, out Sprite cachedIcon))
        {
            return cachedIcon;
        }

        Sprite icon = Resources.Load<Sprite>(resourcePath);
        if (icon == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                icon = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        if (icon != null)
        {
            IconCache[resourcePath] = icon;
        }

        return icon;
    }

    string ResolveEventId(GameObject card)
    {
        if (card == null)
        {
            return "";
        }

        UniversityMainlineCardHook mainlineHook = card.GetComponent<UniversityMainlineCardHook>();
        if (mainlineHook != null && !string.IsNullOrEmpty(mainlineHook.cardId))
        {
            return NormalizeEventId(mainlineHook.cardId);
        }

        return NormalizeEventId(card.name);
    }

    string NormalizeEventId(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
        {
            return "";
        }

        string normalized = rawName.Replace("(Clone)", "").Trim();
        if (normalized.StartsWith("US_"))
        {
            normalized = normalized.Substring(3);
        }

        return normalized.ToUpperInvariant();
    }

    bool Matches(string expected, string actual)
    {
        return string.Equals(NormalizeEventId(expected), NormalizeEventId(actual), System.StringComparison.OrdinalIgnoreCase);
    }
}
