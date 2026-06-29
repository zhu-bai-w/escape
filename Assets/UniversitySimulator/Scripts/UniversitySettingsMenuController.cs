using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UniversitySettingsMenuController : MonoBehaviour
{
    const string BottomNavigationBarName = "BottomNavigationBar";
    const string SettingsNavButtonName = "SettingsNavButton";
    const string AchievementsNavButtonName = "AchievementsNavButton";
    const string ReturnGameNavButtonName = "ReturnGameNavButton";
    const string ObsoleteQuestsNavButtonName = "QuestsNavButton";
    const float NavigationBarWidth = 980f;
    const float NavigationBarHeight = 84f;
    const float NavigationBarBottomOffset = 24f;
    const float NavigationButtonWidth = 280f;
    const float NavigationButtonHeight = 58f;

    static readonly Color NavigationBarColor = new Color(0.16f, 0.14f, 0.12f, 0.92f);
    static readonly Color NavigationButtonColor = new Color(0.36f, 0.33f, 0.28f, 1f);
    static readonly Color NavigationButtonSelectedColor = new Color(0.93f, 0.68f, 0.05f, 1f);
    static readonly Color NavigationButtonTextColor = new Color(1f, 0.97f, 0.9f, 1f);
    static readonly Color NavigationButtonSelectedTextColor = new Color(0.18f, 0.13f, 0.06f, 1f);

    enum SlotPanelMode
    {
        Save,
        Load
    }

    public GameObject menuPanel;
    public GameObject settingsPanel;
    public GameObject exitPanel;
    public GameObject playerInfoPanel;
    public GameObject achievementsPanel;
    public GameObject questsPanel;
    public GameObject settingsSelectedIcon;
    public GameObject slotSelectPanel;
    public Text slotSelectTitleText;
    public Text slot1ButtonText;
    public Text slot2ButtonText;
    public Text slot3ButtonText;
    public CardStack cardStack;
    public Text saveStatusText;
    public Button achievementsButton;
    public GameObject bottomNavigationBar;
    public Button settingsNavigationButton;
    public Button achievementsNavigationButton;
    public Button returnGameNavigationButton;

    SlotPanelMode currentSlotPanelMode = SlotPanelMode.Save;

    void Start()
    {
        UniversityAchievementSystem.EnsureInstance();
        BindAchievementsButton();
        EnsureBottomNavigationBar();
        UniversityAchievementSystem.RefreshAchievementPanel(achievementsPanel);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OpenSettingsPanel();
        }
    }

    public void OpenSettingsPanel()
    {
        OpenMenuPanel(settingsPanel);
    }

    public void OpenAchievementsPanel()
    {
        OpenMenuPanel(achievementsPanel);
        UniversityAchievementSystem.RefreshAchievementPanel(achievementsPanel);
    }

    public void ReturnToGame()
    {
        CloseSlotPanel();
        SetActive(settingsPanel, false);
        SetActive(exitPanel, false);
        SetActive(playerInfoPanel, false);
        SetActive(achievementsPanel, false);
        SetActive(questsPanel, false);
        SetActive(settingsSelectedIcon, false);
        SetActive(bottomNavigationBar, false);
        SetActive(menuPanel, false);
        SetCardMoveEnabled(true);
    }

    public void SaveCurrentProgress()
    {
        OpenSaveSlotPanel();
    }

    public void LoadCurrentProgress()
    {
        OpenLoadSlotPanel();
    }

    public void OpenSaveSlotPanel()
    {
        OpenSlotPanel(SlotPanelMode.Save);
    }

    public void OpenLoadSlotPanel()
    {
        OpenSlotPanel(SlotPanelMode.Load);
    }

    public void SaveToSlot1()
    {
        SaveToSlot(1);
    }

    public void SaveToSlot2()
    {
        SaveToSlot(2);
    }

    public void SaveToSlot3()
    {
        SaveToSlot(3);
    }

    public void ChooseSlot1()
    {
        ChooseSlot(1);
    }

    public void ChooseSlot2()
    {
        ChooseSlot(2);
    }

    public void ChooseSlot3()
    {
        ChooseSlot(3);
    }

    public void LoadFromSlot1()
    {
        LoadFromSlot(1);
    }

    public void LoadFromSlot2()
    {
        LoadFromSlot(2);
    }

    public void LoadFromSlot3()
    {
        LoadFromSlot(3);
    }

    public void CloseSlotPanel()
    {
        SetActive(slotSelectPanel, false);
    }

    public void ShowExitConfirmation()
    {
        CloseSlotPanel();
        SetActive(exitPanel, true);
    }

    void ChooseSlot(int slotNumber)
    {
        if (currentSlotPanelMode == SlotPanelMode.Save)
        {
            SaveToSlot(slotNumber);
            return;
        }

        LoadFromSlot(slotNumber);
    }

    void OpenSlotPanel(SlotPanelMode mode)
    {
        currentSlotPanelMode = mode;
        SetActive(slotSelectPanel, true);

        if (slotSelectTitleText != null)
        {
            slotSelectTitleText.text = mode == SlotPanelMode.Save
                ? "\u9009\u62e9\u5b58\u6863\u69fd\u4f4d"
                : "\u9009\u62e9\u8bfb\u6863\u69fd\u4f4d";
        }

        UpdateSlotButtonText(slot1ButtonText, 1);
        UpdateSlotButtonText(slot2ButtonText, 2);
        UpdateSlotButtonText(slot3ButtonText, 3);
    }

    void UpdateSlotButtonText(Text text, int slotNumber)
    {
        if (text == null)
        {
            return;
        }

        UniversityManualSaveSystem.SlotInfo slotInfo = UniversityManualSaveSystem.GetSlotInfo(slotNumber);
        string suffix = "";
        if (slotInfo.hasSave)
        {
            suffix = currentSlotPanelMode == SlotPanelMode.Save ? "\uff08\u5df2\u6709\uff09" : "";
        }
        else if (currentSlotPanelMode == SlotPanelMode.Load)
        {
            suffix = "\uff08\u7a7a\uff09";
        }

        text.text = "\u69fd\u4f4d " + slotNumber + suffix;
    }

    void SaveToSlot(int slotNumber)
    {
        UniversityManualSaveSystem.SaveCurrentProgress(slotNumber);
        SetStatus("\u5df2\u5b58\u6863\u5230\u69fd\u4f4d " + slotNumber);
        CloseSlotPanel();
    }

    void LoadFromSlot(int slotNumber)
    {
        if (!UniversityManualSaveSystem.HasManualSave(slotNumber))
        {
            SetStatus("\u69fd\u4f4d " + slotNumber + " \u6682\u65e0\u5b58\u6863");
            UpdateSlotButtonText(slot1ButtonText, 1);
            UpdateSlotButtonText(slot2ButtonText, 2);
            UpdateSlotButtonText(slot3ButtonText, 3);
            return;
        }

        if (!UniversityManualSaveSystem.TryLoadCurrentProgress(slotNumber))
        {
            SetStatus("\u8bfb\u6863\u5931\u8d25");
            return;
        }

        SetStatus("\u6b63\u5728\u8bfb\u53d6\u69fd\u4f4d " + slotNumber + "...");
        CloseSlotPanel();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void SetStatus(string message)
    {
        if (saveStatusText != null)
        {
            saveStatusText.text = message;
        }
    }

    void SetCardMoveEnabled(bool enabled)
    {
        if (cardStack == null)
        {
            cardStack = CardStack.instance;
        }

        if (cardStack != null)
        {
            cardStack.setCardMoveEnable(enabled);
        }
    }

    void BindAchievementsButton()
    {
        if (achievementsButton == null)
        {
            achievementsButton = FindButtonInMenu("AchievementsButton");
        }

        if (achievementsButton != null)
        {
            achievementsButton.onClick.RemoveListener(OpenAchievementsPanel);
            achievementsButton.onClick.AddListener(OpenAchievementsPanel);
        }
    }

    void OpenMenuPanel(GameObject activePanel)
    {
        SetActive(menuPanel, true);
        SetActive(settingsPanel, activePanel == settingsPanel);
        SetActive(exitPanel, false);
        SetActive(playerInfoPanel, false);
        SetActive(achievementsPanel, activePanel == achievementsPanel);
        SetActive(questsPanel, false);
        SetActive(settingsSelectedIcon, activePanel == settingsPanel);
        EnsureBottomNavigationBar();
        SetActive(bottomNavigationBar, true);
        UpdateNavigationButtonStates(activePanel);
        CloseSlotPanel();
        SetCardMoveEnabled(false);
    }

    void EnsureBottomNavigationBar()
    {
        if (menuPanel == null)
        {
            return;
        }

        if (bottomNavigationBar == null)
        {
            Transform existingBar = menuPanel.transform.Find(BottomNavigationBarName);
            bottomNavigationBar = existingBar != null ? existingBar.gameObject : CreateBottomNavigationBar();
        }

        if (bottomNavigationBar == null)
        {
            return;
        }

        settingsNavigationButton = ResolveNavigationButton(SettingsNavButtonName, "\u8bbe\u7f6e", OpenSettingsPanel);
        achievementsNavigationButton = ResolveNavigationButton(AchievementsNavButtonName, "\u6210\u5c31", OpenAchievementsPanel);
        RemoveObsoleteNavigationButton(ObsoleteQuestsNavButtonName);
        returnGameNavigationButton = ResolveNavigationButton(ReturnGameNavButtonName, "\u8fd4\u56de\u6e38\u620f", ReturnToGame);
        bottomNavigationBar.transform.SetAsLastSibling();
    }

    GameObject CreateBottomNavigationBar()
    {
        GameObject bar = new GameObject(BottomNavigationBarName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
        bar.transform.SetParent(menuPanel.transform, false);

        RectTransform rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, NavigationBarBottomOffset);
        rect.sizeDelta = new Vector2(NavigationBarWidth, NavigationBarHeight);

        Image image = bar.GetComponent<Image>();
        image.color = NavigationBarColor;

        HorizontalLayoutGroup layout = bar.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 12, 12);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return bar;
    }

    Button ResolveNavigationButton(string buttonName, string label, UnityAction action)
    {
        Button button = null;
        Transform existingButton = bottomNavigationBar.transform.Find(buttonName);
        if (existingButton != null)
        {
            button = existingButton.GetComponent<Button>();
        }

        if (button == null)
        {
            button = CreateNavigationButton(buttonName, label);
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
        SetNavigationButtonLabel(button, label, false);
        return button;
    }

    Button CreateNavigationButton(string buttonName, string label)
    {
        GameObject buttonObject = new GameObject(buttonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(bottomNavigationBar.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(NavigationButtonWidth, NavigationButtonHeight);

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.minWidth = NavigationButtonWidth;
        layout.preferredWidth = NavigationButtonWidth;
        layout.minHeight = NavigationButtonHeight;
        layout.preferredHeight = NavigationButtonHeight;

        Image image = buttonObject.GetComponent<Image>();
        image.color = NavigationButtonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.color = NavigationButtonTextColor;
        text.text = label;

        return button;
    }

    void UpdateNavigationButtonStates(GameObject activePanel)
    {
        SetNavigationButtonState(settingsNavigationButton, activePanel == settingsPanel, settingsPanel != null);
        SetNavigationButtonState(achievementsNavigationButton, activePanel == achievementsPanel, achievementsPanel != null);
        SetNavigationButtonState(returnGameNavigationButton, false, true);
    }

    void RemoveObsoleteNavigationButton(string buttonName)
    {
        if (bottomNavigationBar == null)
        {
            return;
        }

        Transform obsoleteButton = bottomNavigationBar.transform.Find(buttonName);
        if (obsoleteButton == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(obsoleteButton.gameObject);
        }
        else
        {
            DestroyImmediate(obsoleteButton.gameObject);
        }
    }

    void SetNavigationButtonState(Button button, bool selected, bool visible)
    {
        if (button == null)
        {
            return;
        }

        button.gameObject.SetActive(visible);
        button.interactable = visible && !selected;

        Color targetColor = selected ? NavigationButtonSelectedColor : NavigationButtonColor;
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = targetColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = targetColor;
        colors.highlightedColor = selected ? targetColor : Color.Lerp(targetColor, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(targetColor, Color.black, 0.16f);
        colors.selectedColor = targetColor;
        colors.disabledColor = targetColor;
        button.colors = colors;

        SetNavigationButtonLabel(button, null, selected);
    }

    void SetNavigationButtonLabel(Button button, string label, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Text text = button.GetComponentInChildren<Text>(true);
        if (text == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(label))
        {
            text.text = label;
        }

        text.color = selected ? NavigationButtonSelectedTextColor : NavigationButtonTextColor;
    }

    Button FindButtonInMenu(string buttonName)
    {
        if (menuPanel != null)
        {
            Button[] menuButtons = menuPanel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < menuButtons.Length; i++)
            {
                if (menuButtons[i] != null && menuButtons[i].name == buttonName)
                {
                    return menuButtons[i];
                }
            }
        }

        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
        for (int i = 0; i < allButtons.Length; i++)
        {
            Button button = allButtons[i];
            if (button != null && button.name == buttonName && button.gameObject.scene.IsValid())
            {
                return button;
            }
        }

        return null;
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
