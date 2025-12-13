using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static BetManager;

public class CG1Manager : MonoBehaviour
{
    public rewardanimation RewardAnimation;

    [Header("Audio")]
    public AudioSource buttonClickAudio;
    public AudioSource chipSelectAudio;
    public AudioSource betPlaceAudio;
    public AudioSource countdownSoundtrack;
    public AudioSource winAudio;
    public AudioSource nowinAudio;

    public static CG1Manager Instance;
    [Header("User Info")]
    public TMP_Text usernameText;
    [Header("Win Effects")]
    public GameObject glowEffect;

    [Header("Cross Panel")]
    public GameObject crossPanel;

    [Header("UI References")]
    public TMP_Text totalBetText;
    public TMP_Text statusText;

    [Header("Wheel Animation")]
    public WheelSpinner wheelSpinner;
    private bool spinning = false;
    private bool roundComplete = false;

    [Header("Chip Prefabs")]
    public GameObject chip5Prefab;
    public GameObject chip10Prefab;
    public GameObject chip50Prefab;
    public GameObject chip100Prefab;
    public GameObject chip500Prefab;

    [Header("API Settings")]
    public string baseUrl = "https://casino-backend.realtimevillage.com/api";
    public int gameId = 1;
    public float fallbackTime = 30f;
    public Image timerFillImage;

    [Header("Round End UI")]
    public GameObject endRoundPanel;
    public Button restartButton;
    public Button exitButton;

    [Header("Timer UI")]
    public TMP_Text timerText;
    public float warningThreshold = 15f;
    public float closingThreshold = 10f;
    private float maxGameTime = 60f;
    private float gameTime = 0f;
    private bool bettingOpen = false;
    private int currentGameResultId = 0;
    private bool countdownSoundPlayed = false;

    [Header("Wallet Balance")]
    public TMP_Text walletBalanceText;
    public TMP_Text winningNumberText;
    public TMP_Text winningAmountText;

    private int currentWalletBalance = 0;
    private int totalWinnings = 0;
    private int winningNum = -1;

    [Header("History Display - Simple")]
    public Transform historyGrid;
    public List<Sprite> numberCardSprites;
    public GameObject historyCardPrefab;

   

    [Header("Cards (0–9)")]
    public List<Spin2WinCard> cards = new List<Spin2WinCard>();

    [Header("Chips")]
    public List<Spin2WinChip> chipButtons = new List<Spin2WinChip>();

    private int selectedChipValue = 0;
    private GameObject selectedChipButton;

    private Dictionary<int, int> bets = new Dictionary<int, int>();
    private Dictionary<int, int> previousBets = new Dictionary<int, int>();

    [System.Serializable]
    public class GameHistoryResponse
    {
        public bool status;
        public string message;
        public List<GameHistoryData> data;
    }

    [System.Serializable]
    public class GameHistoryData
    {
        public int id;
        public string result;
        public string created_at;
        public int game_id;
    }



    [System.Serializable]
    public class Spin2WinBetRequest
    {
        public int game_result_id;
        public int game_id;
        public int play_point;

        public int zero, one, two, three, four, five, six, seven, eight, nine;
    }
    #region Audio Helpers
    public void PlayButtonClickSound()
    {
        if (buttonClickAudio != null)
        {
            buttonClickAudio.Play();
        }
    }

    public void PlayChipSelectSound()
    {
        if (chipSelectAudio != null)
        {
            chipSelectAudio.Play();
        }
    }

    public void PlayBetPlaceSound()
    {
        if (betPlaceAudio != null)
        {
            betPlaceAudio.Play();
        }
    }

    #endregion
    private void LoadUsername()
    {
        // Get username from PlayerPrefs (saved by UserManager)
        string username = PlayerPrefs.GetString("Username", "Guest");

        if (usernameText != null)
            usernameText.text = " " + username.ToUpper();

        Debug.Log($"Loaded username: {username}");
    }
    void Awake()
    {
        Instance = this;
    }
    public void SelectChip(int value, GameObject chipButton)
    {
        foreach (var chip in chipButtons)
        {
            chip.transform.localScale = Vector3.one; // Normal size
        }
        selectedChipValue = value;
        selectedChipButton = chipButton;
        chipButton.transform.localScale = Vector3.one * 1.2f; // 20% bigger
        PlayChipSelectSound();
        if (statusText != null)
            statusText.text = "Selected chip: " + value;
    }
    public void ShowCrossPanel()
    {
        if (crossPanel != null)
            crossPanel.SetActive(true);
        PlayButtonClickSound();
    }

    public void HideCrossPanel()
    {
        if (crossPanel != null)
            crossPanel.SetActive(false);
    }

