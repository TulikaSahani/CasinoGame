using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CircularTimer : MonoBehaviour
{
    [Header("UI References")]
    public Image timerFill;
    public TMP_Text timerText;

    [Header("Timer Settings")]
    public float maxTime = 60f;
    public float currentTime = 0f;

    [Header("Colors")]
    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;
    public float warningThreshold = 15f;
    public float criticalThreshold = 10f;

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            if (currentTime < 0) currentTime = 0;
            UpdateTimerUI();
        }
    }

    public void StartTimer(float duration)
    {
        maxTime = duration;
        currentTime = duration;
        UpdateTimerUI();
    }

    public void StopTimer()
    {
        currentTime = 0;
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        // Update circular fill
        if (timerFill != null)
        {
            float fillAmount = currentTime / maxTime;
            timerFill.fillAmount = fillAmount;

            // Change color based on time
            if (currentTime <= criticalThreshold)
                timerFill.color = criticalColor;
            else if (currentTime <= warningThreshold)
                timerFill.color = warningColor;
            else
                timerFill.color = normalColor;
        }

        // Update text
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public bool IsTimeUp()
    {
        return currentTime <= 0;
    }
}