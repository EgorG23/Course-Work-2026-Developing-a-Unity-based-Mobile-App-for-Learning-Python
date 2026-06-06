using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CodeLineDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private readonly List<RectTransform> lines = new List<RectTransform>();
    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private RectTransform originalParent;
    private RectTransform dragCanvas;
    private LayoutElement layoutElement;
    private LayoutElement placeholder;

    public void Configure(IEnumerable<Transform> lineTransforms)
    {
        lines.Clear();
        lines.AddRange(lineTransforms.OfType<RectTransform>());
        rect = transform as RectTransform;
        dragCanvas = GetComponentInParent<Canvas>()?.transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rect == null)
        {
            return;
        }

        originalParent = rect.parent as RectTransform;
        GameObject placeholderObject = new GameObject("CodeLinePlaceholder", typeof(RectTransform), typeof(LayoutElement));
        placeholderObject.transform.SetParent(originalParent, false);
        placeholderObject.transform.SetSiblingIndex(rect.GetSiblingIndex());
        placeholder = placeholderObject.GetComponent<LayoutElement>();
        placeholder.preferredHeight = rect.rect.height;
        placeholder.minHeight = rect.rect.height;

        layoutElement.ignoreLayout = true;
        rect.SetParent(dragCanvas, true);
        rect.SetAsLastSibling();
        MoveToPointer(eventData);
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (originalParent == null || placeholder == null ||
            !RectTransformUtility.ScreenPointToWorldPointInRectangle(
                originalParent,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 worldPointer))
        {
            return;
        }

        MoveToPointer(eventData);
        List<RectTransform> orderedLines = lines
            .Where(line => line != null && line != rect)
            .OrderBy(line => line.GetSiblingIndex())
            .ToList();
        int targetIndex = orderedLines.Count(line => line.position.y > worldPointer.y);
        placeholder.transform.SetSiblingIndex(Mathf.Clamp(targetIndex, 0, orderedLines.Count));
        LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup == null || originalParent == null || placeholder == null)
        {
            return;
        }

        int targetIndex = placeholder.transform.GetSiblingIndex();
        rect.SetParent(originalParent, false);
        rect.SetSiblingIndex(targetIndex);
        Destroy(placeholder.gameObject);
        placeholder = null;
        layoutElement.ignoreLayout = false;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent);
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        if (rect == null || dragCanvas == null ||
            !RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragCanvas,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 worldPointer))
        {
            return;
        }

        rect.position = worldPointer;
    }
}
