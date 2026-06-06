using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Practice1Manager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject userDataScreenPrefab;

    [Header("Messages")]
    [TextArea] public string noPowerMessage = "ПК не работает. Сначала почини щиток.";
    [TextArea] public string doorLockedMessage = "Дверь закрыта. Восстанови программу проверки в терминале.";
    [TextArea] public string codeSolvedMessage = "Проверка восстановлена. Дверь разблокирована.";

    private readonly Dictionary<string, GameObject> screens = new Dictionary<string, GameObject>(StringComparer.Ordinal);
    private PracticeManager practiceManager;
    private TypeMatchingPuzzle typePuzzle;
    private ToastPresenter toastPresenter;

    public bool PowerFixed { get; private set; }
    public bool CodeSolved { get; private set; }

    private void Start()
    {
        practiceManager = PracticeManager.Instance;
        if (practiceManager == null || practiceManager.screenContainer == null)
        {
            Debug.LogError("Practice1Manager requires PracticeManager with a screen container.");
            enabled = false;
            return;
        }

        practiceManager.lessonIndex = 1;
        toastPresenter = practiceManager.screenContainer.GetComponent<ToastPresenter>();
        if (toastPresenter == null)
        {
            toastPresenter = practiceManager.screenContainer.gameObject.AddComponent<ToastPresenter>();
        }
        CacheScreens();
        NormalizeElectricityBackParent();
        HideHotspotVisuals();
        NormalizeStartButtons();
        WireNavigation();
        ConfigurePuzzle();
        ConfigureTerminal();
        practiceManager.ScreenChanged += OnScreenChanged;
        RefreshOffice();
    }

    private void OnDestroy()
    {
        if (practiceManager != null)
        {
            practiceManager.ScreenChanged -= OnScreenChanged;
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
        Wire("Start", "Back", practiceManager.ExitPractice);

        Wire("Office", "Back", practiceManager.GoBack);
        Wire("Office", "PaperHotspot", OpenUserData);
        Wire("Office", "PanelHotspot", () => Show("Electricity"));
        Wire("Office", "ComputerHotspot", OpenComputer);
        Wire("Office", "DoorHotspot", OpenDoor);
        Wire("Office", "Button", () => Show("Electricity"));

        Wire("OfficeOn", "Back", practiceManager.GoBack);
        Wire("OfficeOn", "PaperHotspot", OpenUserData);
        Wire("OfficeOn", "PanelHotspot", () => Show("Electricity"));
        Wire("OfficeOn", "ComputerHotspot", OpenComputer);
        Wire("OfficeOn", "DoorHotspot", OpenDoor);
        Wire("OfficeOn", "Button", OpenComputer);

        Wire("Electricity", "Back", practiceManager.GoBack);
        Wire("Electricity", "ElectricityBackHotspot", practiceManager.GoBack);
        Wire("UserData", "Back", practiceManager.GoBack);
        Wire("UserData", "Button", practiceManager.GoBack);

        Wire("Attention", "Back", practiceManager.GoBack);
        WireAny("Attention", OpenTerminal, "Enter", "Button", "Go", "Next", "ToConsole");
        Wire("Computer", "Back", practiceManager.GoBack);
        Wire("Computer", "ToConsole", OpenTerminal);

        Wire("Terminal", "Back", practiceManager.GoBack);
        Wire("FinalScreen", "Back", practiceManager.GoBack);
        WireAny("FinalScreen", practiceManager.FinishPractice, "Button", "FinalDoorHotspot");
        Wire("End", "Go", practiceManager.FinishPractice);
        EnsureFinalDoorHotspot();
    }

    private void ConfigurePuzzle()
    {
        GameObject electricity = GetScreen("Electricity");
        if (electricity == null)
        {
            return;
        }

        Transform puzzleRoot = FindDeep(electricity.transform, "TypePuzzle");
        if (puzzleRoot == null)
        {
            Debug.LogWarning("Practice1 TypePuzzle is missing.");
            return;
        }

        typePuzzle = puzzleRoot.GetComponent<TypeMatchingPuzzle>();
        if (typePuzzle == null)
        {
            typePuzzle = puzzleRoot.gameObject.AddComponent<TypeMatchingPuzzle>();
        }

        typePuzzle.Solved += OnPowerFixed;
    }

    private void ConfigureTerminal()
    {
        GameObject screen = GetScreen("Terminal");
        if (screen == null)
        {
            return;
        }

        Terminal terminal = screen.GetComponent<Terminal>();
        if (terminal == null)
        {
            terminal = screen.AddComponent<Terminal>();
        }

        terminal.inputField = screen.GetComponentInChildren<TMP_InputField>(true);
        terminal.outputText = FindNamedComponent<TMP_Text>(screen.transform, "Output");
        terminal.answerChecker = EnsureChecker(screen, TerminalTask.UserCredentials);
        terminal.Solved += OnTerminalSolved;

        Button enter = FindNamedComponent<Button>(screen.transform, "Enter");
        if (enter != null)
        {
            enter.onClick.RemoveAllListeners();
            enter.onClick.AddListener(terminal.CheckCommand);
        }
    }

    private void OnPowerFixed()
    {
        PowerFixed = true;
        ReturnToOffice();
    }

    private void OnTerminalSolved()
    {
        CodeSolved = true;
        toastPresenter.Show(codeSolvedMessage);
        Show("OfficeOn");
    }

    private void OpenComputer()
    {
        if (!PowerFixed)
        {
            toastPresenter.Show(noPowerMessage);
            return;
        }

        Show(GetScreen("Attention") != null ? "Attention" : "Computer");
    }

    private void OpenTerminal()
    {
        Show("Terminal");
    }

    private void OpenDoor()
    {
        if (!CodeSolved)
        {
            toastPresenter.Show(doorLockedMessage);
            return;
        }

        Show(GetScreen("FinalScreen") != null ? "FinalScreen" : "End");
    }

    private void OpenUserData()
    {
        GameObject userData = GetScreen("UserData");
        if (userData != null)
        {
            Show("UserData");
            return;
        }

        if (userDataScreenPrefab != null)
        {
            practiceManager.ShowScreen(userDataScreenPrefab);
            return;
        }

        toastPresenter.Show("Данные с листка:\nuser_name = \"Alex\"\nuser_password = 1234567890", 5f);
    }

    private void ReturnToOffice()
    {
        Show(PowerFixed ? "OfficeOn" : "Office");
    }

    private void RefreshOffice()
    {
        GameObject office = GetScreen("Office");
        GameObject officeOn = GetScreen("OfficeOn");
        if (office != null && practiceManager.CurrentScreen == office && PowerFixed && officeOn != null)
        {
            Show("OfficeOn");
        }
    }

    private void OnScreenChanged(GameObject _)
    {
        RefreshOffice();
    }

    private void Show(string screenName)
    {
        GameObject screen = GetScreen(screenName);
        if (screen != null)
        {
            practiceManager.ShowScreen(screen);
        }
        else
        {
            Debug.LogWarning($"Practice1 screen not found: {screenName}");
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

        foreach (Button button in screen.GetComponentsInChildren<Button>(true))
        {
            if (button.name.Trim() != buttonName)
            {
                continue;
            }

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

    private static TerminalAnswerChecker EnsureChecker(GameObject owner, TerminalTask task)
    {
        TerminalAnswerChecker checker = owner.GetComponent<TerminalAnswerChecker>();
        if (checker == null)
        {
            checker = owner.AddComponent<TerminalAnswerChecker>();
        }

        checker.task = task;
        return checker;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Trim() == name)
            {
                return child;
            }
        }

        return null;
    }

    private static T FindNamedComponent<T>(Transform root, string name) where T : Component
    {
        Transform found = FindDeep(root, name);
        return found != null ? found.GetComponent<T>() : null;
    }

    private void HideHotspotVisuals()
    {
        foreach (GameObject screen in screens.Values)
        {
            foreach (Transform child in screen.GetComponentsInChildren<Transform>(true))
            {
                if (!child.name.Contains("Hotspot", StringComparison.Ordinal))
                {
                    continue;
                }

                Image image = child.GetComponent<Image>();
                if (image != null)
                {
                    Color color = image.color;
                    color.a = 0.002f;
                    image.color = color;
                    image.raycastTarget = true;
                }

                foreach (Graphic label in child.GetComponentsInChildren<Graphic>(true))
                {
                    if (label != image)
                    {
                        label.enabled = false;
                    }
                }
            }
        }
    }

    private void NormalizeStartButtons()
    {
        GameObject start = GetScreen("Start");
        if (start == null)
        {
            return;
        }

        NormalizeButton(start.transform, "Go", new Vector2(0.5f, 0.18f), new Vector2(620f, 170f));
        NormalizeButton(start.transform, "Back", new Vector2(0.5f, 0.08f), new Vector2(420f, 120f));
    }

    private void NormalizeElectricityBackParent()
    {
        GameObject electricity = GetScreen("Electricity");
        Button back = electricity != null
            ? FindNamedComponent<Button>(electricity.transform, "Back")
            : null;
        if (back == null || back.transform.parent == electricity.transform)
        {
            return;
        }

        back.transform.SetParent(electricity.transform, false);
        back.transform.SetAsLastSibling();
    }

    private static void NormalizeButton(Transform root, string buttonName, Vector2 anchor, Vector2 size)
    {
        Button button = FindNamedComponent<Button>(root, buttonName);
        RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        layout.minWidth = size.x;
        layout.minHeight = size.y;
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;
    }

    private void EnsureFinalDoorHotspot()
    {
        GameObject finalScreen = GetScreen("FinalScreen");
        if (finalScreen == null)
        {
            return;
        }

        Transform existing = FindDeep(finalScreen.transform, "FinalDoorHotspot");
        GameObject hotspotObject;
        if (existing != null)
        {
            hotspotObject = existing.gameObject;
        }
        else
        {
            hotspotObject = new GameObject(
                "FinalDoorHotspot",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            hotspotObject.transform.SetParent(finalScreen.transform, false);
        }

        RectTransform rect = hotspotObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.50f, 0.16f);
        rect.anchorMax = new Vector2(0.98f, 0.88f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = hotspotObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.002f);
        image.raycastTarget = true;

        Button button = hotspotObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(practiceManager.FinishPractice);
        hotspotObject.transform.SetAsLastSibling();

        Button back = FindNamedComponent<Button>(finalScreen.transform, "Back");
        if (back != null)
        {
            back.transform.SetAsLastSibling();
        }
    }

}
