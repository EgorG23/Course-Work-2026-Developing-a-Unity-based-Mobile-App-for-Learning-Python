using UnityEngine;

public class ButtonMove : MonoBehaviour
{
    public float angle = 10f;
    public float speed = 2f;

    RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        float rotation = Mathf.Sin(Time.time * speed) * angle;

        rect.rotation = Quaternion.Euler(0f, 0f, rotation);
    }
}