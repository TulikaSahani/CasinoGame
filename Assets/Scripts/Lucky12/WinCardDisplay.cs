using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WinCardDisplay : MonoBehaviour
{
    public static WinCardDisplay Instance;

    [Header("UI References")]
    public GameObject winningCardPanel;
    public Image rankImage;      
    public Image suitImage;      
    public TMP_Text winningCardText; // Optional: Show full code like "QH"

    [Header("Rank Sprites (K,Q,J)")]
    public Sprite[] rankSprites;
    public string[] rankCodes = new string[] { "Q", "J", "K" };

    [Header("Suit Sprites (H,D,C,S)")]
    public Sprite[] suitSprites;
    public string[] suitCodes = new string[] { "H", "S", "D", "C" };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (winningCardPanel != null)
            winningCardPanel.SetActive(false);
    }

    
    public void ShowWinningCard(string cardCode)
    {
       

        if (winningCardPanel != null)
            winningCardPanel.SetActive(true);

        // Extract rank and suit (e.g., "QH" -> rank="Q", suit="H")
        string rank = cardCode.Length >= 1 ? cardCode[0].ToString() : "";
        string suit = cardCode.Length >= 2 ? cardCode[1].ToString() : "";

        // Set rank image
        if (rankImage != null)
            rankImage.sprite = GetRankSprite(rank);

        // Set suit image  
        if (suitImage != null)
            suitImage.sprite = GetSuitSprite(suit);

        // Set full card code text if available
        if (winningCardText != null)
        {
            winningCardText.text = cardCode;
        }

        Debug.Log($"Showing winning card: {cardCode} (Rank: {rank}, Suit: {suit})");
    }

    public void HideWinningCard()
    {
        if (winningCardPanel != null)
            winningCardPanel.SetActive(false);
    }

    private Sprite GetRankSprite(string rank)
    {
        for (int i = 0; i < rankCodes.Length; i++)
        {
            if (rankCodes[i] == rank && i < rankSprites.Length)
            {
                return rankSprites[i];
            }
        }
        Debug.LogWarning($"Rank sprite not found for: {rank}");
        return null;
    }

    private Sprite GetSuitSprite(string suit)
    {
        for (int i = 0; i < suitCodes.Length; i++)
        {
            if (suitCodes[i] == suit && i < suitSprites.Length)
            {
                return suitSprites[i];
            }
        }
        Debug.LogWarning($"Suit sprite not found for: {suit}");
        return null;
    }
}