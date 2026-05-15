using UnityEngine;
using UnityEngine.SceneManagement;

public class Door : MonoBehaviour
{
    public GameObject successScreen;
    public GameObject noKeyScreen;
    public bool finishPracticeIfNoSuccessScreen = true;
    public string fallbackSceneName = "LessonsList";

    public void TryOpen()
    {
        bool hasKey = QuestManager.Instance != null && QuestManager.Instance.hasKey;

        if (hasKey)
        {
            Debug.Log("Door opened");

            if (PracticeManager.Instance != null && successScreen != null)
            {
                PracticeManager.Instance.ShowScreen(successScreen);
                return;
            }

            if (finishPracticeIfNoSuccessScreen)
            {
                if (PracticeManager.Instance != null)
                {
                    PracticeManager.Instance.FinishPractice();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(fallbackSceneName))
                {
                    SceneManager.LoadScene(fallbackSceneName);
                }
            }
        }
        else
        {
            Debug.Log("Key required");

            if (PracticeManager.Instance != null && noKeyScreen != null)
            {
                PracticeManager.Instance.ShowScreen(noKeyScreen);
            }
        }
    }
}
