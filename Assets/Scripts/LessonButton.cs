using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LessonButton : MonoBehaviour
{
    public List<GameObject> lessonScreens;
    public string practiceSceneName;
    public string lessonId;

    public void OpenLesson()
    {
        if (LessonLoader.Instance == null)
        {
            GameObject go = new GameObject("LessonLoader");
            go.AddComponent<LessonLoader>();
        }

        LessonLoader.Instance.screens = lessonScreens;
        LessonLoader.Instance.practiceScene = practiceSceneName;
        LessonLoader.Instance.lessonId = lessonId;

        SceneManager.LoadScene("LessonScene");
    }
}