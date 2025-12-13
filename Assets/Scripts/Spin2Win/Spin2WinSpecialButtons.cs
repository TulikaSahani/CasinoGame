using UnityEngine;

public class Spin2WinSpecialButtons : MonoBehaviour
{
    public void OnOddsClicked()
    {
        CG1Manager.Instance.ApplyChipToOdds();
    }

    public void OnEvensClicked()
    {
        CG1Manager.Instance.ApplyChipToEvens();
    }
}
