using UnityEngine;

public class SpecialBTN : MonoBehaviour
{
    // wire these UI buttons to these methods (OnClick)
    public void OnHeartClicked()
    {
        if (CG2Manager.Instance == null) return;
        CG2Manager.Instance.ApplyChipToSuits('H');
    }
    public void OnSpadeClicked()
    {
        if (CG2Manager.Instance == null) return;
        CG2Manager.Instance.ApplyChipToSuits('S');
    }
    public void OnDiamondClicked()
    {
        if (CG2Manager.Instance == null) return;
        CG2Manager.Instance.ApplyChipToSuits('D');
    }
    public void OnClubsClicked()
    {
        if (CG2Manager.Instance == null) return;
        CG2Manager.Instance.ApplyChipToSuits('C');
    }

    public void OnJClicked()
    {
        if (CG2Manager.Instance == null) return;
        CG2Manager.Instance.ApplyChipToRank('J');
    }

    public void OnQClicked()
    {
        if (CG2Manager.Instance == null) return;
        CG2Manager.Instance.ApplyChipToRank('Q');
    }

    public void OnKClicked()
    {
        if (CG2Manager.Instance == null) return;
        CG2Manager.Instance.ApplyChipToRank('K');
    }
}

