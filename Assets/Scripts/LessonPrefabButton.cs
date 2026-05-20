using UnityEngine;
using UnityEngine.SceneManagement;

public class LessonPrefabButton : MonoBehaviour
{
    public enum ActionType
    {
        Next,
        Back,
        StartPractice,
        GoToMenu,
        FinishPractice // <-- Добавили действие для кнопки "Далее" на экране победы
    }

    public ActionType action;

    private LessonManager manager;

    void Start()
    {
        manager = Object.FindFirstObjectByType<LessonManager>();
    }

    public void OnClick()
    {
        // Для завершения практики LessonManager не нужен, поэтому обрабатываем FinishPractice отдельно
        if (action == ActionType.FinishPractice)
        {
            if (PracticeManager.Instance != null)
            {
                PracticeManager.Instance.FinishPractice();
            }
            else
            {
                SceneManager.LoadScene("LessonsList");
            }
            return;
        }

        // Для остальных действий проверяем наличие LessonManager
        if (manager == null)
        {
            Debug.LogError("Нет LM (LessonManager)");
            return;
        }

        switch (action)
        {
            case ActionType.Next:
                manager.Next();
                break;

            case ActionType.Back:
                manager.Back();
                break;

            case ActionType.StartPractice:
                manager.StartPractice();
                break;

            case ActionType.GoToMenu:
                SceneManager.LoadScene("LessonsList");
                break;
        }
    }
}