using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LessonButton : MonoBehaviour
{
    public List<GameObject> lessonScreens;
    public string practiceSceneName;
    public string lessonId;

    [Header("Индекс урока для Банка (0, 1, 2, 3)")]
    public int lessonIndex;

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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentLessonIndex = lessonIndex;
            Debug.Log($"Переход на урок. Банк переключен на индекс: {lessonIndex}");
        }

        SceneManager.LoadScene("LessonScene");
    }
}