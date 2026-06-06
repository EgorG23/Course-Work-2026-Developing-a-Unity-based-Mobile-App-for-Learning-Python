using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TypeMatchingPuzzle : MonoBehaviour
{
    private readonly HashSet<string> solvedKeys = new HashSet<string>();
    private string selectedLeft;

    public event Action Solved;

    private void Awake()
    {
        Image blocker = GetComponent<Image>();
        if (blocker != null)
        {
            blocker.raycastTarget = false;
        }

        BindButtons();
    }

    private void BindButtons()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            string name = button.name.Trim();
            if (!TryParse(name, out bool isLeft, out string key))
            {
                continue;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => Select(isLeft, key));
            ConfigureHighlight(button);
        }
    }

    private void Select(bool isLeft, string key)
    {
        if (solvedKeys.Contains(key))
        {
            return;
        }

        if (isLeft)
        {
            selectedLeft = key;
            SetSelection(key);
            return;
        }

        if (selectedLeft != key)
        {
            FlashWrongPair(selectedLeft, key);
            selectedLeft = null;
            SetSelection(null);
            return;
        }

        solvedKeys.Add(key);
        SetPairSolved(key);
        selectedLeft = null;

        if (solvedKeys.Count == 4)
        {
            Solved?.Invoke();
        }
    }

    private void SetSelection(string key)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (!TryParse(button.name.Trim(), out bool isLeft, out string buttonKey) || !isLeft || solvedKeys.Contains(buttonKey))
            {
                continue;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = buttonKey == key ? new Color(0.65f, 0.85f, 1f) : Color.white;
            button.colors = colors;
        }
    }

    private void SetPairSolved(string key)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (TryParse(button.name.Trim(), out _, out string buttonKey) && buttonKey == key)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(0.55f, 1f, 0.55f);
                button.colors = colors;
                if (button.targetGraphic != null)
                {
                    button.targetGraphic.color = new Color(0.55f, 1f, 0.55f);
                }
                button.interactable = false;
            }
        }
    }

    private static bool TryParse(string objectName, out bool isLeft, out string key)
    {
        isLeft = objectName.StartsWith("Left_", StringComparison.Ordinal);
        bool isRight = objectName.StartsWith("Right_", StringComparison.Ordinal);
        key = isLeft || isRight ? objectName.Substring(objectName.IndexOf('_') + 1).ToLowerInvariant() : string.Empty;
        return (isLeft || isRight) && (key == "int" || key == "str" || key == "float" || key == "bool");
    }

    private static void ConfigureHighlight(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.65f, 0.9f, 1f);
        colors.pressedColor = new Color(0.35f, 0.75f, 1f);
        colors.selectedColor = new Color(0.65f, 0.9f, 1f);
        colors.disabledColor = new Color(0.55f, 1f, 0.55f);
        colors.colorMultiplier = 1.15f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private void FlashWrongPair(string leftKey, string rightKey)
    {
        if (!string.IsNullOrEmpty(leftKey))
        {
            StartCoroutine(FlashRed($"Left_{leftKey}"));
        }

        StartCoroutine(FlashRed($"Right_{rightKey}"));
    }

    private IEnumerator FlashRed(string objectName)
    {
        Transform target = transform.Find(objectName);
        Graphic graphic = target != null ? target.GetComponent<Graphic>() : null;
        if (graphic == null)
        {
            yield break;
        }

        Color original = graphic.color;
        graphic.color = new Color(1f, 0.28f, 0.28f);
        yield return new WaitForSeconds(0.35f);
        graphic.color = original;
    }
}
