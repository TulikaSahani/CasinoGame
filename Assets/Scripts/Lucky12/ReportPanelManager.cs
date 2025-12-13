using System;
using System.Collections;
using TMPro;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ReportPanelManager : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string baseUrl = "https://casino-backend.realtimevillage.com/api";

    [Header("Print Report")]
    [SerializeField] private Button printButton;


    [Header("UI References")]
    [SerializeField] private TMP_Text fromDateText;
    [SerializeField] private TMP_Text toDateText;
    [SerializeField] private Button viewButton;
    [SerializeField] private Transform reportDataContainer;
    [SerializeField] private GameObject reportRowPrefab;

    // Current dates
    private DateTime fromDate;
    private DateTime toDate;
    private ReportData currentReportData;
    [System.Serializable]
    public class ReportData
    {
        public bool status;
        public string message;
        public PlayerResult result;
    }

    [System.Serializable]
    public class PlayerResult
    {
        public string username;
        public int total_play_point;
        public int total_win_point;
        public int total_claim_point;
        public int total_unclaim_point;
        public int total_end_point;
        public int total_comm_poin;
        public int total_ntp_poin;
    }

    void Start()
    {
        // Initialize with default dates (last 7 days)
        fromDate = DateTime.Now.AddDays(-7);
        toDate = DateTime.Now;

        if (printButton != null)
            printButton.onClick.AddListener(SaveReportAsPDF);
        
        UpdateDateDisplay();

        
        viewButton.onClick.AddListener(FetchReportData);
    }
    public void SaveReportAsPDF()
    {
        if (currentReportData == null || currentReportData.result == null)
        {
            Debug.LogWarning("No report data to save");
            return;
        }

        string reportText = GenerateReportText();
        if (SettingsManager.Instance.PrintTicket)
        {
            SaveTextAsFile(reportText, "GameReport.pdf");
            if (!string.IsNullOrEmpty(SettingsManager.Instance.BluetoothPrinterName))
            {
                PrintToBluetooth(reportText, SettingsManager.Instance.BluetoothPrinterName);
            }
        }
        else
        {
            Debug.Log("Printing is disabled in settings");
            // Just show the report without saving/printing
            ShowMessage("Report generated (Printing disabled in settings)");
        }
    
    }
    void PrintToBluetooth(string text, string printerName)
    {
        // Implement Bluetooth printing logic here
        Debug.Log($"Printing to {printerName}: {text.Substring(0, Mathf.Min(50, text.Length))}...");
    }
    string GenerateReportText()
    {
        var result = currentReportData.result;

        string report = @"
================================
        GAME REPORT
================================
Date Range: " + fromDate.ToString("yyyy-MM-dd") + " to " + toDate.ToString("yyyy-MM-dd") + @"

Player: " + result.username + @"
Play Points: " + result.total_play_point.ToString("N0") + @"
Win Points: " + result.total_win_point.ToString("N0") + @"
Claim Points: " + result.total_claim_point.ToString("N0") + @"
Unclaim Points: " + result.total_unclaim_point.ToString("N0") + @"
End Points: " + result.total_end_point.ToString("N0") + @"
Commission Points: " + result.total_comm_poin.ToString("N0") + @"
NTP Points: " + result.total_ntp_poin.ToString("N0") + @"

Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"
================================
";

        return report;
    }

    void SaveTextAsFile(string content, string fileName)
    {
        // For mobile (Android/iOS)
#if UNITY_ANDROID || UNITY_IOS
        string path = Application.persistentDataPath + "/" + fileName;
#else
        // For PC/Mac
        string path = Application.dataPath + "/" + fileName;
#endif

        File.WriteAllText(path, content);
        Debug.Log("Report saved to: " + path);

        // Show message to user
        ShowMessage("Report saved to:\n" + path);
    }
    void ShowMessage(string message)
    {
        // Simple message display - you can replace with your own UI
        Debug.Log(message);

        // If you have a popup system, use it here
        // messagePopup.Show(message);
    }
    void UpdateDateDisplay()
    {
        fromDateText.text = fromDate.ToString("yyyy-MM-dd");
        toDateText.text = toDate.ToString("yyyy-MM-dd");
    }

    // Call this method from DatePicker's OnDaySelected event in Inspector
    public void SetFromDate(DateTime selectedDate)
    {
        fromDate = selectedDate;
        UpdateDateDisplay();
    }

    // Call this method from DatePicker's OnDaySelected event in Inspector
    public void SetToDate(DateTime selectedDate)
    {
        toDate = selectedDate;
        UpdateDateDisplay();
    }

    public void FetchReportData()
    {
        StartCoroutine(FetchReportDataCoroutine());
    }

    IEnumerator FetchReportDataCoroutine()
    {
        ClearReportData();
        string token = PlayerPrefs.GetString("AUTH_KEY", "");

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("AUTH_KEY not found in PlayerPrefs");
            yield break;
        }

        string url = $"{baseUrl}/v1/report/game-report-list?" +
                    $"token={token}" +
                    $"&game_id=2" +
                    $"&from_date={fromDate:yyyy-MM-dd}" +
                    $"&to_date={toDate:yyyy-MM-dd}";

        Debug.Log($"Fetching report: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"API Response: {webRequest.downloadHandler.text}");
                ProcessReportData(webRequest.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"API Error: {webRequest.error}");
                Debug.LogError($"Full URL: {url}");
                Debug.LogError($"Response Code: {webRequest.responseCode}");
            }
        }
    }

    void ProcessReportData(string jsonData)
    {
        try
        {
            ReportData reportData = JsonUtility.FromJson<ReportData>(jsonData);

            if (reportData.status && reportData.result != null)
            {
                currentReportData = reportData;
                DisplayReportData(reportData.result);
            }
            else
            {
                Debug.LogError($"Report data error: {reportData.message}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"JSON Parse Error: {e.Message}");
            Debug.LogError($"Raw JSON: {jsonData}");
        }
    }

    void DisplayReportData(PlayerResult playerResult)
    {
        GameObject row = Instantiate(reportRowPrefab, reportDataContainer);

        for (int i = 0; i < 8; i++)
        {
            string path = $"Panel/Text (TMP){(i > 0 ? $" ({i})" : "")}";
            var textObj = row.transform.Find(path);
            Debug.Log($"Text {i}: Found={textObj != null}, Active={textObj?.gameObject.activeSelf}");
        }
        // Find each Text component by its Transform path
        TMP_Text playerName = row.transform.Find("Panel/Text (TMP)")?.GetComponent<TMP_Text>();
        TMP_Text playPoints = row.transform.Find("Panel/Text (TMP) (1)")?.GetComponent<TMP_Text>();
        TMP_Text winPoints = row.transform.Find("Panel/Text (TMP) (2)")?.GetComponent<TMP_Text>();
        TMP_Text claimPoints = row.transform.Find("Panel/Text (TMP) (3)")?.GetComponent<TMP_Text>();
        TMP_Text unclaimPoints = row.transform.Find("Panel/Text (TMP) (4)")?.GetComponent<TMP_Text>();
        TMP_Text endPoints = row.transform.Find("Panel/Text (TMP) (5)")?.GetComponent<TMP_Text>();
        TMP_Text commPoints = row.transform.Find("Panel/Text (TMP) (6)")?.GetComponent<TMP_Text>();
        TMP_Text ntpPoints = row.transform.Find("Panel/Text (TMP) (7)")?.GetComponent<TMP_Text>();

        if (ntpPoints == null)
        {
            Debug.LogError("NTP Text (TMP) (7) not found in prefab!");
            // Try alternative names
            ntpPoints = row.transform.Find("Panel/Text (TMP)(7)")?.GetComponent<TMP_Text>();
            if (ntpPoints == null)
                ntpPoints = row.transform.Find("Panel/Text(TMP)(7)")?.GetComponent<TMP_Text>();
        }
        // Assign values
        if (playerName != null) playerName.text = playerResult.username;
        if (playPoints != null) playPoints.text = playerResult.total_play_point.ToString("N0");
        if (winPoints != null) winPoints.text = playerResult.total_win_point.ToString("N0");
        if (claimPoints != null) claimPoints.text = playerResult.total_claim_point.ToString("N0");
        if (unclaimPoints != null) unclaimPoints.text = playerResult.total_unclaim_point.ToString("N0");
        if (endPoints != null) endPoints.text = playerResult.total_end_point.ToString("N0");
        if (commPoints != null) commPoints.text = playerResult.total_comm_poin.ToString("N0");
        // if (ntpPoints != null) ntpPoints.text = playerResult.total_ntp_point.ToString("N0");
        if (ntpPoints != null)
        {
            ntpPoints.text = playerResult.total_ntp_poin.ToString("N0");
            Debug.Log($"Set NTP Text to: {ntpPoints.text}");
        }
    }

    void ClearReportData()
    {
        foreach (Transform child in reportDataContainer)
        {
            Destroy(child.gameObject);
        }
    }
}

[System.Serializable]
public class ReportRowUI : MonoBehaviour
{
    public TMP_Text playerName;
    public TMP_Text playPoints;
    public TMP_Text winPoints;
    public TMP_Text claimPoints;
    public TMP_Text unclaimPoints;
    public TMP_Text endPoints;
    public TMP_Text commPoints;
    public TMP_Text ntpPoints;
}