using System;
using System.Linq;
using UnityEngine;

public enum TerminalTask
{
    PythonVersion,
    UserCredentials,
    Arithmetic,
    Formula,
    LastDigit
}

public readonly struct TerminalResult
{
    public TerminalResult(bool isCorrect, string message)
    {
        IsCorrect = isCorrect;
        Message = message;
    }

    public bool IsCorrect { get; }
    public string Message { get; }
}

public class TerminalAnswerChecker : MonoBehaviour
{
    public TerminalTask task;

    public TerminalResult Evaluate(string input)
    {
        bool isCorrect = IsCorrect(task, input);
        return new TerminalResult(isCorrect, isCorrect ? SuccessMessage(task) : "<color=red>> Неверный код</color>");
    }

    public static bool IsCorrect(TerminalTask task, string input)
    {
        string normalized = Normalize(input);
        string compact = normalized.Replace(" ", string.Empty);

        return task switch
        {
            TerminalTask.PythonVersion =>
                string.Equals(normalized.Trim(), "python --version", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized.Trim(), "python -V", StringComparison.OrdinalIgnoreCase),
            TerminalTask.UserCredentials => IsUserCredentialsCorrect(normalized),
            TerminalTask.Arithmetic =>
                compact.Contains("a=25") && compact.Contains("b=5") &&
                compact.Contains("print(a+b)") && compact.Contains("print(a-b)") &&
                compact.Contains("print(a*b)") && compact.Contains("print(a/b)"),
            TerminalTask.Formula => IsFormulaCorrect(compact.ToLowerInvariant()),
            TerminalTask.LastDigit => IsLastDigitCorrect(normalized, compact.ToLowerInvariant()),
            _ => false
        };
    }

    public static string Normalize(string input)
    {
        return (input ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace('„', '"')
            .Replace('«', '"')
            .Replace('»', '"')
            .Replace('\u00A0', ' ')
            .Trim();
    }

    private static bool IsUserCredentialsCorrect(string normalized)
    {
        string[] lines = normalized.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToArray();
        return lines.Length == 4 &&
               lines[0] == "user_name = \"Alex\"" &&
               lines[1] == "user_password = 1234567890" &&
               lines[2] == "print(user_name)" &&
               lines[3] == "print(user_password)";
    }

    private static bool IsFormulaCorrect(string compact)
    {
        bool hasX = compact.Contains("x=5");
        bool hasPrint = compact.Contains("print(f)") || compact.Contains("print(16)");
        bool hasFormula = compact.Contains("f=x**2+2*x-19") ||
                          compact.Contains("f=x^2+2*x-19") ||
                          compact.Contains("f=16");
        return hasX && hasPrint && hasFormula;
    }

    private static bool IsLastDigitCorrect(string normalized, string compact)
    {
        int lineCount = normalized.Split('\n').Count(line => !string.IsNullOrWhiteSpace(line));
        return lineCount == 1 &&
               (compact == "print(143%10)" || compact == "print(str(143)[-1])" ||
                compact == "print(3)" || compact == "print('3')" || compact == "print(\"3\")");
    }

    private static string SuccessMessage(TerminalTask task)
    {
        return task switch
        {
            TerminalTask.PythonVersion => "<color=#00FF00>> Python 3.11.4</color>",
            TerminalTask.UserCredentials => "<color=#00FF00>> Доступ разрешен</color>",
            TerminalTask.Arithmetic => "<color=#7CFC00>Верно! Задание выполнено.\n\nOutput:\n30\n20\n125\n5.0</color>",
            TerminalTask.Formula => "<color=#7CFC00>Верно! Задание выполнено.\n\nOutput:\n16</color>",
            TerminalTask.LastDigit => "<color=#7CFC00>Верно! Задание выполнено.\n\nOutput:\n3</color>",
            _ => "<color=#00FF00>> Верно</color>"
        };
    }
}
