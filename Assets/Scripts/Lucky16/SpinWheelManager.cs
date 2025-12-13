using UnityEngine;
using System;
using System.Collections;

public class SpinWheelManager : MonoBehaviour
{
    public static SpinWheelManager Instance;

    public RectTransform wheelOuter;
    public RectTransform wheelInner;
    public float defaultDuration = 4f;
    private bool isSpinning = false;
    public AudioSource wheelSpinAudio;

    // UPDATED: 16 slots with Q, J, A, K and H, S, D, C
    private string[] outerWheelSequence = new string[] {
        "Q", "J", "A", "K", "Q", "J", "A", "K", "Q", "J", "A", "K", "Q", "J", "A", "K"
    };

    private string[] innerWheelSequence = new string[] {
        "H", "S", "D", "C", "H", "S", "D", "C", "H", "S", "D", "C", "H", "S", "D", "C"
    };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // MAIN METHOD TO USE: Spin 3 rounds then to result
    public void SpinToResultWithThreeRounds(string resultCode, Action onComplete = null)
    {
        if (!isSpinning)
        {
            Debug.Log($"Starting 3-round spin to result: {resultCode}");
            StartCoroutine(ThreeRoundsSpinRoutine(resultCode, defaultDuration, onComplete));
        }
        else
        {
            Debug.LogWarning("Wheel is already spinning!");
        }
    }

    private IEnumerator ThreeRoundsSpinRoutine(string resultCode, float totalDuration, Action onComplete)
    {
        if (wheelOuter == null || wheelInner == null)
        {
            Debug.LogError("Wheel references are missing!");
            onComplete?.Invoke();
            yield break;
        }

        isSpinning = true;
        if (wheelSpinAudio != null)
        {
            wheelSpinAudio.Play();
        }
        Debug.Log("ThreeRoundsSpinRoutine started");

        // Get the target indices for the result
        var indices = ParseResultCode(resultCode);

        // UPDATED: 16 slots instead of 12
        int slotCount = 16;
        float anglePerSlot = 360f / slotCount; // 22.5 degrees per slot

        // Calculate target angles
        float outerTargetAngle = indices.outerIndex * anglePerSlot;
        float innerTargetAngle = indices.innerIndex * anglePerSlot;

        // Get current rotation
        float startOuter = NormalizeAngle(wheelOuter.localEulerAngles.z);
        float startInner = NormalizeAngle(wheelInner.localEulerAngles.z);

        // Calculate final rotation: 3 full rounds (1080°) + target angle
        float finalOuter = startOuter + 1080f + outerTargetAngle;
        float finalInner = startInner + 1080f + innerTargetAngle;

        Debug.Log($"Spin Details:");
        Debug.Log($"- Result: {resultCode}");
        Debug.Log($"- Outer: start={startOuter}, target={outerTargetAngle}, final={finalOuter}");
        Debug.Log($"- Inner: start={startInner}, target={innerTargetAngle}, final={finalInner}");
        Debug.Log($"- Duration: {totalDuration} seconds");
        Debug.Log($"- Slot Count: {slotCount}, Angle per slot: {anglePerSlot}°");

        float elapsedTime = 0f;
        bool audioStopped = false;

        while (elapsedTime < totalDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / totalDuration);

            if (!audioStopped && elapsedTime >= totalDuration - 1f)
            {
                if (wheelSpinAudio != null && wheelSpinAudio.isPlaying)
                {
                    wheelSpinAudio.Stop();
                }
                audioStopped = true;
            }

            // Use ease-out effect for smooth deceleration
            float easedProgress = EaseOutCubic(progress);

            // Apply rotation
            float currentOuterAngle = Mathf.Lerp(startOuter, finalOuter, easedProgress);
            float currentInnerAngle = Mathf.Lerp(startInner, finalInner, easedProgress);

            wheelOuter.localEulerAngles = new Vector3(0f, 0f, currentOuterAngle);
            wheelInner.localEulerAngles = new Vector3(0f, 0f, currentInnerAngle);

            yield return null;
        }

        // Ensure exact final position
        wheelOuter.localEulerAngles = new Vector3(0f, 0f, finalOuter);
        wheelInner.localEulerAngles = new Vector3(0f, 0f, finalInner);

        if (wheelSpinAudio != null && wheelSpinAudio.isPlaying)
        {
            wheelSpinAudio.Stop();
        }

        isSpinning = false;
        Debug.Log("3-round spin completed successfully!");
        onComplete?.Invoke();
    }

    // Helper method to normalize angles to 0-360 range
    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }

    // Easing function for smooth deceleration
    private float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    // Parse result code like "QH", "JS", "AD", "KC" to wheel indices
    public (int outerIndex, int innerIndex) ParseResultCode(string resultCode)
    {
        if (string.IsNullOrEmpty(resultCode) || resultCode.Length < 2)
        {
            Debug.LogError($"Invalid result code: {resultCode}");
            return (0, 0);
        }

        string rank = resultCode[0].ToString();
        string suit = resultCode[1].ToString();

        int outerIndex = FindBestOuterWheelIndex(rank);
        int innerIndex = FindBestInnerWheelIndex(suit);

        Debug.Log($"Parsed {resultCode} -> Rank: {rank} (outer index {outerIndex}), Suit: {suit} (inner index {innerIndex})");

        return (outerIndex, innerIndex);
    }

    private int FindBestOuterWheelIndex(string targetRank)
    {
        for (int i = 0; i < outerWheelSequence.Length; i++)
        {
            if (outerWheelSequence[i] == targetRank)
            {
                return i;
            }
        }
        Debug.LogWarning($"Rank {targetRank} not found in outer sequence, using index 0");
        return 0;
    }

    private int FindBestInnerWheelIndex(string targetSuit)
    {
        for (int i = 0; i < innerWheelSequence.Length; i++)
        {
            if (innerWheelSequence[i] == targetSuit)
            {
                return i;
            }
        }
        Debug.LogWarning($"Suit {targetSuit} not found in inner sequence, using index 0");
        return 0;
    }

    // Keep old method for compatibility (but you shouldn't use it)
    [System.Obsolete("Use SpinToResultWithThreeRounds instead")]
    public void StartSpinToIndex(int index, float duration, Action onComplete)
    {
        Debug.LogWarning("Using deprecated StartSpinToIndex method!");
        if (!isSpinning) StartCoroutine(OldSpinRoutine(index, duration, onComplete));
    }

    private IEnumerator OldSpinRoutine(int index, float duration, Action onComplete)
    {
        Debug.Log("OLD SPIN METHOD CALLED - This might be the problem!");
        // Your original spin code here...
        yield return null;
    }

    public static implicit operator SpinWheelManager(WheelSpinManager v)
    {
        throw new NotImplementedException();
    }
}