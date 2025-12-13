using System;
using UnityEngine;

public class ChipManager : MonoBehaviour
{
    public static ChipManager Instance;
    private ChipSelector currentChip;

    void Awake() {
        if (Instance == null) {
            Instance = this;

        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SelectChip(ChipSelector chip)
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

    internal void SelectChip(Chip_Selector chip_Selector)
    {
        throw new NotImplementedException();
    }

    public static implicit operator ChipManager(ChipController v)
    {
        throw new NotImplementedException();
    }
}
