using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class GameCardController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private TMP_Text titleText;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    public void Setup(GameData game, UnityAction onClick, Sprite defaultThumbnail = null)
    {
        if (titleText != null)
            titleText.text = game.game_name;

        // Example: set thumbnail if you have one, else fallback
        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = defaultThumbnail;
            thumbnailImage.preserveAspect = true;
        }

        // Example: color variation for background
        if (backgroundImage != null)
        {
            int hash = Mathf.Abs(game.game_code.GetHashCode());
            float hue = (hash % 360) / 360f;
            backgroundImage.color = Color.HSVToRGB(hue, 0.6f, 0.95f);
        }

        // Wire button click
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }
    }
}
