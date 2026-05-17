using UnityEngine;

public class BackgroundMove : MonoBehaviour
{
    public float amplitude = 10f;
    public float speed = 1f;

    private RectTransform rect;

    private Vector2 startPos;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        startPos = rect.anchoredPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * amplitude;
        rect.anchoredPosition = startPos + new Vector2(offset, 0);
    }
}
