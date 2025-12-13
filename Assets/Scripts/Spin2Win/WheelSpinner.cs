using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WheelSpinner : MonoBehaviour
{
    [Header("Wheel Settings")]
    public Transform wheelTransform;
    public float spinDuration = 5f;
    public int minFullSpins = 3;
    public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Events")]
    public UnityEvent onSpinStart;
    public UnityEvent<int> onSpinComplete;

    public AudioSource WheelSpinAudio;
    public bool IsSpinning { get; private set; }

    private Coroutine spinCoroutine;
    private float degreesPerNumber = 36f; // 360/10 numbers

    public void SpinToNumber(int targetNumber)
    {
        if (IsSpinning || wheelTransform == null) return;

        if (targetNumber < 0 || targetNumber >= 10)
        {
            Debug.LogError($"Invalid number: {targetNumber}. Use 0-9");
            return;
        }

        if (spinCoroutine != null) StopCoroutine(spinCoroutine);
        spinCoroutine = StartCoroutine(SpinCoroutine(targetNumber));
    }

    private IEnumerator SpinCoroutine(int targetNumber)
    {
        IsSpinning = true;
        if (WheelSpinAudio != null)
        {
            WheelSpinAudio.Play();
        }
        onSpinStart?.Invoke();

        float startRotation = wheelTransform.eulerAngles.z;
        float targetAngle = GetAngleForNumber(targetNumber);
        float totalRotation = (minFullSpins * 360f) + targetAngle;
        float elapsedTime = 0f;

        while (elapsedTime < spinDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = spinCurve.Evaluate(elapsedTime / spinDuration);

          
            wheelTransform.eulerAngles = new Vector3(0, 0, startRotation + Mathf.Lerp(0, totalRotation, progress));
            yield return null;
        }

        wheelTransform.eulerAngles = new Vector3(0, 0, startRotation + totalRotation);
        IsSpinning = false;

        if (WheelSpinAudio != null && WheelSpinAudio.isPlaying) WheelSpinAudio.Stop();

        float finalAngle = wheelTransform.eulerAngles.z % 360f;
        int finalNumber = GetNumberFromAngle(finalAngle);
        onSpinComplete?.Invoke(targetNumber);
    }
    private float GetAngleForNumber(int number)
    {
        // Convert number to position in 1-2-3-4-5-6-7-8-9-0 sequence
        int position = (number == 0) ? 9 : (number - 1);
        return position * 36f;

        
    }
    private int GetNumberFromAngle(float angle)
    {
        // Convert angle back to number for verification
        angle = angle % 360f;
        if (angle < 0) angle += 360f;

        if (angle >= 342f || angle < 18f) return 1;      // 0° ±18°
        else if (angle >= 18f && angle < 54f) return 2;  // 36° ±18°
        else if (angle >= 54f && angle < 90f) return 3;  // 72° ±18°
        else if (angle >= 90f && angle < 126f) return 4; // 108° ±18°
        else if (angle >= 126f && angle < 162f) return 5; // 144° ±18°
        else if (angle >= 162f && angle < 198f) return 6; // 180° ±18°
        else if (angle >= 198f && angle < 234f) return 7; // 216° ±18°
        else if (angle >= 234f && angle < 270f) return 8; // 252° ±18°
        else if (angle >= 270f && angle < 306f) return 9; // 288° ±18°
        else return 0; // 324° ±18°
    }
    public void StopSpin()
    {
        if (WheelSpinAudio != null && WheelSpinAudio.isPlaying)
        {
            WheelSpinAudio.Stop();
        }
        if (spinCoroutine != null) StopCoroutine(spinCoroutine);
        IsSpinning = false;

       
    }
}
