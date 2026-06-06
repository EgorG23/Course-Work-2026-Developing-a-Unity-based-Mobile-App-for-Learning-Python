using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TakeKey : MonoBehaviour
{
    [HideInInspector] public GameObject keyNotification;
    [HideInInspector] public float notificationDuration = 2f;
    [HideInInspector] public string notificationMessage = "Ключик найден";

    public GameObject objectToHide;
    public bool hideAfterTake = true;

    [Header("Pickup Visual")]
    public RectTransform keyVisual;
    public RectTransform inventoryTarget;
    public bool moveKeyToInventory = true;
    public float moveDuration = 0.9f;
    public float inventoryScale = 1.05f;
    [Range(0f, 1f)] public float inventoryBottomScreenPercent = 0.12f;
    [Range(0f, 1f)] public float inventoryRightScreenPercent = 0.06f;
    [Range(0f, 1f)] public float inventoryIconSizePercent = 0.18f;

    [Header("Flow")]
    public bool autoReturnToPreviousScreen = false;
    public float autoReturnDelay = 0.2f;

    [Header("Baked Key Mask")]
    public bool hideBakedKeyWithMask = false;
    public Color keyMaskColor = new Color32(44, 44, 50, 255);
    public Vector2 keyMaskSizeMultiplier = new Vector2(0.55f, 0.85f);

    [Header("Runtime Art")]
    public bool applySafeBackgroundSprite = true;
    public bool applyKeySprite = true;
    public string safeBackgroundResourcePath = "Lesson0Safe/openmpEmptySafe";
    public string keySpriteResourcePath = "Lesson0Safe/Group34256";
    [Range(0.1f, 0.9f)] public float safeKeyHeightPercent = 0.48f;

    private bool isProcessingPickup;
    private Coroutine moveToInventoryRoutine;

    private void Awake()
    {
        CacheVisualReferences();
        ApplyRuntimeSprites();
    }

    private void OnEnable()
    {
        CacheVisualReferences();
        ApplyRuntimeSprites();

        if (Practice0Manager.Instance == null || !Practice0Manager.Instance.HasKey || !hideAfterTake)
        {
            return;
        }

        DockTakenKeyPersistent(ResolveKeyObject(null), false);
    }

    private void CacheVisualReferences()
    {
        if (keyVisual == null)
        {
            Transform keyByPath = transform.Find("Content/Safe/Button");
            if (keyByPath != null)
            {
                keyVisual = keyByPath as RectTransform;
            }
            else
            {
                KeyPickupButton keyButton = GetComponentInChildren<KeyPickupButton>(true);
                if (keyButton != null)
                {
                    keyVisual = keyButton.GetComponent<RectTransform>();
                }
            }
        }

        if (objectToHide == null && keyVisual != null)
        {
            objectToHide = keyVisual.gameObject;
        }
    }

    private void ApplyRuntimeSprites()
    {
        if (applySafeBackgroundSprite)
        {
            Sprite safeSprite = LoadSpriteFromResources(safeBackgroundResourcePath);
            if (safeSprite != null)
            {
                Transform safeTransform = transform.Find("Content/Safe");
                if (safeTransform != null)
                {
                    Image safeImage = safeTransform.GetComponent<Image>();
                    if (safeImage != null)
                    {
                        safeImage.sprite = safeSprite;
                        safeImage.preserveAspect = true;
                        safeImage.color = Color.white;
                    }
                }
            }
        }

        if (applyKeySprite)
        {
            Sprite keySprite = LoadSpriteFromResources(keySpriteResourcePath);
            if (keySprite != null && keyVisual != null)
            {
                Image keyImage = keyVisual.GetComponent<Image>();
                if (keyImage != null)
                {
                    keyImage.sprite = keySprite;
                    keyImage.preserveAspect = true;
                    keyImage.color = Color.white;
                }
            }
        }

        NormalizeSafeKeyVisual();
    }

    private void NormalizeSafeKeyVisual()
    {
        if (keyVisual == null)
        {
            return;
        }

        bool alreadyTaken = Practice0Manager.Instance != null && Practice0Manager.Instance.HasKey;
        if (alreadyTaken)
        {
            return;
        }

        // Preserve manual position/anchors from prefab (e.g., "on the nail").
        // Only repair the size if it is accidentally tiny.
        if (keyVisual.rect.height < 40f || keyVisual.rect.width < 20f)
        {
            RectTransform safeRect = null;
            Transform safeTransform = transform.Find("Content/Safe");
            if (safeTransform != null)
            {
                safeRect = safeTransform as RectTransform;
            }

            float containerHeight = safeRect != null ? Mathf.Max(1f, safeRect.rect.height) : Mathf.Max(1f, keyVisual.rect.height * 10f);
            float targetHeight = Mathf.Clamp(containerHeight * Mathf.Clamp01(safeKeyHeightPercent), 180f, 760f);

            float aspect = 0.18f;
            Image keyImage = keyVisual.GetComponent<Image>();
            if (keyImage != null && keyImage.sprite != null && keyImage.sprite.rect.height > 0.01f)
            {
                aspect = Mathf.Clamp(keyImage.sprite.rect.width / keyImage.sprite.rect.height, 0.08f, 1.2f);
            }

            float targetWidth = Mathf.Clamp(targetHeight * aspect, 70f, 260f);
            keyVisual.sizeDelta = new Vector2(targetWidth, targetHeight);
        }
    }

    private static Sprite LoadSpriteFromResources(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
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

        bool keyAlreadyTaken = Practice0Manager.Instance != null && Practice0Manager.Instance.HasKey;
        if (keyAlreadyTaken)
        {
            if (moveKeyToInventory)
            {
                DockTakenKeyPersistent(ResolveKeyObject(clickedObject), false);
            }
            else if (hideAfterTake)
            {
                HideAllKeyTargets(clickedObject);
            }
            isProcessingPickup = false;
            return;
        }

        Practice0Manager.Instance?.TakeKey();

        GameObject keyObject = ResolveKeyObject(clickedObject);

        if (moveKeyToInventory)
        {
            DockTakenKeyPersistent(keyObject, true);
        }
        else if (hideAfterTake)
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

    public static void HideInventoryKeyIconGlobal()
    {
        TakeKey[] allPickupControllers = FindObjectsByType<TakeKey>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allPickupControllers.Length; i++)
        {
            TakeKey pickup = allPickupControllers[i];
            if (pickup != null && pickup.keyVisual != null)
            {
                Destroy(pickup.keyVisual.gameObject);
            }
        }
    }

    private void DockTakenKeyPersistent(GameObject sourceObject, bool animateMove)
    {
        RectTransform keyRect = keyVisual != null
            ? keyVisual
            : (sourceObject != null ? sourceObject.GetComponent<RectTransform>() : null);
        if (keyRect == null)
        {
            return;
        }

        Canvas rootCanvas = null;
        if (PracticeManager.Instance != null && PracticeManager.Instance.screenContainer != null)
        {
            rootCanvas = PracticeManager.Instance.screenContainer.GetComponentInParent<Canvas>();
        }

        if (rootCanvas == null)
        {
            rootCanvas = FindAnyObjectByType<Canvas>();
        }

        if (rootCanvas == null)
        {
            return;
        }

        RectTransform rootRect = rootCanvas.transform as RectTransform;
        if (rootRect == null)
        {
            return;
        }

        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        Vector2 startAnchoredPosition = ScreenToLocal(rootRect, uiCamera, keyRect.TransformPoint(keyRect.rect.center));

        keyRect.SetParent(rootRect, false);
        keyRect.SetAsLastSibling();
        keyRect.localRotation = Quaternion.identity;

        float minSide = Mathf.Min(Mathf.Max(1f, rootRect.rect.width), Mathf.Max(1f, rootRect.rect.height));
        float iconSize = Mathf.Clamp(minSide * Mathf.Clamp01(inventoryIconSizePercent), 90f, 240f);
        float rightMargin = Mathf.Clamp(rootRect.rect.width * Mathf.Clamp01(inventoryRightScreenPercent), 18f, 140f);
        float bottomMargin = Mathf.Clamp(rootRect.rect.height * Mathf.Clamp01(inventoryBottomScreenPercent), 18f, 180f);

        float aspect = 1f;
        Image keyImage = keyRect.GetComponent<Image>();
        if (keyImage != null && keyImage.sprite != null && keyImage.sprite.rect.height > 0.01f)
        {
            aspect = keyImage.sprite.rect.width / keyImage.sprite.rect.height;
        }

        float iconHeight = iconSize;
        float iconWidth = Mathf.Clamp(iconHeight * aspect, 28f, iconHeight * 2f);
        keyRect.sizeDelta = new Vector2(iconWidth, iconHeight);

        Vector2 targetAnchoredPosition = new Vector2(-rightMargin, bottomMargin);
        Vector2 targetCenterPosition = new Vector2(
            rootRect.rect.width * 0.5f - rightMargin - iconWidth * 0.5f,
            -rootRect.rect.height * 0.5f + bottomMargin + iconHeight * 0.5f);
        Vector3 targetScale = Vector3.one * Mathf.Clamp(inventoryScale, 0.45f, 1.6f);
        Vector3 startScaleForAnim = Vector3.one * Mathf.Clamp(targetScale.x * 1.7f, 1.25f, 2.6f);

        if (keyImage != null)
        {
            Sprite keySprite = LoadSpriteFromResources(keySpriteResourcePath);
            if (keySprite != null)
            {
                keyImage.sprite = keySprite;
            }
            keyImage.type = Image.Type.Simple;
            keyImage.preserveAspect = true;
            keyImage.color = new Color(1f, 1f, 1f, 1f);
            keyImage.raycastTarget = false;
        }

        CanvasGroup keyCanvasGroup = keyRect.GetComponent<CanvasGroup>();
        if (keyCanvasGroup == null)
        {
            keyCanvasGroup = keyRect.gameObject.AddComponent<CanvasGroup>();
        }
        keyCanvasGroup.alpha = 1f;
        keyCanvasGroup.interactable = false;
        keyCanvasGroup.blocksRaycasts = false;

        Button button = keyRect.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = false;
            button.onClick = new Button.ButtonClickedEvent();
        }

        KeyPickupButton pickupButton = keyRect.GetComponent<KeyPickupButton>();
        if (pickupButton != null)
        {
            pickupButton.enabled = false;
        }

        keyRect.gameObject.SetActive(true);

        if (moveToInventoryRoutine != null)
        {
            StopCoroutine(moveToInventoryRoutine);
            moveToInventoryRoutine = null;
        }

        if (animateMove)
        {
            keyRect.anchorMin = new Vector2(0.5f, 0.5f);
            keyRect.anchorMax = new Vector2(0.5f, 0.5f);
            keyRect.pivot = new Vector2(0.5f, 0.5f);
            moveToInventoryRoutine = StartCoroutine(AnimateMoveToInventory(
                keyRect,
                startAnchoredPosition,
                startScaleForAnim,
                targetCenterPosition,
                targetScale,
                targetAnchoredPosition));
        }
        else
        {
            SnapToInventoryCorner(keyRect, targetAnchoredPosition, targetScale);
        }
    }

    private IEnumerator AnimateMoveToInventory(
        RectTransform keyRect,
        Vector2 startPos,
        Vector3 startScale,
        Vector2 targetPos,
        Vector3 targetScale,
        Vector2 targetCornerAnchoredPosition)
    {
        float duration = Mathf.Max(0.2f, moveDuration);
        float elapsed = 0f;
        keyRect.anchoredPosition = startPos;
        keyRect.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            keyRect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, eased);
            keyRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
            yield return null;
        }

        SnapToInventoryCorner(keyRect, targetCornerAnchoredPosition, targetScale);
        moveToInventoryRoutine = null;
    }

    private static void SnapToInventoryCorner(RectTransform keyRect, Vector2 cornerAnchoredPosition, Vector3 targetScale)
    {
        keyRect.anchorMin = new Vector2(1f, 0f);
        keyRect.anchorMax = new Vector2(1f, 0f);
        keyRect.pivot = new Vector2(1f, 0f);
        keyRect.anchoredPosition = cornerAnchoredPosition;
        keyRect.localScale = targetScale;
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

}
