using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OpenAssistantTrigger : MonoBehaviour

{
    public static string BeforeAssistantScene = "LessonsList"; 
    public static string SavedTopic = "Введение";
    public static string SavedSectionType = "Теория";
    public static string SavedTaskDescription = "Изучение базовых понятий Python.";
    
    [Header("Настройки сцены чата")]
    public string assistantSceneName = "Vorona"; 

    [Header("Контекст этой локации/кнопки для Вороны")]
    public string topicName = "Введение";
    public string sectionType = "Теория";
    [TextArea(3, 5)]
    public string taskDescription = "Изучение базовых понятий Python.";

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(ExecuteTransition);
        }
    }


    public void ExecuteTransition()
    {
        AssistantManager.BeforeAssistantScene = SceneManager.GetActiveScene().name;

        AssistantManager.SavedTopic = topicName;
        AssistantManager.SavedSectionType = sectionType;
        AssistantManager.SavedTaskDescription = taskDescription;

        SceneManager.LoadScene(assistantSceneName);
    }
}