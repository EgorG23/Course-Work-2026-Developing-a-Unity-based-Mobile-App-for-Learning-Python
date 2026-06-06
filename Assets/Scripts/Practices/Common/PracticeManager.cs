using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;

public class PracticeManager : MonoBehaviour
{
    public static PracticeManager Instance;

    [Min(0)] public int lessonIndex;
    public Transform screenContainer;
    public GameObject startScreen;

    private GameObject currentScreen;
    private readonly Stack<GameObject> history = new Stack<GameObject>();
    private readonly HashSet<GameObject> sceneScreens = new HashSet<GameObject>();
    private int lastScreenWidth;
    private int lastScreenHeight;
    private int lastTransitionFrame = -1;
    private bool isFinished;

    public event Action<GameObject> ScreenChanged;

    public GameObject CurrentScreen => currentScreen;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (screenContainer == null)
        {
            GameObject foundContainer = GameObject.Find("ScreenContainer");
            if (foundContainer != null)
            {
                screenContainer = foundContainer.transform;
            }
        }

        RegisterSceneScreens();

        if (startScreen == null && screenContainer != null)
        {
            Transform foundStart = FindChildByTrimmedName(screenContainer, "Start");
            if (foundStart != null)
            {
                startScreen = foundStart.gameObject;
            }
        }

        if (startScreen != null)
        {
            bool isSceneScreen = IsSceneScreen(startScreen);
            if (isSceneScreen)
            {
                history.Clear();
                DeactivateAllScreensExcept(startScreen);
                currentScreen = startScreen;
                currentScreen.SetActive(true);
                ScreenChanged?.Invoke(currentScreen);
                ConfigureBackButtons(currentScreen);
                ApplyAdaptiveBackButtons(currentScreen);
            }
            else
            {
                ShowScreen(startScreen);
            }
        }
        else
        {
            Debug.LogError("startScreen is not assigned");
        }
        
