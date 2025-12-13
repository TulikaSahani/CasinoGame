using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChipSelector : MonoBehaviour
{
    public int chipValue;        
    public TMP_Text chipText;    
    public Image chipImage;      
    public AudioSource chipSelectAudio;
    private Vector3 originalScale;
    private bool isSelected = false;

    void Start()
    {
        originalScale = transform.localScale;
        if (chipText != null)
            chipText.text = chipValue.ToString();
    }

    public void OnChipClicked()
    {
        if (isSelected)
        {
            Deselect();
            ChipManager.Instance.ClearSelection();
        }
        else
        {
            ChipManager.Instance.SelectChip(this);
        }
    }

    public void Select()
    {
        isSelected = true;
        transform.localScale = originalScale * 1.2f; // grow
                                                     //transform.Rotate(Vector3.forward, 15f);      // spin
        if (chipSelectAudio != null)
        {
            chipSelectAudio.Play();
        }
    }

    public void Deselect()
    {
        isSelected = false;
        transform.localScale = originalScale;
       // transform.rotation = Quaternion.identity;
    }

    public Sprite GetSprite()
    {
        return chipImage != null ? chipImage.sprite : null;
    }
}
