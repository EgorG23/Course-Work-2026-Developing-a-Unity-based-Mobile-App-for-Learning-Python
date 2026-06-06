using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToastPresenter : MonoBehaviour
{
    private GameObject toast;
    private Coroutine hideRoutine;

    public void Show(string message, float duration = 1.4f)
    {
        EnsureToast();
        toast.GetComponentInChildren<Text>().text = message;
        toast.SetActive(true);

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
        }

        hideRoutine = StartCoroutine(HideAfter(duration));
    }

    private void EnsureToast()
    {
        if (toast != null)
        {
            return;
        }

        toast = new GameObject("PracticeToast", typeof(RectTransform), typeof(Image));
        toast.transform.SetParent(transform, false);

        RectTransform rect = toast.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.72f);
        rect.anchorMax = new Vector2(0.85f, 0.9f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        toast.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(toast.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 16f);
        textRect.offsetMax = new Vector2(-24f, -16f);

        Text text = textObject.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 30;
        text.color = Color.white;
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        toast.SetActive(false);
        hideRoutine = null;
    }
}
