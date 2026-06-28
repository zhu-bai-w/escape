using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UniversitySettingsMenuController : MonoBehaviour
{
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

    SlotPanelMode currentSlotPanelMode = SlotPanelMode.Save;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OpenSettingsPanel();
        }
    }

    public void OpenSettingsPanel()
    {
        SetActive(menuPanel, true);
        SetActive(settingsPanel, true);
        SetActive(exitPanel, false);
        SetActive(playerInfoPanel, false);
        SetActive(achievementsPanel, false);
        SetActive(questsPanel, false);
        SetActive(settingsSelectedIcon, true);
        CloseSlotPanel();
        SetCardMoveEnabled(false);
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

    static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
