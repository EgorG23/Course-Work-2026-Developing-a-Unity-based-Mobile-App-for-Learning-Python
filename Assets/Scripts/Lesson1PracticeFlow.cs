using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class Lesson1PracticeFlow : MonoBehaviour
{
    [Header("Screens")]
    public GameObject startScreen;
    public GameObject officeScreen;
    public GameObject officeOnScreen;
    public GameObject electricityScreen;
    public GameObject computerScreen;
    public GameObject attentionScreen;
    public GameObject terminalScreen;
    public GameObject finalDoorScreen;
    public GameObject endScreen;
    public GameObject userDataScreenPrefab;
    public Sprite userDataPaperSprite;

    [Header("Messages")]
    [TextArea] public string noPowerMessage = "ПК не работает. Сначала почини щиток.";
    [TextArea] public string doorLockedMessage = "Дверь закрыта. Восстанови программу проверки в терминале.";
    [TextArea] public string codeSolvedMessage = "Проверка восстановлена. Дверь разблокирована.";

    [Header("Puzzle Layout")]
    public bool autoArrangePuzzleButtons = false;
    public bool showPuzzleHitboxes = false;
    public bool createPuzzleInEditor = true;

    [Header("Editor Hotspots")]
    public bool createHotspotsInEditor = true;
    public bool showHotspotsInEditor = true;
    public bool keepExistingHotspotPlacement = true;

    private readonly string[] typeOrder = { "int", "str", "float", "bool" };
    private readonly HashSet<string> matchedLeftKeys = new HashSet<string>();
    private const string ElectricityResourceFolder = "Lesson1Electricity/";
    private static readonly string[] HotspotNames =
    {
        "PaperHotspot",
        "PanelHotspot",
        "ComputerHotspot",
        "DoorHotspot",
        "ElectricityBackHotspot",
        "FinalDoorHotspot"
    };

    private PracticeManager practiceManager;
    private string selectedLeftKey;
    private Text puzzleStatusText;
    private Sprite runtimeUserDataPaperSprite;
    private Sprite runtimeElectricityBackgroundSprite;
    private readonly Dictionary<string, string> leftPuzzleSpriteByKey = new Dictionary<string, string>
    {
        { "int", "Group 34415" },
        { "str", "Group 34417" },
        { "float", "Group 34415000" },
        { "bool", "Group 34416" }
    };
    private readonly Dictionary<string, string> rightPuzzleSpriteByKey = new Dictionary<string, string>
    {
        { "int", "Group 34422" },
        { "str", "Group 34421" },
        { "float", "Group 34420" },
        { "bool", "Group 34419" }
    };

    private void Start()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Practice1_KvestScene")
        {
            enabled = false;
            return;
        }

        practiceManager = PracticeManager.Instance;
        if (practiceManager == null)
        {
            Debug.LogWarning("Lesson1PracticeFlow: PracticeManager is missing.");
            enabled = false;
            return;
        }

        ResolveScreens();
        RemoveLegacyComputerAttentionInlineInScene();

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ResetQuest();
        }

        practiceManager.ScreenChanged += OnScreenChanged;

        if (startScreen != null)
        {
            practiceManager.startScreen = startScreen;
        }

        if (practiceManager.CurrentScreen != null)
        {
            ConfigureScreen(practiceManager.CurrentScreen);
        }
    }

    private void OnDestroy()
    {
        if (practiceManager != null)
        {
            practiceManager.ScreenChanged -= OnScreenChanged;
        }
    }

    private void LateUpdate()
    {
        if (practiceManager == null || practiceManager.CurrentScreen == null)
        {
            return;
        }

        string currentName = NormalizeName(practiceManager.CurrentScreen.name);
        if (currentName != "Electricity")
        {
            return;
        }

        MaintainElectricityRuntimeBindings(practiceManager.CurrentScreen.transform);
    }

#if UNITY_EDITOR
    private bool pendingValidationRefresh;

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (pendingValidationRefresh)
        {
            return;
        }

        pendingValidationRefresh = true;

        UnityEditor.EditorApplication.delayCall += () =>
        {
            pendingValidationRefresh = false;

            if (this == null)
            {
                return;
            }

            if (createPuzzleInEditor)
            {
                EnsureEditorPuzzleExists();
            }

            if (createHotspotsInEditor)
            {
                EnsureEditorHotspotsExist();
            }
        };
    }

    [ContextMenu("Create Electricity Puzzle In Scene")]
    private void CreateElectricityPuzzleInScene()
    {
        EnsureEditorPuzzleExists(forceRebuild: true);
    }

    [ContextMenu("Create Lesson1 Hotspots In Scene")]
    private void CreateLesson1HotspotsInScene()
    {
        EnsureEditorHotspotsExist(forceUpdateLayout: true);
    }
