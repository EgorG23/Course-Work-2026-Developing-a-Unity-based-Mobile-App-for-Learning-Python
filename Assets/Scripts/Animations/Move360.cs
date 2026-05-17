using UnityEngine;

public class Move360 : MonoBehaviour
{
    public float speed = 100f;

    private RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        rect.Rotate(0f, 0f, speed * Time.deltaTime);
    }
}