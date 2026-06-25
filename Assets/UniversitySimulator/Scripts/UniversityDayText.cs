using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Text))]
public class UniversityDayText : MonoBehaviour
{
    [SerializeField] Text targetText;
    [SerializeField] string dayFormat = "\u7b2c{0}\u5929";

    int lastDisplayedDays = int.MinValue;
    string lastDisplayedText = "";

    void Reset()
    {
        targetText = GetComponent<Text>();
    }

    void Awake()
    {
        EnsureTargetText();
    }

    void OnEnable()
    {
        UniversityTrueEndingProgress.OnCurrentRunDaysChanged += UpdateDayText;
        UpdateDayText(UniversityTrueEndingProgress.CurrentRunDays);
    }

    void OnDisable()
    {
        UniversityTrueEndingProgress.OnCurrentRunDaysChanged -= UpdateDayText;
    }

    void LateUpdate()
    {
        UpdateDayText(UniversityTrueEndingProgress.CurrentRunDays);
    }

    void UpdateDayText(int days)
    {
        EnsureTargetText();
        if (targetText == null)
        {
            return;
        }

        if (lastDisplayedDays != days || string.IsNullOrEmpty(lastDisplayedText))
        {
            lastDisplayedText = string.Format(dayFormat, days);
            lastDisplayedDays = days;
        }

        if (targetText.text == lastDisplayedText)
        {
            return;
        }

        targetText.text = lastDisplayedText;
    }

    void EnsureTargetText()
    {
        if (targetText == null)
        {
            targetText = GetComponent<Text>();
        }
    }
}
