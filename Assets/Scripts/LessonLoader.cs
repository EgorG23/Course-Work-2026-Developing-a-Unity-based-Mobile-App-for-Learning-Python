using UnityEngine;
using System.Collections.Generic;

public class LessonLoader : MonoBehaviour
{
    public static LessonLoader Instance;

    public List<GameObject> screens;
    public string practiceScene;
    public GameObject practicePrefab;

    public string lessonId;

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