#endif

    private void OnScreenChanged(GameObject screen)
    {
        ConfigureScreen(screen);
    }

    private void ResolveScreens()
    {
        Transform container = practiceManager.screenContainer;
        if (container == null)
        {
            GameObject found = GameObject.Find("ScreenContainer");
            if (found != null)
            {
                practiceManager.screenContainer = found.transform;
                container = found.transform;
            }
        }

        if (container == null)
        {
            return;
        }

        startScreen = startScreen ?? FindScreenByAnyName(container, "Start");
        officeScreen = officeScreen
            ?? FindScreenByAnyName(container, "Office")
            ?? FindScreenByContains(container, "office");
        officeOnScreen = officeOnScreen
            ?? FindScreenByAnyName(container, "OfficeOn", "OfficeOn ")
            ?? FindScreenByContains(container, "officeon");
        electricityScreen = electricityScreen ?? FindScreenByName(container, "Electricity");
        computerScreen = computerScreen
            ?? FindScreenByAnyName(container, "Computer")
            ?? FindScreenByContains(container, "computer");
        attentionScreen = attentionScreen
            ?? FindScreenByAnyName(container, "Attention")
            ?? FindScreenByContains(container, "attention");
        terminalScreen = terminalScreen
            ?? FindScreenByAnyName(container, "Terminal")
            ?? FindScreenByContains(container, "terminal");
        finalDoorScreen = finalDoorScreen
            ?? FindScreenByAnyName(container, "FinalScreen", "Final")
            ?? FindScreenByContains(container, "finalscreen", "final");
        endScreen = endScreen
            ?? FindScreenByAnyName(container, "End")
            ?? FindScreenByContains(container, "end");
        userDataScreenPrefab = userDataScreenPrefab
            ?? FindScreenByAnyName(container, "UserData");

#if UNITY_EDITOR
        if (userDataScreenPrefab == null)
        {
            userDataScreenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/lesson1practice/UserData.prefab");
        }
#endif
    }

    private static GameObject FindScreenByAnyName(Transform container, params string[] names)
    {
        foreach (string name in names)
        {
            GameObject found = FindScreenByName(container, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string normalized = value.Trim().Trim('\'', '"').Trim();
        const string cloneSuffix = "(Clone)";
        if (normalized.EndsWith(cloneSuffix, StringComparison.Ordinal))
        {
            normalized = normalized.Substring(0, normalized.Length - cloneSuffix.Length).Trim();
        }

        return normalized;
    }

    private static GameObject FindScreenByName(Transform container, string targetName)
    {
        string target = NormalizeName(targetName);
        foreach (Transform child in container)
        {
            if (NormalizeName(child.name) == target)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static GameObject FindScreenByContains(Transform container, params string[] tokens)
    {
        if (container == null || tokens == null || tokens.Length == 0)
        {
            return null;
        }

        foreach (Transform child in container)
        {
            string candidate = NormalizeName(child.name).ToLowerInvariant();
            foreach (string token in tokens)
            {
                if (!string.IsNullOrEmpty(token) && candidate.Contains(token.ToLowerInvariant()))
                {
                    return child.gameObject;
                }
            }
        }

        return null;
    }

    private static GameObject FindScreenGlobally(params string[] tokens)
    {
        if (tokens == null || tokens.Length == 0)
        {
            return null;
        }

        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            string candidate = NormalizeName(t.name).ToLowerInvariant();
            foreach (string token in tokens)
            {
                if (!string.IsNullOrEmpty(token) && candidate.Contains(token.ToLowerInvariant()))
                {
                    return t.gameObject;
                }
            }
        }

        return null;
    }

    private void ConfigureScreen(GameObject screen)
    {
        if (screen == null)
        {
            return;
        }

        DisableLegacyScreenButtons(screen.transform);

        string screenName = NormalizeName(screen.name);
        switch (screenName)
        {
            case "Start":
                ConfigureStart(screen);
                break;
            case "Office":
                ConfigureOffice(screen, false);
                break;
            case "OfficeOn":
                ConfigureOffice(screen, true);
                break;
            case "Electricity":
                ConfigureElectricity(screen);
                break;
            case "Computer":
                ResolveScreens();
                if (attentionScreen != null && attentionScreen != screen)
                {
                    practiceManager.ShowScreen(attentionScreen);
                    return;
                }
                ConfigureComputer(screen);
                break;
            case "Terminal":
                ConfigureTerminal(screen);
                break;
            case "FinalScreen":
                ConfigureFinalDoor(screen);
                break;
            case "End":
                ConfigureEnd(screen);
                break;
            case "UserData":
                ConfigureUserData(screen);
                break;
            case "Attention":
                ConfigureAttention(screen);
                break;
        }
    }

    private void ConfigureStart(GameObject screen)
    {
        Button goButton = FindButtonByName(screen.transform, "Go");
        if (goButton != null)
        {
            goButton.onClick = new Button.ButtonClickedEvent();
            goButton.onClick.AddListener(() =>
            {
                ResolveScreens();
                if (officeScreen != null)
                {
                    practiceManager.ShowScreen(officeScreen);
                    return;
                }

                GameObject next = FindOfficeDirect();
                if (next == null)
                {
                    ShowToast("Не найден экран кабинета (Office).");
                    return;
                }

                practiceManager.ShowScreen(next);
            });
        }

        SetButtonAction(screen.transform, "Back", practiceManager.ExitToMenu);
    }

    private GameObject FindOfficeDirect()
    {
        ResolveScreens();

        if (officeScreen != null)
        {
            return officeScreen;
        }

        Transform container = practiceManager != null ? practiceManager.screenContainer : null;
        if (container != null)
        {
            GameObject exact = FindScreenByAnyName(container, "Office");
            if (exact != null)
            {
                return exact;
            }

            foreach (Transform child in container)
            {
                string n = NormalizeName(child.name).ToLowerInvariant();
                if (n.Contains("office") && !n.Contains("officeon"))
                {
                    return child.gameObject;
                }
            }
        }

        GameObject byPath = GameObject.Find("Canvas/ScreenContainer/Office");
        if (byPath != null)
        {
            return byPath;
        }

        return GameObject.Find("Office");
    }

    private void ActivateScreenDirect(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Transform parent = target.transform.parent;
        if (parent != null)
        {
            foreach (Transform sibling in parent)
            {
                if (sibling != null && sibling.gameObject != target)
                {
                    sibling.gameObject.SetActive(false);
                }
            }
        }

        target.SetActive(true);
    }

    private void ConfigureOffice(GameObject screen, bool hasPower)
    {
        SetButtonAction(screen.transform, "Back", practiceManager.GoBack);
        Transform hotspotRoot = GetOfficeHotspotAnchorRoot(screen.transform);
        bool isCodeSolved = QuestManager.Instance != null && QuestManager.Instance.codeSolved;

        EnsureHotspotButton(
            hotspotRoot,
            "PaperHotspot",
            new Vector2(0.18f, 0.45f),
            new Vector2(0.46f, 0.72f),
            string.Empty,
            OpenUserDataScreen);

        SetButtonAction(screen.transform, "Button", OpenElectricityFromOffice);
        EnsureHotspotButton(
            hotspotRoot,
            "PanelHotspot",
            new Vector2(0.62f, 0.76f),
            new Vector2(0.94f, 0.95f),
            string.Empty,
            OpenElectricityFromOffice);

        if (hasPower)
        {
            SetButtonAction(screen.transform, "Button", OpenTerminalFromPoweredOffice);
            EnsureHotspotButton(
                hotspotRoot,
                "ComputerHotspot",
                new Vector2(0.45f, 0.28f),
                new Vector2(0.76f, 0.61f),
                string.Empty,
                OpenTerminalFromPoweredOffice);
        }
        else
        {
            EnsureHotspotButton(
                hotspotRoot,
                "ComputerHotspot",
                new Vector2(0.45f, 0.28f),
                new Vector2(0.76f, 0.61f),
                string.Empty,
                () => ShowToast(noPowerMessage));
        }

        Vector2 doorMin = isCodeSolved ? new Vector2(0.58f, 0.20f) : new Vector2(0.72f, 0.28f);
        Vector2 doorMax = isCodeSolved ? new Vector2(0.98f, 0.88f) : new Vector2(0.95f, 0.73f);
        Button doorButton = EnsureHotspotButton(
            hotspotRoot,
            "DoorHotspot",
            doorMin,
            doorMax,
            string.Empty,
            OnDoorHotspotPressed);

        if (doorButton != null)
        {
            bool visible = hasPower;
            doorButton.gameObject.SetActive(visible);
            if (visible)
            {
                doorButton.transform.SetAsLastSibling();
            }
        }

        NormalizeHotspotsForAdaptive(hotspotRoot);
    }

    private void OnDoorHotspotPressed()
    {
        ResolveScreens();

        if (QuestManager.Instance != null && QuestManager.Instance.codeSolved)
        {
            ShowFinalOrEndScreen();

            return;
        }

        ShowToast(doorLockedMessage);
    }

    private void OpenTerminalFromPoweredOffice()
    {
        ResolveScreens();
        if (attentionScreen != null)
        {
            practiceManager.ShowScreen(attentionScreen);
            return;
        }

        Transform container = practiceManager != null ? practiceManager.screenContainer : null;
        if (container != null)
        {
            GameObject fallbackAttention = FindScreenByAnyName(container, "Attention")
                ?? FindScreenByContains(container, "attention");
            if (fallbackAttention != null)
            {
                attentionScreen = fallbackAttention;
                practiceManager.ShowScreen(attentionScreen);
                return;
            }
        }

        ShowToast("Не найден экран Attention.");
    }

    private void OpenUserDataScreen()
    {
        ResolveScreens();
        if (userDataScreenPrefab != null)
        {
            practiceManager.ShowScreen(userDataScreenPrefab);
            return;
        }

        ShowToast("Данные с листка:\nuser_name = \"Alex\"\nuser_password = 1234567890", 5f);
    }

    private void OpenElectricityFromOffice()
    {
        ResolveScreens();
        if (electricityScreen == null)
        {
            Transform container = practiceManager != null ? practiceManager.screenContainer : null;
            if (container != null)
            {
                electricityScreen = FindScreenByContains(container, "electricity");
            }
        }

        if (electricityScreen == null)
        {
            ShowToast("Не найден экран щитка (Electricity).");
            return;
        }

        practiceManager.ShowScreen(electricityScreen);
    }

    private void ConfigureElectricity(GameObject screen)
    {
        Debug.Log("[Lesson1Puzzle] ConfigureElectricity on " + screen.name);
        DisableContentLayoutDrivers(screen.transform);
        ApplyElectricityBackgroundArt(screen.transform);

        SetButtonAction(screen.transform, "Back", BackFromElectricity);
        EnsureHotspotButton(
            screen.transform,
            "ElectricityBackHotspot",
            new Vector2(0.28f, 0.02f),
            new Vector2(0.72f, 0.18f),
                string.Empty,
                BackFromElectricity);

        Button legacyButton = FindButtonByName(screen.transform, "Button");
        if (legacyButton != null)
        {
            legacyButton.gameObject.SetActive(false);
        }

        BuildOrRefreshPuzzle(screen.transform);
        EnsureBackButtonOnTop(screen);
        NormalizeHotspotsForAdaptive(screen.transform);
    }

#if UNITY_EDITOR
    private void EnsureEditorPuzzleExists(bool forceRebuild = false)
    {
        GameObject electricity = GameObject.Find("Canvas/ScreenContainer/Electricity");
        if (electricity == null)
        {
            return;
        }

        Transform puzzleAnchorRoot = GetElectricityPuzzleAnchorRoot(electricity.transform);
        if (puzzleAnchorRoot == null)
        {
            puzzleAnchorRoot = electricity.transform;
        }

        Transform existing = FindChildByName(puzzleAnchorRoot, "TypePuzzle");
        if (existing == null)
        {
            Transform legacy = FindChildByName(electricity.transform, "TypePuzzle");
            if (legacy != null && legacy.parent != puzzleAnchorRoot)
            {
                legacy.SetParent(puzzleAnchorRoot, false);
                existing = legacy;
            }
        }

        if (existing != null && !forceRebuild)
        {
            BuildOrRefreshPuzzle(electricity.transform);
            EditorSceneManager.MarkSceneDirty(electricity.scene);
            return;
        }

        if (forceRebuild && existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        BuildOrRefreshPuzzle(electricity.transform);
        EditorSceneManager.MarkSceneDirty(electricity.scene);
    }

    private void EnsureEditorHotspotsExist(bool forceUpdateLayout = false)
    {
        Transform container = FindEditorScreenContainer();
        if (container == null)
        {
            return;
        }

        GameObject office = FindScreenByAnyName(container, "Office");
        if (office != null)
        {
            Transform officeHotspotRoot = GetOfficeHotspotAnchorRoot(office.transform);
            EnsureHotspotForEditor(
                officeHotspotRoot,
                "PaperHotspot",
                new Vector2(0.18f, 0.45f),
                new Vector2(0.46f, 0.72f),
                "paper",
                forceUpdateLayout);
            EnsureHotspotForEditor(
                officeHotspotRoot,
                "PanelHotspot",
                new Vector2(0.62f, 0.76f),
                new Vector2(0.94f, 0.95f),
                "panel",
                forceUpdateLayout);
            EnsureHotspotForEditor(
                officeHotspotRoot,
                "ComputerHotspot",
                new Vector2(0.45f, 0.28f),
                new Vector2(0.76f, 0.61f),
                "pc",
                forceUpdateLayout);
            EnsureHotspotForEditor(
                officeHotspotRoot,
                "DoorHotspot",
                new Vector2(0.72f, 0.28f),
                new Vector2(0.95f, 0.73f),
                "door",
                forceUpdateLayout);
        }

        GameObject officeOn = FindScreenByAnyName(container, "OfficeOn", "OfficeOn ");
        if (officeOn != null)
        {
            Transform officeOnHotspotRoot = GetOfficeHotspotAnchorRoot(officeOn.transform);
            EnsureHotspotForEditor(
                officeOnHotspotRoot,
                "PaperHotspot",
                new Vector2(0.18f, 0.45f),
                new Vector2(0.46f, 0.72f),
                "paper",
                forceUpdateLayout);
            EnsureHotspotForEditor(
                officeOnHotspotRoot,
                "PanelHotspot",
                new Vector2(0.62f, 0.76f),
                new Vector2(0.94f, 0.95f),
                "panel",
                forceUpdateLayout);
            EnsureHotspotForEditor(
                officeOnHotspotRoot,
                "ComputerHotspot",
                new Vector2(0.45f, 0.28f),
                new Vector2(0.76f, 0.61f),
                "pc",
                forceUpdateLayout);
            EnsureHotspotForEditor(
                officeOnHotspotRoot,
                "DoorHotspot",
                new Vector2(0.72f, 0.28f),
                new Vector2(0.95f, 0.73f),
                "door",
                forceUpdateLayout);
        }

        GameObject electricity = FindScreenByAnyName(container, "Electricity");
        if (electricity != null)
        {
            EnsureHotspotForEditor(
                electricity.transform,
                "ElectricityBackHotspot",
                new Vector2(0.28f, 0.02f),
                new Vector2(0.72f, 0.18f),
                "back",
                forceUpdateLayout);
        }

        if (container.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(container.gameObject.scene);
        }
    }

    private Transform FindEditorScreenContainer()
    {
        if (practiceManager != null && practiceManager.screenContainer != null)
        {
            return practiceManager.screenContainer;
        }

        GameObject byPath = GameObject.Find("Canvas/ScreenContainer");
        if (byPath != null)
        {
            return byPath.transform;
        }

        GameObject byName = GameObject.Find("ScreenContainer");
        return byName != null ? byName.transform : null;
    }

    private void EnsureHotspotForEditor(
        Transform root,
        string hotspotName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        string label,
        bool forceUpdateLayout)
    {
        Button hotspot = EnsureHotspotButton(
            root,
            hotspotName,
            anchorMin,
            anchorMax,
            label,
            null,
            keepExistingHotspotPlacement && !forceUpdateLayout);

        if (hotspot == null)
        {
            return;
        }

        Image image = hotspot.GetComponent<Image>();
        if (image != null)
        {
            if (showHotspotsInEditor)
            {
                image.color = new Color(1f, 0.5f, 0.05f, 0.28f);
            }
            else
            {
                image.color = new Color(1f, 1f, 1f, 0.002f);
            }
        }

        Text txt = hotspot.GetComponentInChildren<Text>(true);
        if (txt != null)
        {
            txt.enabled = showHotspotsInEditor;
            if (showHotspotsInEditor && !string.IsNullOrWhiteSpace(label))
            {
                txt.text = label;
            }
        }
    }
#endif

    private void ConfigureComputer(GameObject screen)
    {
        SetButtonAction(screen.transform, "Back", BackFromComputer);
        SetButtonAction(screen.transform, "ToConsole", OpenTerminalFromComputer);
        RemoveLegacyComputerAttentionInline(screen.transform);
    }

    private void ConfigureAttention(GameObject screen)
    {
        SetButtonAction(screen.transform, "Back", BackFromComputer);
        SetButtonAction(screen.transform, "Enter", OpenTerminalFromComputer);
        SetButtonAction(screen.transform, "Button", OpenTerminalFromComputer);
        SetButtonAction(screen.transform, "Go", OpenTerminalFromComputer);
        SetButtonAction(screen.transform, "Next", OpenTerminalFromComputer);
        SetButtonAction(screen.transform, "ToConsole", OpenTerminalFromComputer);
    }

    private void OpenTerminalFromComputer()
    {
        ResolveScreens();
        if (terminalScreen != null)
        {
            practiceManager.ShowScreen(terminalScreen);
            return;
        }

        Transform container = practiceManager != null ? practiceManager.screenContainer : null;
        if (container != null)
        {
            GameObject fallbackTerminal = FindScreenByAnyName(container, "Terminal")
                ?? FindScreenByContains(container, "terminal");
            if (fallbackTerminal != null)
            {
                terminalScreen = fallbackTerminal;
                practiceManager.ShowScreen(terminalScreen);
                return;
            }
        }

        ShowToast("Не найден экран терминала.");
    }

    private void RemoveLegacyComputerAttentionInline(Transform computerRoot)
    {
        if (computerRoot == null)
        {
            return;
        }

        Transform screenSurface = FindChildByName(computerRoot, "Screen") ?? computerRoot;
        Transform existing = FindChildByName(screenSurface, "AttentionInline");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }
    }

    private void RemoveLegacyComputerAttentionInlineInScene()
    {
        Transform container = practiceManager != null ? practiceManager.screenContainer : null;
        if (container == null)
        {
            return;
        }

        GameObject computer = FindScreenByAnyName(container, "Computer")
            ?? FindScreenByContains(container, "computer");
        if (computer == null)
        {
            return;
        }

        RemoveLegacyComputerAttentionInline(computer.transform);
    }

    private void ConfigureTerminal(GameObject screen)
    {
        SetButtonAction(screen.transform, "Back", BackFromTerminal);

        Button enterButton = FindButtonByName(screen.transform, "Enter");
        if (enterButton == null)
        {
            return;
        }

        TMP_InputField input = screen.GetComponentInChildren<TMP_InputField>(true);
        TMP_Text output = FindOutputText(screen.transform);
        Terminal terminal = screen.GetComponent<Terminal>() ?? screen.AddComponent<Terminal>();
        terminal.inputField = input;
        terminal.outputText = output;

        enterButton.onClick.RemoveAllListeners();
        enterButton.onClick.AddListener(() =>
        {
            terminal.CheckCommand();
            if (QuestManager.Instance != null && QuestManager.Instance.codeSolved)
            {
                ShowToast(codeSolvedMessage);
                ShowOfficeAfterCodeSolved();
            }
        });
    }

    private void ShowOfficeAfterCodeSolved()
    {
        ResolveScreens();
        if (officeOnScreen == null)
        {
            Transform container = practiceManager != null ? practiceManager.screenContainer : null;
            if (container != null)
            {
                officeOnScreen = FindScreenByAnyName(container, "OfficeOn", "OfficeOn ")
                    ?? FindScreenByContains(container, "officeon");
            }
        }

        if (officeOnScreen != null)
        {
            practiceManager.ShowScreen(officeOnScreen);
            return;
        }

        if (officeScreen != null)
        {
            practiceManager.ShowScreen(officeScreen);
        }
    }

    private void ConfigureFinalDoor(GameObject screen)
    {
        DisableContentLayoutDrivers(screen.transform);
        Transform hotspotRoot = GetOfficeHotspotAnchorRoot(screen.transform);

        SetButtonAction(screen.transform, "Button", OpenEndFromFinalScreen);
        SetButtonAction(screen.transform, "Back", () => practiceManager.ShowScreen(officeOnScreen));

        EnsureHotspotButton(
            hotspotRoot,
            "FinalDoorHotspot",
            new Vector2(0.58f, 0.20f),
            new Vector2(0.98f, 0.88f),
            string.Empty,
            OpenEndFromFinalScreen);
        NormalizeHotspotsForAdaptive(hotspotRoot);
    }

    private void ConfigureEnd(GameObject screen)
    {
        SetButtonAction(screen.transform, "Go", practiceManager.ExitToMenu);
    }

    private void ShowFinalOrEndScreen()
    {
        GameObject target = ResolveByNames("FinalScreen", "Final");
        if (target == null)
        {
            target = ResolveByNames("End");
        }

        if (target != null)
        {
            practiceManager.ShowScreen(target);
            return;
        }

        ShowToast("Не найден финальный экран.");
    }

    private void OpenEndFromFinalScreen()
    {
        GameObject endTarget = ResolveByNames("End");
        if (endTarget != null)
        {
            practiceManager.ShowScreen(endTarget);
            return;
        }

        ShowToast("Не найден экран завершения.");
    }

    private GameObject ResolveByNames(params string[] names)
    {
        ResolveScreens();
        Transform container = practiceManager != null ? practiceManager.screenContainer : null;
        if (container == null || names == null || names.Length == 0)
        {
            return null;
        }

        GameObject byExact = FindScreenByAnyName(container, names);
        if (byExact != null)
        {
            return byExact;
        }

        return FindScreenByContains(container, names);
    }

    private void ConfigureUserData(GameObject screen)
    {
        ApplyUserDataPaperVisual(screen);
        SetButtonAction(screen.transform, "Back", practiceManager.GoBack);
        SetButtonAction(screen.transform, "Button", practiceManager.GoBack);
        EnsureBackButtonOnTop(screen);
    }

    private void ApplyUserDataPaperVisual(GameObject screen)
    {
        if (screen == null)
        {
            return;
        }

        Transform content = FindChildByName(screen.transform, "Content");
        if (content != null)
        {
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
            }
        }

        Image paperImage = null;
        Transform office = FindChildByName(screen.transform, "Office");
        if (office != null)
        {
            paperImage = office.GetComponent<Image>();
            RectTransform officeRect = office.GetComponent<RectTransform>();
            if (officeRect != null)
            {
                officeRect.anchorMin = new Vector2(0.5f, 0.5f);
                officeRect.anchorMax = new Vector2(0.5f, 0.5f);
                officeRect.pivot = new Vector2(0.5f, 0.5f);
                officeRect.offsetMin = Vector2.zero;
                officeRect.offsetMax = Vector2.zero;
                officeRect.anchoredPosition = Vector2.zero;
                FitUserDataPaperRect(screen.transform, officeRect);
            }
        }

        if (paperImage == null)
        {
            Image[] images = screen.GetComponentsInChildren<Image>(true);
            foreach (Image img in images)
            {
                if (img == null || NormalizeName(img.gameObject.name) == "Back")
                {
                    continue;
                }

                paperImage = img;
                break;
            }
        }

        if (paperImage != null)
        {
            Sprite sprite = ResolveUserDataPaperSprite();
            if (sprite != null)
            {
                paperImage.sprite = sprite;
                paperImage.preserveAspect = true;
                paperImage.raycastTarget = false;

                RectTransform paperRect = paperImage.GetComponent<RectTransform>();
                if (paperRect != null)
                {
                    FitUserDataPaperRect(screen.transform, paperRect);
                }
            }
        }
    }

    private static void FitUserDataPaperRect(Transform screenRoot, RectTransform paperRect)
    {
        if (screenRoot == null || paperRect == null)
        {
            return;
        }

        RectTransform screenRect = screenRoot.GetComponent<RectTransform>();
        if (screenRect == null)
        {
            return;
        }

        float screenWidth = Mathf.Max(screenRect.rect.width, 1f);
        float screenHeight = Mathf.Max(screenRect.rect.height, 1f);

        // Source image ratio (Group 34413): width/height.
        const float paperAspect = 1766f / 2500f;
        float targetHeight = screenHeight * 0.72f;
        float targetWidth = targetHeight * paperAspect;

        float maxWidth = screenWidth * 0.82f;
        if (targetWidth > maxWidth)
        {
            targetWidth = maxWidth;
            targetHeight = targetWidth / paperAspect;
        }

        paperRect.sizeDelta = new Vector2(targetWidth, targetHeight);
    }

    private Sprite ResolveUserDataPaperSprite()
    {
        if (userDataPaperSprite != null)
        {
            return userDataPaperSprite;
        }

        if (runtimeUserDataPaperSprite != null)
        {
            return runtimeUserDataPaperSprite;
        }

        Sprite loadedSprite = Resources.Load<Sprite>("UserDataPaper");
        if (loadedSprite != null)
        {
            runtimeUserDataPaperSprite = loadedSprite;
            return runtimeUserDataPaperSprite;
        }

        Texture2D loadedTexture = Resources.Load<Texture2D>("UserDataPaper");
        if (loadedTexture != null)
        {
            runtimeUserDataPaperSprite = Sprite.Create(
                loadedTexture,
                new Rect(0, 0, loadedTexture.width, loadedTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        return runtimeUserDataPaperSprite;
    }

    private static void EnsureBackButtonOnTop(GameObject screen)
    {
        if (screen == null)
        {
            return;
        }

        Button back = FindButtonByName(screen.transform, "Back");
        if (back == null)
        {
            return;
        }

        back.gameObject.SetActive(true);
        RectTransform screenRect = screen.GetComponent<RectTransform>();
        RectTransform backRect = back.GetComponent<RectTransform>();
        if (screenRect == null || backRect == null)
        {
            return;
        }

        if (backRect.parent != screen.transform)
        {
            backRect.SetParent(screen.transform, false);
        }

        float width = Mathf.Max(screenRect.rect.width, 1f);
        float height = Mathf.Max(screenRect.rect.height, 1f);
        float targetWidth = Mathf.Clamp(width * 0.30f, 200f, 360f);
        float targetHeight = Mathf.Clamp(height * 0.095f, 84f, 150f);
        float bottomOffset = Mathf.Clamp(height * 0.11f, 70f, 170f);

        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0.5f);
        backRect.sizeDelta = new Vector2(targetWidth, targetHeight);
        backRect.anchoredPosition = new Vector2(0f, bottomOffset);
        backRect.SetAsLastSibling();

        Canvas backCanvas = back.GetComponent<Canvas>();
        if (backCanvas == null)
        {
            backCanvas = back.gameObject.AddComponent<Canvas>();
        }
        backCanvas.overrideSorting = true;
        backCanvas.sortingOrder = 1200;

        GraphicRaycaster raycaster = back.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = back.gameObject.AddComponent<GraphicRaycaster>();
        }
        raycaster.enabled = true;
    }

    private static void NormalizeHotspotsForAdaptive(Transform root)
    {
        if (root == null)
        {
            return;
        }

        foreach (string hotspotName in HotspotNames)
        {
            Transform hotspot = FindChildByName(root, hotspotName);
            if (hotspot == null)
            {
                continue;
            }

            RectTransform hotspotRect = hotspot as RectTransform;
            RectTransform parentRect = hotspotRect != null ? hotspotRect.parent as RectTransform : null;
            if (hotspotRect == null || parentRect == null)
            {
                continue;
            }

            ConvertRectToParentAnchors(hotspotRect, parentRect);
        }
    }

    private static Transform GetOfficeHotspotAnchorRoot(Transform screenRoot)
    {
        if (screenRoot == null)
        {
            return null;
        }

        Transform content = FindChildByName(screenRoot, "Content");
        if (content != null)
        {
            foreach (Transform child in content)
            {
                if (child == null)
                {
                    continue;
                }

                string n = NormalizeName(child.name);
                if (n.Equals("Office", StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
        }

        return screenRoot;
    }

    private static void ConvertRectToParentAnchors(RectTransform rect, RectTransform parentRect)
    {
        Vector2 parentSize = parentRect.rect.size;
        if (parentSize.x <= 0.01f || parentSize.y <= 0.01f)
        {
            return;
        }

        Vector3[] worldCorners = new Vector3[4];
        rect.GetWorldCorners(worldCorners);

        Vector3 localBL = parentRect.InverseTransformPoint(worldCorners[0]);
        Vector3 localTR = parentRect.InverseTransformPoint(worldCorners[2]);

        Vector2 parentMin = parentRect.rect.min;
        Vector2 anchorMin = new Vector2(
            Mathf.Clamp01((localBL.x - parentMin.x) / parentSize.x),
            Mathf.Clamp01((localBL.y - parentMin.y) / parentSize.y));
        Vector2 anchorMax = new Vector2(
            Mathf.Clamp01((localTR.x - parentMin.x) / parentSize.x),
            Mathf.Clamp01((localTR.y - parentMin.y) / parentSize.y));

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition3D = new Vector3(0f, 0f, rect.anchoredPosition3D.z);
    }

    private void BuildOrRefreshPuzzle(Transform electricityScreenRoot)
    {
        Transform puzzleParent = GetElectricityPuzzleAnchorRoot(electricityScreenRoot);
        if (puzzleParent == null)
        {
            puzzleParent = electricityScreenRoot;
        }

        Transform existingRoot = FindChildByName(puzzleParent, "TypePuzzle");
        if (existingRoot == null)
        {
            Transform legacyRoot = FindChildByName(electricityScreenRoot, "TypePuzzle");
            if (legacyRoot != null && legacyRoot.parent != puzzleParent)
            {
                legacyRoot.SetParent(puzzleParent, false);
                existingRoot = legacyRoot;
            }
        }

        GameObject puzzleRoot;

        if (existingRoot == null)
        {
            puzzleRoot = new GameObject("TypePuzzle", typeof(RectTransform), typeof(Image), typeof(Canvas));
            RectTransform rootRect = puzzleRoot.GetComponent<RectTransform>();
            rootRect.SetParent(puzzleParent, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.SetAsLastSibling();

            Image rootImage = puzzleRoot.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0f);
            rootImage.raycastTarget = false;
            Canvas rootCanvas = puzzleRoot.GetComponent<Canvas>();
            rootCanvas.overrideSorting = true;
            rootCanvas.sortingOrder = 400;
            if (puzzleRoot.GetComponent<GraphicRaycaster>() == null)
            {
                puzzleRoot.AddComponent<GraphicRaycaster>();
            }

            puzzleStatusText = CreateStatusLabel(rootRect, "Соедини одинаковые типы");
        }
        else
        {
            puzzleRoot = existingRoot.gameObject;
            Canvas rootCanvas = puzzleRoot.GetComponent<Canvas>();
            if (rootCanvas != null)
            {
                rootCanvas.overrideSorting = true;
                rootCanvas.sortingOrder = 400;
                if (puzzleRoot.GetComponent<GraphicRaycaster>() == null)
                {
                    puzzleRoot.AddComponent<GraphicRaycaster>();
                }
            }
            if (puzzleStatusText == null)
            {
                puzzleStatusText = puzzleRoot.GetComponentInChildren<Text>(true);
            }
        }

        RectTransform puzzleRect = puzzleRoot.GetComponent<RectTransform>();
        if (puzzleRect != null)
        {
            if (puzzleRect.parent != puzzleParent)
            {
                puzzleRect.SetParent(puzzleParent, false);
            }

            puzzleRect.anchorMin = Vector2.zero;
            puzzleRect.anchorMax = Vector2.one;
            puzzleRect.pivot = new Vector2(0.5f, 0.5f);
            puzzleRect.anchoredPosition = Vector2.zero;
            puzzleRect.sizeDelta = Vector2.zero;
            puzzleRect.offsetMin = Vector2.zero;
            puzzleRect.offsetMax = Vector2.zero;
            puzzleRect.SetAsLastSibling();
        }

        EnsurePuzzleButtons(puzzleRect);

        if (IsPowerFixed())
        {
            SetPuzzleInteractable(puzzleRoot.transform, false);
            SetAllPairsSolvedVisuals(puzzleRoot.transform);
            if (puzzleStatusText != null)
            {
                puzzleStatusText.text = "Питание восстановлено";
            }
            return;
        }

        selectedLeftKey = null;
        matchedLeftKeys.Clear();
        BindPuzzleButtons(puzzleRoot.transform);
        SetPuzzleInteractable(puzzleRoot.transform, true);
        ResetPuzzleVisuals(puzzleRoot.transform);
        if (puzzleStatusText != null)
        {
            puzzleStatusText.text = "Соедини одинаковые типы";
        }
    }

    private void EnsurePuzzleButtons(RectTransform puzzleRoot)
    {
        if (puzzleRoot == null)
        {
            return;
        }

        List<Transform> toRemove = new List<Transform>();
        foreach (Transform child in puzzleRoot)
        {
            if (child == null)
            {
                continue;
            }

            string name = child.name ?? string.Empty;
            if (name.StartsWith("Left_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Right_", StringComparison.OrdinalIgnoreCase))
            {
                toRemove.Add(child);
            }
        }

        foreach (Transform child in toRemove)
        {
            if (child == null)
            {
                continue;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(child.gameObject);
            }
            else
            {
                Destroy(child.gameObject);
            }
#else
            Destroy(child.gameObject);
#endif
        }

        CreateTypeButtons(puzzleRoot, true);
        CreateTypeButtons(puzzleRoot, false);
    }

    private void MaintainElectricityRuntimeBindings(Transform electricityScreenRoot)
    {
        if (electricityScreenRoot == null)
        {
            return;
        }

        DisableContentLayoutDrivers(electricityScreenRoot);
        Transform puzzleParent = GetElectricityPuzzleAnchorRoot(electricityScreenRoot);
        if (puzzleParent == null)
        {
            puzzleParent = electricityScreenRoot;
        }

        Transform puzzle = FindChildByName(puzzleParent, "TypePuzzle");
        if (puzzle == null)
        {
            Transform legacy = FindChildByName(electricityScreenRoot, "TypePuzzle");
            if (legacy != null && legacy.parent != puzzleParent)
            {
                legacy.SetParent(puzzleParent, false);
                puzzle = legacy;
            }
        }

        if (puzzle == null)
        {
            return;
        }

        if (autoArrangePuzzleButtons)
        {
            RectTransform puzzleRect = puzzle.GetComponent<RectTransform>();
            if (puzzleRect != null)
            {
                if (puzzleRect.parent != electricityScreenRoot)
                {
                    puzzleRect.SetParent(puzzleParent, false);
                }

                puzzleRect.anchorMin = Vector2.zero;
                puzzleRect.anchorMax = Vector2.one;
                puzzleRect.pivot = new Vector2(0.5f, 0.5f);
                puzzleRect.anchoredPosition = Vector2.zero;
                puzzleRect.sizeDelta = Vector2.zero;
                puzzleRect.offsetMin = Vector2.zero;
                puzzleRect.offsetMax = Vector2.zero;
                puzzleRect.SetAsLastSibling();
            }
        }

        BindPuzzleButtons(puzzle);
    }

    private static Transform GetElectricityPuzzleAnchorRoot(Transform screenRoot)
    {
        if (screenRoot == null)
        {
            return null;
        }

        Transform content = FindChildByName(screenRoot, "Content");
        if (content != null)
        {
            foreach (Transform child in content)
            {
                if (child == null)
                {
                    continue;
                }

                if (string.Equals(NormalizeName(child.name), "Office", StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
        }

        return screenRoot;
    }

    private void BindPuzzleButtons(Transform puzzleRoot)
    {
        Button[] buttons = puzzleRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            string rawName = button.gameObject.name ?? string.Empty;
            bool isLeft = rawName.StartsWith("Left_", StringComparison.OrdinalIgnoreCase);
            bool isRight = rawName.StartsWith("Right_", StringComparison.OrdinalIgnoreCase);
            if (!isLeft && !isRight)
            {
                continue;
            }

            int splitIndex = rawName.IndexOf('_');
            if (splitIndex < 0 || splitIndex >= rawName.Length - 1)
            {
                continue;
            }

            string key = ExtractPuzzleKey(rawName);
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            button.enabled = true;
            button.interactable = true;
            if (autoArrangePuzzleButtons)
            {
                AlignPuzzleButton(button.GetComponent<RectTransform>(), isLeft, key);
            }
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                Debug.Log("[Lesson1Puzzle] click " + (isLeft ? "L:" : "R:") + key);
                OnTypeButtonPressed(key, isLeft, button.gameObject);
            });

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                if (image.color.a < 0.95f)
                {
                    image.color = Color.white;
                }
            }
        }
    }

    private Text CreateStatusLabel(RectTransform parent, string message)
    {
        GameObject textGo = new GameObject("PuzzleStatus", typeof(RectTransform), typeof(Text));
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.anchorMin = new Vector2(0.18f, 0.86f);
        textRect.anchorMax = new Vector2(0.82f, 0.96f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textGo.GetComponent<Text>();
        text.text = message;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.resizeTextForBestFit = true;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return text;
    }

    private void CreateTypeButtons(RectTransform parent, bool isLeftColumn)
    {
        for (int i = 0; i < typeOrder.Length; i++)
        {
            string typeName = typeOrder[i];
            string buttonName = (isLeftColumn ? "Left_" : "Right_") + typeName;

            GameObject buttonGo = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            rect.SetParent(parent, false);

            AlignPuzzleButton(rect, isLeftColumn, typeName);

            Image image = buttonGo.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            image.sprite = GetPuzzleSpriteForKey(typeName, isLeftColumn);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;

            Button button = buttonGo.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.96f, 1f, 1f);
            colors.pressedColor = new Color(0.82f, 0.9f, 1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            button.colors = colors;
            button.onClick.AddListener(() => OnTypeButtonPressed(typeName, isLeftColumn, buttonGo));
        }
    }

    private void OnTypeButtonPressed(string key, bool isLeftColumn, GameObject buttonGo)
    {
        Debug.Log("[Lesson1Puzzle] OnTypeButtonPressed key=" + key + " isLeft=" + isLeftColumn + " selectedLeft=" + selectedLeftKey);
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (isLeftColumn)
        {
            if (matchedLeftKeys.Contains(key))
            {
                return;
            }

            selectedLeftKey = key;
            HighlightSelectedLeft(buttonGo.transform.parent, key);
            if (puzzleStatusText != null)
            {
                puzzleStatusText.text = "Выбрано слева. Теперь выбери справа.";
            }
            return;
        }

        if (string.IsNullOrEmpty(selectedLeftKey))
        {
            if (puzzleStatusText != null)
            {
                puzzleStatusText.text = "Сначала выбери тип слева.";
            }
            return;
        }

        string expectedRightKey = GetExpectedRightForLeft(selectedLeftKey);
        if (expectedRightKey == key)
        {
            matchedLeftKeys.Add(selectedLeftKey);
            MarkPairSolved(buttonGo.transform.parent, selectedLeftKey, key);
            selectedLeftKey = null;

            if (matchedLeftKeys.Count == 4)
            {
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.powerFixed = true;
                }

                if (puzzleStatusText != null)
                {
                    puzzleStatusText.text = "Питание восстановлено";
                }

                SetPuzzleInteractable(buttonGo.transform.parent, false);
                ShowPoweredOffice();
            }
            else if (puzzleStatusText != null)
            {
                puzzleStatusText.text = "Верно! Осталось: " + (4 - matchedLeftKeys.Count);
            }
        }
        else
        {
            selectedLeftKey = null;
            HighlightSelectedLeft(buttonGo.transform.parent, null);
            if (puzzleStatusText != null)
            {
                puzzleStatusText.text = "Неверная пара. Попробуй ещё.";
            }
        }
    }

    private static void SetPuzzleInteractable(Transform puzzleRoot, bool interactable)
    {
        Button[] buttons = puzzleRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.gameObject.name.StartsWith("Left_") || button.gameObject.name.StartsWith("Right_"))
            {
                button.interactable = interactable;
            }
        }
    }

    private static void ResetPuzzleVisuals(Transform puzzleRoot)
    {
        Button[] buttons = puzzleRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (!button.gameObject.name.StartsWith("Left_") && !button.gameObject.name.StartsWith("Right_"))
            {
                continue;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = Color.white;
            }
        }
    }

    private static void HighlightSelectedLeft(Transform puzzleRoot, string selectedKey)
    {
        Button[] buttons = puzzleRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (!button.gameObject.name.StartsWith("Left_"))
            {
                continue;
            }

            Image image = button.GetComponent<Image>();
            if (image == null)
            {
                continue;
            }

            string key = ExtractPuzzleKey(button.gameObject.name);
            image.color = key == selectedKey
                ? new Color(1f, 0.95f, 0.6f, 1f)
                : Color.white;
        }
    }

    private static void MarkPairSolved(Transform puzzleRoot, string solvedLeftKey, string solvedRightKey)
    {
        Button[] buttons = puzzleRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            bool isLeft = button.gameObject.name.StartsWith("Left_", StringComparison.OrdinalIgnoreCase);
            bool isRight = button.gameObject.name.StartsWith("Right_", StringComparison.OrdinalIgnoreCase);
            string key = ExtractPuzzleKey(button.gameObject.name);
            bool isSolvedPairButton = (isLeft && key == solvedLeftKey) || (isRight && key == solvedRightKey);
            if (!isSolvedPairButton)
            {
                continue;
            }

            button.interactable = false;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.72f, 1f, 0.72f, 1f);
            }
        }
    }

    private static void SetAllPairsSolvedVisuals(Transform puzzleRoot)
    {
        Button[] buttons = puzzleRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (!button.gameObject.name.StartsWith("Left_") && !button.gameObject.name.StartsWith("Right_"))
            {
                continue;
            }

            button.interactable = false;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.72f, 1f, 0.72f, 1f);
            }
        }
    }

    private static void AlignPuzzleButton(RectTransform rect, bool isLeft, string key)
    {
        if (rect == null)
        {
            return;
        }

        if (!TryGetPuzzleAnchors(isLeft, key, out Vector2 anchorMin, out Vector2 anchorMax))
        {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static bool TryGetPuzzleAnchors(bool isLeft, string key, out Vector2 anchorMin, out Vector2 anchorMax)
    {
        // Deliberately shuffled layout (still adaptive) so puzzle feels less linear.
        if (isLeft)
        {
            switch (key)
            {
                case "float":
                    anchorMin = new Vector2(0.10f, 0.64f);
                    anchorMax = new Vector2(0.36f, 0.72f);
                    return true;
                case "int":
                    anchorMin = new Vector2(0.10f, 0.52f);
                    anchorMax = new Vector2(0.36f, 0.60f);
                    return true;
                case "bool":
                    anchorMin = new Vector2(0.10f, 0.40f);
                    anchorMax = new Vector2(0.36f, 0.48f);
                    return true;
                case "str":
                    anchorMin = new Vector2(0.10f, 0.28f);
                    anchorMax = new Vector2(0.36f, 0.36f);
                    return true;
            }
        }
        else
        {
            switch (key)
            {
                case "str":
                    anchorMin = new Vector2(0.64f, 0.64f);
                    anchorMax = new Vector2(0.90f, 0.72f);
                    return true;
                case "bool":
                    anchorMin = new Vector2(0.64f, 0.52f);
                    anchorMax = new Vector2(0.92f, 0.60f);
                    return true;
                case "int":
                    anchorMin = new Vector2(0.64f, 0.40f);
                    anchorMax = new Vector2(0.92f, 0.48f);
                    return true;
                case "float":
                    anchorMin = new Vector2(0.64f, 0.28f);
                    anchorMax = new Vector2(0.92f, 0.36f);
                    return true;
            }
        }

        anchorMin = Vector2.zero;
        anchorMax = Vector2.zero;
        return false;
    }

    private static string ExtractPuzzleKey(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return string.Empty;
        }

        int splitIndex = objectName.IndexOf('_');
        if (splitIndex < 0 || splitIndex >= objectName.Length - 1)
        {
            return string.Empty;
        }

        return objectName.Substring(splitIndex + 1).Trim().ToLowerInvariant();
    }

    private static string GetExpectedRightForLeft(string leftKey)
    {
        return leftKey ?? string.Empty;
    }

    private Sprite GetPuzzleSpriteForKey(string key, bool isLeftColumn)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        Dictionary<string, string> map = isLeftColumn ? leftPuzzleSpriteByKey : rightPuzzleSpriteByKey;
        if (!map.TryGetValue(key, out string spriteName) || string.IsNullOrEmpty(spriteName))
        {
            return null;
        }

        return Resources.Load<Sprite>(ElectricityResourceFolder + spriteName);
    }

    private void ApplyElectricityBackgroundArt(Transform screenRoot)
    {
        if (screenRoot == null)
        {
            return;
        }

        Transform puzzleAnchor = GetElectricityPuzzleAnchorRoot(screenRoot);
        if (puzzleAnchor == null)
        {
            return;
        }

        Image bgImage = puzzleAnchor.GetComponent<Image>();
        if (bgImage == null)
        {
            return;
        }

        if (runtimeElectricityBackgroundSprite == null)
        {
            runtimeElectricityBackgroundSprite = Resources.Load<Sprite>(ElectricityResourceFolder + "Group 34414999");
        }

        if (runtimeElectricityBackgroundSprite != null)
        {
            bgImage.sprite = runtimeElectricityBackgroundSprite;
            bgImage.type = Image.Type.Simple;
            bgImage.preserveAspect = true;
            bgImage.color = Color.white;
        }
    }

    private void ShowPoweredOffice()
    {
        ResolveScreens();
        if (officeOnScreen == null)
        {
            Transform container = practiceManager != null ? practiceManager.screenContainer : null;
            if (container != null)
            {
                officeOnScreen = FindScreenByAnyName(container, "OfficeOn", "OfficeOn ")
                    ?? FindScreenByContains(container, "officeon");
            }
        }

        if (officeOnScreen != null)
        {
            practiceManager.ShowScreen(officeOnScreen);
            return;
        }

        if (officeScreen != null)
        {
            practiceManager.ShowScreen(officeScreen);
        }
    }

    private bool IsPowerFixed()
    {
        return QuestManager.Instance != null && QuestManager.Instance.powerFixed;
    }

    private void SetButtonAction(Transform root, string buttonName, Action action)
    {
        Button button = FindButtonByName(root, buttonName);
        if (button == null)
        {
            return;
        }

        ScreenButton legacyScreenButton = button.GetComponent<ScreenButton>();
        if (legacyScreenButton != null)
        {
            legacyScreenButton.enabled = false;
        }

        // Replace UnityEvent instance to drop persistent prefab listeners like GoToMenu.
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => action?.Invoke());
    }

    private static Button FindButtonByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.name.Trim() == targetName)
            {
                return button;
            }
        }

        return null;
    }

    private static TMP_Text FindOutputText(Transform root)
    {
        Transform output = FindChildByName(root, "Output");
        if (output == null)
        {
            return root.GetComponentInChildren<TMP_Text>(true);
        }

        TMP_Text text = output.GetComponentInChildren<TMP_Text>(true);
        return text;
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Trim() == targetName)
            {
                return child;
            }
        }

        return null;
    }

    private Button EnsureHotspotButton(
        Transform root,
        string hotspotName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        string label,
        Action onClick,
        bool preserveExistingPlacement = true)
    {
        if (root == null)
        {
            return null;
        }

        Transform existing = FindChildByName(root, hotspotName);
        if (existing == null)
        {
            Transform screenRoot = GetOwningScreenRoot(root);

            existing = FindChildByName(screenRoot, hotspotName);
            if (existing != null && existing.parent != root)
            {
                existing.SetParent(root, true);
            }
        }

        GameObject hotspotGo;
        if (existing == null)
        {
            hotspotGo = new GameObject(hotspotName, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = hotspotGo.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = hotspotGo.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.002f);

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textGo.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.enabled = !string.IsNullOrEmpty(label);
        }
        else
        {
            hotspotGo = existing.gameObject;
        }

        RectTransform hotspotRect = hotspotGo.GetComponent<RectTransform>();
        if (hotspotRect != null)
        {
            bool shouldApplyLayout = existing == null || !preserveExistingPlacement;
            if (shouldApplyLayout)
            {
                hotspotRect.anchorMin = anchorMin;
                hotspotRect.anchorMax = anchorMax;
                hotspotRect.offsetMin = Vector2.zero;
                hotspotRect.offsetMax = Vector2.zero;
            }
            hotspotRect.SetAsLastSibling();
        }

        Button hotspotButton = hotspotGo.GetComponent<Button>();
        if (hotspotButton != null)
        {
            hotspotButton.onClick.RemoveAllListeners();
            hotspotButton.onClick.AddListener(() => onClick?.Invoke());
        }

        Image hotspotImage = hotspotGo.GetComponent<Image>();
        if (hotspotImage != null && Application.isPlaying)
        {
            hotspotImage.color = new Color(1f, 1f, 1f, 0.002f);
        }

        Text hotspotLabel = hotspotGo.GetComponentInChildren<Text>(true);
        if (hotspotLabel != null)
        {
            hotspotLabel.text = label;
            hotspotLabel.enabled = !string.IsNullOrEmpty(label);
        }

        return hotspotButton;
    }

    private static Transform GetOwningScreenRoot(Transform anyChild)
    {
        if (anyChild == null)
        {
            return null;
        }

        Transform current = anyChild;
        while (current.parent != null)
        {
            if (string.Equals(NormalizeName(current.parent.name), "ScreenContainer", StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            current = current.parent;
        }

        return anyChild;
    }

    private static void DisableLegacyScreenButtons(Transform root)
    {
        if (root == null)
        {
            return;
        }

        ScreenButton[] screenButtons = root.GetComponentsInChildren<ScreenButton>(true);
        foreach (ScreenButton screenButton in screenButtons)
        {
            if (screenButton != null)
            {
                screenButton.enabled = false;
            }
        }
    }

    private static void DisableButtonRaycast(Transform root, string buttonName)
    {
        Button button = FindButtonByName(root, buttonName);
        if (button == null)
        {
            return;
        }

        button.onClick = new Button.ButtonClickedEvent();
        button.interactable = false;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = false;
        }

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private static void DisableContentLayoutDrivers(Transform screenRoot)
    {
        Transform content = FindChildByName(screenRoot, "Content");
        if (content == null)
        {
            return;
        }

        LayoutGroup[] layoutGroups = content.GetComponents<LayoutGroup>();
        foreach (LayoutGroup layoutGroup in layoutGroups)
        {
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
            }
        }

        ContentSizeFitter[] fitters = content.GetComponents<ContentSizeFitter>();
        foreach (ContentSizeFitter fitter in fitters)
        {
            if (fitter != null)
            {
                fitter.enabled = false;
            }
        }
    }

    private void BackFromElectricity()
    {
        ResolveScreens();
        if (IsPowerFixed() && officeOnScreen != null)
        {
            practiceManager.ShowScreen(officeOnScreen);
            return;
        }

        if (officeScreen != null)
        {
            practiceManager.ShowScreen(officeScreen);
            return;
        }

        practiceManager.GoBack();
    }

    private void BackFromComputer()
    {
        ResolveScreens();
        if (officeOnScreen != null)
        {
            practiceManager.ShowScreen(officeOnScreen);
            return;
        }

        if (officeScreen != null)
        {
            practiceManager.ShowScreen(officeScreen);
            return;
        }

        practiceManager.GoBack();
    }

    private void BackFromTerminal()
    {
        ResolveScreens();
        if (computerScreen != null)
        {
            practiceManager.ShowScreen(computerScreen);
            return;
        }

        BackFromComputer();
    }

    private void ShowToast(string message, float duration = 1.4f)
    {
        if (string.IsNullOrEmpty(message) || practiceManager == null || practiceManager.screenContainer == null)
        {
            return;
        }

        Transform existing = FindChildByName(practiceManager.screenContainer, "Lesson1Toast");
        GameObject toastGo;
        if (existing == null)
        {
            toastGo = new GameObject("Lesson1Toast", typeof(RectTransform), typeof(Image));
            RectTransform toastRect = toastGo.GetComponent<RectTransform>();
            toastRect.SetParent(practiceManager.screenContainer, false);
            toastRect.anchorMin = new Vector2(0.18f, 0.84f);
            toastRect.anchorMax = new Vector2(0.82f, 0.96f);
            toastRect.offsetMin = Vector2.zero;
            toastRect.offsetMax = Vector2.zero;

            Image bg = toastGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.72f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(toastRect, false);
            textRect.anchorMin = new Vector2(0.05f, 0.1f);
            textRect.anchorMax = new Vector2(0.95f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text label = textGo.GetComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        else
        {
            toastGo = existing.gameObject;
        }

        Text toastText = toastGo.GetComponentInChildren<Text>(true);
        if (toastText != null)
        {
            toastText.text = message;
        }

        toastGo.SetActive(true);
        CancelInvoke(nameof(HideToast));
        Invoke(nameof(HideToast), Mathf.Max(0.2f, duration));
    }

    private void HideToast()
    {
        if (practiceManager == null || practiceManager.screenContainer == null)
        {
            return;
        }

        Transform toast = FindChildByName(practiceManager.screenContainer, "Lesson1Toast");
        if (toast != null)
        {
            toast.gameObject.SetActive(false);
        }
    }
}
