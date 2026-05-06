using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LessonManager : MonoBehaviour
{
    public Transform screenContainer;
    public GlitchEffect glitchEffect;

    private List<GameObject> screens;
    private GameObject currentScreen;
    private int currentIndex;

    void Start()
    {
        if (LessonLoader.Instance == null || LessonLoader.Instance.screens == null)
        {
            Debug.LogError("Нет префабов урока");
            return;
        }

        screens = LessonLoader.Instance.screens;
        ShowScreen(0);
    }

    public void ShowScreen(int index)
    {
        if (index < 0 || index >= screens.Count) return;

        currentIndex = index;

        if (currentScreen != null)
            Destroy(currentScreen);

        currentScreen = Instantiate(screens[index], screenContainer);

        glitchEffect?.TriggerGlitch();
    }

    public void Next()
    {
        if (currentIndex + 1 < screens.Count)
        {
            ShowScreen(currentIndex + 1);
        }
        else
        {
            LoadPractice();
        }
    }

    public void Back()
    {
        if (currentIndex - 1 >= 0)
        {
            ShowScreen(currentIndex - 1);
        }
        else
        {
            SceneManager.LoadScene("LessonsList");
        }
    }

    public void StartPractice()
    {
        LoadPractice();
    }

    void LoadPractice()
    {
        SceneManager.LoadScene(LessonLoader.Instance.practiceScene);
    }
}