using UnityEngine;
using TMPro;

public class Terminal : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text outputText;

    public void CheckCommand()
    {
        string cmd = inputField.text.Trim();

        if (cmd == "python --version")
        {
            outputText.text = "<color=#00FF00>> Python 3.11.4</color>";

            QuestManager.Instance.knowsVersion = true;
            QuestManager.Instance.pythonVersion = "3.11";
        }
        else
        {
            outputText.text = "<color=red>> Command not found</color>";
        }

        inputField.text = "";
    }
}