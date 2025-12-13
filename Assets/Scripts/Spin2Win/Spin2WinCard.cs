using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Spin2WinCard : MonoBehaviour
{
    public Transform chipContainer;
    public int cardNumber;   
    public TMP_Text cardLabel;



    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
    }

    void Start()
    {
        if (cardLabel != null)
            cardLabel.text = cardNumber.ToString();
    }

    void OnClicked()
    {
        CG1Manager.Instance.PlaceChipOnCard(cardNumber, this.gameObject);
    }
}
