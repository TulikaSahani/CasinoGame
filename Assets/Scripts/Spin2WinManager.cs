using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System;

public class Spin2WinManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text timerText;
    public TMP_Text resultText;
    public TMP_Text walletText;
    public TMP_Text statusText;
    public TMP_InputField betAmountInput;
    public Button placeBetButton;

    [Header("Number buttons (grid)")]
    public Transform numberGridParent;      // assign NumberGrid (with GridLayoutGroup)
    public GameObject numberButtonPrefab;   // assign NumberButton prefab

    [Header("API")]
    public string baseUrl = "https://casino-backend.realtimevillage.com/api";

    int gameId;
    int gameResultId;
    float game_time = 0f;

    int selectedNumber = -1;
    bool bettingOpen = false;
    bool fetchingResult = false;

    // keep references to instantiated buttons so we can highlight and enable/disable them
    private List<Button> numberButtons = new List<Button>();

    void Start()
    {
        gameId = PlayerPrefs.GetInt("SelectedGameId", 1);
        placeBetButton.onClick.AddListener(OnPlaceBetButtonClicked);

        // create number buttons (0..9)
        CreateNumberButtons();

        // start round setup
        StartCoroutine(SetupRound());
    }

    void CreateNumberButtons()
    {
        if (numberGridParent == null || numberButtonPrefab == null)
        {
            Debug.LogWarning("Number grid or prefab not assigned.");
            return;
        }

        // clear existing children (if any)
        for (int i = numberGridParent.childCount - 1; i >= 0; i--)
            Destroy(numberGridParent.GetChild(i).gameObject);

        numberButtons.Clear();

        for (int i = 0; i <= 9; i++)
        {
            GameObject go = Instantiate(numberButtonPrefab, numberGridParent);
            // assume prefab has a TMP child text and Button on root
            TMP_Text t = go.GetComponentInChildren<TMP_Text>();
            if (t != null) t.text = i.ToString();

            Button b = go.GetComponent<Button>();
            if (b != null)
            {
                int idx = i; // capture local
                b.onClick.AddListener(() => OnNumberSelected(idx));
                numberButtons.Add(b);
            }
            else
            {
                Debug.LogWarning("Number button prefab has no Button component on root.");
            }
        }

        UpdateNumberButtonsInteractable(false); // disabled until we have timer
    }

    void OnNumberSelected(int num)
    {
        if (!bettingOpen) { statusText.text = "Betting is closed."; return; }

        selectedNumber = num;
        statusText.text = "Selected: " + num;

        // highlight selection (simple color change)
        for (int i = 0; i < numberButtons.Count; i++)
        {
            var colors = numberButtons[i].colors;
            colors.normalColor = (i == num) ? Color.cyan : Color.white;
            numberButtons[i].colors = colors;
        }
    }

    void OnPlaceBetButtonClicked()
    {
        if (!bettingOpen)
        {
            statusText.text = "Betting is closed.";
            return;
        }

        if (selectedNumber < 0)
        {
            statusText.text = "Please select a number (0-9).";
            return;
        }

        if (!int.TryParse(betAmountInput.text, out int amount) || amount <= 0)
        {
            statusText.text = "Enter a valid amount.";
            return;
        }

        StartCoroutine(PlaceBetCoroutine(selectedNumber, amount));
    }

    IEnumerator SetupRound()
    {
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No AUTH_KEY in PlayerPrefs. Login first.");
            statusText.text = "Not logged in.";
            yield break;
        }

        // 1) get latest game_result_id
        string urlId = $"{baseUrl}/v1/result/latest-game-result-id?token={token}&game_id={gameId}";
        UnityWebRequest reqId = UnityWebRequest.Get(urlId);
        yield return reqId.SendWebRequest();
        if (reqId.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching latest-game-result-id: " + reqId.error);
            statusText.text = "Failed to get round id.";
            yield break;
        }
        string idJson = reqId.downloadHandler.text;
        Debug.Log("latest-game-result-id: " + idJson);

        // try to parse id -> postman shows: { status: true, message: "...", data: { id: 1 } }
        try
        {
            LatestGameIdWrapper idWrapper = JsonUtility.FromJson<LatestGameIdWrapper>(idJson);
            if (idWrapper != null && idWrapper.data != null)
                gameResultId = idWrapper.data.id;
            else
                Debug.LogWarning("Could not parse latest-game-result-id, raw: " + idJson);
        }
        catch { Debug.LogWarning("Exception parsing latest-game-result-id."); }

        // 2) get latest-game-time
        string urlTime = $"{baseUrl}/v1/result/latest-game-time?token={token}&game_id={gameId}&game_result_id={gameResultId}";
        UnityWebRequest reqTime = UnityWebRequest.Get(urlTime);
        yield return reqTime.SendWebRequest();

        if (reqTime.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching latest-game-time: " + reqTime.error);
            statusText.text = "Failed to get timer.";
            yield break;
        }

        Debug.Log("latest-game-time raw: " + reqTime.downloadHandler.text);

        // set a fallback first
        game_time = 30;
        Debug.LogWarning("API gave game_time = 0, using fallback 30s for testing.");

        // parse JSON and only overwrite if value > 0
        string timeJson = reqTime.downloadHandler.text;
        try
        {
            GameTimeWrapper timeWrapper = JsonUtility.FromJson<GameTimeWrapper>(timeJson);
            if (timeWrapper != null && timeWrapper.data != null && timeWrapper.data.game_time > 0)
            {
                game_time = timeWrapper.data.game_time;
                Debug.Log("Using API game_time: " + game_time);
            }
            else
            {
                Debug.LogWarning("API gave 0, keeping fallback 30s.");
            }
        }
        catch { Debug.LogWarning("Exception parsing game time, using fallback 30s."); }

        // ✅ now betting is open
        bettingOpen = true;
        UpdateNumberButtonsInteractable(true);
        placeBetButton.interactable = true;
        statusText.text = "Betting open";

        // update timer immediately
        int minutes = Mathf.FloorToInt(game_time / 60);
        int seconds = Mathf.FloorToInt(game_time % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        // refresh wallet in background
        StartCoroutine(RefreshWalletCoroutine());


        // set UI and enable betting
        bettingOpen = true;
        UpdateNumberButtonsInteractable(true);
        placeBetButton.interactable = true;
        statusText.text = "Betting open";
        // optionally refresh wallet
        StartCoroutine(RefreshWalletCoroutine());
    }

    void Update()
    {
        if (bettingOpen && game_time > 0)
        {
            game_time -= Time.deltaTime;

            int minutes = Mathf.FloorToInt(game_time / 60);
            int seconds = Mathf.FloorToInt(game_time % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // optional: color change
            if (game_time <= 3)
                timerText.color = Color.red;
            else if (game_time <= 15)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.white;
        }

        // when timer crosses to zero, fetch result once
        else if (bettingOpen && game_time <= 0)
        {
            bettingOpen = false;
            UpdateNumberButtonsInteractable(false);
            placeBetButton.interactable = false;
            statusText.text = "Round complete! Waiting for result...";
        }
    }

    IEnumerator PlaceBetCoroutine(int number, int amount)
    {
        placeBetButton.interactable = false;
        statusText.text = "Placing ticket...";

        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        string url = $"{baseUrl}/v1/ticket/add-spin2win-ticket?token={token}";

        AddSpin2WinTicketRequest reqObj = new AddSpin2WinTicketRequest();
        reqObj.game_result_id = gameResultId;
        reqObj.game_id = gameId;
        reqObj.play_point = amount;

        // zero..nine set to 0 except selected
        reqObj.zero = reqObj.one = reqObj.two = reqObj.three = reqObj.four =
            reqObj.five = reqObj.six = reqObj.seven = reqObj.eight = reqObj.nine = 0;

        switch (number)
        {
            case 0: reqObj.zero = amount; break;
            case 1: reqObj.one = amount; break;
            case 2: reqObj.two = amount; break;
            case 3: reqObj.three = amount; break;
            case 4: reqObj.four = amount; break;
            case 5: reqObj.five = amount; break;
            case 6: reqObj.six = amount; break;
            case 7: reqObj.seven = amount; break;
            case 8: reqObj.eight = amount; break;
            case 9: reqObj.nine = amount; break;
        }

        string json = JsonUtility.ToJson(reqObj);
        UnityWebRequest uw = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        uw.uploadHandler = new UploadHandlerRaw(bodyRaw);
        uw.downloadHandler = new DownloadHandlerBuffer();
        uw.SetRequestHeader("Content-Type", "application/json");

        Debug.Log("PlaceBet payload: " + json);
        yield return uw.SendWebRequest();

        if (uw.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Place bet failed: " + uw.error + " body: " + uw.downloadHandler.text);
            statusText.text = "Failed to place bet.";
        }
        else
        {
            Debug.Log("Place bet response: " + uw.downloadHandler.text);
            statusText.text = "Ticket placed!";
            // refresh wallet
            StartCoroutine(RefreshWalletCoroutine());
        }

        // re-enable button if still betting open
        placeBetButton.interactable = bettingOpen;
    }

    IEnumerator FetchResultCoroutine()
    {
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        string url = $"{baseUrl}/v1/result/fetch-game-result-data?token={token}&game_result_id={gameResultId}";
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fetch result failed: " + req.error);
            resultText.text = "Result: (error)";
            statusText.text = "Failed to fetch result.";
        }
        else
        {
            string raw = req.downloadHandler.text;
            Debug.Log("Fetch result raw: " + raw);

            // Try to parse a "result" field (adjust based on your actual JSON)
            try
            {
                GameFetchWrapper wrap = JsonUtility.FromJson<GameFetchWrapper>(raw);
                // attempt common patterns: wrap.data.result or wrap.data.game_slot_obj etc
                if (wrap != null && wrap.data != null)
                {
                    // If API returns "result" as a number/string
                    if (!string.IsNullOrEmpty(wrap.data.result))
                        resultText.text = "Winning: " + wrap.data.result;
                    else
                        resultText.text = "Result JSON: (see console)";
                }
                else
                {
                    resultText.text = "Result JSON: (see console)";
                }
            }
            catch
            {
                resultText.text = "Result JSON: (see console)";
            }
        }

        // short delay then start a new round (fetch next round)
        yield return new WaitForSeconds(3f);
        fetchingResult = false;
        StartCoroutine(SetupRound());
    }

    IEnumerator RefreshWalletCoroutine()
    {
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        string url = $"{baseUrl}/v1/users/wallet-balance?token={token}";
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Wallet response: " + req.downloadHandler.text);
            try
            {
                WalletWrapper w = JsonUtility.FromJson<WalletWrapper>(req.downloadHandler.text);
                if (w != null && w.data != null)
                {
                    walletText.text = "Wallet: " + w.data.wallet_balance.ToString();
                }
            }
            catch { /* ignore parsing errors for now */ }
        }
    }

    void UpdateNumberButtonsInteractable(bool on)
    {
        foreach (var b in numberButtons)
        {
            if (b != null) b.interactable = on;
        }
    }

    // ------------------ JSON wrapper classes (adjust if your API shape differs) ------------------
    [System.Serializable]
    public class LatestGameIdWrapper
    {
        public bool status;
        public string message;
        public LatestGameIdData data;
    }

    [System.Serializable]
    public class LatestGameIdData
    {
        public int id;
    }



    [System.Serializable]
    public class GameTimeWrapper
    {
        public bool status;
        public string message;
        public GameTimeData data;
    }

    [System.Serializable]
    public class GameTimeData
    {
        public int game_time;
    }


    [Serializable]
    public class AddSpin2WinTicketRequest
    {
        public int game_result_id;
        public int game_id;
        public int play_point;
        public int zero; public int one; public int two; public int three; public int four;
        public int five; public int six; public int seven; public int eight; public int nine;
    }

    [Serializable]
    public class GameFetchWrapper { public bool status; public GameFetchData data; }
    [Serializable] public class GameFetchData { public string result; /* adjust shape to match API */ }

    [Serializable]
    public class WalletWrapper { public bool status; public WalletData data; }
    [Serializable] public class WalletData { public int wallet_balance; }
}
