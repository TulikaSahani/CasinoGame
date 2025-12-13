using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class BetManager : MonoBehaviour
{
    [Header("Circular Timer")]
    public Image circularTimerFill; 

    

    public static BetManager Instance;
    public AudioSource buttonClickAudio;
    [Header("API")]
    public string baseUrl = "https://casino-backend.realtimevillage.com/api";
    public int gameId = 2;
    public float fallbackGameTime = 30f;
    public WinningCardDisplay winningCardDisplay;

    [Header("UI - Texts & Buttons")]
    public TMP_Text timerText;
    public TMP_Text statusText;
    public TMP_Text totalBetText;
    public TMP_Text walletBalanceText;
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
    public SimpleGlowAnimation glowEffect;

    [Header("Timer thresholds (seconds)")]
    public float warningThreshold = 15f;
    public float lockThreshold = 10f;

    public rewardanimation rewardAnimation;
   
    
    [HideInInspector] public List<CardBetSpot> cardSpots = new List<CardBetSpot>();
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
    // Wheel mapping 
    private readonly string[] cardCodes = new string[]
    {
        "JS","QS","KS","JD","QD","KD","JC","QC","KC","JH","QH","KH","AH","AS","AD","AC"
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
        game_time = 0f;
        roundInitialized = false;
        countdownSoundPlayed = false;
        StartCoroutine(InitializeWalletAndRound());
    }
   

    void Update()
    {
        if (!roundInitialized || spinning || roundComplete) return;

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
            if (bettingOpen && game_time <= lockThreshold)
            {
                bettingOpen = false;
                SetAllInteractive(false);
                if (statusText != null) statusText.text = "Betting closed";
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
       
       

        // Stop any existing glow
        if (glowEffect != null)
            glowEffect.StopGlow();
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

        int totalBet = CalculateTotal();
        if (totalBet <= 0)
        {
            if (statusText != null) statusText.text = "Place a bet first.";
            return;
        }

        if (totalBet > currentWalletBalance)
        {
            if (statusText != null) statusText.text = "Insufficient balance!";
            return;
        }

        currentWalletBalance -= totalBet;
        UpdateWalletUI();

        bettingOpen = false;
        SetAllInteractive(false);
        if (statusText != null) statusText.text = "Bet locked. Resolving...";

        StartCoroutine(PlaceBetsThenResolve());
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

        string url = $"{baseUrl}/v1/ticket/add-lucky16-ticket?token={token}";

        Lucky12BetRequest reqObj = new Lucky12BetRequest();
        reqObj.game_result_id = currentGameResultId;
        reqObj.game_id = gameId;
        reqObj.play_point = CalculateTotal();

        // Initialize all bets to 0
        reqObj.JS = reqObj.QS = reqObj.KS = reqObj.JD = reqObj.QD = reqObj.KD = 
        reqObj.JC = reqObj.QC = reqObj.KC = reqObj.JH = reqObj.QH = reqObj.KH = 0;
        reqObj.AH = reqObj.AS = reqObj.AD = reqObj.AC = 0;
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
                    case "AH": reqObj.AH = bet; break;
                    case "AS": reqObj.AS = bet; break;
                    case "AD": reqObj.AD = bet; break;
                    case "AC": reqObj.AC = bet; break;
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
        lastRoundWinning = CalculateWinning(resultCode);
        Debug.Log($"Result: {resultCode}, Winning: {lastRoundWinning}");

        if (statusText != null) statusText.text = "Spinning...";
       
        // Spin wheel
        if (SpinWheelManager.Instance != null)
        {
            bool spinFinished = false;
            SpinWheelManager.Instance.SpinToResultWithThreeRounds(resultCode, () => spinFinished = true);
            yield return new WaitUntil(() => spinFinished);
            
        }
        else
        {
            yield return new WaitForSeconds(4f);
        }
        if (winningCardDisplay != null)
        {
            winningCardDisplay.ShowWinningCard(resultCode);
        }
        // Show result and handle winning
        if (lastRoundWinning > 0)
        {
            
            if (statusText != null) statusText.text = $" You won {lastRoundWinning}!";
            if (rewardAnimation != null)
            {
                rewardAnimation.Rewardcoin(lastRoundWinning);
                yield return new WaitForSeconds(2f);
            }
            

            if (winAudio != null) //ply win sound
            {
                winAudio.Play();
            }
           
            // Start glow effect
            if (glowEffect != null)
                glowEffect.StartGlow();
            
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

        // Start new round
        spinning = false;
        roundComplete = false;
        StartCoroutine(InitializeRound());
    }

    IEnumerator StopGlowAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (glowEffect != null)
            glowEffect.StopGlow();
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
        return totalWinning;
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
    public void RegisterCard(CardBetSpot card)
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

    internal void RegisterCard(CardBettingSpots cardBettingSpots)
    {
        throw new NotImplementedException();
    }
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
        public int AH, AS, AD, AC;
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

    public static implicit operator BetManager(CG2Manager v)
    {
        throw new NotImplementedException();
    }
    #endregion
}