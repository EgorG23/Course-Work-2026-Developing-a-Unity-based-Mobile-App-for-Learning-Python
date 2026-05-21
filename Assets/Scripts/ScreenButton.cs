using UnityEngine;

public class ScreenButton : MonoBehaviour
{
    public GameObject targetScreen;

    public void GoToScreen()
    {
        if (targetScreen != null)
        {
            if (PracticeManager.Instance != null)
            {
                PracticeManager.Instance.ShowScreen(targetScreen);
            }
            else
            {
                ActivateTargetDirectly();
            }
            return;
        }

        if (PracticeManager.Instance != null)
        {
            PracticeManager.Instance.GoBack();
        }
    }

    private void ActivateTargetDirectly()
    {
        if (targetScreen == null) return;

        Transform parent = targetScreen.transform.parent;
        if (parent != null)
        {
            foreach (Transform sibling in parent)
            {
                if (sibling != null && sibling.gameObject != targetScreen)
                {
                    sibling.gameObject.SetActive(false);
                }
            }
        }
        targetScreen.SetActive(true);
    }

    public void GoToMenu()
    {
        EndPractice();
    }

    public void EndPractice()
    {
    Debug.Log("[КНОПКА] Клик зафиксирован. Напрямую отправляю команду в GameManager!");

    if (GameManager.Instance != null)
    {
        GameManager.Instance.CompletePractice();
    }
    else
    {
        Debug.LogError("[КНОПКА] Критическая ошибка: GameManager не найден на сцене!");
    }

    Debug.Log("[КНОПКА] Перехожу на сцену LessonsList.");
    UnityEngine.SceneManagement.SceneManager.LoadScene("LessonsList");
    }
}