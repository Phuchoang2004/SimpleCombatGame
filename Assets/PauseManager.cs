using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pauseWindow;
    public KeyCode pauseKey = KeyCode.Escape;
    private bool isPaused = false;
    private Canvas pauseCanvas;
    private float pausePlaneDistance = 1f;
    private float unpausePlaneDistance = 1000f;

    void Start()
    {
        if (pauseWindow != null)
            pauseCanvas = pauseWindow.GetComponent<Canvas>();
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pauseWindow.SetActive(isPaused);
        Time.timeScale = isPaused ? 0 : 1;
        pauseCanvas.planeDistance = isPaused ? pausePlaneDistance : unpausePlaneDistance;

    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseWindow.SetActive(false);
        Time.timeScale = 1;
        pauseCanvas.planeDistance = unpausePlaneDistance;
    }
}