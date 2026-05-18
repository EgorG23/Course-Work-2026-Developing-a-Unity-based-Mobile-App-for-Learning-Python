using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public bool foundClue = false;
    public bool knowsCommand = false;
    public bool knowsVersion = false;
    public bool hasKey = false;
    public string pythonVersion = "";
    public bool powerFixed = false;
    public bool codeSolved = false;

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
        pythonVersion = "";
        powerFixed = false;
        codeSolved = false;
    }
}
