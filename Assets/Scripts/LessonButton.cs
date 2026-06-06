using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LessonButton : MonoBehaviour
{
    public List<GameObject> lessonScreens;
    public string practiceSceneName;
    public string lessonId;

    [Header("Индекс урока для Банка (0, 1, 2, 3)")]
    public int lessonIndex;

    [Header("Дочерний объект-замок (Block)")]
    public GameObject lockObject;

    private Button myButton;

    void Start()
    {
        myButton = GetComponent<Button>();

        // Автоматический поиск: если замок не перетащен в инспектор,
        // пытаемся найти дочерний объект с именем "Block" самостоятельно
        if (lockObject == null)
        {
            Transform foundBlock = transform.Find("Block");
            if (foundBlock != null)
            {
                lockObject = foundBlock.gameObject;
            }
        }

        CheckLockState();
    }

    public void CheckLockState()
    {
        // Урок №1 (индекс 0) всегда открыт по умолчанию
        if (lessonIndex == 0)
        {
            Unlock();
            return;
        }

        if (GameManager.Instance != null)
        {
            int previousIndex = lessonIndex - 1;
            
            if (previousIndex >= 0 && previousIndex < GameManager.Instance.practiceCompleted.Length)
            {
                // Если предыдущая практика пройдена -> разблокируем
                if (GameManager.Instance.practiceCompleted[previousIndex])
                {
                    Unlock();
                }
                else
                {
                    Lock();
                }
            }
        }
    }

    private void Unlock()
    {
        if (myButton != null)
        {
            myButton.interactable = true;
            
            // Сбрасываем цвета кнопок в нормальное (яркое) состояние
            ColorBlock colors = myButton.colors;
            colors.normalColor = Color.white;
            myButton.colors = colors;
        }

        // Прячем замок, открывая цветную картинку под ним
        if (lockObject != null)
        {
            lockObject.SetActive(false);
        }
    }

    private void Lock()
    {
        if (myButton != null)
        {
            myButton.interactable = false;

            // Жестко красим заблокированную кнопку в серый цвет через Color Block,
            // чтобы Unity не пыталась сделать её белой
            ColorBlock colors = myButton.colors;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            myButton.colors = colors;
        }

        // Показываем замок (серый слой) поверх дискеты
        if (lockObject != null)
        {
            lockObject.SetActive(true);
        }
    }

    public void OpenLesson()
    {
        if (myButton != null && !myButton.interactable) return;

        if (LessonLoader.Instance == null)
        {
            GameObject go = new GameObject("LessonLoader");
            go.AddComponent<LessonLoader>();
        }

        LessonLoader.Instance.screens = lessonScreens;
        LessonLoader.Instance.practiceScene = practiceSceneName;
        LessonLoader.Instance.lessonId = lessonId;
        LessonLoader.Instance.returnToLastTheoryScreen = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentLessonIndex = lessonIndex;
            Debug.Log($"Переход на урок. Банк переключен на индекс: {lessonIndex}");
        }

        SceneManager.LoadScene("LessonScene");
    }
}
