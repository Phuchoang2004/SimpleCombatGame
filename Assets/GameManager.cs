using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public enum GameMode { None, OneVOne, OneVAI }
    public GameMode SelectedMode = GameMode.None;

    public GameObject menuPanel; 
    public GameObject restartButton; 
    public OpponentAI opponentAI; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ShowMenu();
        if (restartButton != null)
            restartButton.SetActive(false);
    }

    public void Select1v1()
    {
        SelectedMode = GameMode.OneVOne;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (opponentAI != null) opponentAI.enabled = false;
    }

    public void Select1vAI()
    {
        SelectedMode = GameMode.OneVAI;
        if (menuPanel != null) menuPanel.SetActive(false);
        if (opponentAI != null) opponentAI.enabled = true;
    }

    public void ShowMenu()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
    }

    public void OnCharacterDeath()
    {
        if (restartButton != null)
            restartButton.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

