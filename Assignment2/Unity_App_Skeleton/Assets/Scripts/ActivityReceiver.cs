using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ActivityReceiver : MonoBehaviour
{
    [Header("UI References")]
    public GameObject ActivityNotifyContainer;
    public GameObject ActivityText;
    public GameObject SuggestionText;
    public TextMeshProUGUI DebugText;
    public ScrollRect DebugScrollRect;
    
    [Header("Functionality")]
    public GameObject videoPlayer; // Drag your VideoPlayerScreen here
    public NoteTaker noteTaker;

    [Header("System")]
    public HTTPListener HTTPListener;
    public GameObject DebugLogContainer;

    // Internal state
    public string tmpActivity = "";
    public float tmpProbability = 0f;
    public bool newActivityArrived = false;

    void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.SetActive(false); // Ensure video player is off at the start
        }
        if (noteTaker == null)
        {
            Debug.LogError("NoteTaker reference is not set in ActivityReceiver!");
        }
        HideDebugLogContainer();
        Debug.Log("start activity receiver");
    }

    public void ShowDebugLogContainer()
    {
        DebugLogContainer.SetActive(true);
    }

    public void HideDebugLogContainer()
    {
        DebugLogContainer.SetActive(false);
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
        if (videoPlayer != null && videoPlayer.activeSelf)
        {
            videoPlayer.SetActive(false);
            videoPlayer.GetComponent<VideoPlayer>().Stop();
        }
        // ---

        switch (activity)
        {
            case "Reading":
                suggestion = "Let's watch a video!";
                if (videoPlayer != null)
                {
                    videoPlayer.SetActive(true);
                    videoPlayer.GetComponent<VideoPlayer>().Play();
                }
                break;
            case "Inspection":
                suggestion = "You can now take notes. Say 'take a note' to begin.";
                Debug.Log("ActivityReceiver: 'Inspection' case reached.");
                // noteTaker is now always listening for the keyword.
                break;
            case "Search":
                suggestion = "Searching for something? Say 'find my...' followed by an item name.";
                break;
            default:
                // No specific suggestion, ensure video is stopped.
                break;
        }
        SuggestionText.GetComponent<TextMeshPro>().text = suggestion;
    }
}
