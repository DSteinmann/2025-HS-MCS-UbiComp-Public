using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActivityReceiver : MonoBehaviour
{
    [Header("UI References")]
    public GameObject ActivityNotifyContainer;
    public GameObject ActivityText;
    public GameObject SuggestionText;
    public TextMeshProUGUI DebugText;
    public ScrollRect DebugScrollRect;
    public GameObject CountdownText;
    public GameObject ChecklistPanel;
    public GameObject[] ChecklistItems;
    public GameObject[] SearchMarkers;
    
    [Header("Functionality")]
    // Video player removed

    [Header("System")]
    public HTTPListener HTTPListener;
    public GameObject DebugLogContainer;

    // Internal state
    public string tmpActivity = "";
    public float tmpProbability = 0f;
    public bool newActivityArrived = false;

    // Pomodoro Timer state
    private float pomodoroTimer = 0f;
    private bool isReadingActivity = false;

    // Inspection Checklist state
    private bool isInspectionActivity = false;
    private int currentChecklistIndex = 0;
    private string[] checklistSteps = {
        "1. Check for physical damage",
        "2. Verify safety seal",
        "3. Confirm all lights are green"
    };
    private bool[] checklistCompleted;

    // Search Activity state
    private bool isSearchingActivity = false;
    private int currentSearchMarker = 0;

    void Start()
    {
        HideDebugLogContainer();
        Debug.Log("start activity receiver");

        // Initialize countdown text
        if (CountdownText != null)
        {
            CountdownText.SetActive(false);
        }

        // Initialize checklist
        if (ChecklistPanel != null)
        {
            ChecklistPanel.SetActive(false);
        }
        checklistCompleted = new bool[checklistSteps.Length];
        UpdateChecklistDisplay();

        // Initialize search markers
        if (SearchMarkers != null)
        {
            foreach (GameObject marker in SearchMarkers)
            {
                if (marker != null)
                {
                    marker.SetActive(false);
                }
            }
        }
    }

    public void ShowDebugLogContainer()
    {
        DebugLogContainer.SetActive(true);
    }

    public void HideDebugLogContainer()
    {
        DebugLogContainer.SetActive(false);
    }

    private void UpdateChecklistDisplay()
    {
        if (ChecklistItems != null && ChecklistItems.Length >= checklistSteps.Length)
        {
            for (int i = 0; i < checklistSteps.Length; i++)
            {
                if (ChecklistItems[i] != null)
                {
                    TextMeshProUGUI textComponent = ChecklistItems[i].GetComponent<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        string status = checklistCompleted[i] ? "✓ " : "○ ";
                        textComponent.text = status + checklistSteps[i];
                        textComponent.color = checklistCompleted[i] ? Color.green : Color.white;
                    }
                }
            }
        }
    }

    public void MarkChecklistItemComplete()
    {
        if (isInspectionActivity && currentChecklistIndex < checklistSteps.Length)
        {
            checklistCompleted[currentChecklistIndex] = true;
            UpdateChecklistDisplay();
            currentChecklistIndex++;
            
            if (currentChecklistIndex >= checklistSteps.Length)
            {
                // All items completed
                SuggestionText.GetComponent<TextMeshPro>().text = "Inspection complete! All items checked.";
                Debug.Log("Inspection checklist completed");
            }
            else
            {
                SuggestionText.GetComponent<TextMeshPro>().text = $"Item {currentChecklistIndex} checked. Say 'Check' or tap to continue.";
            }
        }
    }

    public void OnVoiceCommand(string command)
    {
        if (command.ToLower().Contains("check") && isInspectionActivity)
        {
            MarkChecklistItemComplete();
        }
        else if (command.ToLower().Contains("next area") && isSearchingActivity)
        {
            AdvanceToNextSearchMarker();
        }
    }

    private void ActivateSearchMarkers()
    {
        if (SearchMarkers != null && SearchMarkers.Length > 0)
        {
            foreach (GameObject marker in SearchMarkers)
            {
                if (marker != null)
                {
                    marker.SetActive(true);
                    // Set to dimmed state initially
                    SetMarkerHighlight(marker, false);
                }
            }
            // Highlight the first marker
            if (SearchMarkers[0] != null)
            {
                SetMarkerHighlight(SearchMarkers[0], true);
            }
        }
    }

    private void SetMarkerHighlight(GameObject marker, bool highlighted)
    {
        if (marker != null)
        {
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (highlighted)
                {
                    // Bright/highlighted color (e.g., bright yellow)
                    renderer.material.color = Color.yellow;
                    // You could also scale it up or add glow effects
                    marker.transform.localScale = Vector3.one * 1.2f;
                }
                else
                {
                    // Dimmed color (e.g., dark gray)
                    renderer.material.color = Color.gray;
                    marker.transform.localScale = Vector3.one;
                }
            }
        }
    }

    private void AdvanceToNextSearchMarker()
    {
        if (isSearchingActivity && SearchMarkers != null && SearchMarkers.Length > 0)
        {
            // Dim the current marker
            if (currentSearchMarker < SearchMarkers.Length && SearchMarkers[currentSearchMarker] != null)
            {
                SetMarkerHighlight(SearchMarkers[currentSearchMarker], false);
            }

            currentSearchMarker++;

            if (currentSearchMarker >= SearchMarkers.Length)
            {
                // Search complete
                SuggestionText.GetComponent<TextMeshPro>().text = "Search complete! All areas have been checked.";
                Debug.Log("Search activity completed");
                isSearchingActivity = false;
                // Hide all markers
                foreach (GameObject marker in SearchMarkers)
                {
                    if (marker != null)
                    {
                        marker.SetActive(false);
                    }
                }
            }
            else
            {
                // Highlight next marker
                if (SearchMarkers[currentSearchMarker] != null)
                {
                    SetMarkerHighlight(SearchMarkers[currentSearchMarker], true);
                }
                SuggestionText.GetComponent<TextMeshPro>().text = $"Now search the area around marker {currentSearchMarker + 1}. Say 'Next area' when done.";
            }
        }
    }

    void Update()
    {
        if (HTTPListener.httpNewActivityArrived)
        {
            ReceiveNewActivity(HTTPListener.httpTmpActivity, HTTPListener.httpTmpProbability);
            HTTPListener.httpNewActivityArrived = false;
            HTTPListener.httpTmpActivity = "";
            HTTPListener.httpTmpProbability = 0f;
        }

        // Handle Pomodoro Timer
        if (isReadingActivity && CountdownText != null)
        {
            pomodoroTimer -= Time.deltaTime;
            CountdownText.GetComponent<TextMeshProUGUI>().text = Mathf.Ceil(pomodoroTimer).ToString();
            if (pomodoroTimer <= 0f)
            {
                // Time for a pause
                SuggestionText.GetComponent<TextMeshPro>().text = "Time for a break! Take a 5-minute pause.";
                CountdownText.SetActive(false);
                isReadingActivity = false;
            }
        }
    }

    private void ReceiveNewActivity(string activity, float probability)
    {
        Debug.Log($"Displaying new activity: {activity}");
        DebugText.text += $"\n[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}]";
        DebugText.text += $"\nDisplaying new activity: {activity}";
        var probPercent = probability.ToString("P", CultureInfo.InvariantCulture);
        ActivityText.GetComponent<TextMeshPro>().text = $"{activity} ({probPercent}).";
        var suggestion = "";

        // --- Deactivate all special features by default ---
        // ---

        switch (activity)
        {
            case "Reading":
                suggestion = "Let's watch a video!";
                // Start Pomodoro Timer
                isReadingActivity = true;
                pomodoroTimer = 20f;
                if (CountdownText != null)
                {
                    CountdownText.SetActive(true);
                    CountdownText.GetComponent<TextMeshProUGUI>().text = "20";
                    Debug.Log("Pomodoro Timer started and countdown text displayed");
                }
                else
                {
                    Debug.LogWarning("CountdownText is not assigned in ActivityReceiver!");
                }
                // Hide checklist if showing
                if (ChecklistPanel != null)
                {
                    ChecklistPanel.SetActive(false);
                }
                isInspectionActivity = false;
                // Hide search markers
                if (SearchMarkers != null)
                {
                    foreach (GameObject marker in SearchMarkers)
                    {
                        if (marker != null)
                        {
                            marker.SetActive(false);
                        }
                    }
                }
                isSearchingActivity = false;
                break;
            case "Inspection":
                suggestion = "Inspection checklist activated. Say 'Check' or tap to mark items complete.";
                // Start Inspection Checklist
                isInspectionActivity = true;
                currentChecklistIndex = 0;
                // Reset checklist completion
                for (int i = 0; i < checklistCompleted.Length; i++)
                {
                    checklistCompleted[i] = false;
                }
                UpdateChecklistDisplay();
                if (ChecklistPanel != null)
                {
                    ChecklistPanel.SetActive(true);
                    Debug.Log("Inspection checklist displayed");
                }
                else
                {
                    Debug.LogWarning("ChecklistPanel is not assigned in ActivityReceiver!");
                }
                // Hide countdown
                if (CountdownText != null)
                {
                    CountdownText.SetActive(false);
                }
                isReadingActivity = false;
                // Hide search markers
                if (SearchMarkers != null)
                {
                    foreach (GameObject marker in SearchMarkers)
                    {
                        if (marker != null)
                        {
                            marker.SetActive(false);
                        }
                    }
                }
                isSearchingActivity = false;
                break;
            case "Search":
                suggestion = "Let's search this area systematically. First, search the area around marker 1.";
                // Start Search Activity
                isSearchingActivity = true;
                currentSearchMarker = 0;
                ActivateSearchMarkers();
                if (SearchMarkers == null || SearchMarkers.Length == 0)
                {
                    Debug.LogWarning("No search markers assigned in ActivityReceiver!");
                }
                else
                {
                    Debug.Log("Search markers activated");
                }
                // Hide other UI elements
                if (CountdownText != null)
                {
                    CountdownText.SetActive(false);
                }
                if (ChecklistPanel != null)
                {
                    ChecklistPanel.SetActive(false);
                }
                isReadingActivity = false;
                isInspectionActivity = false;
                break;
            default:
                // No specific suggestion, ensure video is stopped.
                // Reset timer
                isReadingActivity = false;
                if (CountdownText != null)
                {
                    CountdownText.SetActive(false);
                }
                // Hide checklist
                if (ChecklistPanel != null)
                {
                    ChecklistPanel.SetActive(false);
                }
                isInspectionActivity = false;
                // Hide search markers
                if (SearchMarkers != null)
                {
                    foreach (GameObject marker in SearchMarkers)
                    {
                        if (marker != null)
                        {
                            marker.SetActive(false);
                        }
                    }
                }
                isSearchingActivity = false;
                break;
        }
        SuggestionText.GetComponent<TextMeshPro>().text = suggestion;
    }
}
