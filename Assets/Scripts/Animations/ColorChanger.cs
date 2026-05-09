using UnityEngine;
using UnityEngine.UI;

public class ColorChanger : MonoBehaviour
{
    private Image img;

    private float speed = 0.5f;

    private Color startColor;
    private Color endColor;

    public string startColorCode = "#450099";
    public string endColorCode = "#C11639";


    void Start()
    {
        img = GetComponent<Image>();

        ColorUtility.TryParseHtmlString(startColorCode, out startColor);
        ColorUtility.TryParseHtmlString(endColorCode, out endColor);
    }

    void Update()
    {
        if (img == null) return;

        float t = Mathf.PingPong(Time.time * speed, 1f);
        img.color = Color.Lerp(startColor, endColor, t);
    }
}