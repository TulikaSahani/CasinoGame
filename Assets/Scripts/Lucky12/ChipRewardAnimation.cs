using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class ChipRewardAnimation : MonoBehaviour
{
    public static ChipRewardAnimation Instance;

    [Header("Animation Settings")]
    public Transform walletTarget;
    public float animationDuration = 1.5f;

    [Header("Chip References")]
    public List<Transform> chips = new List<Transform>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void PlayChipRewardAnimation(Vector2 startPosition, int rewardAmount)
    {
        StartCoroutine(ChipAnimationRoutine());
    }

    private IEnumerator ChipAnimationRoutine()
    {
        Debug.Log("Starting chip animation with " + chips.Count + " chips");
        if (chips.Count == 0)
        {
            Debug.LogError("No chips assigned!");
            yield break;
        }

        for (int i = 0; i < chips.Count; i++)
        {
            if (chips[i] != null)
            {
                chips[i].gameObject.SetActive(true);

                // Force enable all components
                Renderer renderer = chips[i].GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = true;

                CanvasRenderer canvasRenderer = chips[i].GetComponent<CanvasRenderer>();
                if (canvasRenderer != null) canvasRenderer.SetAlpha(1f);

                UnityEngine.UI.Image image = chips[i].GetComponent<UnityEngine.UI.Image>();
                if (image != null) image.enabled = true;

                Debug.Log($"Chip {i} activated at position: {chips[i].position}");
            }
        }

        // Wait one frame to ensure activation
        yield return null;

        // Simple position test - move chips to visible position
        for (int i = 0; i < chips.Count; i++)
        {
            if (chips[i] != null)
            {
                chips[i].position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                Debug.Log($"Chip {i} moved to center screen");
            }
        }

        yield return new WaitForSeconds(2f); // Keep them visible for 2 seconds

        Debug.Log("Test completed - chips should be visible");
    }
}