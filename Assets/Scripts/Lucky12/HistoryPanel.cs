using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HistoryPanel : MonoBehaviour
{
    [Header("API")]
    public string baseUrl = "https://casino-backend.realtimevillage.com/api";

    [Header("UI References")]
    public Transform historyContainer; // Parent object for history items
    public GameObject historyItemPrefab; // Your prefab for single history entry

    [Header("Sprites")]
    public Sprite jSprite, qSprite, kSprite;
    public Sprite hSprite, sSprite, cSprite, dSprite;

    [Header("Date Filter")]
    public string selectedDateFilter = "";

    [Header("Pagination")]
    public int pageLimit = 25;
    public TMP_Text pageText;
    public Button prevPageButton;
    public Button nextPageButton;

    private List<HistoryResult> allResults = new List<HistoryResult>(); // Store all results
    private int currentPage = 1;
    private int totalPages = 1;

    void Start()
    {
        selectedDateFilter = DateTime.Now.ToString("yyyy-MM-dd");
        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(OnPrevPageClicked);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(OnNextPageClicked);

        FetchHistory();
    }
    public void FetchHistoryForDate(string dateString)
    {
        selectedDateFilter = dateString;
        currentPage = 1;
        FetchHistory();
    }
    public void RefreshHistory()
    {
        currentPage = 1;
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

        string url = $"{baseUrl}/v1/result/game-result-list?token={token}&game_id=2&page=1&page_limit=25";
        if (!string.IsNullOrEmpty(selectedDateFilter))
        {
            url += $"&to_date={selectedDateFilter}";
            Debug.Log($"Fetching history for date: {selectedDateFilter}");
        }
        /*if (!string.IsNullOrEmpty(selectedDateFilter))
        {
            url += $"&date={selectedDateFilter}";
            Debug.Log($"Fetching history for date: {selectedDateFilter}");
        }*/

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"API Response: {req.downloadHandler.text}");

                try
                {
                    var historyWrapper = JsonUtility.FromJson<HistoryWrapper>(req.downloadHandler.text);
                    if (historyWrapper != null && historyWrapper.status)
                    {
                        if (historyWrapper.result != null && historyWrapper.result.data != null)
                        {
                            UpdateHistoryPanel(historyWrapper.result.data);
                        }
                        else
                        {
                            Debug.LogWarning("No data in response");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("History panel parse error: " + e.Message);
                }
            }
            else
            {
                Debug.LogError($"API Request Failed: {req.error}");
            }
        }
    }
    public void OnViewButtonClicked()
    {
        
        FetchHistory();
    }
    void UpdateHistoryPanel(List<HistoryResult> results)
    {
        allResults = results;
        if (allResults == null || allResults.Count == 0)
        {
            // Clear container
            foreach (Transform child in historyContainer)
            {
                Destroy(child.gameObject);
            }
            Debug.Log("No results to display");
            return;
        }

        // Calculate total pages
        totalPages = Mathf.CeilToInt((float)allResults.Count / pageLimit);
        Debug.Log($"Total results: {allResults.Count}, Page limit: {pageLimit}, Total pages: {totalPages}");

        // Reset to page 1 when new data comes
        currentPage = 1;

        // Show only current page
        ShowCurrentPage();

        // Update pagination UI
        UpdatePaginationUI();
    }
    // Clear existing items
    /*foreach (Transform child in historyContainer)
    {
        Destroy(child.gameObject);
    }

    // Create new history items
    foreach (var result in results)
    {
        GameObject historyItem = Instantiate(historyItemPrefab, historyContainer);
        SetupHistoryItem(historyItem, result);
    }*/
    void ShowCurrentPage()
    {
        // Clear container
        foreach (Transform child in historyContainer)
        {
            Destroy(child.gameObject);
        }

        if (allResults.Count == 0)
        {
            Debug.Log("No results to show");
            return;
        }

        // Calculate start and end index
        int startIndex = (currentPage - 1) * pageLimit;
        int endIndex = Mathf.Min(startIndex + pageLimit, allResults.Count);

        Debug.Log($"Showing page {currentPage}: Results {startIndex + 1} to {endIndex} of {allResults.Count}");

        // Create items only for current page
        for (int i = startIndex; i < endIndex; i++)
        {
            if (i < allResults.Count)
            {
                GameObject historyItem = Instantiate(historyItemPrefab, historyContainer);
                if (historyItem != null)
                {
                    SetupHistoryItem(historyItem, allResults[i]);
                }
            }
        }
    }
    void UpdatePaginationUI()
    {
        if (pageText != null)
        {
            pageText.text = $"{currentPage}";
        }

        if (prevPageButton != null)
        {
            prevPageButton.interactable = currentPage > 1;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = currentPage < totalPages;
        }

        Debug.Log($"Pagination: Page {currentPage}/{totalPages}");
    }

    public void OnPrevPageClicked()
    {
        if (currentPage > 1)
        {
            currentPage--;
            ShowCurrentPage();
            UpdatePaginationUI();
            Debug.Log($"Previous page clicked: Now on page {currentPage}");
        }
    }

    public void OnNextPageClicked()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            ShowCurrentPage();
            UpdatePaginationUI();
            Debug.Log($"Next page clicked: Now on page {currentPage}");
        }
    }
    void SetupHistoryItem(GameObject item, HistoryResult result)
    {
        // Get references from your prefab
        Image rankImage = item.transform.Find("RankImage")?.GetComponent<Image>();
        Image suitImage = item.transform.Find("SuitImage")?.GetComponent<Image>();
        TextMeshProUGUI drawTimeText = item.transform.Find("DrawTimeText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI resultText = item.transform.Find("ResultText")?.GetComponent<TextMeshProUGUI>();

        string resultCode = result.result;
        if (resultCode.Length >= 2)
        {
            string rank = resultCode[0].ToString(); // J, Q, K
            string suit = resultCode[1].ToString(); // H, S, C, D

            // Set visual elements
            if (rankImage != null) rankImage.sprite = GetRankSprite(rank);
            if (suitImage != null) suitImage.sprite = GetSuitSprite(suit);

            // Set text elements
            if (resultText != null) resultText.text = resultCode;
            if (drawTimeText != null) drawTimeText.text = FormatDrawTime(result.drawn_time);
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

                return drawTime.ToString("dd-MM-yyyy HH:mm:ss");
            }

            System.DateTime fallbackTime = System.DateTime.Parse(rawTime);
            return fallbackTime.ToString("dd-MM-yyyy HH:mm:ss");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Time parsing error: {e.Message} for input: {rawTime}");
            return rawTime; // Return original if parsing fails
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

    // Reuse the same wrapper classes from existing code
    [System.Serializable]
    public class HistoryWrapper
    {
        public bool status;
        public string message;
        public HistoryResultWrapper result;
        public int current_page;    // Add these for pagination
        public int total_pages;     // if the API returns them
        public int total_items;
    }
    [System.Serializable]
    public class HistoryResultWrapper
    {
        public List<HistoryResult> data;
        public PaginationData pagination; // Add pagination support
    }
    [System.Serializable]
    public class PaginationData
    {
        public int page;
        public int page_limit;
        public int total_records;
        public int total_pages;
    }
    [System.Serializable]
    public class HistoryResult
    {
        public int winning_poin; // Note: API returns "poin" not "point"
        public int bit_point;
        public string result;
        public string drawn_time;
        public string drawn_time_unix;
     
    }
}