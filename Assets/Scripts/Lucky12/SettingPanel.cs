using TMPro;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;
/*
public class SettingPanel : MonoBehaviour
{
    [SerializeField] private Toggle BluetoothToggle;
    [SerializeField] private GameObject bluetoothSearchPanel;
    [SerializeField] private Transform deviceListContainer;
    [SerializeField] private GameObject deviceItemPrefab;
    [SerializeField] private Button searchButton;
    private void Start()
    {
        
        BluetoothToggle.isOn = IsBluetoothEnabled();

       
        BluetoothToggle.onValueChanged.AddListener(OnBluetoothToggleChanged);
        bluetoothSearchPanel.SetActive(false);

        if (searchButton != null)
            searchButton.onClick.AddListener(ShowBluetoothDevices);
    }
    private void OnBluetoothToggleChanged(bool isOn)
    {
        if (isOn)
        {
            TurnBluetoothOn();

        }
        else
        {
            TurnBluetoothOff();
        }
    }
    public void ShowBluetoothDevices()
    {
        if (!IsBluetoothEnabled())
        {
            Debug.Log("Please turn Bluetooth ON first");
            return;
        }

        bluetoothSearchPanel.SetActive(true);
        PopulateDeviceList();
    }
    private void PopulateDeviceList()
    {
        // Clear old devices
        foreach (Transform child in deviceListContainer)
            Destroy(child.gameObject);

        // Get available devices (simulated/minimal version)
        List<string> deviceNames = GetAvailableBluetoothDevices();

        foreach (string deviceName in deviceNames)
        {
            // Create device item
            GameObject deviceItem = Instantiate(deviceItemPrefab, deviceListContainer);

            // Set device name
            TMP_Text nameText = deviceItem.GetComponentInChildren<TMP_Text>();
            if (nameText != null)
                nameText.text = deviceName;

            // Add select button listener
            Button selectButton = deviceItem.GetComponentInChildren<Button>();
            if (selectButton != null)
                selectButton.onClick.AddListener(() => SelectDevice(deviceName));
        }
    }

    private void TurnBluetoothOn()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android Bluetooth enable code
        using (AndroidJavaClass bluetoothAdapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter"))
        {
            AndroidJavaObject bluetoothAdapter = bluetoothAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter");
            if (bluetoothAdapter != null)
            {
                bluetoothAdapter.Call<bool>("enable");
            }
        }
#endif

        Debug.Log("Bluetooth turned ON");
    }
    private void TurnBluetoothOff()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android Bluetooth disable code
        using (AndroidJavaClass bluetoothAdapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter"))
        {
            AndroidJavaObject bluetoothAdapter = bluetoothAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter");
            if (bluetoothAdapter != null)
            {
                bluetoothAdapter.Call<bool>("disable");
            }
        }
#endif

        Debug.Log("Bluetooth turned OFF");
    }
    private bool IsBluetoothEnabled()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass bluetoothAdapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter"))
        {
            AndroidJavaObject bluetoothAdapter = bluetoothAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter");
            return bluetoothAdapter != null && bluetoothAdapter.Call<bool>("isEnabled");
        }
#endif

        return true;
    }
}
*/