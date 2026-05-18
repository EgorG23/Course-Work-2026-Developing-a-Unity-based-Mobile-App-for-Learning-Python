using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public static class Practice0QuestFixer
{
    private const string ScenePath = "Assets/Scenes/Practice0_KvestScene.unity";
    private const string PendingKey = "Practice0QuestFixer.Pending";

    [MenuItem("Tools/Practice0/Fix Terminal And Key Logic")]
    public static void FixTerminalAndKeyLogic()
    {
        SessionState.SetBool(PendingKey, true);
        ContinueWhenReady();
    }

    [InitializeOnLoadMethod]
    private static void ContinueAfterReload()
    {
        EditorApplication.delayCall += ContinueWhenReady;
    }

    private static void ContinueWhenReady()
    {
        if (!SessionState.GetBool(PendingKey, false))
        {
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += ContinueWhenReady;
            return;
        }

        ApplyFixes();
        SessionState.SetBool(PendingKey, false);
    }

    private static void ApplyFixes()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath);
        }

        GameObject screenContainer = GameObject.Find("Canvas/ScreenContainer");
        if (screenContainer == null)
        {
            Debug.LogError("Practice0 fixer: Canvas/ScreenContainer was not found.");
            return;
        }

        GameObject terminal = FindChild(screenContainer.transform, "Terminal");
        GameObject safeOpen = FindChild(screenContainer.transform, "Safe Open");
        GameObject lockerOpen = FindChild(screenContainer.transform, "LockerOpen");
        GameObject exit = FindChild(screenContainer.transform, "Exit");
        GameObject end = FindChild(screenContainer.transform, "End");
        GameObject keyNotification = FindChild(screenContainer.transform, "KeyNotification");
        GameObject lockerOpenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/lesson0practice/LockerOpen.prefab");
        GameObject endPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/lesson0practice/End.prefab");
        GameObject keyNotificationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/lesson0practice/KeyNotification.prefab");

        SavePrefabFix("Assets/UI/lesson0practice/Terminal.prefab", root =>
        {
            FixTerminalLayout(root);
            FixTerminalEvents(root);
        });
        SavePrefabFix("Assets/UI/lesson0practice/Safe Open.prefab", root => FixKeyPickup(root, keyNotificationPrefab, lockerOpenPrefab));
        SavePrefabFix("Assets/UI/lesson0practice/Exit.prefab", root => FixDoor(root, endPrefab));

        if (terminal != null)
        {
            FixTerminalLayout(terminal);
            FixTerminalEvents(terminal);
            EditorUtility.SetDirty(terminal);
        }

        if (safeOpen != null)
        {
            FixKeyPickup(safeOpen, keyNotification, lockerOpen);
            EditorUtility.SetDirty(safeOpen);
        }

        if (exit != null)
        {
            FixDoor(exit, end);
            EditorUtility.SetDirty(exit);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("Practice0 fixer: terminal layout and key-door logic updated.");
    }

    private static void FixTerminalLayout(GameObject terminal)
    {
        RectTransform root = terminal.GetComponent<RectTransform>();
        if (root != null)
        {
            Stretch(root, Vector2.zero, Vector2.one);
        }

        RectTransform background = FindRect(terminal.transform, "Background");
        if (background != null)
        {
            Stretch(background, Vector2.zero, Vector2.one);
        }

        RectTransform output = FindRect(terminal.transform, "Output");
        if (output != null)
        {
            Stretch(output, new Vector2(0.12f, 0.51f), new Vector2(0.88f, 0.61f));
        }

        RectTransform input = FindRect(terminal.transform, "Input");
        if (input != null)
        {
            Stretch(input, new Vector2(0.12f, 0.39f), new Vector2(0.66f, 0.47f));
        }

        RectTransform enter = FindRect(terminal.transform, "Enter");
        if (enter != null)
        {
            Stretch(enter, new Vector2(0.70f, 0.39f), new Vector2(0.88f, 0.47f));
        }

        RectTransform back = FindRect(terminal.transform, "Back");
        if (back != null)
        {
            Stretch(back, new Vector2(0.06f, 0.06f), new Vector2(0.26f, 0.13f));
        }

        TMP_InputField inputField = terminal.GetComponent<Terminal>()?.inputField;
        if (inputField == null && input != null)
        {
            inputField = input.GetComponent<TMP_InputField>();
        }

        TMP_Text outputText = terminal.GetComponent<Terminal>()?.outputText;
        if (outputText == null && output != null)
        {
            outputText = output.GetComponent<TMP_Text>();
        }

        ConfigureInput(inputField);
        ConfigureText(outputText, 38f, TextAlignmentOptions.MidlineLeft);
        ConfigureButtonText(enter, 38f);
        ConfigureButtonText(back, 38f);
    }

    private static void FixTerminalEvents(GameObject terminal)
    {
        Terminal terminalComponent = terminal.GetComponent<Terminal>();
        if (terminalComponent == null)
        {
            terminalComponent = terminal.AddComponent<Terminal>();
        }

        TMP_InputField inputField = FindChild(terminal.transform, "Input")?.GetComponent<TMP_InputField>();
        TMP_Text outputText = FindChild(terminal.transform, "Output")?.GetComponent<TMP_Text>();
        Button enterButton = FindChild(terminal.transform, "Enter")?.GetComponent<Button>();

        terminalComponent.inputField = inputField;
        terminalComponent.outputText = outputText;

        if (enterButton != null)
        {
            ReplacePersistentClick(enterButton, terminalComponent.CheckCommand);
        }
    }

    private static void FixKeyPickup(GameObject safeOpen, GameObject keyNotification, GameObject backTarget)
    {
        TakeKey takeKey = safeOpen.GetComponent<TakeKey>();
        if (takeKey == null)
        {
            takeKey = safeOpen.AddComponent<TakeKey>();
        }

        GameObject safeObject = FindPath(safeOpen.transform, "Content/Safe");
        GameObject keyButton = safeObject != null ? FindChild(safeObject.transform, "Button") : null;

        takeKey.keyNotification = keyNotification;
        takeKey.objectToHide = keyButton;
        takeKey.hideAfterTake = false;
        takeKey.notificationDuration = 2f;
        takeKey.keyVisual = keyButton != null ? keyButton.GetComponent<RectTransform>() : null;
        takeKey.moveKeyToInventory = false;
        takeKey.autoReturnToPreviousScreen = false;
        takeKey.hideBakedKeyWithMask = true;
        takeKey.keyMaskSizeMultiplier = new Vector2(0.55f, 0.85f);

        GameObject backObject = FindPath(safeOpen.transform, "Content/Back");
        ScreenButton backButton = backObject != null ? backObject.GetComponent<ScreenButton>() : null;
        if (backButton == null && backObject != null)
        {
            backButton = backObject.AddComponent<ScreenButton>();
        }

        if (backButton != null)
        {
            backButton.targetScreen = backTarget;
            EditorUtility.SetDirty(backButton);
        }

        if (backObject != null)
        {
            Button backUiButton = backObject.GetComponent<Button>();
            if (backUiButton != null && backButton != null)
            {
                ReplacePersistentClick(backUiButton, backButton.GoToScreen);
            }
        }

        Button button = keyButton != null ? keyButton.GetComponent<Button>() : null;
        if (button == null && keyButton != null)
        {
            button = keyButton.AddComponent<Button>();
        }

        ScreenButton screenButton = keyButton != null ? keyButton.GetComponent<ScreenButton>() : null;
        if (screenButton != null)
        {
            UnityEngine.Object.DestroyImmediate(screenButton, true);
        }

        if (button != null)
        {
            ReplacePersistentClick(button, takeKey.TakeKeyMethod);
            EnsureKeyPickupButton(keyButton);
        }

        if (safeObject != null)
        {
            Button safeButton = safeObject.GetComponent<Button>();
            if (safeButton != null)
            {
                UnityEngine.Object.DestroyImmediate(safeButton, true);
            }

            Component rootKeyPickup = safeObject.GetComponent("KeyPickupButton");
            if (rootKeyPickup != null)
            {
                UnityEngine.Object.DestroyImmediate(rootKeyPickup, true);
            }

            EditorUtility.SetDirty(safeObject);
        }
    }

    private static void EnsureKeyPickupButton(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Component existing = target.GetComponent("KeyPickupButton");
        if (existing == null)
        {
            Type keyPickupType = Type.GetType("KeyPickupButton, Assembly-CSharp");
            if (keyPickupType != null)
            {
                target.AddComponent(keyPickupType);
            }
            else
            {
                Debug.LogError("Practice0 fixer: KeyPickupButton script was not found.");
            }
        }

        EditorUtility.SetDirty(target);
    }

    private static void FixDoor(GameObject exit, GameObject end)
    {
        Door door = exit.GetComponent<Door>();
        if (door == null)
        {
            door = exit.AddComponent<Door>();
        }

        door.successScreen = end;
        door.noKeyScreen = null;

        GameObject doorObject = FindPath(exit.transform, "Content/Door");
        if (doorObject == null)
        {
            doorObject = FindChild(exit.transform, "Door");
        }

        if (doorObject == null)
        {
            return;
        }

        Image image = doorObject.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
        }

        Button button = doorObject.GetComponent<Button>();
        if (button == null)
        {
            button = doorObject.AddComponent<Button>();
        }

        if (image != null)
        {
            button.targetGraphic = image;
        }

        ScreenButton screenButton = doorObject.GetComponent<ScreenButton>();
        if (screenButton != null)
        {
            UnityEngine.Object.DestroyImmediate(screenButton, true);
        }

        ReplacePersistentClick(button, door.TryOpen);
    }

    private static void SavePrefabFix(string assetPath, UnityAction<GameObject> fixer)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            fixer(root);
            PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ReplacePersistentClick(Button button, UnityAction action)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void ConfigureInput(TMP_InputField inputField)
    {
        if (inputField == null)
        {
            return;
        }

        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.pointSize = 42f;

        if (inputField.textComponent != null)
        {
            ConfigureText(inputField.textComponent, 42f, TextAlignmentOptions.MidlineLeft);
            inputField.textComponent.margin = new Vector4(24f, 0f, 24f, 0f);
        }

        if (inputField.placeholder is TMP_Text placeholder)
        {
            ConfigureText(placeholder, 40f, TextAlignmentOptions.MidlineLeft);
            placeholder.margin = new Vector4(24f, 0f, 24f, 0f);
        }

        EditorUtility.SetDirty(inputField);
    }

    private static void ConfigureButtonText(RectTransform buttonRect, float fontSize)
    {
        if (buttonRect == null)
        {
            return;
        }

        foreach (TMP_Text text in buttonRect.GetComponentsInChildren<TMP_Text>(true))
        {
            ConfigureText(text, fontSize, TextAlignmentOptions.Center);
            text.margin = Vector4.zero;
        }
    }

    private static void ConfigureText(TMP_Text text, float fontSize, TextAlignmentOptions alignment)
    {
        if (text == null)
        {
            return;
        }

        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.alignment = alignment;
        EditorUtility.SetDirty(text);
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        EditorUtility.SetDirty(rect);
    }

    private static RectTransform FindRect(Transform root, string name)
    {
        return FindChild(root, name)?.GetComponent<RectTransform>();
    }

    private static GameObject FindPath(Transform root, string path)
    {
        Transform current = root;
        foreach (string part in path.Split('/'))
        {
            current = current.Find(part);
            if (current == null)
            {
                return null;
            }
        }

        return current.gameObject;
    }

    private static GameObject FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child.gameObject;
            }
        }

        return null;
    }
}
