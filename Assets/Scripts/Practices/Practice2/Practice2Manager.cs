using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Practice2Manager : MonoBehaviour
{
    private readonly Dictionary<string, GameObject> screens = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    private readonly HashSet<int> solvedTasks = new HashSet<int>();
    private readonly Dictionary<int, Terminal> terminals = new Dictionary<int, Terminal>();
    private PracticeManager practiceManager;

    private readonly string[] templates =
    {
        string.Empty,
        "a = 25\nb = 5\nprint(a b) # сложение\nprint(a b) # вычитание\nprint(a b) # умножение\nprint(a b) # деление",
        "x =\nf =\nprint(f)",
        string.Empty
    };

    private readonly string[] prompts =
    {
        string.Empty,
        "ПК1. Надо дописать программу «Арифметика a и b».",
        "ПК2. Надо дописать программу «Найти значение функции f = x^2 + 2x - 19, где x = 5».",
        "ПК3. Написать программу из одной строки, которая выводит последнюю цифру числа 143"
    };

    private void Awake()
    {
        practiceManager = GetComponent<PracticeManager>();
        if (practiceManager == null)
        {
            practiceManager = gameObject.AddComponent<PracticeManager>();
        }

        practiceManager.lessonIndex = 2;
        practiceManager.screenContainer = transform;
    }

    private void Start()
    {
        practiceManager = PracticeManager.Instance ?? practiceManager;
        if (practiceManager == null || practiceManager.screenContainer == null)
        {
            Debug.LogError("Practice2Manager requires PracticeManager with a screen container.");
            enabled = false;
            return;
        }

        CacheScreens();
        WireNavigation();
        for (int task = 1; task <= 3; task++)
        {
            ConfigureTerminal(task);
        }
    }

    private void CacheScreens()
    {
        screens.Clear();
        foreach (Transform child in practiceManager.screenContainer)
        {
            screens[child.name.Trim()] = child.gameObject;
        }
    }

    private void WireNavigation()
    {
        Wire("Start", "Go", () => Show("Office"));
        Wire("Office", "Back", () => Show("Start"));
        Wire("Office", "PC1", () => OpenTask(1));
        Wire("Office", "PC2", () => OpenTask(2));
        Wire("Office", "PC3", () => OpenTask(3));

        for (int task = 1; task <= 3; task++)
        {
            int capturedTask = task;
            Wire($"Task{task}", "Back", () => Show("Office"));
            Wire($"Task{task}", "Enter", () => Show($"Terminal{capturedTask}"));
            Wire($"Terminal{task}", "Back", BackFromTerminal);
        }

        Wire("End", "Go", FinishIfAllTasksSolved);
    }

    private void ConfigureTerminal(int taskIndex)
    {
        GameObject screen = GetScreen($"Terminal{taskIndex}");
        if (screen == null)
        {
            return;
        }

        TMP_InputField input = screen.GetComponentInChildren<TMP_InputField>(true);
        TMP_Text promptText = EnsureTerminalOutput(screen, input);
        TMP_Text resultText = EnsureResultText(screen, promptText);
        Terminal terminal = screen.GetComponent<Terminal>();
        if (terminal == null)
        {
            terminal = screen.AddComponent<Terminal>();
        }

        TerminalAnswerChecker checker = screen.GetComponent<TerminalAnswerChecker>();
        if (checker == null)
        {
            checker = screen.AddComponent<TerminalAnswerChecker>();
        }

        checker.task = (TerminalTask)(taskIndex + 1);
        terminal.inputField = input;
        terminal.outputText = resultText;
        terminal.promptText = promptText;
        terminal.resultText = resultText;
        terminal.answerChecker = checker;
        terminal.prompt = prompts[taskIndex];
        terminal.Solved += () => OnTaskSolved(taskIndex);
        terminals[taskIndex] = terminal;
        ApplySharedOutputStyle(promptText);
        ApplySharedOutputStyle(resultText);
        ConfigurePromptLayout(promptText, screen.transform);
        ConfigureResultLayout(resultText, screen.transform);
        ConfigureInputLayout(input);

        if (input != null)
        {
            input.text = templates[taskIndex];
            input.onSelect.RemoveListener(terminal.ClearTransientResult);
            input.onSelect.AddListener(terminal.ClearTransientResult);
        }

        if (promptText != null)
        {
            promptText.text = prompts[taskIndex];
        }

        if (resultText != null)
        {
            resultText.text = string.Empty;
        }
        else
        {
            Debug.LogWarning($"Practice2 Terminal{taskIndex} has no text suitable for task output.");
        }

        Button enter = FindNamedComponent<Button>(screen.transform, "Enter");
        if (enter != null)
        {
            enter.onClick.RemoveAllListeners();
            enter.onClick.AddListener(terminal.CheckCommand);
        }
    }

    private void OnTaskSolved(int taskIndex)
    {
        solvedTasks.Add(taskIndex);
        GameObject terminalScreen = GetScreen($"Terminal{taskIndex}");
        SetChildActive(terminalScreen, "Input", false);
        SetChildActive(terminalScreen, "Enter", false);
        ExpandSolvedResult(terminals.TryGetValue(taskIndex, out Terminal terminal) ? terminal.resultText : null);

        if (solvedTasks.Count == 3)
        {
            Show("End");
        }
    }

    private void BackFromTerminal()
    {
        Show(solvedTasks.Count == 3 ? "End" : "Office");
    }

    private void OpenTask(int taskIndex)
    {
        if (solvedTasks.Contains(taskIndex))
        {
            Show($"Terminal{taskIndex}");
            return;
        }

        GameObject taskScreen = GetScreen($"Task{taskIndex}");
        Show(taskScreen != null ? $"Task{taskIndex}" : $"Terminal{taskIndex}");

        if (terminals.TryGetValue(taskIndex, out Terminal terminal))
        {
            terminal.ShowPrompt();
        }
    }

    private void FinishIfAllTasksSolved()
    {
        if (solvedTasks.Count == 3)
        {
            practiceManager.FinishPractice();
            return;
        }

        Show("Office");
    }

    private void Show(string screenName)
    {
        GameObject screen = GetScreen(screenName);
        if (screen != null)
        {
            practiceManager.ShowScreen(screen);
        }
    }

    private GameObject GetScreen(string name)
    {
        screens.TryGetValue(name, out GameObject screen);
        return screen;
    }

    private void Wire(string screenName, string buttonName, Action action)
    {
        GameObject screen = GetScreen(screenName);
        if (screen == null)
        {
            return;
        }

        foreach (Button button in screen.GetComponentsInChildren<Button>(true).Where(button => button.name.Trim() == buttonName))
        {
            ScreenButton legacyNavigation = button.GetComponent<ScreenButton>();
            if (legacyNavigation != null)
            {
                legacyNavigation.UseManagedNavigation();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
        }
    }

    private static void SetChildActive(GameObject screen, string name, bool active)
    {
        Transform child = screen != null
            ? screen.GetComponentsInChildren<Transform>(true).FirstOrDefault(transform => transform.name.Trim() == name)
            : null;
        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private static T FindNamedComponent<T>(Transform root, string name) where T : Component
    {
        Transform found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Trim() == name);
        return found != null ? found.GetComponent<T>() : null;
    }

    private TMP_Text EnsureTerminalOutput(GameObject screen, TMP_InputField input)
    {
        TMP_Text namedOutput = FindNamedComponent<TMP_Text>(screen.transform, "Output");
        if (namedOutput != null)
        {
            return namedOutput;
        }

        GameObject firstTerminal = GetScreen("Terminal1");
        TMP_Text reference = firstTerminal != null
            ? FindNamedComponent<TMP_Text>(firstTerminal.transform, "Output")
            : null;
        if (reference == null)
        {
            return input?.placeholder as TMP_Text;
        }

        GameObject outputObject = Instantiate(reference.gameObject, screen.transform);
        outputObject.name = "Output";
        outputObject.SetActive(true);
        return outputObject.GetComponent<TMP_Text>();
    }

    private void ApplySharedOutputStyle(TMP_Text output)
    {
        GameObject firstTerminal = GetScreen("Terminal1");
        TMP_Text reference = firstTerminal != null
            ? FindNamedComponent<TMP_Text>(firstTerminal.transform, "Output")
            : null;
        if (output == null || reference == null || output == reference)
        {
            return;
        }

        output.font = reference.font;
        output.fontSize = reference.fontSize;
        output.fontStyle = reference.fontStyle;
        output.color = reference.color;
        output.alignment = reference.alignment;
        output.enableAutoSizing = reference.enableAutoSizing;
        output.textWrappingMode = reference.textWrappingMode;
    }

    private static TMP_Text EnsureResultText(GameObject screen, TMP_Text reference)
    {
        TMP_Text existing = FindNamedComponent<TMP_Text>(screen.transform, "Result");
        if (existing != null)
        {
            return existing;
        }

        if (reference == null)
        {
            return null;
        }

        GameObject resultObject = Instantiate(reference.gameObject, screen.transform);
        resultObject.name = "Result";
        resultObject.SetActive(true);
        return resultObject.GetComponent<TMP_Text>();
    }

    private static void ConfigurePromptLayout(TMP_Text output, Transform screen)
    {
        if (output == null)
        {
            return;
        }

        output.transform.SetParent(screen, false);
        RectTransform rect = output.rectTransform;
        rect.anchorMin = new Vector2(0.08f, 0.76f);
        rect.anchorMax = new Vector2(0.92f, 0.94f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        ContentSizeFitter fitter = output.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        output.enableAutoSizing = true;
        output.fontSizeMin = 26f;
        output.fontSizeMax = 64f;
        output.textWrappingMode = TextWrappingModes.Normal;
        output.overflowMode = TextOverflowModes.Overflow;
        output.raycastTarget = false;
        output.transform.SetAsLastSibling();
    }

    private static void ConfigureResultLayout(TMP_Text output, Transform screen)
    {
        if (output == null)
        {
            return;
        }

        output.transform.SetParent(screen, false);
        RectTransform rect = output.rectTransform;
        rect.anchorMin = new Vector2(0.08f, 0.66f);
        rect.anchorMax = new Vector2(0.92f, 0.75f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        ContentSizeFitter fitter = output.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        output.enableAutoSizing = true;
        output.fontSizeMin = 24f;
        output.fontSizeMax = 54f;
        output.textWrappingMode = TextWrappingModes.Normal;
        output.overflowMode = TextOverflowModes.Overflow;
        output.raycastTarget = false;
        output.transform.SetAsLastSibling();
    }

    private static void ConfigureInputLayout(TMP_InputField input)
    {
        if (input == null)
        {
            return;
        }

        RectTransform rect = input.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.08f, 0.36f);
        rect.anchorMax = new Vector2(0.92f, 0.64f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void ExpandSolvedResult(TMP_Text result)
    {
        if (result == null)
        {
            return;
        }

        RectTransform rect = result.rectTransform;
        rect.anchorMin = new Vector2(0.08f, 0.36f);
        rect.anchorMax = new Vector2(0.92f, 0.75f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
