using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ColorChangerText : MonoBehaviour
{
    private TMP_Text txt;

    private float speed = 0.5f;

    private Color startColor;
    private Color endColor;

    public string startColorCode = "#FFFFFF";
    public string endColorCode = "#FFFFFF";

    void Start()
    {
        txt = GetComponent<TMP_Text>();

        ColorUtility.TryParseHtmlString(startColorCode, out startColor);
        ColorUtility.TryParseHtmlString(endColorCode, out endColor);
    }

    void Update()
    {
        if (txt == null) return;

        float t = Mathf.PingPong(Time.time * speed, 1f);
        txt.color = Color.Lerp(startColor, endColor, t);
    }
}