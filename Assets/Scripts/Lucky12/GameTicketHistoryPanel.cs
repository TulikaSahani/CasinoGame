using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameTicketHistoryPanel : MonoBehaviour
{
    [Header("API Configuration")]
    public string baseUrl = "https://casino-backend.realtimevillage.com/api";

    [Header("History Panel")]
    public Transform historyContainer; // Content object of ScrollView
    public GameObject historyItemPrefab; // GAMEHISPRE prefab

    [Header("Date Filter UI")]
    public TMP_InputField toDateInput; // For "to_date" parameter
    public Button viewButton;
    public Button refreshButton;

    [Header("Other UI")]
    public TMP_Dropdown statusDropdown;
    public TMP_Text noDataText;
    public GameObject loadingPanel;

    [Header("Pagination")]
    public TMP_Text pageText;
    public Button prevPageButton;
    public Button nextPageButton;
    
    private List<GameTicketData> allTickets = new List<GameTicketData>(); // Store all tickets

    [Header("Game Slot Details Modal")]
    public GameObject detailsModal;
    public TMP_Text modalDetailsText;
    public Button modalCloseButton;

    private string currentToken;
    private int currentPage = 1;
    private int pageLimit = 25;
    private int totalPages = 1;
    private string currentToDate = "";
    private string currentStatusFilter = "all";
    private GameTicketData currentModalTicket;

    void Start()
    {
        Debug.Log("GameTicketHistoryPanel Started");
        if (historyContainer == null)
        {
            Debug.LogError("historyContainer is NULL! Drag Content object here.");
            return;
        }

        if (historyItemPrefab == null)
        {
            Debug.LogError("historyItemPrefab is NULL! Drag GAMEHISPRE prefab here.");
            return;
        }

        Debug.Log($"Container: {historyContainer.name}, Prefab: {historyItemPrefab.name}");
       
        InitializeUI();
        
        currentToken = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(currentToken))
        {
            Debug.LogError("No AUTH_KEY found in PlayerPrefs!");
        }
        
        SetDefaultDate();
        FetchHistory();
    }
   /* public void ShowGameSlotDetails(GameTicketData ticket)
    {
        currentModalTicket = ticket;

        if (detailsModal != null && modalDetailsText != null)
        {
            string details = FormatGameSlotDetails(ticket.game_slot_obj);
            modalDetailsText.text = $"Ticket #{ticket.id} - Game Slot Details:\n\n{details}";
            detailsModal.SetActive(true);
        }
    }

    string FormatGameSlotDetails(string rawData)
    {
        if (string.IsNullOrEmpty(rawData)) return "No details";

        // Simple formatting
        if (rawData.StartsWith("{") && rawData.EndsWith("}"))
        {
            return rawData
                .Replace("{", "")
                .Replace("}", "")
                .Replace("\"", "")
                .Replace(",", "\n")
                .Replace(":", ": ");
        }

        return rawData;
    }*/
    public void FetchHistoryForDate(string dateString)
    {
        currentToDate = dateString;
        currentPage = 1;
        FetchHistory();
    }

    void InitializeUI()
    {
        // Setup status dropdown
        if (statusDropdown != null)
        {
            statusDropdown.ClearOptions();
            List<string> options = new List<string> { "ALL", "WON", "LOST", "PENDING", "CLAIMED" };
            statusDropdown.AddOptions(options);
            statusDropdown.onValueChanged.AddListener(OnStatusChanged);
        }

        
        if (viewButton != null)
            viewButton.onClick.AddListener(OnViewButtonClicked);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);

        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(OnPrevPageClicked);

        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(OnNextPageClicked);
    }

    void SetDefaultDate()
    {
        
        if (toDateInput != null)
        {
            currentToDate = DateTime.Now.ToString("yyyy-MM-dd");
            toDateInput.text = currentToDate;
            Debug.Log($"Default date set to: {currentToDate}");
        }
    }

    void OnStatusChanged(int index)
    {
        if (statusDropdown != null)
        {
            currentStatusFilter = statusDropdown.options[index].text.ToLower();
            currentPage = 1;
            FetchHistory();
        }
    }

    void OnViewButtonClicked()
    {
        /*if (toDateInput != null)
        {
            currentToDate = toDateInput.text;
            Debug.Log($"To Date set to: {currentToDate}");
        }*/
        Debug.Log($"Fetching history for date: {currentToDate}");
        currentPage = 1;
        FetchHistory();
    }

    void OnRefreshButtonClicked()
    {
        FetchHistory();
    }

    void OnPrevPageClicked()
    {
        if (currentPage > 1)
        {
            currentPage--;
            ShowCurrentPage();
            UpdatePaginationUI();
            
        }
    }

    void OnNextPageClicked()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            ShowCurrentPage();
            UpdatePaginationUI();
           
        }
    }

    void UpdatePaginationUI()
    {
        if (pageText != null)
            pageText.text = $"{currentPage}";

        if (prevPageButton != null)
            prevPageButton.interactable = currentPage > 1;

        if (nextPageButton != null)
            nextPageButton.interactable = currentPage < totalPages;
    }

    public void FetchHistory()
    {
        StartCoroutine(FetchHistoryCoroutine());
    }

    IEnumerator FetchHistoryCoroutine()
    {
        Debug.Log("Starting FetchHistoryCoroutine");

        if (string.IsNullOrEmpty(currentToken))
        {
            Debug.LogError("Token is empty! Cannot fetch history.");
            ShowNoDataMessage("Please login first");
            yield break;
        }

     
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        ClearHistoryContainer();

        string url = $"{baseUrl}/v1/ticket/game-ticket-history-list?" +
                    $"token={currentToken}&" +
                    $"game_id=2&" +
                    $"page={currentPage}&" +
                    $"page_limit={pageLimit}";

        if (!string.IsNullOrEmpty(currentToDate))
        {
            url += $"&to_date={currentToDate}";
        }

        url += "&is_all_select=true";

        Debug.Log($"API URL: {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("API Request Successful");
                ProcessHistoryResponse(req.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"API Request Failed: {req.error}");
                ShowNoDataMessage($"Failed to load: {req.error}");
            }
        }
    }

    void ProcessHistoryResponse(string jsonResponse)
    {
        try
        {
            Debug.Log($"Raw Response: {jsonResponse}");

            //if (jsonResponse.Contains("total_pages") || jsonResponse.Contains("total_pages"))
            //{
              //  Debug.Log("JSON contains pagination fields");
            //}


            var response = JsonUtility.FromJson<ApiResponse>(jsonResponse);

            if (response != null && response.status)
            {
                Debug.Log($"API Status: {response.status}, Message: {response.message}");

                if (response.result != null && response.result.data != null)
                {
                    Debug.Log($"Found {response.result.data.Count} tickets");
                    UpdateHistoryPanel(response.result.data);

                    // CRITICAL: Update pagination from API response
                   // if (response.result.current_page > 0)
                     //   currentPage = response.result.current_page;

                    // Update pagination info
                  //  if (response.result.total_pages > 0)
                    //    totalPages = response.result.total_pages;
                   // else
                //    {
                        // Calculate total pages if not provided
                    //    totalPages = Mathf.CeilToInt((float)response.result.total_items / pageLimit);
                    //    if (totalPages == 0) totalPages = 1;
               //     }

                  //  Debug.Log($"Updated: currentPage={currentPage}, totalPages={totalPages}");

//                    UpdatePaginationUI();
                }
                else
                {
                    Debug.Log("No data in response");
                    ShowNoDataMessage("No tickets found");
                }
            }
            else
            {
                Debug.Log($"API returned error: {response?.message}");
                ShowNoDataMessage(response?.message ?? "Failed to load data");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Parse Error: {e.Message}\nStack: {e.StackTrace}");
            ShowNoDataMessage("Error processing data");
        }
    }

    void ClearHistoryContainer()
    {
        foreach (Transform child in historyContainer)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"Cleared container. Child count: {historyContainer.childCount}");
    }

    void UpdateHistoryPanel(List<GameTicketData> tickets)
    {
        allTickets = tickets;
        if (allTickets == null || allTickets.Count == 0)
        {
            ShowNoDataMessage("No tickets to display");
            totalPages = 1;
            UpdatePaginationUI();
            return;
        }
        totalPages = Mathf.CeilToInt((float)allTickets.Count / pageLimit);
        Debug.Log($"Total tickets: {allTickets.Count}, Page limit: {pageLimit}, Total pages: {totalPages}");

        ShowCurrentPage();
        UpdatePaginationUI();
    }
    /* if (tickets == null || tickets.Count == 0)
     {
         ShowNoDataMessage("No tickets to display");
         return;
     }

     Debug.Log($"Creating {tickets.Count} history items");

     foreach (var ticket in tickets)
     {
         // Instantiate the prefab
         GameObject historyItem = Instantiate(historyItemPrefab, historyContainer);

         if (historyItem == null)
         {
             Debug.LogError("Failed to instantiate prefab!");
             continue;
         }

         // Set up the item with ticket data
         SetupHistoryItem(historyItem, ticket);
     }

     // Hide no data text
     if (noDataText != null)
         noDataText.gameObject.SetActive(false);
 }*/
    void ShowCurrentPage()
    {
      
        ClearHistoryContainer();

        if (allTickets.Count == 0)
        {
            ShowNoDataMessage("No tickets found");
            return;
        }

      
        int startIndex = (currentPage - 1) * pageLimit;
        int endIndex = Mathf.Min(startIndex + pageLimit, allTickets.Count);

        Debug.Log($"Showing page {currentPage}: Items {startIndex + 1} to {endIndex} of {allTickets.Count}");

        
        for (int i = startIndex; i < endIndex; i++)
        {
            if (i < allTickets.Count)
            {
                GameObject historyItem = Instantiate(historyItemPrefab, historyContainer);
                if (historyItem != null)
                {
                    SetupHistoryItem(historyItem, allTickets[i]);
                }
            }
        }

     
        if (noDataText != null)
            noDataText.gameObject.SetActive(false);
    }
    void SetupHistoryItem(GameObject item, GameTicketData ticket)
    {
        try
        {
            // Your prefab hierarchy: GAMEHISPRE > Panel > [Text elements]
            // Since Panel is a direct child, we'll search within it

            Transform panelTransform = item.transform.Find("Panel");
            if (panelTransform == null)
            {
                Debug.LogError("Panel not found in prefab!");
                return;
            }
           
            
            SetTextInChild(panelTransform, "TicketIdText", ticket.id.ToString());
            SetTextInChild(panelTransform, "GameIdText", ticket.game_id.ToString());
            SetTextInChild(panelTransform, "ResultText", GetFormattedResult(ticket));
            SetTextInChild(panelTransform, "PlayPointsText", ticket.play_point.ToString());
            SetTextInChild(panelTransform, "WinPointsText", ticket.win_point.ToString());
            SetTextInChild(panelTransform, "ClaimPointsText", ticket.claim_point.ToString());
            SetTextInChild(panelTransform, "StatusText", GetTicketStatus(ticket));
            SetTextInChild(panelTransform, "TicketTimeText", FormatDateTime(ticket.ticket_time));
            SetTextInChild(panelTransform, "EndPointText", CalculateEndPoint(ticket).ToString());

            Debug.Log($"Successfully setup ticket #{ticket.id}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error setting up ticket {ticket.id}: {e.Message}");
        }
    }

    void SetTextInChild(Transform parent, string childName, string text)
    {
        Transform childTransform = parent.Find(childName);
        if (childTransform != null)
        {
            TMP_Text textComponent = childTransform.GetComponent<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
            else
            {
                Debug.LogWarning($"No TMP_Text component on {childName}");
            }
        }
        else
        {
           
            childTransform = FindDeepChild(parent, childName);
            if (childTransform != null)
            {
                TMP_Text textComponent = childTransform.GetComponent<TMP_Text>();
                if (textComponent != null)
                {
                    textComponent.text = text;
                }
            }
            else
            {
                Debug.LogWarning($"Could not find {childName} in prefab");
            }
        }
    }

    Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    string GetFormattedResult(GameTicketData ticket)
    {
       
        if (!string.IsNullOrEmpty(ticket.result))
        {
            return ticket.result;
        }

        // Check if game_slot_obj contains data
        if (!string.IsNullOrEmpty(ticket.game_slot_obj))
        {
            try
            {
                // Try to parse as JSON array
                if (ticket.game_slot_obj.StartsWith("[") && ticket.game_slot_obj.EndsWith("]"))
                {
                    // This is a JSON array, extract numbers
                    string clean = ticket.game_slot_obj.Replace("[", "").Replace("]", "").Replace("\"", "");
                    return clean;
                }
                return ticket.game_slot_obj;
            }
            catch
            {
                return "N/A";
            }
        }

        return "N/A";
    }

    string GetTicketStatus(GameTicketData ticket)
    {
        if (ticket.win_point > 0)
        {
            if (ticket.claim_point > 0)
                return "CLAIMED";
            else
                return "WON";
        }
        else if (ticket.play_point > 0)
        {
            return "PLAYED";
        }
        else
        {
            return "PENDING";
        }
    }

    int CalculateEndPoint(GameTicketData ticket)
    {
        // End Point calculation based on your UI
        // This might be: win_point - play_point or similar
        // Adjust according to your game logic
        return ticket.win_point; // Simplified - adjust as needed
    }

    string FormatDateTime(string rawDateTime)
    {
        if (string.IsNullOrEmpty(rawDateTime))
            return "N/A";

        try
        {
            // Remove the timezone part if present
            if (rawDateTime.Contains("."))
            {
                rawDateTime = rawDateTime.Split('.')[0];
            }

            if (DateTime.TryParse(rawDateTime, out DateTime dateTime))
            {
                return dateTime.ToString("dd-MM-yyyy HH:mm:ss");
            }

            return rawDateTime;
        }
        catch
        {
            return rawDateTime;
        }
    }

    void ShowNoDataMessage(string message)
    {
        ClearHistoryContainer();

        if (noDataText != null)
        {
            noDataText.text = message;
            noDataText.gameObject.SetActive(true);
        }

        UpdatePaginationUI();
    }
}

// API Response Classes
[System.Serializable]
public class ApiResponse
{
    public bool status;
    public string message;
    public TicketResult result;
}

[System.Serializable]
public class TicketResult
{
    public List<GameTicketData> data;
    public int current_page;
    public int total_pages;
    public int total_items;
}

[System.Serializable]
public class GameTicketData
{
    public int id;
    public int game_id;
    public int game_result_id;
    public int user_id;
    public int play_point;
    public int win_point;
    public int claim_point;
    public string result;
    public string ticket_time;
    public string game_slot_obj;
}