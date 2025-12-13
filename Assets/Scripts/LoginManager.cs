using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using UnityEngine.SceneManagement;
using SimpleJSON;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField usernameInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private TMP_Text MessageText;

    [Header("Message Panel")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text panelMessageText;
    [SerializeField] private Button okButton;

    [Header("API Settings")]
    [SerializeField] private string baseUrl = "https://casino-backend.realtimevillage.com/api";

    [Header("Override Settings")]
    [SerializeField] private bool overrideDeviceAndPlatform = true;
    [SerializeField] private string overrideDeviceId = "test";
    [SerializeField] private string overridePlatform = "android";

    void Start()
    {
        if (messagePanel != null)
            messagePanel.SetActive(false);

        if (okButton != null)
            okButton.onClick.AddListener(HideMessagePanel);
    }

    public void OnSubmitLogin()
    {
        if (usernameInputField == null || passwordInputField == null)
        {
            ShowMessage("UI references are not assigned!");
            return;
        }

        string username = usernameInputField.text;
        string password = passwordInputField.text;

        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.Save();

        string loginCheckMessage = CheckLoginInfo(username, password);

        if (string.IsNullOrEmpty(loginCheckMessage))
        {
            ShowMessage("Logging in...");
            StartCoroutine(LoginRequest(username, password));
        }
        else
        {
            ShowMessage(loginCheckMessage);
        }
    }

    private string CheckLoginInfo(string username, string password)
    {
        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
            return "Please enter username and password";
        if (string.IsNullOrEmpty(username))
            return "Please enter the username";
        if (string.IsNullOrEmpty(password))
            return "Please enter the password";
        return "";
    }

    // Message Panel Methods
    public void ShowMessage(string message)
    {
        if (panelMessageText != null)
            panelMessageText.text = message;

        if (messagePanel != null)
            messagePanel.SetActive(true);
    }

    public void HideMessagePanel()
    {
        if (messagePanel != null)
            messagePanel.SetActive(false);
    }

    public void ShowAutoHideMessage(string message, float delay = 2f)
    {
        ShowMessage(message);
        StartCoroutine(AutoHideMessage(delay));
    }

    private IEnumerator AutoHideMessage(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideMessagePanel();
    }

    private IEnumerator LoginRequest(string username, string password)
    {
        var loginData = new LoginRequestData
        {
            username = username,
            password = password,
            device_id = overrideDeviceAndPlatform ? overrideDeviceId : SystemInfo.deviceUniqueIdentifier,
            platform = overrideDeviceAndPlatform ? overridePlatform : Application.platform.ToString().ToLower()
        };

        string json = JsonUtility.ToJson(loginData);
        string url = baseUrl + "/v1/auth/login";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        request.certificateHandler = new SSLLoggingCertificateHandler();

        ShowMessage("Logging in...");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            ShowMessage("Network error. Try again.");
        }
        else
        {
            string responseText = request.downloadHandler.text;
            var jsonResponse = JSON.Parse(responseText);

            string authToken = "";

            if (jsonResponse["data"] != null && !string.IsNullOrEmpty(jsonResponse["data"]["access_token"]))
                authToken = jsonResponse["data"]["access_token"].Value;
            else if (!string.IsNullOrEmpty(jsonResponse["access_token"]))
                authToken = jsonResponse["access_token"].Value;

            if (!string.IsNullOrEmpty(authToken))
            {
                PlayerPrefs.SetString("AUTH_KEY", authToken);

                int walletBalance = 0;
                if (jsonResponse["data"] != null && jsonResponse["data"]["wallet_balance"] != null)
                    walletBalance = jsonResponse["data"]["wallet_balance"].AsInt;
                PlayerPrefs.SetInt("wallet_balance", walletBalance);

                PlayerPrefs.Save();

                ShowAutoHideMessage("Login successful!", 1f);
                yield return new WaitForSeconds(1.5f);

                if (SceneManager.sceneCountInBuildSettings > 1)
                    SceneManager.LoadScene(1);
                else
                    ShowMessage("Login successful! (Scene not configured)");
            }
            else
            {
                string errorMessage = "Login failed";
                if (jsonResponse["message"] != null)
                    errorMessage = "Login failed: " + jsonResponse["message"].Value;

                ShowMessage(errorMessage);
            }
        }
    }

    public class SSLLoggingCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    [System.Serializable]
    public class LoginRequestData
    {
        public string username;
        public string password;
        public string device_id;
        public string platform;
    }

    [System.Serializable]
    public class LoginData
    {
        public int id;
        public string email;
        public string name;
        public int wallet_balance;
        public string access_token;
    }

    [System.Serializable]
    public class LoginSuccessResponse
    {
        public bool status;
        public string message;
        public LoginData data;
        public string access_token;
    }

    [System.Serializable]
    public class ErrorResponse
    {
        public int statusCode;
        public string message;
        public bool status;
    }
}