using UnityEngine;

public class CrowMove : MonoBehaviour
{
    RectTransform crow;

    public float speed = 200f;
    public Vector2[] points;

    int currentPoint = 0;

    void Awake()
    {
        crow = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (points.Length > 0)
        {
            crow.anchoredPosition = points[0];
        }
    }

    void Update()
    {
        if (points.Length == 0) return;

        Vector2 target = points[currentPoint];

        crow.anchoredPosition = Vector2.MoveTowards(
            crow.anchoredPosition,
            target,
            speed * Time.deltaTime
        );

        if (Vector2.Distance(crow.anchoredPosition, target) < 5f)
        {
            currentPoint++;

            if (currentPoint >= points.Length)
            {
                currentPoint = 0;
                crow.anchoredPosition = points[0];
            }
        }
    }
}