        GameManager.GetOrCreate().StartPracticeTimer();
    }

    private void Update()
    {
        if (currentScreen == null)
        {
            return;
        }

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            ApplyAdaptiveBackButtons(currentScreen);
        }
    }

    public void ShowScreen(GameObject screenPrefab)
    {
        if (screenPrefab == null)
        {
            Debug.LogWarning("ShowScreen called with null screenPrefab");
            return;
        }

        if (!TryBeginTransition())
        {
            return;
        }

        if (currentScreen != null)
        {
            history.Push(currentScreen);
            currentScreen.SetActive(false);
        }

        if (IsSceneScreen(screenPrefab))
        {
            currentScreen = screenPrefab;
            currentScreen.SetActive(true);
        }
        else
        {
            currentScreen = Instantiate(screenPrefab, screenContainer);
        }

        ScreenChanged?.Invoke(currentScreen);
        ConfigureBackButtons(currentScreen);
        ApplyAdaptiveBackButtons(currentScreen);
    }

    public void GoBack()
    {
        if (!TryBeginTransition())
        {
            return;
        }

        if (currentScreen != null)
        {
            currentScreen.SetActive(false);
        }

        if (history.Count > 0)
        {
            currentScreen = history.Pop();
            if (currentScreen != null)
            {
                currentScreen.SetActive(true);
            }
        }
        else
        {
            currentScreen = null;
        }

        ScreenChanged?.Invoke(currentScreen);
        ConfigureBackButtons(currentScreen);
        ApplyAdaptiveBackButtons(currentScreen);
    }

    public void FinishPractice()
    {
        if (isFinished)
        {
            return;
        }

        isFinished = true;
        GameManager.GetOrCreate().CompletePractice(lessonIndex);
        if (LessonLoader.Instance != null)
        {
            LessonLoader.Instance.returnToLastTheoryScreen = false;
        }

        SceneManager.LoadScene("LessonsList");
    }

    public void ExitPractice()
    {
        if (LessonLoader.Instance != null &&
            LessonLoader.Instance.returnToLastTheoryScreen &&
            LessonLoader.Instance.screens != null &&
            LessonLoader.Instance.screens.Count > 0)
        {
            SceneManager.LoadScene("LessonScene");
            return;
        }

        SceneManager.LoadScene("LessonsList");
    }

    public void ExitToMenu()
    {
        ExitPractice();
    }

    private bool IsSceneScreen(GameObject screen)
    {
        if (screen == null || screenContainer == null)
        {
            return false;
        }

        return sceneScreens.Contains(screen) ||
               (screen.scene.IsValid() && screen.transform.IsChildOf(screenContainer) &&
                !screen.name.EndsWith("(Clone)", StringComparison.Ordinal));
    }

    private void RegisterSceneScreens()
    {
        sceneScreens.Clear();
        if (screenContainer == null)
        {
            return;
        }

        foreach (Transform child in screenContainer)
        {
            if (child != null)
            {
                sceneScreens.Add(child.gameObject);
            }
        }
    }

    private bool TryBeginTransition()
    {
        if (lastTransitionFrame == Time.frameCount)
        {
            return false;
        }

        lastTransitionFrame = Time.frameCount;
        return true;
    }

    private static Transform FindChildByTrimmedName(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Trim() == targetName)
            {
                return child;
            }
        }

        return null;
    }

    private void DeactivateAllScreensExcept(GameObject keepActive)
    {
        if (screenContainer == null)
        {
            return;
        }

        foreach (Transform child in screenContainer)
        {
            if (child != null && child.gameObject != keepActive)
            {
                
                child.gameObject.SetActive(false);
            }
        }
    }

    private static void ApplyAdaptiveBackButtons(GameObject screen)
    {
        if (screen == null)
        {
            return;
        }

        RectTransform screenRect = screen.GetComponent<RectTransform>();
        if (screenRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        float width = Mathf.Max(screenRect.rect.width, 1f);
        float height = Mathf.Max(screenRect.rect.height, 1f);

        Button[] buttons = screen.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null || button.name.Trim() != "Back")
            {
                continue;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                continue;
            }

            float targetWidth = Mathf.Clamp(width * 0.30f, 200f, 360f);
            float targetHeight = Mathf.Clamp(height * 0.095f, 84f, 150f);
            float bottomOffset = Mathf.Clamp(height * 0.11f, 70f, 170f);

            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(targetWidth, targetHeight);
            rect.anchoredPosition = new Vector2(0f, bottomOffset);
            button.transform.SetAsLastSibling();
        }
    }

    private void ConfigureBackButtons(GameObject screen)
    {
        if (screen == null)
        {
            return;
        }

        EnsureMapBackButton(screen);

        foreach (Button button in screen.GetComponentsInChildren<Button>(true))
        {
            if (button.name.Trim() != "Back")
            {
                continue;
            }

            button.gameObject.SetActive(true);
            ScreenButton legacyNavigation = button.GetComponent<ScreenButton>();
            if (legacyNavigation != null)
            {
                legacyNavigation.UseManagedNavigation();
            }

            PrepareBackButton(button);
            button.onClick.RemoveAllListeners();
            if (history.Count > 0)
            {
                button.onClick.AddListener(GoBack);
            }
            else
            {
                button.onClick.AddListener(ExitPractice);
            }
        }
    }

    private void EnsureMapBackButton(GameObject screen)
    {
        if (!screen.name.StartsWith("MapScreen", StringComparison.Ordinal) ||
            FindChildByTrimmedName(screen.transform, "Back") != null)
        {
            return;
        }

        Button template = null;
        foreach (Button candidate in screenContainer.GetComponentsInChildren<Button>(true))
        {
            if (candidate.name.Trim() == "Back" && !candidate.transform.IsChildOf(screen.transform))
            {
                template = candidate;
                break;
            }
        }

        if (template == null)
        {
            return;
        }

        GameObject backObject = Instantiate(template.gameObject, screen.transform);
        backObject.name = "Back";
        backObject.SetActive(true);
    }

    private static void PrepareBackButton(Button button)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 115f);
            rect.sizeDelta = new Vector2(420f, 120f);
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.enabled = true;
            image.raycastTarget = true;
            image.color = Color.white;
        }

        Canvas canvas = button.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = button.gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;

        if (button.GetComponent<GraphicRaycaster>() == null)
        {
            button.gameObject.AddComponent<GraphicRaycaster>();
        }

        CanvasGroup group = button.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = button.gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        button.transform.SetAsLastSibling();
    }
}
