using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PracticeManager : MonoBehaviour
{
    public static PracticeManager Instance;

    public Transform screenContainer;

    public GameObject startScreen;
    private GameObject currentScreen;
    private Stack<GameObject> history = new Stack<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (startScreen != null)
        {
            ShowScreen(startScreen);
        }
        else
        {
            Debug.LogError("startScreen нету");
        }
    }

    public void ShowScreen(GameObject screenPrefab)
    {
        if (currentScreen != null)
        {
            history.Push(currentScreen);
            currentScreen.SetActive(false);
        }

        currentScreen = Instantiate(screenPrefab, screenContainer);
    }

    public void GoBack()
    {
        if (currentScreen != null)
            Destroy(currentScreen);

        if (history.Count > 0)
        {
            currentScreen = history.Pop();
            currentScreen.SetActive(true);
        }
    }

    public void FinishPractice()
    {
        GameManager.Instance.practiceCompleted[
            GameManager.Instance.currentLessonIndex
        ] = true;

        SceneManager.LoadScene("LessonsList");
    }

    public void ExitToMenu()
    {
        GameManager.Instance.practiceCompleted[
            GameManager.Instance.currentLessonIndex
        ] = false;

        SceneManager.LoadScene("LessonsList");
    }
}