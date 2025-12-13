using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DatePickerIntegration : MonoBehaviour
{
    [Header("DatePicker Reference")]
    public GameObject datePickerObject; 

    [Header("UI References")]
    public TMP_InputField dateInputField; 
    public GameTicketHistoryPanel historyPanel; 

    [Header("Settings")]
    public string dateFormat = "yyyy-MM-dd"; 

    private DateTime selectedDate;

    void Start()
    {
        
        var datePickerComponent = GetDatePickerComponent();

        if (datePickerComponent != null)
        {
            
            SetupDatePickerEvents(datePickerComponent);
        }

       
        selectedDate = DateTime.Now;
        UpdateDateInput();
    }

   
    Component GetDatePickerComponent()
    {
        if (datePickerObject == null) return null;

     
        var datePicker = datePickerObject.GetComponent("DatePicker");
        if (datePicker != null) return datePicker;

        datePicker = datePickerObject.GetComponent("DatePicker_Popup");
        if (datePicker != null) return datePicker;

        datePicker = datePickerObject.GetComponent("DatePicker_DatePicker");
        if (datePicker != null) return datePicker;

        Debug.LogError("Could not find DatePicker component on " + datePickerObject.name);
        return null;
    }

    void SetupDatePickerEvents(Component datePickerComponent)
    {
        // Method 1: If the event is exposed as UnityEvent in Inspector
        // You'll connect it in the Unity Editor directly

        // Method 2: If we need to connect via code
        try
        {
          
            var eventField = datePickerComponent.GetType().GetField("OnDaySelected");
            if (eventField != null)
            {
                // This is usually a UnityEvent<DateTime>
                var unityEvent = eventField.GetValue(datePickerComponent);
                if (unityEvent != null)
                {
                    // Add listener via reflection
                    var addMethod = unityEvent.GetType().GetMethod("AddListener");
                    if (addMethod != null)
                    {
                        // Create delegate for our OnDateSelected method
                        var action = new Action<DateTime>(OnDateSelected);
                        addMethod.Invoke(unityEvent, new object[] { action });
                        Debug.Log("Successfully connected to DatePicker event via reflection");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not connect to DatePicker events via code: " + e.Message);
            Debug.Log("You'll need to connect the event in the Inspector instead");
        }
    }

    // This method will be called when a date is selected
    public void OnDateSelected(DateTime selectedDateTime)
    {
        selectedDate = selectedDateTime;
        UpdateDateInput();

        // Trigger history fetch
        if (historyPanel != null)
        {
            // Call a public method on your history panel
            historyPanel.FetchHistoryForDate(selectedDate.ToString(dateFormat));
        }

        Debug.Log($"Date selected: {selectedDate.ToString(dateFormat)}");
    }

    void UpdateDateInput()
    {
        if (dateInputField != null)
        {
            dateInputField.text = selectedDate.ToString(dateFormat);
        }
    }

    // Manual method to fetch history (call from button)
    public void FetchHistoryForSelectedDate()
    {
        if (historyPanel != null)
        {
            historyPanel.FetchHistoryForDate(selectedDate.ToString(dateFormat));
        }
    }

    public string GetSelectedDateString()
    {
        return selectedDate.ToString(dateFormat);
    }
}