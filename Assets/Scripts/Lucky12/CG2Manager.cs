using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
//using static UnityEngine.Rendering.DebugUI;

public class CG2Manager : MonoBehaviour
{
    [Header("Circular Timer")]
    public Image circularTimerFill;

    public TMP_Text usernameText;
public TMP_Text dateTimeText;

    public static CG2Manager Instance;
    public AudioSource buttonClickAudio;
    [Header("API")]
    public string baseUrl = "https://casino-backend.realtimevillage.com/api";
    public int gameId = 2;
    public float fallbackGameTime = 30f;
    public WinCardDisplay WinCardDisplay;

    [Header("UI - Texts & Buttons")]
    public TMP_Text timerText;
    public TMP_Text statusText;
    public TMP_Text totalBetText;
    public TMP_Text walletBalanceText;
    public TMP_Text walletBalanceText2;
    public TMP_Text winPointsText;
    public Button placeBetButton;
    public AudioSource winAudio;
    public AudioSource noWinAudio;
    public AudioSource countdownSoundtrack;
    [Header("Chips & Interaction")]
    public List<Button> chipButtons = new List<Button>();

    [Header("Wheel")]
    public RectTransform wheelOuter;
    public RectTransform wheelInner;

    [Header("Win Effects")]
    public SimpleGlowAnimation SimpleGlowAnimation;

    [Header("UI Panels")]
    public GameObject infoPanel;
    public GameObject settingPanel;
    public GameObject resultPanel;
    public GameObject Gamehistory;
    public GameObject rulesPanel;
    public GameObject reportPanel;
    public GameObject pointLogPanel;

    private GameObject[] allPanels;


    [Header("Timer thresholds (seconds)")]
    public float warningThreshold = 15f;
    public float lockThreshold = 10f;

    public reward reward;
    public Lucky12HistoryDisplay historyDisplay;

    [HideInInspector] public List<CardBettingSpots> cardSpots = new List<CardBettingSpots>();
    private float game_time = 0f;
    private bool bettingOpen = false;
    private bool spinning = false;
    private bool roundComplete = false;
    private int currentGameResultId = 0;
    private bool roundInitialized = false;
    private int currentWalletBalance = 0;
    private int currentWinPoints = 0;
    private int lastRoundWinning = 0;
    private bool countdownSoundPlayed = false;
    private float maxGameTime = 60f;
    private bool betPlacedThisRound = false;
    private int currentTotalBetThisRound = 0;
    // Wheel mapping 
    private readonly string[] cardCodes = new string[]
    {
        "JS","QS","KS","JD","QD","KD","JC","QC","KC","JH","QH","KH"
    };
    #region Audio Helpers
    public void PlayButtonClickSound()
    {
        if (buttonClickAudio != null)
        {
            buttonClickAudio.Play();
        }
    }
    #endregion
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        allPanels = new GameObject[] { Gamehistory, resultPanel, rulesPanel, reportPanel, pointLogPanel };

        game_time = 0f;
        roundInitialized = false;
        countdownSoundPlayed = false;

        betPlacedThisRound = false;
        currentTotalBetThisRound = 0;

