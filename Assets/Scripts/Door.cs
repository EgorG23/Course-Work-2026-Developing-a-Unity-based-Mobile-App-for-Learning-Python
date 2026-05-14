using UnityEngine;

public class Door : MonoBehaviour
{
    public GameObject successScreen;
    public GameObject noKeyScreen;

    public void TryOpen()
    {
        bool hasKey = QuestManager.Instance != null && QuestManager.Instance.hasKey;

        if (hasKey)
        {
            Debug.Log("Door opened");

            if (PracticeManager.Instance != null && successScreen != null)
            {
                PracticeManager.Instance.ShowScreen(successScreen);
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
