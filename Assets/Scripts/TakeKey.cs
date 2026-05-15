using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TakeKey : MonoBehaviour
{
    public GameObject keyNotification;
    public GameObject objectToHide;
    public bool hideAfterTake = true;
    public float notificationDuration = 2f;
    public string notificationMessage = "Ключик найден";

    [Header("Pickup Visual")]
    public RectTransform keyVisual;
    public RectTransform inventoryTarget;
    public bool moveKeyToInventory = true;
    public float moveDuration = 0.4f;
    public float inventoryScale = 0.4f;
    [Range(0f, 1f)] public float inventoryBottomScreenPercent = 0.12f;

    [Header("Flow")]
    public bool autoReturnToPreviousScreen = false;
    public float autoReturnDelay = 0.2f;

    [Header("Baked Key Mask")]
    public bool hideBakedKeyWithMask = false;
    public Color keyMaskColor = new Color32(44, 44, 50, 255);
    public Vector2 keyMaskSizeMultiplier = new Vector2(0.55f, 0.85f);

    private bool isProcessingPickup;
    private Coroutine hideNotificationRoutine;

    private void OnEnable()
    {
        if (QuestManager.Instance == null || !QuestManager.Instance.hasKey || !hideAfterTake)
        {
            return;
        }

        GameObject explicitKeyObject = objectToHide != null
            ? objectToHide
            : (keyVisual != null ? keyVisual.gameObject : null);

        if (explicitKeyObject != null)
        {
            explicitKeyObject.SetActive(false);
        }
    }

    public void TakeKeyMethod()
    {
        TakeKeyInternal(null);
    }

    public void TakeKeyMethodFromObject(GameObject clickedObject)
    {
        TakeKeyInternal(clickedObject);
    }

    private void TakeKeyInternal(GameObject clickedObject)
    {
        if (isProcessingPickup)
        {
            return;
        }

        isProcessingPickup = true;

        GameObject notification = keyNotification != null ? keyNotification : FindKeyNotification();
        if (notification != null)
        {
            ApplyNotificationText(notification);
            DisableNotificationRaycasts(notification);
            notification.transform.SetAsLastSibling();
            notification.SetActive(true);

            if (hideNotificationRoutine != null)
            {
                StopCoroutine(hideNotificationRoutine);
            }

            hideNotificationRoutine = StartCoroutine(HideNotificationAfterDelay(notification));
        }

        bool keyAlreadyTaken = QuestManager.Instance != null && QuestManager.Instance.hasKey;
        if (keyAlreadyTaken)
        {
            if (hideAfterTake)
            {
                HideAllKeyTargets(clickedObject);
            }
            isProcessingPickup = false;
            return;
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.hasKey = true;
        }

        GameObject keyObject = ResolveKeyObject(clickedObject);

        if (moveKeyToInventory)
        {
            StartCoroutine(AnimatePickupFx(keyObject));
        }

        if (hideAfterTake)
        {
            HideAllKeyTargets(keyObject);
        }

        ClearUiSelection();

        if (autoReturnToPreviousScreen)
        {
            StartCoroutine(ReturnToPreviousScreenAfterDelay());
        }

        isProcessingPickup = false;
        Debug.Log("Key taken");
    }

    private GameObject ResolveKeyObject(GameObject clickedObject)
    {
        if (clickedObject != null)
        {
            return clickedObject;
        }

        if (objectToHide != null)
        {
            return objectToHide;
        }

        if (keyVisual != null)
        {
            return keyVisual.gameObject;
        }

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            return EventSystem.current.currentSelectedGameObject;
        }

        return null;
    }

    private void ClearUiSelection()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private GameObject FindKeyNotification()
    {
        if (PracticeManager.Instance == null || PracticeManager.Instance.screenContainer == null)
        {
            return null;
        }

        Transform found = PracticeManager.Instance.screenContainer.Find("KeyNotification");
        return found != null ? found.gameObject : null;
    }

    private void ApplyNotificationText(GameObject notification)
    {
        if (string.IsNullOrWhiteSpace(notificationMessage))
        {
            return;
        }

        TMP_Text text = notification.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = notificationMessage;
        }
    }

    private void DisableNotificationRaycasts(GameObject notification)
    {
        Graphic[] graphics = notification.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }
    }

    private IEnumerator AnimatePickupFx(GameObject sourceObject)
    {
        RectTransform sourceRect = keyVisual != null ? keyVisual : null;
        if (sourceRect == null && sourceObject != null)
        {
            sourceRect = sourceObject.GetComponent<RectTransform>();
        }

        if (sourceRect == null)
        {
            yield break;
        }

        Canvas sourceCanvas = sourceRect.GetComponentInParent<Canvas>();
        if (sourceCanvas == null)
        {
            yield break;
        }

        Canvas rootCanvas = sourceCanvas.rootCanvas;
        RectTransform rootRect = rootCanvas.transform as RectTransform;
        if (rootRect == null)
        {
            yield break;
        }

        GameObject fxObject = new GameObject("KeyPickupFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform fxRect = fxObject.GetComponent<RectTransform>();
        Image fxImage = fxObject.GetComponent<Image>();
        Image sourceImage = FindSourceImage(sourceObject, sourceRect);

        fxRect.SetParent(rootRect, false);
        fxRect.SetAsLastSibling();
        fxRect.anchorMin = new Vector2(0.5f, 0.5f);
        fxRect.anchorMax = new Vector2(0.5f, 0.5f);
        fxRect.pivot = new Vector2(0.5f, 0.5f);

        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        Vector2 startPos = ScreenToLocal(rootRect, uiCamera, sourceRect.TransformPoint(sourceRect.rect.center));
        Vector2 targetPos = GetTargetLocalPosition(rootRect, uiCamera);

        if (sourceImage == null || sourceImage.sprite == null)
        {
            Destroy(fxObject);
            yield return AnimateFallbackGlyph(rootRect, startPos, targetPos);
            yield break;
        }

        fxImage.sprite = sourceImage.sprite;
        fxImage.color = sourceImage.color;
        fxImage.preserveAspect = true;
        fxImage.raycastTarget = false;

        Vector2 sourceSize = sourceRect.rect.size;
        if (sourceSize.x <= 1f || sourceSize.y <= 1f)
        {
            sourceSize = sourceImage.rectTransform.rect.size;
        }

        if (sourceSize.x <= 1f || sourceSize.y <= 1f)
        {
            sourceSize = new Vector2(120f, 120f);
        }

        fxRect.sizeDelta = sourceSize;

        fxRect.anchoredPosition = startPos;
        fxRect.localScale = Vector3.one;

        float duration = Mathf.Max(0.05f, moveDuration);
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one * Mathf.Clamp(inventoryScale, 0.1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            fxRect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, eased);
            fxRect.localScale = Vector3.LerpUnclamped(Vector3.one, targetScale, eased);
            yield return null;
        }

        Destroy(fxObject);
    }

    private static Vector2 ScreenToLocal(RectTransform rootRect, Camera uiCamera, Vector3 worldPoint)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screenPoint, uiCamera, out Vector2 localPoint);
        return localPoint;
    }

    private Vector2 GetTargetLocalPosition(RectTransform rootRect, Camera uiCamera)
    {
        Vector2 targetScreen;
        if (inventoryTarget != null)
        {
            targetScreen = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                inventoryTarget.TransformPoint(inventoryTarget.rect.center)
            );
        }
        else
        {
            targetScreen = new Vector2(
                Screen.width * 0.5f,
                Screen.height * Mathf.Clamp01(inventoryBottomScreenPercent)
            );
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, targetScreen, uiCamera, out Vector2 localTarget);
        return localTarget;
    }

    private static Image FindSourceImage(GameObject sourceObject, RectTransform sourceRect)
    {
        if (sourceRect != null)
        {
            Image directImage = sourceRect.GetComponent<Image>();
            if (directImage != null && directImage.sprite != null && directImage.color.a > 0.01f)
            {
                return directImage;
            }
        }

        if (sourceObject != null)
        {
            Image objImage = sourceObject.GetComponent<Image>();
            if (objImage != null && objImage.sprite != null)
            {
                return objImage;
            }
        }

        return null;
    }

    private void PlaceKeyMask(GameObject sourceObject)
    {
        RectTransform sourceRect = sourceObject != null ? sourceObject.GetComponent<RectTransform>() : null;
        if (sourceRect == null)
        {
            return;
        }

        RectTransform parent = sourceRect.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        GameObject mask = new GameObject("TakenKeyMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform maskRect = mask.GetComponent<RectTransform>();
        Image maskImage = mask.GetComponent<Image>();

        maskRect.SetParent(parent, false);
        maskRect.SetSiblingIndex(sourceRect.GetSiblingIndex() + 1);
        maskRect.anchorMin = sourceRect.anchorMin;
        maskRect.anchorMax = sourceRect.anchorMax;
        maskRect.pivot = sourceRect.pivot;
        maskRect.anchoredPosition = sourceRect.anchoredPosition;
        maskRect.localRotation = sourceRect.localRotation;
        maskRect.localScale = sourceRect.localScale;
        maskRect.sizeDelta = Vector2.Scale(sourceRect.sizeDelta, keyMaskSizeMultiplier);

        maskImage.color = keyMaskColor;
        maskImage.raycastTarget = false;
    }

    private void HideAllKeyTargets(GameObject resolvedKeyObject)
    {
        HideObjectIfAny(objectToHide);
        HideObjectIfAny(resolvedKeyObject);

        Transform keyPath = transform.Find("Content/Safe/Button");
        if (keyPath != null)
        {
            HideObjectIfAny(keyPath.gameObject);
        }

        KeyPickupButton keyPickupButton = GetComponentInChildren<KeyPickupButton>(true);
        if (keyPickupButton != null)
        {
            HideObjectIfAny(keyPickupButton.gameObject);
        }
    }

    private static void HideObjectIfAny(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(false);
    }

    private IEnumerator AnimateFallbackGlyph(RectTransform rootRect, Vector2 startPos, Vector2 targetPos)
    {
        GameObject glyphObject = new GameObject("KeyPickupGlyph", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform glyphRect = glyphObject.GetComponent<RectTransform>();
        TextMeshProUGUI glyph = glyphObject.GetComponent<TextMeshProUGUI>();

        glyphRect.SetParent(rootRect, false);
        glyphRect.SetAsLastSibling();
        glyphRect.anchorMin = new Vector2(0.5f, 0.5f);
        glyphRect.anchorMax = new Vector2(0.5f, 0.5f);
        glyphRect.pivot = new Vector2(0.5f, 0.5f);
        glyphRect.sizeDelta = new Vector2(140f, 140f);
        glyphRect.anchoredPosition = startPos;

        glyph.text = "\ud83d\udd11";
        glyph.fontSize = 120f;
        glyph.alignment = TextAlignmentOptions.Center;
        glyph.raycastTarget = false;

        float duration = Mathf.Max(0.05f, moveDuration);
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one * Mathf.Clamp(inventoryScale, 0.1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            glyphRect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, eased);
            glyphRect.localScale = Vector3.LerpUnclamped(Vector3.one, targetScale, eased);
            yield return null;
        }

        Destroy(glyphObject);
    }

    private IEnumerator ReturnToPreviousScreenAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, autoReturnDelay));

        if (PracticeManager.Instance != null)
        {
            PracticeManager.Instance.GoBack();
        }
    }

    private IEnumerator HideNotificationAfterDelay(GameObject notification)
    {
        float visibleTime = Mathf.Max(1.5f, notificationDuration);
        yield return new WaitForSeconds(visibleTime);

        if (notification != null)
        {
            notification.SetActive(false);
        }

        hideNotificationRoutine = null;
    }
}
