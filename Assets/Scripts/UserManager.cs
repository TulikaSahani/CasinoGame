using UnityEngine;
using TMPro;
public class UserManager : MonoBehaviour
{
    public TMP_Text usernameText;  
    public TMP_Text walletText;
    void Start()
    {
        // Get saved username
        string username = PlayerPrefs.GetString("Username", "Guest"); // Default "Guest" if not found
        int wallet = PlayerPrefs.GetInt("wallet_balance", 0);

        usernameText.text = " " + username.ToUpper() ;
        walletText.text = " " + wallet.ToString();
    }
}

