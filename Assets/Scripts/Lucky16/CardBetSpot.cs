using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardBetSpot : MonoBehaviour
{

    public TMP_Text betAmountText;     
    public Transform chipContainer;      
    public GameObject chipVisualPrefab; 
    public string CardCode;
    public AudioSource betPlaceAudio;
    private int totalBet = 0;
    internal object cardCode;

    void Awake()
    {
        if (BetManager.Instance != null)
        {
            BetManager.Instance.RegisterCard(this);
        }
    }

    void Start()

    {
        // optional: register with BetManager if you use that
        if (BetManager.Instance != null)
            BetManager.Instance.RegisterCard(this);
        UpdateBetText();
    }


    // Called by the Button onClick
    public void OnCardClicked()
    {
        if (betPlaceAudio != null)
        {
            betPlaceAudio.Play();
        }
        int chipValue = ChipManager.Instance.GetSelectedChipValue();
        Sprite chipSprite = ChipManager.Instance.GetSelectedChipSprite();

        if (chipValue <= 0 || chipSprite == null)
        {
            Debug.Log("CardBetSpot: no chip selected");
            return;
        }

        AddBet(chipValue, chipSprite);

        if (BetManager.Instance != null)
        {
            BetManager.Instance.UpdateTotalUI();
            BetManager.Instance.UpdateBetButtonState();
        }
    }
    public void AddBet(int amount, Sprite chipSprite = null)
    {
        if (amount <= 0) return;

        totalBet += amount;
        UpdateBetText();

        if (chipVisualPrefab != null && chipContainer != null && chipSprite != null)
        {
            GameObject chip = Instantiate(chipVisualPrefab, chipContainer);
            Image img = chip.GetComponent<Image>();
            if (img != null) img.sprite = chipSprite;

            // give a small random offset so chips look stacked
            (chip.transform as RectTransform).anchoredPosition = new Vector2(
                Random.Range(-12f, 12f),
                Random.Range(-8f, 8f)
            );
        }
        if (BetManager.Instance != null)
        {
            BetManager.Instance.UpdateTotalUI();
            BetManager.Instance.UpdateBetButtonState();
        }
    }
    public void AddChipFromManager()
    {
        if (ChipManager.Instance == null)
        {
            Debug.LogWarning("ChipManager.Instance is null - cannot add chip");
            return;
        }
        int chipValue = ChipManager.Instance.GetSelectedChipValue();
        Sprite chipSprite = ChipManager.Instance.GetSelectedChipSprite();

        if (chipValue <= 0 || chipSprite == null) return;

        totalBet += chipValue;
        betAmountText.text = "Bet: " + totalBet;

        // visually stack the chip
        GameObject chipGO = new GameObject("Chip");
        chipGO.transform.SetParent(chipContainer, false);
        var img = chipGO.AddComponent<UnityEngine.UI.Image>();
        img.sprite = chipSprite;
        img.SetNativeSize();
        if (BetManager.Instance != null)
        {
            BetManager.Instance.UpdateTotalUI();
            BetManager.Instance.UpdateBetButtonState();
        }
    }


    public int GetTotalBet()
    {
        return totalBet;
    }

    public void DoubleBet()
    {
        int current = totalBet;
        if (current <= 0) return;


        Sprite selectedSprite = ChipManager.Instance.GetSelectedChipSprite();
        if (selectedSprite == null)
        {
            // fallback: simply add numeric value without spawning visuals
            totalBet += current;
            UpdateBetText();
            return;
        }

        else {
            int remaining = current;
            int chipValue = ChipManager.Instance.GetSelectedChipValue();
            if (chipValue <= 0)
            {
                // if no selected chip to represent doubling, just double numeric
                totalBet += current;
                UpdateBetText();
                return;
            }
            else {
                // Add as many selected chips as needed to reach 'current'
                while (remaining > 0)
                {
                    int add = Mathf.Min(chipValue, remaining);
                    AddBet(add, selectedSprite);
                    remaining -= add;
                }

            }
        }
        if (BetManager.Instance != null)
        {
            BetManager.Instance.UpdateTotalUI();
            BetManager.Instance.UpdateBetButtonState();
        }
    } 


    public void ClearBet()
    {
        totalBet = 0;
        UpdateBetText();
        // clear visuals
        if (chipContainer != null)
        {
            for (int i = chipContainer.childCount - 1; i >= 0; i--)
                Destroy(chipContainer.GetChild(i).gameObject);
           
        }
        if (BetManager.Instance != null)
        {
            BetManager.Instance.UpdateTotalUI();
            BetManager.Instance.UpdateBetButtonState();
        }
    }


    private void UpdateBetText()
    {
        if (betAmountText != null)
            betAmountText.text = "Bet: " + totalBet;
    }
}