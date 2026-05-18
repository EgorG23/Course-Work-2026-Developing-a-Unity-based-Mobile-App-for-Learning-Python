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

            // Safety fallback: if manager path didn't actually show target, force direct activation.
            if (!targetScreen.activeInHierarchy)
            {
                ActivateTargetDirectly();
            }
            return;
        }

        if (PracticeManager.Instance != null)
        {
            PracticeManager.Instance.GoBack();
        }
        else
        {
            Debug.LogWarning("ScreenButton: targetScreen is null and PracticeManager is missing.");
        }
    }

    private void ActivateTargetDirectly()
    {
        if (targetScreen == null)
        {
            return;
        }

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
        if (PracticeManager.Instance != null)
        {
            PracticeManager.Instance.ExitToMenu();
        }
    }

    public void EndPractice()
    {
        if (PracticeManager.Instance != null)
        {
            PracticeManager.Instance.FinishPractice();
        }
    }

}
