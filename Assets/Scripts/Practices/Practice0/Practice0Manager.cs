using UnityEngine;

public class Practice0Manager : MonoBehaviour
{
    public static Practice0Manager Instance { get; private set; }

    public bool HasKey { get; private set; }

    private void Awake()
    {
        Instance = this;
        if (PracticeManager.Instance != null)
        {
            PracticeManager.Instance.lessonIndex = 0;
        }
    }

    public void ResetProgress()
    {
        HasKey = false;
    }

    public bool TakeKey()
    {
        if (HasKey)
        {
            return false;
        }

        HasKey = true;
        return true;
    }
}
