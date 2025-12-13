using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class SimpleKeyboardAdjuster : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("UI To Move")]
    public RectTransform contentPanel;
    public float moveDistance = 300f;

    private Vector2 originalPosition;

    void Start()
    {
        if (contentPanel == null)
            contentPanel = GetComponentInParent<RectTransform>();

        originalPosition = contentPanel.anchoredPosition;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (contentPanel != null)
        {
            contentPanel.anchoredPosition = new Vector2(
                originalPosition.x,
                originalPosition.y + moveDistance
            );
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ResetPosition();
    }

    public void OnEndEdit(string text)
    {
        ResetPosition();
    }

    private void ResetPosition()
    {
        if (contentPanel != null)
        {
            contentPanel.anchoredPosition = originalPosition;
        }
    }
}