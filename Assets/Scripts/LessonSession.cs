using UnityEngine;
using System.Collections.Generic;

public class LessonSession : MonoBehaviour
{
    public static LessonSession Instance;

    public List<GameObject> screens;
    public string practiceScene;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}