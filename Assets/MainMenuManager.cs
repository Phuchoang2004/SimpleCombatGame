using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public GameObject mainMenuCanvas;
    private Canvas menuCanvas;
    private float menuPlaneDistance = 1f;
    private float hidePlaneDistance = 1000f;
    public Button TutorialButton;

    void Start()
    {
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(true);
            menuCanvas = mainMenuCanvas.GetComponent<Canvas>();
            menuCanvas.planeDistance = hidePlaneDistance;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetMenuPlaneDistanceFar();
        }
        //if the TutorialButton is clicked, set the menuPlaneDistance to hidePlaneDistance
        if (TutorialButton != null && TutorialButton.onClick != null)
        {
            TutorialButton.onClick.AddListener(SetMenuPlaneDistanceClose);
        }
    }

    public void SetMenuPlaneDistanceClose()
    {
        menuCanvas.planeDistance = menuPlaneDistance;
    }

    public void SetMenuPlaneDistanceFar()
    {
        menuCanvas.planeDistance = hidePlaneDistance;
    }

    public void StartGame()
    {
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(false);
            SetMenuPlaneDistanceFar();
        }
        Time.timeScale = 1;
    }

    public void ShowMenu()
    {
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(true);
            SetMenuPlaneDistanceClose();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}