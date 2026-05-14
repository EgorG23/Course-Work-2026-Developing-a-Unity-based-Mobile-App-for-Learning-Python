using UnityEngine;

public class ScreenButton : MonoBehaviour
{
    public GameObject targetScreen;

    public void GoToScreen()
    {
        PracticeManager.Instance.ShowScreen(targetScreen);
    }

    public void GoToMenu()
    {
        PracticeManager.Instance.ExitToMenu();
    }

    public void EndPractice()
    {
        PracticeManager.Instance.FinishPractice();
    }

}