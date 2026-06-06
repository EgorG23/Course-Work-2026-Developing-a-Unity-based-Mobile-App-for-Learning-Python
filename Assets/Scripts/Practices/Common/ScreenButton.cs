using UnityEngine;

public class ScreenButton : MonoBehaviour
{
    public GameObject targetScreen;

    private bool navigationManagedExternally;

    public void UseManagedNavigation()
    {
        navigationManagedExternally = true;
        targetScreen = null;
    }

    public void GoToScreen()
    {
        if (navigationManagedExternally)
        {
            return;
        }

        if (PracticeManager.Instance == null)
        {
            Debug.LogWarning("ScreenButton requires PracticeManager in the scene.");
            return;
        }

        if (targetScreen != null)
        {
            PracticeManager.Instance.ShowScreen(targetScreen);
            return;
        }

        PracticeManager.Instance.GoBack();
    }

    public void GoToMenu()
    {
        if (navigationManagedExternally)
        {
            return;
        }

        EndPractice();
    }

    public void EndPractice()
    {
        if (navigationManagedExternally)
        {
            return;
        }

        if (PracticeManager.Instance != null)
        {
            PracticeManager.Instance.FinishPractice();
        }
    }
}
