using UnityEngine;

public class UniversityTrueEndingReturnToNewGame : MonoBehaviour
{
    public bool reloadScene = true;

    public void ReturnToNewGame()
    {
        if (GameStateManager.instance == null)
        {
            Debug.LogWarning("True ending return requested, but GameStateManager is missing.");
            return;
        }

        GameStateManager.instance.RestartAsNewGame(reloadScene);
    }
}
