using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;

public class PracticeManager : MonoBehaviour
{
    public static PracticeManager Instance;

    public Transform screenContainer;

    public GameObject startScreen;
    private GameObject currentScreen;
    private Stack<GameObject> history = new Stack<GameObject>();
    private int lastScreenWidth;
    private int lastScreenHeight;

    public event Action<GameObject> ScreenChanged;

    public GameObject CurrentScreen => currentScreen;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (screenContainer == null)
        {
            GameObject foundContainer = GameObject.Find("ScreenContainer");
            if (foundContainer != null)
            {
                screenContainer = foundContainer.transform;
            }
        }

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
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartPracticeTimer();
        }
    }

    void Update()
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

        if (currentScreen != null)
        {
            history.Push(currentScreen);
            if (IsSceneScreen(currentScreen))
            {
                currentScreen.SetActive(false);
            }
            else
            {
                Destroy(currentScreen);
            }
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
        ApplyAdaptiveBackButtons(currentScreen);
    }

    public void GoBack()
    {
        if (currentScreen != null)
        {
            if (IsSceneScreen(currentScreen))
            {
                currentScreen.SetActive(false);
            }
            else
            {
                Destroy(currentScreen);
            }
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
        ApplyAdaptiveBackButtons(currentScreen);
    }

    // МЕТОД ЗАВЕРШЕНИЯ ПРАКТИКИ (ВЫЗЫВАЕТСЯ КНОПКОЙ «ДАЛЕЕ»)
    public void FinishPractice()
    {
        // --- ВЫДАЕМ 10 МОНЕТ ЗА УСПЕШНУЮ ПРАКТИКУ ---
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompletePractice();
        }
        else
        {
            Debug.LogWarning("GameManager is missing, finishing practice without progress save.");
        }

        SceneManager.LoadScene("LessonsList");
    }

    public void ExitToMenu()
    {
        SceneManager.LoadScene("LessonsList");
    }

    private bool IsSceneScreen(GameObject screen)
    {
        if (screen == null || screenContainer == null)
        {
            return false;
        }

        return screen.scene.IsValid() && screen.transform.IsChildOf(screenContainer);
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
        }
    }
}