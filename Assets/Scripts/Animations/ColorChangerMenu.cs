using UnityEngine;
using UnityEngine.UI;

public class ColorChangerMenu : MonoBehaviour
{
    private Image img;

    private float speed = 0.5f;

    private Color startColor;
    private Color endColor;

    void Start()
    {
        img = GetComponent<Image>();

        ColorUtility.TryParseHtmlString("#00D47D", out startColor);
        ColorUtility.TryParseHtmlString("#FFFFFF", out endColor);
    }

    void Update()
    {
        if (img == null) return;

        float t = Mathf.PingPong(Time.time * speed, 1f);
        img.color = Color.Lerp(startColor, endColor, t);
    }
}