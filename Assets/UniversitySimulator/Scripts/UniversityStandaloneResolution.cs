using System.Collections;
using UnityEngine;

public sealed class UniversityStandaloneResolution : MonoBehaviour
{
    private const int TargetWidth = 1469;
    private const int TargetHeight = 791;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
#if UNITY_STANDALONE && !UNITY_EDITOR
        ApplyResolution();

        var runner = new GameObject(nameof(UniversityStandaloneResolution));
        DontDestroyOnLoad(runner);
        runner.hideFlags = HideFlags.HideAndDontSave;
        runner.AddComponent<UniversityStandaloneResolution>();
#endif
    }

    private IEnumerator Start()
    {
        ApplyResolution();
        yield return null;
        ApplyResolution();
        Debug.Log($"Standalone resolution locked to {Screen.width}x{Screen.height} ({Screen.fullScreenMode}).");
        Destroy(gameObject);
    }

    private static void ApplyResolution()
    {
        Screen.SetResolution(TargetWidth, TargetHeight, FullScreenMode.Windowed);
    }
}
