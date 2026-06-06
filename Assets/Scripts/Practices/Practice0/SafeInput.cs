using UnityEngine;
using TMPro;

public class SafeInput : MonoBehaviour
{
    public TMP_Text displayText;
    public GameObject successScreen;

    private string currentCode = "";

    public void AddNumber(string num)
    {
        if (currentCode.Length >= 4)
        {
            return;
        }

        currentCode += num;
        displayText.text = currentCode;
    }

    public void Clear()
    {
        currentCode = "";
        displayText.text = "";
    }

    public void Check()
    {
        if (currentCode == "3114")
        {
            Debug.Log("Safe opened");
            PracticeManager.Instance.ShowScreen(successScreen);
        }
        else
        {
            Debug.Log("Wrong safe code");
            Clear();
        }
    }
}
