using UnityEngine;
using TMPro;
using System.Text;
using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Speech;

public class NoteTaker : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI noteText;
    public GameObject notePanel;

    [Header("Voice Commands")]
    private const string START_COMMAND = "take a note";
    private const string STOP_COMMAND = "stop";

    private KeywordRecognitionSubsystem keywordSubsystem;
    private DictationSubsystem dictationSubsystem;
    private StringBuilder transcribedText;

    private bool isDictating = false;

    void Start()
    {
        transcribedText = new StringBuilder();
        if (notePanel != null)
        {
            notePanel.SetActive(false);
        }

        keywordSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<KeywordRecognitionSubsystem>();
        dictationSubsystem = (DictationSubsystem)XRSubsystemHelpers.DictationSubsystem;

        if (keywordSubsystem != null)
        {
            keywordSubsystem.CreateOrGetEventForKeyword(START_COMMAND).AddListener(OnStartCommand);
            keywordSubsystem.CreateOrGetEventForKeyword(STOP_COMMAND).AddListener(OnStopCommand);
        }

        if (dictationSubsystem != null)
        {
            dictationSubsystem.Recognized += OnDictationRecognized;
            dictationSubsystem.RecognitionFinished += OnDictationFinished;
            dictationSubsystem.RecognitionFaulted += OnDictationFaulted;
        }
    }

    void OnDestroy()
    {
        if (keywordSubsystem != null)
        {
            keywordSubsystem.CreateOrGetEventForKeyword(START_COMMAND)?.RemoveListener(OnStartCommand);
            keywordSubsystem.CreateOrGetEventForKeyword(STOP_COMMAND)?.RemoveListener(OnStopCommand);
        }

        if (dictationSubsystem != null)
        {
            dictationSubsystem.Recognized -= OnDictationRecognized;
            dictationSubsystem.RecognitionFinished -= OnDictationFinished;
            dictationSubsystem.RecognitionFaulted -= OnDictationFaulted;
        }
    }

    private void OnStartCommand()
    {
        if (!isDictating)
        {
            isDictating = true;
            transcribedText.Clear();
            if (notePanel != null)
            {
                notePanel.SetActive(true);
            }
            if (noteText != null)
            {
                noteText.text = "Listening...";
            }
            keywordSubsystem?.Stop();
            dictationSubsystem?.StartDictation();
        }
    }

    private void OnStopCommand()
    {
        if (isDictating)
        {
            dictationSubsystem?.StopDictation();
        }
    }

    private void OnDictationRecognized(DictationResultEventArgs eventData)
    {
        if (eventData.Result != null)
        {
            transcribedText.Append(eventData.Result + " ");
            if (noteText != null)
            {
                noteText.text = transcribedText.ToString();
            }
        }
    }

    private void OnDictationFinished(DictationSessionEventArgs eventData)
    {
        isDictating = false;
        if (notePanel != null)
        {
            notePanel.SetActive(false);
        }
        keywordSubsystem?.Start();
    }

    private void OnDictationFaulted(DictationSessionEventArgs eventData)
    {
        Debug.LogError("Dictation faulted: " + eventData.Reason);
        isDictating = false;
        keywordSubsystem?.Start();
    }
}
