using UnityEngine;
using UnityEngine.SceneManagement;

public class LessonPrefabButton : MonoBehaviour
{
    public enum ActionType
    {
        Next,
        Back,
        StartPractice,
        GoToMenu
    }

    public ActionType action;

    private LessonManager manager;

    void Start()
    {
        manager = Object.FindFirstObjectByType<LessonManager>();
    }

    public void OnClick()
    {
        if (manager == null)
        {
            Debug.LogError("Нет LM");
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