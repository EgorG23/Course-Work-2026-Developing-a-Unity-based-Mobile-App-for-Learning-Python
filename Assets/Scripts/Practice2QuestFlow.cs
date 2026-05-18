using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Practice2QuestFlow : MonoBehaviour
{
    private static readonly Color SuccessGreenColor = new Color(0.4862745f, 0.9882354f, 0f, 1f);
    private static readonly Vector2 OutputUnsolvedAnchoredPosition = new Vector2(-3.17138672f, -316.463379f);
    private static readonly Vector2 OutputSolvedAnchoredPosition = new Vector2(-3.17138672f, -520f);

    private readonly Dictionary<string, GameObject> screens = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    private readonly HashSet<int> solvedTasks = new HashSet<int>();
    private readonly Dictionary<int, string> terminalByTask = new Dictionary<int, string>
    {
        { 1, "Terminal1" },
        { 2, "Terminal2" },
        { 3, "Terminal3" }
    };
    private readonly Dictionary<int, string> inputTemplateByTask = new Dictionary<int, string>
    {
        { 1, "a = 25\nb = 5\nprint(a b) # сложение\nprint(a b) # вычитание\nprint(a b) # умножение\nprint(a b) # деление" },
        { 2, "x =\nf =\nprint(f)" },
        { 3, string.Empty }
    };
    private readonly Dictionary<int, string> promptByTask = new Dictionary<int, string>
    {
        { 1, "ПК1. Надо дописать программу «Арифметика a и b»." },
        { 2, "ПК2. Надо дописать программу «Найти значение функции f = x^2 + 2x - 19, где x = 5»." },
        { 3, "ПК3. Написать программу из одной строки, которая выводит последнюю цифру числа 143" }
    };

    private void Awake()
    {
        Debug.Log("[Practice2QuestFlow] Awake: wiring quest flow.");
        CacheScreens();
        WireStaticNavigation();
        ForceStartState();
    }

    private void CacheScreens()
    {
        screens.Clear();
        foreach (Transform child in transform)
        {
            if (child == null)
            {
                continue;
            }

            screens[child.name.Trim()] = child.gameObject;
        }
    }

    private void ForceStartState()
    {
        solvedTasks.Clear();
        foreach (KeyValuePair<int, string> terminalInfo in terminalByTask)
        {
            GameObject terminal = GetScreen(terminalInfo.Value);
            if (terminal != null)
            {
                SetTerminalSolvedVisualState(terminal, false);
                ResetTerminalContent(terminalInfo.Key, terminal);
                WireInputFocusReset(terminalInfo.Key, terminal);
            }
        }

        ShowOnly("Start");
    }

    private void WireStaticNavigation()
    {
        WireButton("Start/Container/ButtonsContainer/Go", () => ShowOnly("Office"));

        WireButton("Office/Content/PC1", () => ShowOnly("Terminal1"));
        WireButton("Office/Content/PC2", () => ShowOnly("Terminal2"));
        WireButton("Office/Content/PC3", () => ShowOnly("Terminal3"));
        WireButton("Office/Back", () => ShowOnly("Start"));

        WireButton("Task1/Back", () => ShowOnly("Office"));
        WireButton("Task2/Back", () => ShowOnly("Office"));
        WireButton("Task3/Back", () => ShowOnly("Office"));

        WireButton("Task1/Enter", () => ShowOnly("Terminal1"));
        WireButton("Task2/Enter", () => ShowOnly("Terminal2"));
        WireButton("Task3/Enter", () => ShowOnly("Terminal3"));

        WireButton("Terminal1/Back", BackFromTerminal);
        WireButton("Terminal2/Back", BackFromTerminal);
        WireButton("Terminal3/Back", BackFromTerminal);

        WireButton("Terminal1/Enter", () => ValidateTask(1, GetScreen("Terminal1")));
        WireButton("Terminal2/Enter", () => ValidateTask(2, GetScreen("Terminal2")));
        WireButton("Terminal3/Enter", () => ValidateTask(3, GetScreen("Terminal3")));

        WireButton("SecondDone/Content/PC1", () => ShowOnly("Terminal1"));
        WireButton("SecondDone/Content/PC3", () => ShowOnly("Terminal3"));
        WireButton("End/Container/ButtonsContainer/Go", GoToMenu);
        WireButton("End/Container/Award/Go", GoToMenu);
    }

    private void ValidateTask(int taskIndex, GameObject terminalScreen)
    {
        if (terminalScreen == null)
        {
            return;
        }

        TMP_InputField inputField = terminalScreen.GetComponentInChildren<TMP_InputField>(true);
        TMP_Text output = FindOrCreateOutputText(terminalScreen.transform);

        if (inputField == null)
        {
            return;
        }

        string code = inputField.text ?? string.Empty;
        bool isCorrect = taskIndex switch
        {
            1 => IsTask1Correct(code),
            2 => IsTask2Correct(code),
            3 => IsTask3Correct(code),
            _ => false
        };

        if (isCorrect)
        {
            solvedTasks.Add(taskIndex);
            if (output != null)
            {
                output.color = SuccessGreenColor;
                output.text = BuildSuccessOutput(taskIndex);
            }

            inputField.text = string.Empty;
            SetTerminalSolvedVisualState(terminalScreen, true);

            // Keep terminal open so the learner can read the resulting output,
            // then return manually via the Back button.
        }
        else
        {
            if (output != null)
            {
                output.text = "<color=#FF4D4D>Неверно. Проверьте код и попробуйте еще раз.</color>";
            }
        }
    }

    private static TMP_Text FindOutputText(Transform screen)
    {
        if (screen == null)
        {
            return null;
        }

        Transform outputTransform = screen.Find("Output");
        if (outputTransform != null)
        {
            TMP_Text direct = outputTransform.GetComponent<TMP_Text>();
            if (direct != null)
            {
                return direct;
            }

            TMP_Text nested = outputTransform.GetComponentInChildren<TMP_Text>(true);
            if (nested != null)
            {
                return nested;
            }
        }

        return screen.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(t => t != null && t.name.Trim() == "Output");
    }

    private static TMP_Text FindOrCreateOutputText(Transform screen)
    {
        TMP_Text existing = FindOutputText(screen);
        if (existing != null || screen == null)
        {
            return existing;
        }

        GameObject outputObject = new GameObject(
            "Output",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        outputObject.transform.SetParent(screen, false);

        RectTransform rect = outputObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = OutputUnsolvedAnchoredPosition;
        rect.sizeDelta = new Vector2(57.578f, 371.65f);
        rect.localScale = new Vector3(0.94381f, 0.94381f, 0.94381f);

        TextMeshProUGUI text = outputObject.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.richText = true;
        text.fontSize = 64f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.PreserveWhitespace;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = SuccessGreenColor;

        TMP_Text sample = screen.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t != null && t != text);
        if (sample != null)
        {
            text.font = sample.font;
            text.fontSharedMaterial = sample.fontSharedMaterial;
        }
        else if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        return text;
    }

    private void ResetTerminalContent(int taskIndex, GameObject terminalScreen)
    {
        TMP_Text output = FindOrCreateOutputText(terminalScreen.transform);
        if (output != null && promptByTask.TryGetValue(taskIndex, out string prompt))
        {
            output.color = SuccessGreenColor;
            output.text = prompt;
        }

        SetOutputPosition(terminalScreen, solved: false);

        TMP_InputField inputField = terminalScreen.GetComponentInChildren<TMP_InputField>(true);
        if (inputField != null && inputTemplateByTask.TryGetValue(taskIndex, out string template))
        {
            inputField.text = template;
        }
    }

    private static bool IsTask1Correct(string code)
    {
        string normalized = Normalize(code);
        string compact = normalized.Replace(" ", string.Empty);

        return compact.Contains("a=25")
            && compact.Contains("b=5")
            && compact.Contains("print(a+b)")
            && compact.Contains("print(a-b)")
            && compact.Contains("print(a*b)")
            && compact.Contains("print(a/b)");
    }

    private static bool IsTask2Correct(string code)
    {
        string normalized = Normalize(code).ToLowerInvariant();
        string compact = normalized.Replace(" ", string.Empty);

        bool hasX = compact.Contains("x=5");
        bool hasPrint = compact.Contains("print(f)") || compact.Contains("print(16)");
        bool hasFormula = compact.Contains("f=x**2+2*x-19")
            || compact.Contains("f=x^2+2*x-19")
            || compact.Contains("f=16");

        return hasX && hasPrint && hasFormula;
    }

    private static bool IsTask3Correct(string code)
    {
        string normalized = Normalize(code).ToLowerInvariant();
        string compact = normalized.Replace(" ", string.Empty);
        string[] lines = normalized
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        if (lines.Length != 1)
        {
            return false;
        }

        return compact == "print(143%10)"
            || compact == "print(str(143)[-1])"
            || compact == "print(3)"
            || compact == "print('3')"
            || compact == "print(\"3\")";
    }

    private static string BuildSuccessOutput(int taskIndex)
    {
        return taskIndex switch
        {
            1 => "Верно! Задание выполнено.\n\nOutput:\n30\n20\n125\n5.0",
            2 => "Верно! Задание выполнено.\n\nOutput:\n16",
            3 => "Верно! Задание выполнено.\n\nOutput:\n3",
            _ => "Верно! Задание выполнено."
        };
    }

    private static string Normalize(string code)
    {
        return (code ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace('«', '"')
            .Replace('»', '"')
            .Replace('\u00A0', ' ')
            .Trim();
    }

    private void BackFromTerminal()
    {
        if (solvedTasks.Count >= 3)
        {
            ShowOnly("End");
            return;
        }

        ShowOnly("Office");
    }

    private void GoToMenu()
    {
        SceneManager.LoadScene("LessonsList");
    }

    private static void SetTerminalSolvedVisualState(GameObject terminalScreen, bool solved)
    {
        if (terminalScreen == null)
        {
            return;
        }

        Transform input = terminalScreen.transform.Find("Input");
        if (input != null)
        {
            input.gameObject.SetActive(!solved);
        }

        Transform enter = terminalScreen.transform.Find("Enter");
        if (enter != null)
        {
            enter.gameObject.SetActive(!solved);
        }

        SetOutputPosition(terminalScreen, solved);
    }

    private static void SetOutputPosition(GameObject terminalScreen, bool solved)
    {
        if (terminalScreen == null)
        {
            return;
        }

        TMP_Text output = FindOutputText(terminalScreen.transform);
        if (output == null)
        {
            return;
        }

        RectTransform rect = output.rectTransform;
        if (rect == null)
        {
            return;
        }

        rect.anchoredPosition = solved ? OutputSolvedAnchoredPosition : OutputUnsolvedAnchoredPosition;
    }

    private void WireInputFocusReset(int taskIndex, GameObject terminalScreen)
    {
        if (terminalScreen == null)
        {
            return;
        }

        TMP_InputField inputField = terminalScreen.GetComponentInChildren<TMP_InputField>(true);
        if (inputField == null)
        {
            return;
        }

        inputField.onSelect.RemoveAllListeners();
        inputField.onSelect.AddListener(_ =>
        {
            if (solvedTasks.Contains(taskIndex))
            {
                return;
            }

            TMP_Text output = FindOrCreateOutputText(terminalScreen.transform);
            if (output != null && promptByTask.TryGetValue(taskIndex, out string prompt))
            {
                output.color = SuccessGreenColor;
                output.text = prompt;
            }
        });
    }

    private void WireButton(string relativePath, Action callback)
    {
        Transform target = transform.Find(relativePath);
        if (target == null)
        {
            Debug.LogWarning($"[Practice2QuestFlow] Button not found: {relativePath}");
            return;
        }

        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogWarning($"[Practice2QuestFlow] Button component missing at: {relativePath}");
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            Debug.Log($"[Practice2QuestFlow] Click: {relativePath}");
            callback?.Invoke();
        });

        ScreenButton legacy = target.GetComponent<ScreenButton>();
        if (legacy != null)
        {
            legacy.enabled = false;
        }
    }

    private GameObject GetScreen(string name)
    {
        screens.TryGetValue(name, out GameObject screen);
        return screen;
    }

    private void ShowOnly(string screenName)
    {
        GameObject target = GetScreen(screenName);
        if (target == null)
        {
            Debug.LogWarning($"[Practice2QuestFlow] Screen not found: {screenName}");
            return;
        }

        Debug.Log($"[Practice2QuestFlow] Show screen: {screenName}");

        foreach (KeyValuePair<string, GameObject> kv in screens)
        {
            if (kv.Value != null)
            {
                kv.Value.SetActive(kv.Value == target);
            }
        }
    }
}
