using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool autoBet = false;
    [SerializeField] private bool printTicket = true;
    [SerializeField] private bool printCancel = true;
    [SerializeField] private string bluetoothPrinterName = "";

    // Events for when settings change
    public System.Action<bool> OnAutoBetChanged;
    public System.Action<bool> OnPrintTicketChanged;
    public System.Action<bool> OnPrintCancelChanged;
    public System.Action<string> OnPrinterChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Properties with event triggering
    public bool AutoBet
    {
        get => autoBet;
        set
        {
            if (autoBet != value)
            {
                autoBet = value;
                OnAutoBetChanged?.Invoke(value);
                SaveSettings();
            }
        }
    }

    public bool PrintTicket
    {
        get => printTicket;
        set
        {
            if (printTicket != value)
            {
                printTicket = value;
                OnPrintTicketChanged?.Invoke(value);
                SaveSettings();
            }
        }
    }

    public bool PrintCancel
    {
        get => printCancel;
        set
        {
            if (printCancel != value)
            {
                printCancel = value;
                OnPrintCancelChanged?.Invoke(value);
                SaveSettings();
            }
        }
    }

    public string BluetoothPrinterName
    {
        get => bluetoothPrinterName;
        set
        {
            if (bluetoothPrinterName != value)
            {
                bluetoothPrinterName = value;
                OnPrinterChanged?.Invoke(value);
                SaveSettings();
            }
        }
    }

    void SaveSettings()
    {
        PlayerPrefs.SetInt("AutoBet", autoBet ? 1 : 0);
        PlayerPrefs.SetInt("PrintTicket", printTicket ? 1 : 0);
        PlayerPrefs.SetInt("PrintCancel", printCancel ? 1 : 0);
        PlayerPrefs.SetString("BluetoothPrinter", bluetoothPrinterName);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        autoBet = PlayerPrefs.GetInt("AutoBet", 0) == 1;
        printTicket = PlayerPrefs.GetInt("PrintTicket", 1) == 1;
        printCancel = PlayerPrefs.GetInt("PrintCancel", 1) == 1;
        bluetoothPrinterName = PlayerPrefs.GetString("BluetoothPrinter", "");
    }
}