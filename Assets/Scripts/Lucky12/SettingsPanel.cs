using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Toggle autoBetToggle;
    [SerializeField] private Toggle printTicketToggle;
    [SerializeField] private Toggle printCancelToggle;
    [SerializeField] private TMP_InputField printerInputField;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button closeButton;

    [Header("Bluetooth Search")]
    [SerializeField] private GameObject bluetoothSearchPanel;
    [SerializeField] private Transform deviceListContainer;
    [SerializeField] private GameObject deviceItemPrefab;
    [SerializeField] private Button searchButton;

    private void Start()
    {
        // Load current settings
        if (SettingsManager.Instance != null)
        {
            autoBetToggle.isOn = SettingsManager.Instance.AutoBet;
            printTicketToggle.isOn = SettingsManager.Instance.PrintTicket;
            printCancelToggle.isOn = SettingsManager.Instance.PrintCancel;
            printerInputField.text = SettingsManager.Instance.BluetoothPrinterName;
        }

        // Set up listeners
        autoBetToggle.onValueChanged.AddListener(OnAutoBetChanged);
        printTicketToggle.onValueChanged.AddListener(OnPrintTicketChanged);
        printCancelToggle.onValueChanged.AddListener(OnPrintCancelChanged);
        printerInputField.onEndEdit.AddListener(OnPrinterNameChanged);

        if (saveButton != null)
            saveButton.onClick.AddListener(SaveSettings);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (searchButton != null)
            searchButton.onClick.AddListener(ShowBluetoothDevices);

        // Hide bluetooth panel initially
        if (bluetoothSearchPanel != null)
            bluetoothSearchPanel.SetActive(false);
    }

    public void OnAutoBetChanged(bool value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.AutoBet = value;
    }

    public void OnPrintTicketChanged(bool value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.PrintTicket = value;
    }

    public void OnPrintCancelChanged(bool value)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.PrintCancel = value;
    }

    public void OnPrinterNameChanged(string printerName)
    {
        if (SettingsManager.Instance != null)
            SettingsManager.Instance.BluetoothPrinterName = printerName;
    }

    public void SaveSettings()
    {
        Debug.Log("Settings saved!");
        // Settings are automatically saved when changed via SettingsManager
        ClosePanel();
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
        //transform.parent.gameObject.SetActive(false);
        Debug.Log("Settings panel closed");
    }

    public void ShowBluetoothDevices()
    {
        if (bluetoothSearchPanel == null) return;

        bluetoothSearchPanel.SetActive(true);
        PopulateDeviceList();
    }

    private void PopulateDeviceList()
    {
        // Clear existing items
        foreach (Transform child in deviceListContainer)
        {
            Destroy(child.gameObject);
        }

        // Simulated device list (replace with actual Bluetooth scanning)
        string[] simulatedDevices = { "Printer-001", "Printer-ABC", "MyBluetoothPrinter", "Device-123" };

        foreach (string deviceName in simulatedDevices)
        {
            GameObject deviceItem = Instantiate(deviceItemPrefab, deviceListContainer);
            TMP_Text deviceText = deviceItem.GetComponentInChildren<TMP_Text>();
            Button selectButton = deviceItem.GetComponentInChildren<Button>();

            if (deviceText != null)
                deviceText.text = deviceName;

            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => SelectPrinter(deviceName));
            }
        }
    }

    void SelectPrinter(string deviceName)
    {
        printerInputField.text = deviceName;
        OnPrinterNameChanged(deviceName);

        if (bluetoothSearchPanel != null)
            bluetoothSearchPanel.SetActive(false);
    }
}