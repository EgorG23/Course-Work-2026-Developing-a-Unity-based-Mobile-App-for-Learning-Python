using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GlitchEffect : MonoBehaviour
{
    public Image glitchImage;
    public Sprite[] glitchFrames;
    public float frameDelay = 0.05f;

    public void TriggerGlitch()
    {
        StopAllCoroutines();
        StartCoroutine(PlayGlitch());
    }

    IEnumerator PlayGlitch()
    {
        if (glitchImage == null || glitchFrames.Length == 0)
            yield break;

        glitchImage.color = new Color(1, 1, 1, 1);

        for (int i = 0; i < glitchFrames.Length; i++)
        {
            glitchImage.sprite = glitchFrames[i];
            yield return new WaitForSeconds(frameDelay);
        }

        glitchImage.color = new Color(1, 1, 1, 0);
    }
}