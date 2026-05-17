using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButton : MonoBehaviour
{
    public static BackButton Instance;
    public string scene;

    public void GoToScene()
    {
        SceneManager.LoadScene(scene);
    }
}