using UnityEngine;

public class SpecialButtonsManager : MonoBehaviour
{
    // wire these UI buttons to these methods (OnClick)
    public void OnAllClicked()
    {
        if (BetManager.Instance == null) return;
        BetManager.Instance.ApplyChipToRank('A');
    }

    public void OnJClicked()
    {
        if (BetManager.Instance == null) return;
        BetManager.Instance.ApplyChipToRank('J');
    }

    public void OnQClicked()
    {
        if (BetManager.Instance == null) return;
        BetManager.Instance.ApplyChipToRank('Q');
    }

    public void OnKClicked()
    {
        if (BetManager.Instance == null) return;
        BetManager.Instance.ApplyChipToRank('K');
    }
}
