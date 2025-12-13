using UnityEngine;
using UnityEngine.UI;

public class Glow: MonoBehaviour
{
    public Image glowImage;
    public float animationSpeed = 2f;
    public float maxBrightness = 0.8f;

    private bool isAnimating = false;

    void Update()
    {
        if (isAnimating && glowImage != null)
        {
            float alpha = (Mathf.Sin(Time.time * animationSpeed) + 1f) * 0.5f * maxBrightness;
            Color color = glowImage.color;
            color.a = alpha;
            glowImage.color = color;
        }
    }

    public void StartGlow()
    {
        isAnimating = true;
        if (glowImage != null)
            glowImage.gameObject.SetActive(true);
    }

    public void StopGlow()
    {
        isAnimating = false;
        if (glowImage != null)
        {
            Color color = glowImage.color;
            color.a = 0f;
            glowImage.color = color;
            glowImage.gameObject.SetActive(false);
        }
    }
}