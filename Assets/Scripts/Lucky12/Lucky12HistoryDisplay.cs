using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Lucky12HistoryDisplay : MonoBehaviour
{
    [Header("API")]
    public string baseUrl = "https://casino-backend.realtimevillage.com/api";

    [Header("UI References")]
    public Transform historyContainer;
    public GameObject historyItemPrefab;

    [Header("History Slots")]
    public HistorySlot[] historySlots; // Assign 10 slots in inspector

    [System.Serializable]
    public class HistorySlot
    {
        public Image rankImage;
        public Image suitImage;
        public TextMeshProUGUI drawTimeText;
    }

    [Header("Sprites")]
    public Sprite jSprite, qSprite, kSprite;
    public Sprite hSprite, sSprite, cSprite, dSprite;

    void Start()
    {
        FetchHistory();
    }
    public void RefreshHistory()
    {
        FetchHistory();
    }
    public void FetchHistory()
    {
        StartCoroutine(FetchHistoryCoroutine());
    }

    IEnumerator FetchHistoryCoroutine()
    {
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token)) yield break;

        // Use the history endpoint from your Postman collection
        string url = $"{baseUrl}/v1/result/latest-game-result-history?token={token}&game_id=2";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var historyWrapper = JsonUtility.FromJson<HistoryWrapper>(req.downloadHandler.text);
                    if (historyWrapper != null && historyWrapper.data != null)
                    {
                        UpdateHistoryUI(historyWrapper.data);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("History parse error: " + e.Message);
                }
            }
        }
    }

    void UpdateHistoryUI(List<HistoryResult> results)
    {
        // Display latest 10 results (or less if not available)
        int displayCount = Mathf.Min(results.Count, historySlots.Length);

        for (int i = 0; i < displayCount; i++)
        {
            string resultCode = results[i].result;
            if (resultCode.Length >= 2)
            {
                string rank = resultCode[0].ToString(); // J, Q, K
                string suit = resultCode[1].ToString(); // H, S, C, D

                historySlots[i].rankImage.sprite = GetRankSprite(rank);
                historySlots[i].suitImage.sprite = GetSuitSprite(suit);

                string drawTime = results[i].drawn_time;
                

                historySlots[i].drawTimeText.text = FormatDrawTime(drawTime);
            }
        }

        // Clear remaining slots if less than 10 results
        for (int i = displayCount; i < historySlots.Length; i++)
        {
            historySlots[i].rankImage.sprite = null;
            historySlots[i].suitImage.sprite = null;
            historySlots[i].drawTimeText.text = "";
        }
    }
    string FormatDrawTime(string rawTime)
    {
        if (string.IsNullOrEmpty(rawTime))
            return "N/A";

        try
        {
            if (rawTime.Contains("/"))
            {
                System.DateTime drawTime = System.DateTime.ParseExact(
                 rawTime,
                 "dd/MM/yyyy HH:mm:ss tt",
                 System.Globalization.CultureInfo.InvariantCulture);
            
            return drawTime.ToString("hh:mm tt");
        }
            System.DateTime fallbackTime = System.DateTime.Parse(rawTime);
            return fallbackTime.ToString("hh:mm tt");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Time parsing error: {e.Message} for input: {rawTime}");
            // If parsing fails, return shortened version
            if (rawTime.Length >=16)
            {
                return rawTime.Substring(11, 5); // Extract "HH:mm" from longer string
            }
            return "N/A";
        }
    }
    Sprite GetRankSprite(string rank)
    {
        switch (rank)
        {
            case "J": return jSprite;
            case "Q": return qSprite;
            case "K": return kSprite;
            default: return null;
        }
    }

    Sprite GetSuitSprite(string suit)
    {
        switch (suit)
        {
            case "H": return hSprite;
            case "S": return sSprite;
            case "C": return cSprite;
            case "D": return dSprite;
            default: return null;
        }
    }

    [System.Serializable]
    public class HistoryWrapper
    {
        public bool status;
        public string message;
        public List<HistoryResult> data;
    }

    [System.Serializable]
    public class HistoryResult
    {
        public string result;
        public string drawn_time;
        public string drawn_time_unix;
        // Add other fields if needed: game_time, created_at, etc.
    }
}