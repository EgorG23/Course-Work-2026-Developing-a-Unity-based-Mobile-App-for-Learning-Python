using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public bool foundClue = false;
    public bool knowsCommand = false;
    public bool knowsVersion = false;
    public bool hasKey = false;
    public string pythonVersion = "";

    void Awake()
    {
        Instance = this;
    }

    public void ResetQuest()
    {
        foundClue = false;
        knowsCommand = false;
        knowsVersion = false;
        hasKey = false;
    }
}