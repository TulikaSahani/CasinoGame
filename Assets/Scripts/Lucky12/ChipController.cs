using UnityEngine;

public class ChipController : MonoBehaviour
{
    public static ChipController Instance;
    private Chip_Selector currentChip;

    void Awake() {
        if (Instance == null) {
            Instance = this;

        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SelectChip(Chip_Selector chip)
    {
        if (currentChip != null) currentChip.Deselect();
        currentChip = chip;
        currentChip.Select();
    }

    public void ClearSelection()
    {
        if (currentChip != null) currentChip.Deselect();
        currentChip = null;
    }

    public int GetSelectedChipValue()
    {
        return currentChip != null ? currentChip.chipValue : 0;
    }

    public Sprite GetSelectedChipSprite()
    {
        return currentChip != null ? currentChip.GetSprite() : null;
    }
}
