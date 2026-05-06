using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    public void GoToMenu()
    {
        SceneManager.LoadScene("LessonsList");
    }
}