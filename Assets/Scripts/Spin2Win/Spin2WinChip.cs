using UnityEngine;
using UnityEngine.UI;

public class Spin2WinChip : MonoBehaviour
{
    public int chipValue;   

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnChipClicked);
    }

    void OnChipClicked()
    {
        CG1Manager.Instance.SelectChip(chipValue, this.gameObject);
    }
}