    public void OnCrossYesClicked()
    {
        if (countdownSoundtrack != null && countdownSoundtrack.isPlaying)
        {
            countdownSoundtrack.Stop();
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        PlayButtonClickSound();
    }
    private void DebugAPIResponseStructure(string jsonResponse)
    {
        Debug.Log("=== API Response Structure Debug ===");
        Debug.Log("Full JSON: " + jsonResponse);

        try
        {
            // Try to parse as simple object to see structure
            var simple = JsonUtility.FromJson<SimpleResponseWrapper>(jsonResponse);
            if (simple != null)
            {
                Debug.Log($"Status: {simple.status}, Message: {simple.message}");
                Debug.Log($"Data type: {simple.data?.GetType()}");
                Debug.Log($"Data content: {simple.data}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse response structure: " + e.Message);
        }
        Debug.Log("=== End Debug ===");
    }

    public void OnCrossNoClicked()
    {
        PlayButtonClickSound();
        HideCrossPanel();
    }
    // Repeat previous bets
    public void RepeatBets()
    {
        PlayButtonClickSound();
        // Check if there are any previous bets to repeat
        if (previousBets.Count == 0)
        {
            if (statusText != null)
                statusText.text = "No previous bets to repeat!";
            return;
        }

        // Calculate total cost of repeating all previous bets
        int totalCost = 0;
        foreach (var bet in previousBets.Values)
        {
            totalCost += bet;
        }

        // Check if player has sufficient balance
        if (totalCost > currentWalletBalance)
        {
            if (statusText != null)
                statusText.text = $"Need ${totalCost} to repeat previous bets!";
            return;
        }

        // Clear current bets first
        ClearAllBets();

        // Place all previous bets
        foreach (var kvp in previousBets)
        {
            var card = cards.Find(c => c.cardNumber == kvp.Key);
            if (card != null && kvp.Value > 0)
            {
                // Add to current bets
                bets[kvp.Key] = kvp.Value;

                // Deduct from wallet
                currentWalletBalance -= kvp.Value;

                // Create visual chips
                if (card.chipContainer != null)
                {
                    GameObject prefab = GetChipPrefab(GetChipValueForAmount(kvp.Value));
                    if (prefab != null)
                    {
                        Instantiate(prefab, card.chipContainer);
                    }
                }
            }
        }

        UpdateTotalUI();
        UpdateWalletDisplay();

        if (statusText != null)
            statusText.text = "Previous bets repeated!";
    }

    // Called by number buttons (Spin2WinCard)
    public void PlaceChipOnCard(int cardNumber, GameObject cardObj)
    {
        Debug.Log($"Trying to place {selectedChipValue} on {cardNumber}");


        if (selectedChipValue <= 0)
        {
            if (statusText != null) statusText.text = "Select a chip first!";
            return;
        }
        int totalCurrentBet = GetTotalBetAmount();
        if (selectedChipValue > currentWalletBalance)
        {
            if (statusText != null) statusText.text = "Insufficient balance!";
            return;
        }

        DebugBetPlacement(cardNumber, selectedChipValue);

        if (!bets.ContainsKey(cardNumber))
            bets[cardNumber] = 0;
        // DEDUCT FROM WALLET
        currentWalletBalance -= selectedChipValue;
        bets[cardNumber] += selectedChipValue;



        UpdateTotalUI();
        UpdateWalletDisplay();

        Debug.Log($"=== AFTER BET PLACEMENT ===");
        Debug.Log($"Actual new bet on {cardNumber}: {bets[cardNumber]}");
        Debug.Log($"Total after: {GetTotalBetAmount()}");
        Debug.Log($"Wallet balance after: {currentWalletBalance}");
        Debug.Log("======================");

        Spin2WinCard card = cardObj.GetComponent<Spin2WinCard>();
        if (card != null && card.chipContainer != null)
        {
            GameObject prefab = GetChipPrefab(selectedChipValue);
            if (prefab != null)
            {
               // Instantiate(prefab, card.chipContainer);
                GameObject chip = Instantiate(prefab, card.chipContainer);
                RectTransform chipRect = chip.GetComponent<RectTransform>();
                if (chipRect != null)
                {
                    chipRect.anchoredPosition = Vector2.zero;
                    chipRect.localScale = Vector3.one;
                }
                //  Chip.transform.localPosition = Vector3.zero; // Center in container
                //  chip.transform.localScale = Vector3.one;
            }
        }
        PlayBetPlaceSound();


        if (statusText != null)
            statusText.text = $"Placed {selectedChipValue} on {cardNumber}";
    }
    private void DebugBetPlacement(int cardNumber, int selectedChipValue)
    {
        Debug.Log("=== BET PLACEMENT DEBUG ===");
        Debug.Log($"Selected Chip Value: {selectedChipValue}");

        int oldBet = bets.ContainsKey(cardNumber) ? bets[cardNumber] : 0;
        Debug.Log($"Old bet on {cardNumber}: {oldBet}");

        int newBet = oldBet + selectedChipValue;
        Debug.Log($"Expected new bet: {newBet}");

        Debug.Log("Current all bets:");
        foreach (var bet in bets)
        {
            Debug.Log($"Number {bet.Key}: {bet.Value}");
        }
        Debug.Log($"Total before: {GetTotalBetAmount()}");
        Debug.Log("======================");
    }
    private int GetTotalBetAmount()
    {
        int total = 0;
        foreach (var kvp in bets)
        {
            total += kvp.Value;
            Debug.Log($"Adding bet: Number {kvp.Key} = {kvp.Value}"); // Add this for debugging
        }
        Debug.Log($"GetTotalBetAmount returning: {total}");
        return total;
    }
    private GameObject GetChipPrefab(int value)
    {
        switch (value)
        {
            case 5: return chip5Prefab;
            case 10: return chip10Prefab;
            case 50: return chip50Prefab;
            case 100: return chip100Prefab;
            case 500: return chip500Prefab;
        }
        return null;
    }

    // Odds button
    public void ApplyChipToOdds()
    {
        if (selectedChipValue <= 0)
        {
            if (statusText != null) statusText.text = "Select a chip first!";
            return;
        }
        PlayButtonClickSound();

        int oddNumbersCount = 0;
        foreach (var card in cards)
        {
            if (card.cardNumber % 2 == 1) // odd
                oddNumbersCount++;
            // PlaceChipOnCard(card.cardNumber, card.gameObject);
        }
        int totalCost = selectedChipValue * oddNumbersCount;
        if (totalCost > currentWalletBalance)
        {
            if (statusText != null) statusText.text = $"Need {totalCost} for all odds!";
            return;
        }
        foreach (var card in cards)
        {
            if (card.cardNumber % 2 == 1) // odd
                PlaceChipOnCard(card.cardNumber, card.gameObject);
        }
    }

    // Evens button
    public void ApplyChipToEvens()
    {
        if (selectedChipValue <= 0)
        {
            if (statusText != null) statusText.text = "Select a chip first!";
            return;
        }
        PlayButtonClickSound();
        int evenNumbersCount = 0;
        foreach (var card in cards)
        {
            if (card.cardNumber % 2 == 0) // even
                evenNumbersCount++;
        }

        int totalCost = selectedChipValue * evenNumbersCount;
        if (totalCost > currentWalletBalance)
        {
            if (statusText != null) statusText.text = $"Need ${totalCost} for all evens!";
            return;
        }

        foreach (var card in cards)
        {
            if (card.cardNumber % 2 == 0) // even
                PlaceChipOnCard(card.cardNumber, card.gameObject);
        }
    }

    private void UpdateTotalUI()
    {
        int total = 0;
        foreach (var kvp in bets) total += kvp.Value;

        if (totalBetText != null)
            totalBetText.text = " " + total;
    }

    // For API integration later
    public Dictionary<int, int> GetBets()
    {
        return bets;
    }

    // Clear all bets
    public void ClearBets()
    {
        bets.Clear();
        UpdateTotalUI();
        foreach (var card in cards)
        {
            if (card != null && card.chipContainer != null)
            {
                // Destroy all chip children
                foreach (Transform child in card.chipContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        Debug.Log("All bets and chips cleared");

    }
    void Start()
    {
        /*if (restartButton != null)
            restartButton.onClick.AddListener(() =>
            {
                endRoundPanel.SetActive(false);
                ClearBets();
                StartCoroutine(SetupRound()); // start new round
            });*/

        if (exitButton != null)
            exitButton.onClick.AddListener(() =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(1);
            });
        ResetTimer();
        StartCoroutine(SetupRound());
        InitializeHistory();
    }

    IEnumerator SetupRound()
    {
        bettingOpen = false;
        spinning = false;
        roundComplete = false;
        countdownSoundPlayed = false;
        ClearBets();
        ClearWinningsDisplay();

        if (timerFillImage != null)
        {
            timerFillImage.gameObject.SetActive(true);
            timerFillImage.enabled = true;
            timerFillImage.fillAmount = 1f;
            timerFillImage.color = Color.green;
            Debug.Log("Timer fill image initialized");
        }

        if (glowEffect != null)
            glowEffect.SetActive(false);

        LoadUsername();

        yield return StartCoroutine(FetchWalletBalance());

        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No AUTH_KEY found. Login first.");
            yield break;
        }
        gameTime = 0f;
        maxGameTime = 0f;
        // 1. Fetch latest-game-result-id
        string urlId = $"{baseUrl}/v1/result/latest-game-result-id?token={token}&game_id={gameId}";
        UnityWebRequest reqId = UnityWebRequest.Get(urlId);
        yield return reqId.SendWebRequest();

        if (reqId.result == UnityWebRequest.Result.Success)
        {
            try
            {
                // Use GameResultIdWrapper instead of LatestGameIdWrapper
                var idWrap = JsonUtility.FromJson<GameResultIdWrapper>(reqId.downloadHandler.text);
                if (idWrap != null && idWrap.data != null)
                {
                    currentGameResultId = idWrap.data.id;
                    // Also try to get remaining_time from this response if available
                    if (idWrap.data.remaining_time > 0)
                    {
                        gameTime = idWrap.data.remaining_time;
                        maxGameTime = gameTime;
                        Debug.Log($"Got time from game ID response: {gameTime} seconds");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Exception parsing game ID: " + e.Message);
            }
            //var idWrap = JsonUtility.FromJson<LatestGameIdWrapper>(reqId.downloadHandler.text);
            //if (idWrap != null && idWrap.data != null)
            //    currentGameResultId = idWrap.data.id;
        }

        // 2. Fetch latest-game-time
        if (gameTime <= 0)
        {
            string urlTime = $"{baseUrl}/v1/result/latest-game-result-id?token={token}&game_result_id={currentGameResultId}";
            UnityWebRequest reqTime = UnityWebRequest.Get(urlTime);
            yield return reqTime.SendWebRequest();

            Debug.Log("Game Time API Response: " + reqTime.downloadHandler.text);


            //gameTime = fallbackTime; // default
            if (reqTime.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var tw = JsonUtility.FromJson<GameTimeWrapper>(reqTime.downloadHandler.text);
                    if (tw != null && tw.data != null)
                    {
                        // Try both possible field names
                        if (tw.data.remaining_time > 0)
                        {
                            gameTime = tw.data.remaining_time;
                            maxGameTime = gameTime;
                            Debug.Log($"Got time from remaining_time: {gameTime} seconds");
                        }
                        else if (tw.data.game_time > 0)
                        {
                            gameTime = tw.data.game_time;
                            maxGameTime = gameTime;
                            Debug.Log($"Got time from game_time: {gameTime} seconds");
                        }
                        else
                        {
                            gameTime = fallbackTime;
                            maxGameTime = fallbackTime;
                            Debug.Log($"Using fallback time: {gameTime} seconds");
                        }
                    }
                    else
                    {
                        gameTime = fallbackTime;
                        maxGameTime = fallbackTime;
                        Debug.Log($"Null data, using fallback time: {gameTime} seconds");
                    }
                }
                catch (System.Exception e)
                {
                    gameTime = fallbackTime;
                    maxGameTime = fallbackTime;
                    Debug.LogWarning($"Exception parsing time, using fallback: {e.Message}");
                }
            }
            else
            {
                gameTime = fallbackTime;
                maxGameTime = fallbackTime;
                Debug.Log($"API failed, using fallback time: {gameTime} seconds");
            }

        }
        UpdateTimerUI();

        bettingOpen = true;

        if (statusText != null)
            statusText.text = "Betting open!";

        Debug.Log($"Round setup complete - Time: {gameTime}s, GameResultId: {currentGameResultId}");
    }
    private void ClearWinningsDisplay()
    {
        if (winningNumberText != null)
            winningNumberText.text = " ";

        if (winningAmountText != null)
        {
            winningAmountText.text = "";
            winningAmountText.color = Color.white;
        }

        totalWinnings = 0;
        winningNum = -1;
    }
    void Update()
    {

        if (bettingOpen && gameTime > 0)
        {
            gameTime -= Time.deltaTime;
            gameTime = Mathf.Max(0, gameTime);

            int sec = Mathf.FloorToInt(gameTime % 60);
            int min = Mathf.FloorToInt(gameTime / 60);
            timerText.text = $"{min:00}:{sec:00}";

            if (gameTime <= 10f && !countdownSoundPlayed)
            {
                if (countdownSoundtrack != null)
                {
                    countdownSoundtrack.Play();
                    Debug.Log("Countdown soundtrack started");
                }
                countdownSoundPlayed = true;
            }

            if (timerFillImage != null)
            {

                float fillAmount = gameTime / maxGameTime; // Use fallbackTime
                timerFillImage.fillAmount = fillAmount;

                if (gameTime <= closingThreshold)
                {
                    float pulse = Mathf.PingPong(Time.time * 3f, 0.4f) + 0.6f;
                    timerFillImage.color = Color.Lerp(Color.red, new Color(1f, 0.5f, 0.5f), pulse);
                    timerText.color = Color.red;
                }
                else if (gameTime <= warningThreshold)
                {
                    float pulse = Mathf.PingPong(Time.time * 2f, 0.3f) + 0.7f;
                    timerFillImage.color = Color.Lerp(Color.yellow, new Color(1f, 1f, 0.5f), pulse);
                    timerText.color = Color.yellow;
                }
                else
                {
                    timerFillImage.color = Color.green;
                    timerText.color = Color.white;
                }
            }
            if (gameTime <= 0)
            {
                if (countdownSoundtrack != null && countdownSoundtrack.isPlaying)
                {
                    countdownSoundtrack.Stop();
                    Debug.Log("Countdown soundtrack stopped - timer reached 0");
                }
                bettingOpen = false;
                StartCoroutine(AutoPlaceBets());
            }
        }
    }
    IEnumerator AutoPlaceBets()
    {
        previousBets = new Dictionary<int, int>(bets);
        yield return StartCoroutine(SendBetsToApi());
        yield return StartCoroutine(FetchResultAndShow());
    }
    IEnumerator SendBetsToApi()
    {
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No AUTH_KEY, cannot send bet.");
            yield break;
        }

        string url = $"{baseUrl}/v1/ticket/add-spin2win-ticket?token={token}";

        Spin2WinBetRequest reqObj = new Spin2WinBetRequest();
        reqObj.game_result_id = currentGameResultId;
        reqObj.game_id = gameId;
        reqObj.play_point = 0;

        // reset all fields
        reqObj.zero = reqObj.one = reqObj.two = reqObj.three = reqObj.four =
        reqObj.five = reqObj.six = reqObj.seven = reqObj.eight = reqObj.nine = 0;

        // fill with bets
        foreach (var kvp in GetBets())
        {
            reqObj.play_point += kvp.Value; // running total
            switch (kvp.Key)
            {
                case 0: reqObj.zero = kvp.Value; break;
                case 1: reqObj.one = kvp.Value; break;
                case 2: reqObj.two = kvp.Value; break;
                case 3: reqObj.three = kvp.Value; break;
                case 4: reqObj.four = kvp.Value; break;
                case 5: reqObj.five = kvp.Value; break;
                case 6: reqObj.six = kvp.Value; break;
                case 7: reqObj.seven = kvp.Value; break;
                case 8: reqObj.eight = kvp.Value; break;
                case 9: reqObj.nine = kvp.Value; break;
            }
        }

        string json = JsonUtility.ToJson(reqObj);
        Debug.Log("Bet payload: " + json);

        UnityWebRequest uw = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        uw.uploadHandler = new UploadHandlerRaw(bodyRaw);
        uw.downloadHandler = new DownloadHandlerBuffer();
        uw.SetRequestHeader("Content-Type", "application/json; charset=utf-8");

        yield return uw.SendWebRequest();

        if (uw.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Bet API failed: " + uw.error + " body: " + uw.downloadHandler.text);
        }
        else
        {
            Debug.Log("Bet API success: " + uw.downloadHandler.text);
            ParseTotalBetFromResponse(uw.downloadHandler.text);
        }
    }
    // New method to parse total from API response
    private void ParseTotalBetFromResponse(string apiResponse)
    {
        try
        {
            var response = JsonUtility.FromJson<TicketResponseWrapper>(apiResponse);
            if (response != null && response.status && response.data != null)
            {
                int serverTotalBet = response.data.play_point;
                Debug.Log($"Server confirms total bet: {serverTotalBet}");

                // Update your UI with the server value
                // if (totalBetText != null)
                //   totalBetText.text = " " + serverTotalBet;

                // Optional: Sync your local bets dictionary with server data
                // SyncLocalBetsWithServer(response.data);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing ticket response: " + e.Message);
        }
    }
    private IEnumerator FetchWalletBalance()
    {
        /*string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No AUTH_KEY found. Cannot fetch wallet balance.");
            yield break;
        }

        string url = $"{baseUrl}/v1/users/wallet-balance?token={token}";
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fetch wallet balance failed: " + req.error);
            // You can keep the hardcoded value as fallback
            currentWalletBalance = 1000;
        }
        else
        {
            try
            {
                string jsonResponse = req.downloadHandler.text;
                Debug.Log("Wallet balance response: " + jsonResponse);

                var walletData = JsonUtility.FromJson<WalletBalanceWrapper>(jsonResponse);
                if (walletData != null && walletData.status && walletData.data != null)
                {
                    currentWalletBalance = walletData.data.wallet_balance;
                    Debug.Log($"Wallet balance updated: ${currentWalletBalance}");
                }
                else
                {
                    Debug.LogError("Failed to parse wallet balance data");
                    currentWalletBalance = 1000; // Fallback
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing wallet balance: " + e.Message);
                currentWalletBalance = 1000; // Fallback
            }
        }*/
        currentWalletBalance = 1000;


        UpdateWalletDisplay();
        yield return null;
    }
    private void UpdateWalletDisplay()
    {
        if (walletBalanceText != null)
            walletBalanceText.text = $" {currentWalletBalance}";
    }

    private void CalculateAndDisplayWinnings(string resultString)
    {
        int parsedResultNumber = ParseResultToNumber(resultString);
        winningNum = parsedResultNumber;
        totalWinnings = 0;

        if (parsedResultNumber == -1)
        {
            Debug.LogError($"Could not parse result: {resultString}");
            if (statusText != null)
                statusText.text = $"Error: Invalid result '{resultString}'";
            return;
        }

        // Check if player bet on the winning number
        if (bets.ContainsKey(parsedResultNumber) && bets[parsedResultNumber] > 0)
        {
            // Standard 9:1 payout for single number
            totalWinnings = bets[parsedResultNumber] * 9;

            // Update wallet balance locally
            currentWalletBalance += totalWinnings;
            UpdateWalletDisplay();
        }
        else
        {
            Debug.Log($"No winning bet. Result was {parsedResultNumber}");
        }

        DisplayWinningResults(parsedResultNumber, totalWinnings, resultString);
    }
    private void InitializeHistory()
    {
        // if (refreshHistoryButton != null)
        //   refreshHistoryButton.onClick.AddListener(() => StartCoroutine(FetchGameHistory()));

        // Auto-fetch history on game start
        StartCoroutine(FetchGameHistory());
    }

    private IEnumerator FetchGameHistory()
    {
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No AUTH_KEY found for history");
            yield break;
        }

        // Clear existing history cards
        foreach (Transform child in historyGrid)
        {
            Destroy(child.gameObject);
        }

        string url = $"{baseUrl}/v1/result/latest-game-result-history?token={token}&game_id={gameId}";

        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var historyResponse = JsonUtility.FromJson<GameHistoryResponse>(req.downloadHandler.text);
                if (historyResponse != null && historyResponse.status && historyResponse.data != null)
                {
                    PopulateHistoryCards(historyResponse.data);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error parsing history: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("History fetch failed: " + req.error);
        }
    }

    private void PopulateHistoryCards(List<GameHistoryData> historyData)
    {
        // Show all 10 history items (or available data)
        int count = Mathf.Min(historyData.Count, 10);

        for (int i = 0; i < count; i++)
        {
            var historyItem = historyData[i];
            int resultNumber = ParseResultToNumber(historyItem.result);

            if (resultNumber >= 0 && resultNumber <= 9)
            {
                // Instantiate the appropriate colored card
                if (numberCardSprites.Count > resultNumber && numberCardSprites[resultNumber] != null)
                {
                    GameObject card = Instantiate(historyCardPrefab, historyGrid);

                    // Add Image component
                    Image cardImage = card.GetComponent<Image>();
                    if (cardImage != null && numberCardSprites.Count > resultNumber)
                    {
                        cardImage.sprite = numberCardSprites[resultNumber];
                    }

                    // Optional: Add tooltip with round info
                    // HistoryCardTooltip tooltip = card.GetComponent<HistoryCardTooltip>();
                    // if (tooltip == null)
                    //    tooltip = card.AddComponent<HistoryCardTooltip>();

                    // tooltip.SetTooltipData(historyItem);
                }
            }
        }

        // If you want them in chronological order (newest first), you might need to reverse
    }
    private int ParseResultToNumber(string result)
    {
        if (string.IsNullOrEmpty(result))
            return -1;

        // First try to parse as direct number
        if (int.TryParse(result, out int number))
        {
            return number;
        }

        // If that fails, try to parse text numbers
        result = result.Trim().ToLower();

        switch (result)
        {
            case "zero": return 0;
            case "one": return 1;
            case "two": return 2;
            case "three": return 3;
            case "four": return 4;
            case "five": return 5;
            case "six": return 6;
            case "seven": return 7;
            case "eight": return 8;
            case "nine": return 9;
            default: return -1;
        }
    }

    // method to display winning results
    private void DisplayWinningResults(int resultNumber, int winnings, string originalResult)
    {
        if (winningNumberText != null)
        {
            if (resultNumber != -1)
                winningNumberText.text = $" {resultNumber} ";
            else
                winningNumberText.text = $" ";
        }

        if (winningAmountText != null)
        {
            if (winnings > 0)
            {
                winningAmountText.text = $"{winnings}";
                winningAmountText.color = Color.white; 
              
            }
            else
            {
                winningAmountText.text = " ";
                winningAmountText.color = Color.white; 
            }
        }

        // Update status text
        if (statusText != null)
        {
            if (winnings > 0)
                statusText.text = $"Congratulations! You won {winnings}";
            else if (resultNumber != -1)
                statusText.text = $" Better luck next time!";
            else
                statusText.text = $" Better luck next time!";
        }

        // Update wallet on server (if player won)
        if (winnings > 0)
        {
            StartCoroutine(UpdateServerWalletBalance());
        }
    }
    private IEnumerator UpdateServerWalletBalance()
    {
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No AUTH_KEY found. Cannot update server wallet.");
            yield break;
        }

        // Fetch updated balance to sync with server
        yield return StartCoroutine(FetchWalletBalance());

        Debug.Log($"Wallet sync complete. Current balance: ${currentWalletBalance}");
    }

    IEnumerator FetchResultAndShow()
    {
        if (countdownSoundtrack != null && countdownSoundtrack.isPlaying)
        {
            countdownSoundtrack.Stop();
            Debug.Log("Countdown soundtrack stopped - fetching results");
        }
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        string url = $"{baseUrl}/v1/result/fetch-game-result-data?token={token}&game_result_id={currentGameResultId}";

        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fetch result failed: " + req.error);
            if (statusText != null)
                statusText.text = "Error fetching results";
        }
        else
        {
            string raw = req.downloadHandler.text;
            Debug.Log("Fetch result raw: " + raw);

            //DebugAPIResponseStructure(raw);

            try
            {
                var wrap = JsonUtility.FromJson<GameResultDataWrapper>(raw);
                if (wrap != null && wrap.data != null)
                {
                    string result = wrap.data.result;
                    if (wrap != null && wrap.data != null)
                    {
                        if (!string.IsNullOrEmpty(result))
                        {
                            if (statusText != null)
                                statusText.text = "Spinning...";

                            int resultNumber = ParseResultToNumber(result);
                            if (resultNumber != -1)
                            {
                                // Calculate winnings first
                                CalculateWinnings(resultNumber);

                                // Start wheel spin if available
                                if (wheelSpinner != null)
                                {
                                    wheelSpinner.onSpinComplete.AddListener(OnWheelComplete);
                                    wheelSpinner.SpinToNumber(resultNumber);
                                    yield break; // Important: exit here and wait for wheel callback
                                }
                                else
                                {
                                    // No wheel - show results immediately
                                    ShowResults(resultNumber);
                                }
                            }
                        }

                    }
                    else
                    {
                        Debug.LogError("Empty result received");
                        if (statusText != null)
                            statusText.text = "No result data";
                    }
                }
                else
                {
                    Debug.LogError("Invalid result wrapper or data");
                    if (statusText != null)
                        statusText.text = "Invalid result format";
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to parse result JSON: " + e.Message);
                if (statusText != null)
                    statusText.text = "Error parsing result";
            }
        }
        yield return new WaitForSeconds(5f);

        spinning = false;
        roundComplete = false;
        StartCoroutine(SetupRound());
        // if (endRoundPanel != null) endRoundPanel.SetActive(true);
    }
    private void CalculateWinnings(int resultNumber)
    {
        totalWinnings = 0;

        if (bets.ContainsKey(resultNumber) && bets[resultNumber] > 0)
        {
            totalWinnings = bets[resultNumber] * 9;
            currentWalletBalance += totalWinnings;
            UpdateWalletDisplay();


        }
    }

    public void DoubleBets()
    {
        PlayButtonClickSound();
        int currentTotal = GetTotalBetAmount();
        int doubledTotal = currentTotal * 2;

        // Check if player has sufficient balance
        int additionalAmount = doubledTotal - currentTotal;
        if (additionalAmount > currentWalletBalance)
        {
            if (statusText != null)
                statusText.text = "Insufficient balance to double!";
            return;
        }
        currentWalletBalance -= additionalAmount;
        // Double each bet
        foreach (var number in new List<int>(bets.Keys))
        {
            if (bets[number] > 0)
            {
                bets[number] *= 2;
            }
        }

        UpdateTotalUI();
        UpdateWalletDisplay();
        RefreshChipVisuals();

        if (statusText != null)
            statusText.text = "Bets doubled!";
    }
    public void ClearAllBets()
    {
        PlayButtonClickSound();
        if (GetTotalBetAmount() > 0)
        {
            previousBets = new Dictionary<int, int>(bets);
        }
        int totalReturned = GetTotalBetAmount();
        currentWalletBalance += totalReturned;

        bets.Clear();
        UpdateTotalUI();
        UpdateWalletDisplay();
        // Clear visual chips
        foreach (var card in cards)
        {
            if (card != null && card.chipContainer != null)
            {
                foreach (Transform child in card.chipContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        if (statusText != null)
            statusText.text = "All bets cleared!";
    }
    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int sec = Mathf.FloorToInt(gameTime % 60);
            int min = Mathf.FloorToInt(gameTime / 60);
            timerText.text = $"{min:00}:{sec:00}";
        }

        if (timerFillImage != null && maxGameTime > 0)
        {
            // USE maxGameTime FOR CONSISTENT FILL CALCULATION
            float fillAmount = gameTime / maxGameTime;
            timerFillImage.fillAmount = fillAmount;

            Debug.Log($"UpdateTimerUI - Time: {gameTime}, Max: {maxGameTime}, Fill: {fillAmount}");
        }
    }
    private void ResetTimer()
    {
        gameTime = 0f;
        maxGameTime = 0f;

        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = 0f;
        }

        if (timerText != null)
        {
            timerText.text = "00:00";
            timerText.color = Color.white;
        }
    }
    private void RefreshChipVisuals()
    {
        foreach (var card in cards)
        {
            if (card != null && card.chipContainer != null)
            {
                foreach (Transform child in card.chipContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        foreach (var kvp in bets)
        {
            if (kvp.Value > 0)
            {
                var card = cards.Find(c => c.cardNumber == kvp.Key);
                if (card != null && card.chipContainer != null)
                {
                    GameObject prefab = GetChipPrefab(GetChipValueForAmount(kvp.Value));
                    if (prefab != null)
                    {
                        Instantiate(prefab, card.chipContainer);
                    }
                }
            }
        }
    }

    private int GetChipValueForAmount(int amount)
    {
        int[] chipValues = { 500, 100, 50, 10, 5 };
        foreach (int chip in chipValues)
        {
            if (amount >= chip)
                return chip;
        }
        return 5;
    }
    private void OnWheelComplete(int winningNumber)
    {
        wheelSpinner.onSpinComplete.RemoveListener(OnWheelComplete);
        ShowResults(winningNumber);
        StartCoroutine(RestartAfterResults());
    }
    private IEnumerator RestartAfterResults()
    {
        yield return new WaitForSeconds(5f); // Show results for 5 seconds
        StartCoroutine(FetchGameHistory());
        // Start new round
        spinning = false;
        roundComplete = false;
        StartCoroutine(SetupRound());
    }

    private void ShowResults(int resultNumber)
    {
        if (winningNumberText != null)
            winningNumberText.text = $" {resultNumber}";

        if (winningAmountText != null)
        {
            winningAmountText.text = totalWinnings > 0 ? $" {totalWinnings}" : "No Win";
            winningAmountText.color = totalWinnings > 0 ? Color.white : Color.white;
            //if (nowinAudio != null)
            //{
            //  nowinAudio.Play(); //play no win sound
            //}
        }
        if (totalWinnings > 0)
        {
            currentWalletBalance += totalWinnings;
            UpdateWalletDisplay();

            RewardAnimation.Rewardcoin(10);


            if (statusText != null)
                statusText.text = totalWinnings > 0 ? $"{totalWinnings}!" : "Better luck next time!";
            if (winAudio != null) //ply win sound
            {
                winAudio.Play();
            }
            if (glowEffect != null)
            {
                glowEffect.SetActive(totalWinnings > 0);
            }
        }
        else
        {
            if (nowinAudio != null)
            { nowinAudio.Play(); }
        }
    }
   
    private void ShowEndPanel()
    {
        StartCoroutine(RestartRoundAfterDelay());
    }
    private IEnumerator RestartRoundAfterDelay()
    {
        yield return new WaitForSeconds(5f); // Wait 5 seconds

        // Reset states and start new round
        spinning = false;
        roundComplete = false;
        StartCoroutine(SetupRound());
    }
    /*  private IEnumerator ShowPanelAfterDelay()
      {
          yield return new WaitForSeconds(5f); 

          if (endRoundPanel != null)
              endRoundPanel.SetActive(true);

          if (glowEffect != null)
              glowEffect.SetActive(false);
      }
      */
    [System.Serializable]
    public class SimpleResponseWrapper
    {
        public bool status;
        public string message;
        public object data; 
    }

    [System.Serializable]
    public class LatestGameIdWrapper { public bool status; public LatestGameIdData data; }
    [System.Serializable]
    public class LatestGameIdData { public int id; }

    [System.Serializable]
    public class GameTimeWrapper { public bool status; public GameTimeData data; }
    [System.Serializable]
    public class GameTimeData
    {
        public int game_time;
        public int remaining_time;
    }

    [System.Serializable]
    public class GameResultDataWrapper
    {
        public bool status;
        public string message;
        public GameResultInfoData data;
    }

    [System.Serializable]
    public class GameResultInfoData
    {
        public string result;
        public int id;
        public int game_id;
        public string game_time;
        public string status;
    }

    [System.Serializable]
    public class WalletBalanceWrapper
    {
        public bool status;
        public string message;
        public WalletBalanceData data;
    }

    [System.Serializable]
    public class WalletBalanceData
    {
        public int wallet_balance;
    }
    [System.Serializable]
    public class TicketResponseWrapper
    {
        public bool status;
        public string message;
        public TicketResponseData data;
    }

    [System.Serializable]
    public class TicketResponseData
    {
        public int play_point;
        public int id;
        public int game_id;
        public int game_result_id;
        public int user_id;
        public GameSlotObj game_slot_obj;
        
    }
    [System.Serializable]
    public class GameResultIdWrapper
    {
        public bool status;
        public string message;
        public GameResultIdData data;
    }
    [System.Serializable]
    public class GameResultIdData
    {
        public int id;
        public int remaining_time;
    }

    public class GameSlotObj
    {
        public int zero;
        public int one;
        public int two;
        public int three;
        public int four;
        public int five;
        public int six;
        public int seven;
        public int eight;
        public int nine;
    }
}
