using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour {

	[System.Serializable] public class mEvent : UnityEvent {}

	//count the swipes to create the event for the first swipe in game for switching the menu
	[HideInInspector] public int swipeCounter = 0;

	public static GameStateManager instance;

	public enum Gamestate
	{
		idle,
		gameActive,
		gameOver
	}

	[Tooltip("Actual state of the game.")]
	[ReadOnlyInspector] public Gamestate gamestate = Gamestate.idle;

	void loadGameState(){
		gamestate  = (Gamestate)PlayerPrefs.GetInt ("GameState") ;
	}

	void saveGameState(){
		PlayerPrefs.SetInt("GameState",(int)gamestate);
	}


	void Awake(){
		instance = this;
		loadGameState ();
	}

	// Use this for initialization
	void Start () {
		StartCoroutine (OneFrameDelayStartup ());
	}

	IEnumerator OneFrameDelayStartup(){

		//because of Awake-instance linking and registering within startup,
		//we need at least one frame delay to start the game.

		yield return null;
		yield return null;
		GameStartup ();
	}


	void GameStartup(){
		//if we start with a gameover from the last game the game goes to idle.
		if (gamestate == Gamestate.gameOver) {
			gamestate = Gamestate.idle;
		}

		//if we are idle we trigger the start of a new game
		if (gamestate == Gamestate.idle) {
			StartGame ();
		}
	}
		
	public void executeGameover(){
		gamestate = Gamestate.gameOver;
        UniversityTrueEndingProgress.ResetCurrentRunDays();

        //Debug.LogWarning("executeGameover");

		valueManager.instance.saveAllMinMaxValues ();			//save min and max values for all values for the statistics tab
		CardStack.instance.resetCardStack ();					//reset the card stack
        CardStack.instance.clearFollowUpStack();

		saveGameState ();
		string currentSceneName = SceneManager.GetActiveScene ().name;

        OnGameOver.Invoke();

		SceneManager.LoadScene (currentSceneName);						//reload the scene for a clean startup of the game
	}

    public void RestartAsNewGame()
    {
        RestartAsNewGame(true);
    }

    public void RestartAsNewGame(bool reloadScene)
    {
        gamestate = Gamestate.idle;
        UniversityTrueEndingProgress.ResetCurrentRunDays();

        if (CardStack.instance != null)
        {
            CardStack.instance.resetCardStack();
            CardStack.instance.clearFollowUpStack();
        }

        saveGameState();

        if (reloadScene)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }
    }

	public mEvent OnNewGame;
    public mEvent OnGameOver;
	public mEvent OnFirstSwipe;

	public void swipe(){
		swipeCounter++;

		if (swipeCounter == 1) {
			OnFirstSwipe.Invoke ();
		}
	}


	void StartGame(){
		swipeCounter = 0;
		if (gamestate == Gamestate.idle) {
            UniversityTrueEndingProgress.ResetCurrentRunDays();

			//do game start preparations
			OnNewGame.Invoke();

            if (CountryNameGenerator.instance != null)
            {
                CountryNameGenerator.instance.actualizeTexts(true);
                GenderGenerator.instance.actualizeUI();
            }


			gamestate = Gamestate.gameActive;
			saveGameState ();
		}
	}

	void OnDestroy(){
		saveGameState ();
	}

}