        username();
        UpdateDateTime();
        HideAllPanels();
        StartCoroutine(InitializeWalletAndRound());
    }
    void UpdateDateTime()
    {
        if (dateTimeText != null)
        {
            dateTimeText.text = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        }
    }
    void username()
    {
        
        string username = PlayerPrefs.GetString("Username", "Guest"); 
      

        usernameText.text = " " + username.ToUpper();
    }
    void Update()
    {
        if (!roundInitialized || spinning || roundComplete) return;

        UpdateDateTime();

        if (game_time > 0f)
        {
            game_time -= Time.deltaTime;
            if (game_time < 0f) game_time = 0f;
            UpdateTimerUI();

            // PLAY COUNTDOWN SOUNDTRACK WHEN TIMER IS LAST 10 SECONDS
            if (game_time <= 10f && !countdownSoundPlayed)
            {
                if (countdownSoundtrack != null)
                {
                    countdownSoundtrack.Play();
                    Debug.Log("Countdown soundtrack started");
                }
                countdownSoundPlayed = true;
            }
            /* if (bettingOpen && game_time <= lockThreshold)
             {
                 bettingOpen = false;
                 SetAllInteractive(false);
                 if (statusText != null) statusText.text = "Betting closed";
             }*/
            if (bettingOpen && game_time <= lockThreshold)
            {
                bettingOpen = false;
                SetAllInteractive(false);
                if (statusText != null) statusText.text = "Betting closed";
                if (betPlacedThisRound && !spinning && !roundComplete)
                {
                    // Any final processing if needed
                    // This ensures all multiple bets are accounted for
                    Debug.Log("Final betting lock - bets placed this round: " + currentTotalBetThisRound);

                }
                }
        }

        if (game_time <= 0f && !spinning && !roundComplete)
        {
            if (countdownSoundtrack != null && countdownSoundtrack.isPlaying)
            {
                countdownSoundtrack.Stop();
                Debug.Log("Countdown soundtrack stopped - timer reached 0");
            }
            roundComplete = true;
            if (statusText != null) statusText.text = "Time up! Resolving...";
            StartCoroutine(FetchResultAndSpin());
        }
    }

    #region Wallet and Initialization
    IEnumerator InitializeWalletAndRound()
    {
        yield return StartCoroutine(FetchWalletBalanceCoroutine());
        yield return StartCoroutine(InitializeRound());
    }

    IEnumerator FetchWalletBalanceCoroutine()
    {
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token)) yield break;

        string url = $"{baseUrl}/v1/users/wallet-balance?token={token}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    WalletWrapper w = JsonUtility.FromJson<WalletWrapper>(req.downloadHandler.text);
                    if (w != null && w.data != null)
                    {
                        currentWalletBalance = w.data.wallet_balance;
                        UpdateWalletUI();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to parse wallet response: " + e.Message);
                }
            }
        }
    }

    void UpdateWalletUI()
    {
        if (walletBalanceText != null) walletBalanceText.text = $"Balance: {currentWalletBalance}";
        if (walletBalanceText2 != null) walletBalanceText2.text = $"{currentWalletBalance}";
        if (winPointsText != null) winPointsText.text = $"Win: {currentWinPoints}";
    }
    #endregion

    #region Round Management
    IEnumerator InitializeRound()
    {
        roundComplete = false;
        roundInitialized = false;
        spinning = false;
        countdownSoundPlayed = false;

        betPlacedThisRound = false;
        currentTotalBetThisRound = 0;


        if (WheelSpinManager.Instance != null)
            WheelSpinManager.Instance.ResetWheelPosition();

        if (historyDisplay != null)
            historyDisplay.RefreshHistory();
        // Stop any existing glow
        if (SimpleGlowAnimation != null)
            SimpleGlowAnimation.StopGlow();
        if (countdownSoundtrack != null && countdownSoundtrack.isPlaying)
        {
            countdownSoundtrack.Stop();
        }

        // Reset winning and clear bets
        currentWinPoints = 0;
        foreach (var card in cardSpots)
        {
            if (card != null) card.ClearBet();
        }

        SetAllInteractive(false);
        if (statusText != null) statusText.text = "Initializing round...";

        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No AUTH_KEY found. Login first.");
            yield break;
        }

        // Get latest game result ID
        string urlId = $"{baseUrl}/v1/result/latest-game-result-id?token={token}&game_id={gameId}";
        UnityWebRequest reqId = UnityWebRequest.Get(urlId);
        yield return reqId.SendWebRequest();

        if (reqId.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var idWrap = JsonUtility.FromJson<GameResultIdWrapper>(reqId.downloadHandler.text);
                if (idWrap != null && idWrap.data != null)
                {
                    currentGameResultId = idWrap.data.id;
                    // Use remaining_time from this response if available
                    if (idWrap.data.remaining_time > 0)
                    {
                        game_time = idWrap.data.remaining_time;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Exception parsing game ID: " + e.Message);
            }
        }

        PlayerPrefs.SetInt("CurrentGameResultId", currentGameResultId);

        // If we didn't get time from the first call, try the specific endpoint
        if (game_time <= 0)
        {
            string urlTime = $"{baseUrl}/v1/result/latest-game-result-id?token={token}&game_result_id={currentGameResultId}";
            UnityWebRequest reqTime = UnityWebRequest.Get(urlTime);
            yield return reqTime.SendWebRequest();

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
                            game_time = tw.data.remaining_time;
                        }
                        else if (tw.data.game_time > 0)
                        {
                            game_time = tw.data.game_time;
                        }
                        else
                        {
                            game_time = fallbackGameTime;
                        }
                    }
                    else
                    {
                        game_time = fallbackGameTime;
                    }
                }
                catch (Exception)
                {
                    game_time = fallbackGameTime;
                }
            }
            else
            {
                game_time = fallbackGameTime;
            }
        }
        maxGameTime = game_time;
        if (circularTimerFill != null)
        {
            circularTimerFill.fillAmount = 1f;
        }

        bettingOpen = (game_time > lockThreshold);
        SetAllInteractive(bettingOpen);
        UpdateBetButtonState();
        

        if (statusText != null)
            statusText.text = bettingOpen ? "Betting open" : "Betting closed";

        UpdateTimerUI();
        UpdateWalletUI();
        roundInitialized = true;
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(game_time / 60f);
        int seconds = Mathf.FloorToInt(game_time % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (circularTimerFill != null)
        {
            float fillAmount = game_time / maxGameTime;
            circularTimerFill.fillAmount = fillAmount;
            Debug.Log($"GameTime: {game_time}, MaxTime: {maxGameTime}, Fill: {fillAmount}");
        }

        if (game_time <= lockThreshold)
            timerText.color = Color.red;
        else if (game_time <= warningThreshold)
            timerText.color = Color.yellow;
        else
            timerText.color = Color.white;
    }
    #endregion

    #region Betting and Game Logic
    public void OnBetClicked()
    {
        PlayButtonClickSound();
        if (!bettingOpen || spinning) return;

        int newBetAmount = CalculateTotal();
        if (newBetAmount <= 0)
        {
            if (statusText != null) statusText.text = "Place a bet first.";
            return;
        }

        int totalAfterThisBet = currentTotalBetThisRound + newBetAmount;
        if (totalAfterThisBet > currentWalletBalance)
        {
            if (statusText != null) statusText.text = "Insufficient balance!";
            return;
        }

        // Deduct from wallet
        currentWalletBalance -= newBetAmount;
        currentTotalBetThisRound = totalAfterThisBet;
        UpdateWalletUI();

        // Don't disable betting - allow multiple bets
        if (statusText != null) statusText.text = $"Bet placed! Total: {currentTotalBetThisRound}";

        StartCoroutine(SendBetsToApi(success =>
        {
            if (!success)
            {
                // Refund on failure
                currentWalletBalance += newBetAmount;
                currentTotalBetThisRound -= newBetAmount;
                UpdateWalletUI();
                if (statusText != null) statusText.text = "Bet failed. Amount refunded.";
            }
            else
            {
                betPlacedThisRound = true;
                // Clear current bets but keep betting open
                foreach (var c in cardSpots) c.ClearBet();
                UpdateTotalUI();
            }
        }));
    }

    IEnumerator PlaceBetsThenResolve()
    {
        bool ok = false;
        yield return StartCoroutine(SendBetsToApi(success => ok = success));

        if (!ok)
        {
            int totalBet = CalculateTotal();
            currentWalletBalance += totalBet;
            UpdateWalletUI();

            if (statusText != null) statusText.text = "Bet failed. Amount refunded.";
            bettingOpen = true;
            SetAllInteractive(true);
            yield break;
        }

        if (game_time > 0)
        {
            if (statusText != null) statusText.text = "Bet placed. Waiting for round end...";
        }
        else
        {
            yield return StartCoroutine(FetchResultAndSpin());
        }
    }

    IEnumerator SendBetsToApi(System.Action<bool> onComplete)
    {
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token))
        {
            onComplete?.Invoke(false);
            yield break;
        }

        string url = $"{baseUrl}/v1/ticket/add-lucky12-ticket?token={token}";

        Lucky12BetRequest reqObj = new Lucky12BetRequest();
        reqObj.game_result_id = currentGameResultId;
        reqObj.game_id = gameId;
        reqObj.play_point = CalculateTotal();

        // Initialize all bets to 0
        reqObj.JS = reqObj.QS = reqObj.KS = reqObj.JD = reqObj.QD = reqObj.KD =
        reqObj.JC = reqObj.QC = reqObj.KC = reqObj.JH = reqObj.QH = reqObj.KH = 0;
       
        // Assign actual bets
        foreach (var c in cardSpots)
        {
            if (c == null) continue;
            int bet = c.GetTotalBet();
            if (bet > 0 && !string.IsNullOrEmpty(c.CardCode))
            {
                switch (c.CardCode)
                {
                    case "JS": reqObj.JS = bet; break;
                    case "QS": reqObj.QS = bet; break;
                    case "KS": reqObj.KS = bet; break;
                    case "JD": reqObj.JD = bet; break;
                    case "QD": reqObj.QD = bet; break;
                    case "KD": reqObj.KD = bet; break;
                    case "JC": reqObj.JC = bet; break;
                    case "QC": reqObj.QC = bet; break;
                    case "KC": reqObj.KC = bet; break;
                    case "JH": reqObj.JH = bet; break;
                    case "QH": reqObj.QH = bet; break;
                    case "KH": reqObj.KH = bet; break;
                   
                }
            }
        }

        string json = JsonUtility.ToJson(reqObj);
        UnityWebRequest uw = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        uw.uploadHandler = new UploadHandlerRaw(bodyRaw);
        uw.downloadHandler = new DownloadHandlerBuffer();
        uw.SetRequestHeader("Content-Type", "application/json");

        yield return uw.SendWebRequest();

        if (uw.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Bet API failed: " + uw.error);
            onComplete?.Invoke(false);
        }
        else
        {
            Debug.Log("Bet API success");
            onComplete?.Invoke(true);
        }
    }
    #endregion
    private IEnumerator FetchActualBetsFromAPI(string resultCode, System.Action<int> onComplete)
    {
        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        if (string.IsNullOrEmpty(token))
        {
            onComplete?.Invoke(0);
            yield break;
        }

        string url = $"{baseUrl}/v1/result/game-bit-data?token={token}&game_result_id={currentGameResultId}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var betDataWrapper = JsonUtility.FromJson<GameBitDataWrapper>(req.downloadHandler.text);
                    if (betDataWrapper != null && betDataWrapper.status && betDataWrapper.data != null)
                    {
                        int totalWinning = CalculateWinningFromAPIBets(betDataWrapper.data, resultCode);
                        onComplete?.Invoke(totalWinning);
                    }
                    else
                    {
                        onComplete?.Invoke(0);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to parse bet data: " + e.Message);
                    onComplete?.Invoke(0);
                }
            }
            else
            {
                Debug.LogError("Failed to fetch bet data: " + req.error);
                onComplete?.Invoke(0);
            }
        }
    }
    private int CalculateWinningFromAPIBets(GameBitData betData, string resultCode)
    {
        int totalWinning = 0;
        string currentUserToken = PlayerPrefs.GetString("AUTH_KEY", "");

        // You might need to get current user ID from token or store it during login
        // For now, we'll assume all tickets in the response are for the current user

        if (betData.game_tickets != null)
        {
            foreach (var ticket in betData.game_tickets)
            {
                // Check if this ticket has a bet on the winning card
                int betOnWinningCard = GetBetAmountForCard(ticket.game_slot_obj, resultCode);
                if (betOnWinningCard > 0)
                {
                    totalWinning += betOnWinningCard * 2; // 1:1 payout
                    Debug.Log($"Found winning bet in API: {betOnWinningCard} on {resultCode}");
                }
            }
        }

        return totalWinning;
    }

    private int GetBetAmountForCard(GameSlotObj slotObj, string cardCode)
    {
        if (slotObj == null) return 0;

        switch (cardCode)
        {
            case "JS": return slotObj.JS;
            case "QS": return slotObj.QS;
            case "KS": return slotObj.KS;
            case "JD": return slotObj.JD;
            case "QD": return slotObj.QD;
            case "KD": return slotObj.KD;
            case "JC": return slotObj.JC;
            case "QC": return slotObj.QC;
            case "KC": return slotObj.KC;
            case "JH": return slotObj.JH;
            case "QH": return slotObj.QH;
            case "KH": return slotObj.KH;
            default: return 0;
        }
    }
    #region Result Processing
    public IEnumerator FetchResultAndSpin()
    {
        if (spinning) yield break;

        spinning = true;
        roundComplete = true;
        if (countdownSoundtrack != null && countdownSoundtrack.isPlaying)
        {
            countdownSoundtrack.Stop();
        }
        if (statusText != null) statusText.text = "Fetching result...";

        string token = PlayerPrefs.GetString("AUTH_KEY", "");
        string url = $"{baseUrl}/v1/result/fetch-game-result-data?token={token}&game_result_id={currentGameResultId}";
        UnityWebRequest req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        string resultCode = null;

        if (req.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var wrap = JsonUtility.FromJson<GameResultWrapper>(req.downloadHandler.text);
                if (wrap != null && wrap.data != null && !string.IsNullOrEmpty(wrap.data.result))
                {
                    resultCode = wrap.data.result.Trim();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Exception parsing result: " + e.Message);
            }
        }

        if (string.IsNullOrEmpty(resultCode))
        {
            resultCode = cardCodes[UnityEngine.Random.Range(0, cardCodes.Length)];
        }

        // Calculate winning
        //lastRoundWinning = CalculateWinning(resultCode);
        int localWinning = CalculateWinning(resultCode);
        if (localWinning > 0)
        {
            lastRoundWinning = localWinning;
            Debug.Log($"Local winning calculation: {lastRoundWinning}");
        }
        else if (betPlacedThisRound)
        {
            // If no local bets but bets were placed this round, check API
            Debug.Log("No local bets found, checking API for placed bets...");
            yield return StartCoroutine(FetchActualBetsFromAPI(resultCode, (apiWinning) =>
            {
                lastRoundWinning = apiWinning;
                Debug.Log($"API winning calculation: {lastRoundWinning}");
            }));

            // Wait a frame for the coroutine to complete
            yield return null;
        }
        else
        {
            lastRoundWinning = 0;
        }

        Debug.Log($"Final Result: {resultCode}, Winning: {lastRoundWinning}");

        if (statusText != null) statusText.text = "Spinning...";

        // Spin wheel
        if (WheelSpinManager.Instance != null)
        {
            bool spinFinished = false;
            WheelSpinManager.Instance.SpinWithDelayedInnerStop(resultCode, () => spinFinished = true); yield return new WaitUntil(() => spinFinished);

        }
        else
        {
            yield return new WaitForSeconds(4f);
        }
        if (WinCardDisplay != null)
        {
            WinCardDisplay.ShowWinningCard(resultCode);
        }
        // Show result and handle winning
        if (lastRoundWinning > 0)
        {

            if (statusText != null) statusText.text = $" You won {lastRoundWinning}!";
            if (reward != null)
            {
                reward.Rewardcoin(lastRoundWinning);
                yield return new WaitForSeconds(2f);
            }


            if (winAudio != null) //ply win sound
            {
                winAudio.Play();
            }

            // Start glow effect
            if (SimpleGlowAnimation != null)
                SimpleGlowAnimation.StartGlow();

            AddWinningToWallet(lastRoundWinning);

            // Stop glow after 10 seconds
            StartCoroutine(StopGlowAfterDelay(10f));
        }
        else
        {
            if (statusText != null) statusText.text = $"Better luck next time!";

            if (noWinAudio != null)
            {
                noWinAudio.Play();
            }
        }

        // Refresh wallet
        yield return StartCoroutine(FetchWalletBalanceCoroutine());
        yield return new WaitForSeconds(5f);

        if (historyDisplay != null)
            historyDisplay.RefreshHistory();
        // Start new round
        spinning = false;
        roundComplete = false;
        StartCoroutine(InitializeRound());
    }
    public void OnclickGameHistory()
    {
        PlayButtonClickSound();
        SetPanelActive(Gamehistory);
        
    }
    public void OnResultClicked()
    {
        PlayButtonClickSound();
        SetPanelActive(resultPanel);

    }
    public void OnClickRules()
    {
        PlayButtonClickSound();
        SetPanelActive(rulesPanel);
    }

    public void OnClickReport()
    {
        PlayButtonClickSound();
        SetPanelActive(reportPanel);
    }

    public void OnClickPointLog()
    {
        PlayButtonClickSound();
        SetPanelActive(pointLogPanel);
    }
    private void SetPanelActive(GameObject panelName)
    {
        foreach (GameObject panel in allPanels)
        {
            if (panel != null)
            {
                // If this is the panel we want to show, toggle it
                // If it's any other panel, hide it
                if (panel == panelName)
                {
                    panel.SetActive(!panel.activeSelf); // Toggle the clicked panel
                }
                else
                {
                    panel.SetActive(false); // Hide all other panels
                }
            }
        }
}
public void HideAllPanels()
    {
        foreach (GameObject panel in allPanels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
    public void OninfoClicked()
     {
        PlayButtonClickSound();

        if (infoPanel != null)
        {
            infoPanel.SetActive(true); // Show info panel
            Debug.Log("Info panel opened");
        }
        else
        {
            Debug.LogWarning("Info panel reference not assigned!");
        }
    }
    public void OnCloseInfoClicked()
    {
        PlayButtonClickSound();

        if (infoPanel != null)
        {
            infoPanel.SetActive(false); // Hide info panel
            Debug.Log("Info panel closed");
        }
    }
    public void OnMinimizeClicked()
    {
        PlayButtonClickSound();
        Debug.Log("Minimizing game to run in background");

        // Stop game logic
        game_time = 0f;
        roundInitialized = false;
        StopAllCoroutines();

        if (circularTimerFill != null)
        {
            circularTimerFill.fillAmount = 0f;
        }

        // Stop countdown sound
        if (countdownSoundtrack != null && countdownSoundtrack.isPlaying)
        {
            countdownSoundtrack.Stop();
        }

        // Enable running in background
        Application.runInBackground = true;

        // Platform-specific minimization
#if UNITY_STANDALONE_WIN
    MinimizeWindows();
#elif UNITY_ANDROID
        MinimizeAndroid();
#endif
    }

#if UNITY_STANDALONE_WIN
[System.Runtime.InteropServices.DllImport("user32.dll")]
private static extern System.IntPtr GetActiveWindow();

[System.Runtime.InteropServices.DllImport("user32.dll")]
private static extern bool ShowWindow(System.IntPtr hwnd, int nCmdShow);

private void MinimizeWindows()
{
    try
    {
        System.IntPtr hwnd = GetActiveWindow();
        ShowWindow(hwnd, 6); // SW_MINIMIZE
    }
    catch (System.Exception e)
    {
        Debug.LogError("Windows minimize failed: " + e.Message);
    }
}
#endif

#if UNITY_ANDROID
    private void MinimizeAndroid()
    {
        try
        {
            using (AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
            {
                activity.Call<bool>("moveTaskToBack", true);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Android minimize failed: " + e.Message);
        }
    }
#endif
    public void OnCrossClicked()
    {
        PlayButtonClickSound();
        Debug.Log("Go to main menu");

        game_time = 0f;
        roundInitialized = false;
        StopAllCoroutines();

        if (circularTimerFill != null)
        {
            circularTimerFill.fillAmount = 0f;
        }

        // Stop countdown sound
        if (countdownSoundtrack != null && countdownSoundtrack.isPlaying)
        {
            countdownSoundtrack.Stop();
        }

        // Load main menu scene (scene 1)
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
    IEnumerator StopGlowAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (SimpleGlowAnimation != null)
            SimpleGlowAnimation.StopGlow();
    }

    private int CalculateWinning(string resultCode)
    {
        int totalWinning = 0;
        foreach (var cardSpot in cardSpots)
        {
            if (cardSpot.GetTotalBet() > 0 && cardSpot.CardCode == resultCode)
            {
                totalWinning += cardSpot.GetTotalBet() * 2; // 1:1 payout
            }
        }
        if (totalWinning == 0 && betPlacedThisRound )
        {
            Debug.Log($"Checking API bets for winning card: {resultCode}");
            // You might need to fetch the actual bets from API here
            // or keep a simple flag that at least one bet was placed
        }
        return totalWinning; 
        //return lastRoundWinning;
    }

    private void AddWinningToWallet(int winningAmount)
    {
        if (winningAmount > 0)
        {
            currentWinPoints += winningAmount;
            currentWalletBalance += winningAmount;
            UpdateWalletUI();
        }
    }
    #endregion

    #region UI Helpers
    public void RegisterCard(CardBettingSpots card)
    {
        if (!cardSpots.Contains(card)) cardSpots.Add(card);
    }

    public void OnExitClicked()
    {
        PlayButtonClickSound();
        game_time = 0f;
        roundInitialized = false;
        StopAllCoroutines();

        if (circularTimerFill != null)
        {
            circularTimerFill.fillAmount = 0f;
        }

        // Stop countdown sound
        if (countdownSoundtrack != null && countdownSoundtrack.isPlaying)
        {
            countdownSoundtrack.Stop();
        }
        // Load your main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);

    }
    public void OnClearClicked()
    {
        PlayButtonClickSound();
        foreach (var c in cardSpots) c.ClearBet();
        UpdateTotalUI();
    }

    public void OnDoubleClicked()
    {
        PlayButtonClickSound();
        foreach (var c in cardSpots) c.DoubleBet();
        UpdateTotalUI();
    }

    public void ApplyChipToAll()
    {
        if (ChipManager.Instance == null)
        {
            Debug.LogWarning("ChipManager.Instance is null - cannot apply chips to all");
            return;
        }
        PlayButtonClickSound();
        foreach (var spot in cardSpots) spot.AddChipFromManager();
        UpdateTotalUI();
    }

    public void ApplyChipToRank(char rank)
    {
        PlayButtonClickSound();
        foreach (var spot in cardSpots)
        {
            if (!string.IsNullOrEmpty(spot.CardCode) && spot.CardCode.StartsWith(rank.ToString()))
            {
                spot.AddChipFromManager();
            }
        }
        UpdateTotalUI();
    }
    public void ApplyChipToSuits(char suits)
    {
        PlayButtonClickSound();
        foreach (var spot in cardSpots)
        {
            if (!string.IsNullOrEmpty(spot.CardCode) && spot.CardCode.EndsWith(suits.ToString()))
            {
                spot.AddChipFromManager();
            }
        }
        UpdateTotalUI();
    }
    public void OnSettingClicked()
    {
        PlayButtonClickSound ();
        settingPanel.SetActive(true);
    }
    public void OnSetCrossClicked()
    {
        PlayButtonClickSound ();
        settingPanel.SetActive(false);
    }
    
    public int CalculateTotal()
    {
        int t = 0;
        foreach (var c in cardSpots) t += c.GetTotalBet();
        return t;
    }

    public void UpdateTotalUI()
    {
        if (totalBetText != null) totalBetText.text = "Total: " + CalculateTotal();
        UpdateBetButtonState();
    }

    public void UpdateBetButtonState()
    {
        if (placeBetButton != null)
        {
            placeBetButton.interactable = bettingOpen && CalculateTotal() > 0 && !spinning;
        }
    }

    void SetAllInteractive(bool on)
    {
        bool shouldBeInteractive = on && bettingOpen;
        foreach (var c in cardSpots)
        {
            var btn = c.GetComponent<Button>();
            if (btn != null) btn.interactable = on;
        }

        foreach (var b in chipButtons)
            if (b != null) b.interactable = on;

        UpdateBetButtonState();
    }

    bool HasAnyBet() => CalculateTotal() > 0;
    #endregion

    #region JSON Wrapper Classes
    [Serializable] public class WalletWrapper { public bool status; public WalletData data; }
    [Serializable] public class WalletData { public int wallet_balance; }
    [Serializable] public class GameResultWrapper { public bool status; public string message; public GameResultData data; }
    [Serializable] public class GameResultData { public string result; }
    [Serializable] public class LatestGameIdWrapper { public bool status; public string message; public LatestGameIdData data; }
    [Serializable] public class LatestGameIdData { public int id; }
    [Serializable] public class GameTimeWrapper { public bool status; public string message; public GameTimeData data; }
    [Serializable] public class GameTimeData { public int game_time; public int remaining_time; }

    [Serializable]
    public class Lucky12BetRequest
    {
        public int game_result_id;
        public int game_id;
        public int play_point;
        public int JS, QS, KS, JD, QD, KD, JC, QC, KC, JH, QH, KH;
        
    }
    [Serializable]
    public class GameResultIdWrapper
    {
        public bool status;
        public string message;
        public GameResultIdData data;
    }

    [Serializable]
    public class GameResultIdData
    {
        public int id;
        public int remaining_time; // Add this field
    }
    [Serializable]
    public class GameBitDataWrapper
    {
        public bool status;
        public string message;
        public GameBitData data;
    }

    [Serializable]
    public class GameBitData
    {
        public List<GameTicket> game_tickets;
    }

    [Serializable]
    public class GameTicket
    {
        public int user_id;
        public int play_point;
        public int win_point;
        public string result;
        public GameSlotObj game_slot_obj;
    }

    [Serializable]
    public class GameSlotObj
    {
        public int JS;
        public int QS;
        public int KS;
        public int JD;
        public int QD;
        public int KD;
        public int JC;
        public int QC;
        public int KC;
        public int JH;
        public int QH;
        public int KH;
    }
    #endregion
}