using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class KeyPickupButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(TakeKey);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(TakeKey);
        }
    }

    private void TakeKey()
    {
        TakeKey key = GetComponentInParent<TakeKey>();
        if (key == null)
        {
            key = FindAnyObjectByType<TakeKey>();
        }

        if (key != null)
        {
            key.TakeKeyMethod();
        }
    }
}
