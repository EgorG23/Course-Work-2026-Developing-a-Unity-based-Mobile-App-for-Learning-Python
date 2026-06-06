using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Terminal : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text outputText;
    public TMP_Text promptText;
    public TMP_Text resultText;
    public TerminalAnswerChecker answerChecker;
    public TerminalTask defaultTask;
    [TextArea] public string prompt;
    public UnityEvent solved;

    public bool IsSolved { get; private set; }

    public event Action Solved;

    private int lastCheckFrame = -1;

    private void Awake()
    {
        if (answerChecker == null)
        {
            answerChecker = GetComponent<TerminalAnswerChecker>();
        }

        if (answerChecker == null)
        {
            answerChecker = gameObject.AddComponent<TerminalAnswerChecker>();
            answerChecker.task = defaultTask;
        }
    }

    public void CheckCommand()
    {
        if (IsSolved || lastCheckFrame == Time.frameCount)
        {
            return;
        }

        lastCheckFrame = Time.frameCount;

        TMP_Text resultTarget = resultText != null ? resultText : outputText;
        if (inputField == null || resultTarget == null || answerChecker == null)
        {
            Debug.LogWarning("Terminal is missing input, output, or answer checker.");
            return;
        }

        TerminalResult result = answerChecker.Evaluate(inputField.text);
        resultTarget.text = result.Message;

        if (!result.IsCorrect)
        {
            return;
        }

        IsSolved = true;
        solved?.Invoke();
        Solved?.Invoke();
    }

    public void ShowPrompt()
    {
        TMP_Text target = promptText != null ? promptText : outputText;
        if (!IsSolved && target != null && !string.IsNullOrWhiteSpace(prompt))
        {
            target.text = prompt;
        }
    }

    public void ClearTransientResult(string _)
    {
        if (!IsSolved && resultText != null)
        {
            resultText.text = string.Empty;
        }
    }
}
