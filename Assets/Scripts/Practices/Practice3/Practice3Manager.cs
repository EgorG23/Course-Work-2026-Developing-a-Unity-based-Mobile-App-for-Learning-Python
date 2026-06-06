using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class Practice3Manager : MonoBehaviour
{
    private static readonly string[] CorrectOrder = { "Line0", "Line3", "Line2", "Line4", "Line1" };
    private static readonly string[] StartingOrder = { "Line2", "Line0", "Line4", "Line3", "Line1" };
    private static readonly string[] CorrectProgram =
    {
        "a=15",
        "ifa%2==0:",
        "print(\"Да\")",
        "else:",
        "print(\"Нет\")"
    };

    private readonly Dictionary<string, GameObject> screens = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    private PracticeManager practiceManager;
    private Transform linesContainer;
    private Button checkButton;
    private bool solved;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallInPractice3()
    {
        if (SceneManager.GetActiveScene().name != "Practice3_KvestScene" ||
            FindFirstObjectByType<Practice3Manager>() != null)
        {
            return;
        }

        GameObject screenContainer = GameObject.Find("ScreenContainer");
        if (screenContainer == null)
        {
            Debug.LogError("Practice3 requires ScreenContainer.");
            return;
        }

        GameObject managerObject = new GameObject("Practice3Manager");
        PracticeManager commonManager = managerObject.AddComponent<PracticeManager>();
        commonManager.lessonIndex = 3;
        commonManager.screenContainer = screenContainer.transform;
        managerObject.AddComponent<Practice3Manager>();
    }

    private void Start()
    {
        practiceManager = GetComponent<PracticeManager>();
        if (practiceManager == null)
        {
            practiceManager = PracticeManager.Instance;
        }

        if (practiceManager == null || practiceManager.screenContainer == null)
        {
            Debug.LogError("Practice3Manager requires PracticeManager with ScreenContainer.");
            enabled = false;
            return;
        }

        practiceManager.lessonIndex = 3;
        CacheScreens();
        WireNavigation();
        ConfigurePuzzle();
        StartCoroutine(EnsureStartVisible());
    }

    private IEnumerator EnsureStartVisible()
    {
        yield return null;

        if (practiceManager.CurrentScreen != null)
        {
            yield break;
        }

        GameObject start = GetScreen("Start");
        if (start != null)
        {
            practiceManager.ShowScreen(start);
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
        Wire("Start", "Go", () => Show("InformationStand"));
        WireAny("InformationStand", () => Show("Lines"), "Button", "Go");
        Wire("Lines", "Check", CheckPuzzle);
        Wire("End", "Go", practiceManager.FinishPractice);
    }

    private void ConfigurePuzzle()
    {
        GameObject linesScreen = GetScreen("Lines");
        if (linesScreen == null)
        {
            return;
        }

        Transform[] lineObjects = linesScreen.GetComponentsInChildren<Transform>(true)
            .Where(child => CorrectOrder.Contains(child.name.Trim()))
            .ToArray();
        linesContainer = lineObjects.FirstOrDefault()?.parent;
        if (linesContainer != null)
        {
            for (int index = 0; index < StartingOrder.Length; index++)
            {
                Transform line = lineObjects.FirstOrDefault(item => item.name.Trim() == StartingOrder[index]);
                if (line != null)
                {
                    line.SetSiblingIndex(index);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(linesContainer as RectTransform);
        }

        foreach (Transform line in lineObjects)
        {
            CodeLineDraggable draggable = line.GetComponent<CodeLineDraggable>();
            if (draggable == null)
            {
                draggable = line.gameObject.AddComponent<CodeLineDraggable>();
            }

            draggable.Configure(lineObjects);
        }

        checkButton = FindNamedComponent<Button>(linesScreen.transform, "Check");
    }

    private void CheckPuzzle()
    {
        if (solved || linesContainer == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(linesContainer as RectTransform);

        string[] currentProgram = linesContainer.Cast<Transform>()
            .Where(child => CorrectOrder.Contains(child.name.Trim()))
            .OrderByDescending(child => ((RectTransform)child).position.y)
            .Select(child => NormalizeCode(child.GetComponentInChildren<TMP_Text>(true)?.text))
            .ToArray();

        if (!currentProgram.SequenceEqual(CorrectProgram))
        {
            StartCoroutine(FlashCheckButton(new Color(1f, 0.28f, 0.28f)));
            return;
        }

        solved = true;
        foreach (CodeLineDraggable line in linesContainer.GetComponentsInChildren<CodeLineDraggable>(true))
        {
            line.enabled = false;
        }

        StartCoroutine(ShowEndAfterSuccess());
    }

    private IEnumerator ShowEndAfterSuccess()
    {
        yield return FlashCheckButton(new Color(0.35f, 1f, 0.45f));
        Show("End");
    }

    private IEnumerator FlashCheckButton(Color color)
    {
        Graphic graphic = checkButton != null ? checkButton.targetGraphic : null;
        if (graphic == null)
        {
            yield break;
        }

        Color original = graphic.color;
        graphic.color = color;
        yield return new WaitForSeconds(0.45f);
        graphic.color = original;
    }

    private void Show(string screenName)
    {
        GameObject screen = GetScreen(screenName);
        if (screen != null)
        {
            practiceManager.ShowScreen(screen);
        }
    }

    private GameObject GetScreen(string screenName)
    {
        screens.TryGetValue(screenName, out GameObject screen);
        return screen;
    }

    private void Wire(string screenName, string buttonName, Action action)
    {
        GameObject screen = GetScreen(screenName);
        if (screen == null)
        {
            return;
        }

        foreach (Button button in screen.GetComponentsInChildren<Button>(true)
                     .Where(button => button.name.Trim() == buttonName))
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

    private void WireAny(string screenName, Action action, params string[] buttonNames)
    {
        foreach (string buttonName in buttonNames)
        {
            Wire(screenName, buttonName, action);
        }
    }

    private static T FindNamedComponent<T>(Transform root, string objectName) where T : Component
    {
        Transform found = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(child => child.name.Trim() == objectName);
        return found != null ? found.GetComponent<T>() : null;
    }

    private static string NormalizeCode(string code)
    {
        return new string((code ?? string.Empty)
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace('«', '"')
            .Replace('»', '"')
            .Where(character => !char.IsWhiteSpace(character))
            .ToArray());
    }
}
