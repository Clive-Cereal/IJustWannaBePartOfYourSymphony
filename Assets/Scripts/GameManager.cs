using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.IO;

public class GameManager : MonoBehaviour, ISaveable
{
    public static GameManager Instance {get; private set;}
    [HideInInspector] public static GameState currentState = GameState.Init; //this is one to be set
    [Header("Uncheck this if you are testing from non-init scene")]
    [SerializeField] private bool initialiseOnStart = true;

    public static GameState currentGameState => currentState; //no touchy read only
    public static string targetScene;
    public static GameState targetState;


    private void Awake() 
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!initialiseOnStart) currentState = GameState.Playing;
    }

    void Update()
    {
        if (initialiseOnStart && currentState == GameState.Init)
        {
            Initialise();
        }
        Debug.Log("Current Game State : " + currentGameState.ToString());
    }

//---------------------------------------------------------------------

    void Initialise()
    {
        if(currentState == GameState.Init)
        {
            SceneLoader("01_Menu", GameState.Menu);
        }
    }

    public void SceneLoader(string sceneName, GameState stateName) //To use this : eg. GameManager.Instance.SceneLoader("desiredscenename", GameState.desiredstate);
    {
        targetScene = sceneName;
        targetState = stateName;

        SceneManager.LoadScene("_Loading");
    }

//-------------------FOR UI BUTTONS------------------------------------

    public void StartNewGame()
    {
        SceneLoader("02_Main", GameState.Playing);
    }

    public void ContinueGame()
    {
        SaveManager.Instance.LoadGame();
        SceneLoader("02_Main", GameState.Playing);
    }

    public void LoadMenuScene()
    {
        SceneLoader("01_Menu", GameState.Menu);
    }
    
    public void ExitGame()
    {
        Application.Quit();
    }

//---------------------------------------------------------------------

    public void OnSave(SaveData data)
    {

    }

    public void OnLoad(SaveData data)
    {

    }

    public void ConsoleMessage(string message)
    {
        Debug.Log(message);
    }
}