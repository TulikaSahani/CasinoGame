using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardBettingSpots : MonoBehaviour
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
        if (CG2Manager.Instance != null)
        {
            CG2Manager.Instance.RegisterCard(this);
        }
    }

    void Start()

    {
        // optional: register with CG2Manager if you use that
        if (CG2Manager.Instance != null)
            CG2Manager.Instance.RegisterCard(this);
        UpdateBetText();
    }


    // Called by the Button onClick
    public void OnCardClicked()
    {
        if (betPlaceAudio != null)
        {
            betPlaceAudio.Play();
        }
        int chipValue = ChipController.Instance.GetSelectedChipValue();
        Sprite chipSprite = ChipController.Instance.GetSelectedChipSprite();

        if (chipValue <= 0 || chipSprite == null)
        {
            Debug.Log("CardBetSpot: no chip selected");
            return;
        }

        AddBet(chipValue, chipSprite);

        if (CG2Manager.Instance != null)
        {
            CG2Manager.Instance.UpdateTotalUI();
            CG2Manager.Instance.UpdateBetButtonState();
        }
    }
    public void AddBet(int amount, Sprite chipSprite = null)
    {
        if (amount <= 0) return;

        totalBet += amount;
        UpdateBetText();
        if (chipContainer != null && chipSprite != null)
        {
            GameObject chipGO = new GameObject("Chip");
            chipGO.transform.SetParent(chipContainer, false);
            var img = chipGO.AddComponent<UnityEngine.UI.Image>();
            img.sprite = chipSprite;
            img.SetNativeSize();

            // Random offset
            (chipGO.transform as RectTransform).anchoredPosition = new Vector2(
                Random.Range(-12f, 12f),
                Random.Range(-8f, 8f)
            );
        }
        /*if (chipVisualPrefab != null && chipContainer != null && chipSprite != null)
        {
            GameObject chip = Instantiate(chipVisualPrefab, chipContainer);
            Image img = chip.GetComponent<Image>();
            if (img != null) img.sprite = chipSprite;

            // give a small random offset so chips look stacked
            (chip.transform as RectTransform).anchoredPosition = new Vector2(
                Random.Range(-12f, 12f),
                Random.Range(-8f, 8f)
            );*/
    
        if (CG2Manager.Instance != null)
        {
            CG2Manager.Instance.UpdateTotalUI();
            CG2Manager.Instance.UpdateBetButtonState();
        }
    }
    public void AddChipFromManager()
    {
        if (ChipController.Instance == null)
        {
            Debug.LogWarning("ChipController.Instance is null - cannot add chip");
            return;
        }
        int chipValue = ChipController.Instance.GetSelectedChipValue();
        Sprite chipSprite = ChipController.Instance.GetSelectedChipSprite();

        if (chipValue <= 0 || chipSprite == null) return;

        totalBet += chipValue;
       // betAmountText.text = "Bet: " + totalBet;
       UpdateBetText ();
        // visually stack the chip
        if (chipContainer != null && chipSprite != null)
        {
            GameObject chipGO = new GameObject("Chip");
            chipGO.transform.SetParent(chipContainer, false);
            var img = chipGO.AddComponent<UnityEngine.UI.Image>();
            img.sprite = chipSprite;
            img.SetNativeSize();
        }
        if (CG2Manager.Instance != null)
        {
            CG2Manager.Instance.UpdateTotalUI();
            CG2Manager.Instance.UpdateBetButtonState();
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


        Sprite selectedSprite = ChipController.Instance.GetSelectedChipSprite();
        if (selectedSprite == null)
        {
            // fallback: simply add numeric value without spawning visuals
            totalBet += current;
            UpdateBetText();
            return;
        }

        else {
            int remaining = current;
            int chipValue = ChipController.Instance.GetSelectedChipValue();
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
        if (CG2Manager.Instance != null)
        {
            CG2Manager.Instance.UpdateTotalUI();
            CG2Manager.Instance.UpdateBetButtonState();
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
        if (CG2Manager.Instance != null)
        {
            CG2Manager.Instance.UpdateTotalUI();
            CG2Manager.Instance.UpdateBetButtonState();
        }
    }


    private void UpdateBetText()
    {
        if (betAmountText != null)
            betAmountText.text = "Bet: " + totalBet;
    }
}