using UnityEngine;
using TMPro;

public class CoinDisplay : MonoBehaviour
{
    public TextMeshProUGUI coinText; 

    void Update()
    {
        if (GameManager.Instance != null && coinText != null)
        {
            coinText.text = GameManager.Instance.coins.ToString();
        }
    }
}