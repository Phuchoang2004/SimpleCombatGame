using UnityEngine;
using UnityEngine.SceneManagement;

public class WholeManager : MonoBehaviour
{
    public static WholeManager Instance;

    public enum GameMode { PvP, PvE }
    public GameMode gameMode;
    public string player1Character;
    public string player2Character;
    public string aiDifficulty;

    [Header("UI Panels")]
    public GameObject characterSelectPanel;
    public GameObject difficultySelectPanel;

    [Header("Scene Name")]
    public string gameplaySceneName = "SampleScene";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnPvPButton()
    {
        gameMode = GameMode.PvP;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnPvEButton()
    {
        gameMode = GameMode.PvE;
        if (characterSelectPanel != null)
        {
            characterSelectPanel.SetActive(true);
        }
        if (difficultySelectPanel != null)
        {
            difficultySelectPanel.SetActive(false);
        }
    }

    public void OnCharacterSelected(string charID)
    {
        player1Character = charID;
        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(false);
        if (difficultySelectPanel != null)
            difficultySelectPanel.SetActive(true);
    }

    public void OnDifficultySelected(string difficulty)
    {
        aiDifficulty = difficulty;
        player2Character = "AI"; 
        if (difficultySelectPanel != null)
            difficultySelectPanel.SetActive(false);
        SceneManager.LoadScene(gameplaySceneName);
    }
}