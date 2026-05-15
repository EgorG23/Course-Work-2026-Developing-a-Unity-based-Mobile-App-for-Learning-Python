using UnityEngine;
using TMPro;
using System;

public class Terminal : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text outputText;

    public void CheckCommand()
    {
        if (inputField == null || outputText == null)
        {
            Debug.LogWarning("Terminal is not wired with input/output references.");
            return;
        }

        string cmd = inputField.text ?? string.Empty;

        if (IsCorrectProgram(cmd))
        {
            outputText.text = "<color=#00FF00>> Доступ разрешен</color>";

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.codeSolved = true;
            }
        }
        else
        {
            outputText.text = "<color=red>> Неверный код</color>";
        }

        inputField.text = "";
    }

    public static bool IsCorrectProgram(string code)
    {
        if (code == null)
        {
            return false;
        }

        string normalized = NormalizeCode(code);
        string[] rawLines = normalized.Split('\n', StringSplitOptions.None);
        int lastNonEmpty = rawLines.Length - 1;
        while (lastNonEmpty >= 0 && string.IsNullOrWhiteSpace(rawLines[lastNonEmpty]))
        {
            lastNonEmpty--;
        }

        if (lastNonEmpty < 0)
        {
            return false;
        }

        string[] lines = new string[lastNonEmpty + 1];
        Array.Copy(rawLines, lines, lastNonEmpty + 1);

        if (lines.Length != 4)
        {
            return false;
        }

        return lines[0].Trim() == "user_name = \"Alex\""
            && lines[1].Trim() == "user_password = 1234567890"
            && lines[2].Trim() == "print(user_name)"
            && lines[3].Trim() == "print(user_password)";
    }

    private static string NormalizeCode(string code)
    {
        return code
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace('„', '"')
            .Replace('«', '"')
            .Replace('»', '"')
            .Replace('\u00A0', ' ');
    }
}
