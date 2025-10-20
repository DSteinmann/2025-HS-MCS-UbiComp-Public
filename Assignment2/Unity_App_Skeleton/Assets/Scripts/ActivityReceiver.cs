using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SolidInteractionLibrary;

public class ActivityReceiver : MonoBehaviour
{
    [System.Serializable]
    public class ActivityInfo
    {
        public string PersonName;
        public string ActivityName;
        public float Probability;
        public System.DateTime EndTime;
    }

    [System.Serializable]
    public class Contact
    {
        public string Name;
        public string WebId;
        public string ImageUrl;
    }
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

    [Header("Solid Pod Configuration")]
    public string solidServerUrl = "https://wiser-solid-xi.interactions.ics.unisg.ch/";
    public string solidWebId = "https://wiser-solid-xi.interactions.ics.unisg.ch/dominik-ubicomp2025/profile/card#me";
    public string solidEmail = ""; // Set this in Inspector or through UI
    public string solidPassword = ""; // Set this in Inspector or through UI

    [Header("Activity Display UI")]
    public GameObject PersonNameText;
    public GameObject ActivityProbabilityText;
    public GameObject ActivityEndTimeText;

    [Header("Sharing Interface UI")]
    public GameObject SharePanel;
    public GameObject WebIdInputField;
    public GameObject AccessTypeDropdown;
    public GameObject ContactListPanel;
    public GameObject ContactListContent;
    public GameObject ContactButtonPrefab;

    [Header("Overlay Layout")]
    [Tooltip("Automatically keep the primary overlays spaced out in front of the user.")]
    public bool autoArrangeOverlays = true;
    [Tooltip("Distance in meters from the user for the overlay root.")]
    public float overlayDistance = 0.45f;
    [Tooltip("Horizontal (x) and vertical (y) offsets for the activity summary panel.")]
    public Vector2 activityPanelOffset = new Vector2(0f, 0.2f);
    [Tooltip("Horizontal (x) and vertical (y) offsets for the inspection checklist panel.")]
    public Vector2 checklistPanelOffset = new Vector2(0f, -0.25f);
    [Tooltip("Horizontal (x) and vertical (y) offsets for the sharing panel when visible.")]
    public Vector2 sharePanelOffset = new Vector2(0.5f, 0f);
    [Tooltip("Horizontal (x) and vertical (y) offsets for the debug log window.")]
    public Vector2 debugPanelOffset = new Vector2(-0.55f, -0.35f);
    [Tooltip("If true, the overlays will keep following the headset pose each frame.")]
    public bool overlaysFollowCamera = true;

    // Internal state
    public string tmpActivity = "";
    public float tmpProbability = 0f;
    public bool newActivityArrived = false;

    // Solid Pod client
    private AuthenticatedPodClient solidClient;
    private bool solidAuthenticated = false;
    private string gazeDataCsv = "";
    private bool cameraWarningLogged = false;

    // Text orientation cache
    private bool textRotationsCached = false;
    private readonly Dictionary<Transform, Quaternion> textInitialLocalRotations = new Dictionary<Transform, Quaternion>();

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

        // Initialize sharing interface
        InitializeSharingInterface();

        // Initialize Solid client
        InitializeSolidClient();

        // Arrange overlays for first frame
        ArrangeOverlays();
        CacheTextOrientations();
        RestoreTextOrientations();
    }

    private void InitializeSharingInterface()
    {
        // Hide share panel initially
        if (SharePanel != null)
        {
            SharePanel.SetActive(false);
        }

        // Initialize access type dropdown
        if (AccessTypeDropdown != null)
        {
            var dropdown = AccessTypeDropdown.GetComponent<TMPro.TMP_Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string> { "Read", "Write", "ReadWrite" });
            dropdown.value = 0; // Default to Read
        }

        // Hide manual input initially
        if (WebIdInputField != null)
        {
            WebIdInputField.SetActive(false);
        }
    }

    private void ArrangeOverlays()
    {
        if (!autoArrangeOverlays)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            if (!cameraWarningLogged)
            {
                Debug.LogWarning("ActivityReceiver: No camera tagged MainCamera found for overlay arrangement.");
                cameraWarningLogged = true;
            }
            return;
        }

        cameraWarningLogged = false;

        Vector3 flattenedForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
        if (flattenedForward.sqrMagnitude < 0.0001f)
        {
            flattenedForward = cam.transform.forward;
        }
        flattenedForward.Normalize();

        Vector3 basePosition = cam.transform.position + flattenedForward * overlayDistance;
    Quaternion lookRotation = Quaternion.LookRotation(basePosition - cam.transform.position, Vector3.up);

        PositionOverlay(ActivityNotifyContainer, activityPanelOffset, cam, basePosition, lookRotation);
        PositionOverlay(ChecklistPanel, checklistPanelOffset, cam, basePosition, lookRotation);
        PositionOverlay(DebugLogContainer, debugPanelOffset, cam, basePosition, lookRotation);

        if (SharePanel != null && SharePanel.activeInHierarchy)
        {
            PositionOverlay(SharePanel, sharePanelOffset, cam, basePosition, lookRotation);
        }

        RestoreTextOrientations();
    }

    private void PositionOverlay(GameObject target, Vector2 offset, Camera cam, Vector3 basePosition, Quaternion lookRotation)
    {
        if (target == null)
        {
            return;
        }

        Transform targetTransform = target.transform;
        Vector3 horizontalOffset = cam.transform.right * offset.x;
        Vector3 verticalOffset = cam.transform.up * offset.y;

        targetTransform.position = basePosition + horizontalOffset + verticalOffset;
        targetTransform.rotation = lookRotation;
    }

    private async void InitializeSolidClient()
    {
        if (!string.IsNullOrEmpty(solidEmail) && !string.IsNullOrEmpty(solidPassword))
        {
            try
            {
                solidClient = await AuthenticatedPodClient.BuildAsync(solidServerUrl, solidWebId, solidEmail, solidPassword);
                solidAuthenticated = true;
                Debug.Log("Solid client authenticated successfully");

                // Create gazeData container
                await CreateGazeDataContainer();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to authenticate Solid client: {e.Message}");
                solidAuthenticated = false;
            }
        }
        else
        {
            Debug.LogWarning("Solid credentials not provided. Set solidEmail and solidPassword in the Inspector.");
        }
    }

    private async Task CreateGazeDataContainer()
    {
        if (!solidAuthenticated || solidClient == null) return;

        try
        {
            // Create gazeData container
            string containerUrl = $"{solidWebId.Split(new string[] { "/profile/card#me" }, StringSplitOptions.None)[0]}/gazeData/";
            await solidClient.SaveFileAsync(containerUrl, "text/turtle", "");
            Debug.Log($"Created gazeData container: {containerUrl}");
        }
        catch (Exception e)
        {
            // Check if it's a 409 Conflict (container already exists)
            if (e.Message.Contains("409") || e.Message.Contains("Conflict"))
            {
                Debug.Log("GazeData container already exists, continuing...");
            }
            else
            {
                Debug.LogError($"Failed to create gazeData container: {e.Message}");
            }
        }
    }

    private async Task SaveGazeData(string gazeDataCsv)
    {
        if (!solidAuthenticated || solidClient == null) return;

        try
        {
            string gazeDataUrl = $"{solidWebId.Split(new string[] { "/profile/card#me" }, StringSplitOptions.None)[0]}/gazeData/my_gaze_data.csv";
            await solidClient.SaveFileAsync(gazeDataUrl, "text/csv", gazeDataCsv);
            Debug.Log($"Saved gaze data to: {gazeDataUrl}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save gaze data: {e.Message}");
        }
    }

    private async Task SaveCurrentActivity(string activity, float probability)
    {
        if (!solidAuthenticated || solidClient == null) return;

        try
        {
            string activityTypeUri = GetActivityTypeUri(activity);
            string currentActivityTtl = GenerateCurrentActivityTtl(activity, probability, activityTypeUri);
            string activityUrl = $"{solidWebId.Split(new string[] { "/profile/card#me" }, StringSplitOptions.None)[0]}/gazeData/currentActivity.ttl";
            await solidClient.SaveFileAsync(activityUrl, "text/turtle", currentActivityTtl);
            Debug.Log($"Saved current activity to: {activityUrl}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save current activity: {e.Message}");
        }
    }

    public async Task<string> ReadCurrentActivity()
    {
        if (!solidAuthenticated || solidClient == null)
        {
            Debug.LogWarning("Solid client not authenticated");
            return null;
        }

        try
        {
            string activityUrl = $"{solidWebId.Split(new string[] { "/profile/card#me" }, StringSplitOptions.None)[0]}/gazeData/currentActivity.ttl";
            string ttlContent = await solidClient.GetFileAsync(activityUrl);
            Debug.Log($"Read current activity from: {activityUrl}");
            return ttlContent;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read current activity: {e.Message}");
            return null;
        }
    }

    public async Task<ActivityInfo> ParseCurrentActivity()
    {
        string ttlContent = await ReadCurrentActivity();
        if (string.IsNullOrEmpty(ttlContent))
        {
            return null;
        }

        try
        {
            ActivityInfo info = new ActivityInfo();

            // Parse person name (foaf:name)
            var nameMatch = System.Text.RegularExpressions.Regex.Match(ttlContent, @"foaf:name ""([^""]+)""");
            if (nameMatch.Success)
            {
                info.PersonName = nameMatch.Groups[1].Value;
            }

            // Parse probability (bm:probability)
            var probMatch = System.Text.RegularExpressions.Regex.Match(ttlContent, @"bm:probability ""([^""]+)""");
            if (probMatch.Success)
            {
                if (float.TryParse(probMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float prob))
                {
                    info.Probability = prob;
                }
            }

            // Parse end time (prov:endedAtTime)
            var timeMatch = System.Text.RegularExpressions.Regex.Match(ttlContent, @"prov:endedAtTime ""([^""]+)""");
            if (timeMatch.Success)
            {
                if (System.DateTime.TryParse(timeMatch.Groups[1].Value, out System.DateTime endTime))
                {
                    info.EndTime = endTime;
                }
            }

            // Parse activity name (schema:name)
            var activityMatch = System.Text.RegularExpressions.Regex.Match(ttlContent, @"schema:name ""([^""]+) action""");
            if (activityMatch.Success)
            {
                info.ActivityName = activityMatch.Groups[1].Value;
            }

            // Display the parsed information
            DisplayActivityInfo(info);

            return info;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse activity TTL: {e.Message}");
            return null;
        }
    }

    private void DisplayActivityInfo(ActivityInfo info)
    {
        if (info == null)
        {
            Debug.LogWarning("No activity information to display");
            return;
        }

        // Display person name
        if (PersonNameText != null)
        {
            PersonNameText.GetComponent<TextMeshProUGUI>().text = $"Person: {info.PersonName}";
        }
        else
        {
            Debug.LogWarning("PersonNameText not assigned in Inspector");
        }

        // Display probability as percentage
        if (ActivityProbabilityText != null)
        {
            var probPercent = info.Probability.ToString("P", System.Globalization.CultureInfo.InvariantCulture);
            ActivityProbabilityText.GetComponent<TextMeshProUGUI>().text = $"Probability: {probPercent}";
        }
        else
        {
            Debug.LogWarning("ActivityProbabilityText not assigned in Inspector");
        }

        // Display end time in readable format
        if (ActivityEndTimeText != null)
        {
            string timeString = info.EndTime.ToString("yyyy-MM-dd HH:mm:ss");
            ActivityEndTimeText.GetComponent<TextMeshProUGUI>().text = $"Ended: {timeString}";
        }
        else
        {
            Debug.LogWarning("ActivityEndTimeText not assigned in Inspector");
        }

        Debug.Log($"Displayed activity info - Person: {info.PersonName}, Activity: {info.ActivityName}, Probability: {info.Probability:P}, End Time: {info.EndTime}");
    }

    // Sharing Interface Methods
    public void ShowSharePanel()
    {
        if (SharePanel != null)
        {
            SharePanel.SetActive(true);
            LoadContacts();
            if (!overlaysFollowCamera)
            {
                ArrangeOverlays();
            }
        }
        else
        {
            Debug.LogWarning("SharePanel not assigned in Inspector");
        }
    }

    public void HideSharePanel()
    {
        if (SharePanel != null)
        {
            SharePanel.SetActive(false);
            if (!overlaysFollowCamera)
            {
                ArrangeOverlays();
            }
        }
    }

    public async void LoadContacts()
    {
        if (!solidAuthenticated || solidClient == null)
        {
            Debug.LogWarning("Solid client not authenticated");
            return;
        }

        try
        {
            // Try to read contacts from FOAF profile
            string profileUrl = $"{solidWebId.Split(new string[] { "/profile/card#me" }, StringSplitOptions.None)[0]}/profile/card";
            string profileData = await solidClient.GetFileAsync(profileUrl);

            List<Contact> contacts = ParseContactsFromFOAF(profileData);
            DisplayContacts(contacts);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load contacts: {e.Message}");
            // Show manual input option
            DisplayManualInputOption();
        }
    }

    private List<Contact> ParseContactsFromFOAF(string foafData)
    {
        List<Contact> contacts = new List<Contact>();

        try
        {
            // Simple parsing for FOAF knows relationships
            var knowsMatches = System.Text.RegularExpressions.Regex.Matches(foafData, @"<([^>]+)> <http://xmlns.com/foaf/0.1/knows> <([^>]+)>");

            foreach (System.Text.RegularExpressions.Match match in knowsMatches)
            {
                string personUri = match.Groups[2].Value;
                if (personUri.Contains("profile/card#me"))
                {
                    // This is a reference to another person's profile
                    string webId = personUri.Replace("/profile/card#me", "/profile/card#me");
                    contacts.Add(new Contact { Name = "Colleague", WebId = webId });
                }
            }

            // If no contacts found, add some default colleagues
            if (contacts.Count == 0)
            {
                contacts.Add(new Contact { Name = "Alice", WebId = "https://example.com/alice/profile/card#me" });
                contacts.Add(new Contact { Name = "Bob", WebId = "https://example.com/bob/profile/card#me" });
                contacts.Add(new Contact { Name = "Charlie", WebId = "https://example.com/charlie/profile/card#me" });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse FOAF contacts: {e.Message}");
        }

        return contacts;
    }

    private void DisplayContacts(List<Contact> contacts)
    {
        if (ContactListContent == null || ContactButtonPrefab == null)
        {
            Debug.LogWarning("Contact list UI elements not assigned");
            return;
        }

        // Clear existing contacts
        foreach (UnityEngine.Transform child in ContactListContent.transform)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }

        // Create contact buttons
        foreach (Contact contact in contacts)
        {
            GameObject contactButton = UnityEngine.Object.Instantiate(ContactButtonPrefab, ContactListContent.transform);
            SetButtonLabel(contactButton, contact.Name);

            // Add click handler
            contactButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                SelectContact(contact);
            });
        }
    }

    private void DisplayManualInputOption()
    {
        if (ContactListContent == null || ContactButtonPrefab == null)
        {
            Debug.LogWarning("Contact list UI elements not assigned");
            return;
        }

        // Clear existing contacts
        foreach (UnityEngine.Transform child in ContactListContent.transform)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }

        // Create manual input option
        GameObject manualButton = UnityEngine.Object.Instantiate(ContactButtonPrefab, ContactListContent.transform);
        SetButtonLabel(manualButton, "Enter WebID Manually");
        manualButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
        {
            ShowManualInput();
        });
    }

    private void SetButtonLabel(GameObject button, string label)
    {
        if (button == null)
        {
            Debug.LogWarning("SetButtonLabel called with null button reference.");
            return;
        }

        var tmpUGUI = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpUGUI != null)
        {
            tmpUGUI.text = label;
            return;
        }

        var tmp = button.GetComponentInChildren<TextMeshPro>(true);
        if (tmp != null)
        {
            tmp.text = label;
            return;
        }

        var legacyText = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (legacyText != null)
        {
            legacyText.text = label;
            return;
        }

        Debug.LogWarning("ContactButtonPrefab is missing a text component. Please add TextMeshProUGUI or TextMeshPro.");
    }

    private void SelectContact(Contact contact)
    {
        if (WebIdInputField != null)
        {
            WebIdInputField.GetComponent<TMPro.TMP_InputField>().text = contact.WebId;
        }
        Debug.Log($"Selected contact: {contact.Name} ({contact.WebId})");
    }

    private void ShowManualInput()
    {
        if (WebIdInputField != null)
        {
            WebIdInputField.SetActive(true);
            WebIdInputField.GetComponent<TMPro.TMP_InputField>().text = "";
        }
    }

    public async void GrantAccess()
    {
        if (!solidAuthenticated || solidClient == null)
        {
            Debug.LogWarning("Solid client not authenticated");
            return;
        }

        string targetWebId = "";
        string accessType = "Read";

        // Get WebID from input field
        if (WebIdInputField != null)
        {
            targetWebId = WebIdInputField.GetComponent<TMPro.TMP_InputField>().text;
        }

        // Get access type from dropdown
        if (AccessTypeDropdown != null)
        {
            accessType = AccessTypeDropdown.GetComponent<TMPro.TMP_Dropdown>().options[
                AccessTypeDropdown.GetComponent<TMPro.TMP_Dropdown>().value].text;
        }

        if (string.IsNullOrEmpty(targetWebId))
        {
            Debug.LogWarning("No WebID specified");
            return;
        }

        try
        {
            string activityFileUrl = $"{solidWebId.Split(new string[] { "/profile/card#me" }, StringSplitOptions.None)[0]}/gazeData/currentActivity.ttl";

            await solidClient.GrantAccessToFile(activityFileUrl, targetWebId, accessType);
            Debug.Log($"Granted {accessType} access to {activityFileUrl} for {targetWebId}");

            // Hide share panel
            HideSharePanel();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to grant access: {e.Message}");
        }
    }

    public async void RevokeAccess()
    {
        if (!solidAuthenticated || solidClient == null)
        {
            Debug.LogWarning("Solid client not authenticated");
            return;
        }

        string targetWebId = "";

        // Get WebID from input field
        if (WebIdInputField != null)
        {
            targetWebId = WebIdInputField.GetComponent<TMPro.TMP_InputField>().text;
        }

        if (string.IsNullOrEmpty(targetWebId))
        {
            Debug.LogWarning("No WebID specified");
            return;
        }

        try
        {
            string activityFileUrl = $"{solidWebId.Split(new string[] { "/profile/card#me" }, StringSplitOptions.None)[0]}/gazeData/currentActivity.ttl";

            await solidClient.RevokeAccessToResource(activityFileUrl, targetWebId);
            Debug.Log($"Revoked access to {activityFileUrl} for {targetWebId}");

            // Hide share panel
            HideSharePanel();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to revoke access: {e.Message}");
        }
    }

    public async void ShowCurrentAccess()
    {
        if (!solidAuthenticated || solidClient == null)
        {
            Debug.LogWarning("Solid client not authenticated");
            return;
        }

        try
        {
            string activityFileUrl = $"{solidWebId.Split(new string[] { "/profile/card#me" }, StringSplitOptions.None)[0]}/gazeData/currentActivity.ttl";

            var accesses = await solidClient.GetAccessesFromResource(activityFileUrl);

            Debug.Log("Current access permissions for activity file:");
            foreach (var access in accesses)
            {
                string permissions = string.Join(", ", access.Value);
                Debug.Log($"{access.Key}: {permissions}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get current access: {e.Message}");
        }
    }

    private string GetActivityTypeUri(string activity)
    {
        switch (activity.ToLower())
        {
            case "reading":
                return "https://schema.org/ReadAction";
            case "inspection":
                return "https://schema.org/CheckAction";
            case "search":
                return "https://schema.org/SearchAction";
            default:
                return "https://schema.org/Action";
        }
    }

    private string GenerateCurrentActivityTtl(string activity, float probability, string activityTypeUri)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string gazeDataUrl = $"{solidWebId.Split(new string[] { "/profile/card#me" }, StringSplitOptions.None)[0]}/gazeData/my_gaze_data.csv";
        string activityUrl = $"{solidWebId.Split(new string[] { "/profile/card#me" }, StringSplitOptions.None)[0]}/gazeData/currentActivity.ttl";

        return $@"@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .
@prefix foaf: <http://xmlns.com/foaf/0.1/> .
@prefix prov: <http://www.w3.org/ns/prov#> .
@prefix schema: <https://schema.org/> .
@prefix bm: <http://bimerr.iot.linkeddata.es/def/occupancy-profile#> .

<{activityUrl}> a prov:Activity, {activityTypeUri} ;
    schema:name ""{activity} action""^^xsd:string ;
    prov:wasAssociatedWith <{solidWebId}> ;
    prov:used <{gazeDataUrl}> ;
    prov:endedAtTime ""{timestamp}""^^xsd:dateTime ;
    bm:probability ""{probability.ToString(CultureInfo.InvariantCulture)}""^^xsd:float .

<{solidWebId}> a foaf:Person, prov:Agent ;
    foaf:name ""Dominik"" ;
    foaf:mbox <mailto:dominik@ubicomp2025.unisg.ch> .";
    }

    public void ShowDebugLogContainer()
    {
        DebugLogContainer.SetActive(true);
    }

    public void HideDebugLogContainer()
    {
        DebugLogContainer.SetActive(false);
    }

    public async void DisplayStoredActivity()
    {
        Debug.Log("Reading and displaying stored activity from Solid pod...");
        await ParseCurrentActivity();
    }

    // Test method to verify parsing with sample data
    public void TestActivityParsing()
    {
        string sampleTtl = @"@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .
@prefix foaf: <http://xmlns.com/foaf/0.1/> .
@prefix prov: <http://www.w3.org/ns/prov#> .
@prefix schema: <https://schema.org/> .
@prefix bm: <http://bimerr.iot.linkeddata.es/def/occupancy-profile#> .

<> a prov:Activity, schema:ReadAction ;
    schema:name ""Reading action""^^xsd:string ;
    prov:wasAssociatedWith <https://wiser-solid-xi.interactions.ics.unisg.ch/dominik-ubicomp2025/profile/card#me> ;
    prov:used <https://wiser-solid-xi.interactions.ics.unisg.ch/dominik-ubicomp2025/gazeData/my_gaze_data.csv> ;
    prov:endedAtTime ""2025-10-19T14:30:00Z""^^xsd:dateTime ;
    bm:probability ""0.85""^^xsd:float .

<https://wiser-solid-xi.interactions.ics.unisg.ch/dominik-ubicomp2025/profile/card#me> a foaf:Person, prov:Agent ;
    foaf:name ""Dominik"" ;
    foaf:mbox <mailto:dominik@ubicomp2025.unisg.ch> .";

        try
        {
            ActivityInfo info = new ActivityInfo();

            // Parse person name (foaf:name)
            var nameMatch = System.Text.RegularExpressions.Regex.Match(sampleTtl, @"foaf:name ""([^""]+)""");
            if (nameMatch.Success)
            {
                info.PersonName = nameMatch.Groups[1].Value;
            }

            // Parse probability (bm:probability)
            var probMatch = System.Text.RegularExpressions.Regex.Match(sampleTtl, @"bm:probability ""([^""]+)""");
            if (probMatch.Success)
            {
                if (float.TryParse(probMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float prob))
                {
                    info.Probability = prob;
                }
            }

            // Parse end time (prov:endedAtTime)
            var timeMatch = System.Text.RegularExpressions.Regex.Match(sampleTtl, @"prov:endedAtTime ""([^""]+)""");
            if (timeMatch.Success)
            {
                if (System.DateTime.TryParse(timeMatch.Groups[1].Value, out System.DateTime endTime))
                {
                    info.EndTime = endTime;
                }
            }

            // Parse activity name (schema:name)
            var activityMatch = System.Text.RegularExpressions.Regex.Match(sampleTtl, @"schema:name ""([^""]+) action""");
            if (activityMatch.Success)
            {
                info.ActivityName = activityMatch.Groups[1].Value;
            }

            Debug.Log($"Test parsing successful - Person: {info.PersonName}, Activity: {info.ActivityName}, Probability: {info.Probability:P}, End Time: {info.EndTime}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Test parsing failed: {e.Message}");
        }
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
        else if (command.ToLower().Contains("share activity") || command.ToLower().Contains("share"))
        {
            ShowSharePanel();
        }
        else if (command.ToLower().Contains("hide share") || command.ToLower().Contains("close share"))
        {
            HideSharePanel();
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

    private void LateUpdate()
    {
        if (autoArrangeOverlays && overlaysFollowCamera)
        {
            ArrangeOverlays();
        }

        RestoreTextOrientations();
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

    private async void ReceiveNewActivity(string activity, float probability)
    {
        Debug.Log($"Displaying new activity: {activity}");
        DebugText.text += $"\n[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}]";
        DebugText.text += $"\nDisplaying new activity: {activity}";
        string probPercent = probability.ToString("P", CultureInfo.InvariantCulture);
        ActivityText.GetComponent<TextMeshPro>().text = $"{activity} ({probPercent}).";
        string suggestion = "";

        // --- Deactivate all special features by default ---
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
        isInspectionActivity = false;
        isReadingActivity = false;
        if (CountdownText != null)
        {
            CountdownText.SetActive(false);
        }
        if (ChecklistPanel != null)
        {
            ChecklistPanel.SetActive(false);
        }

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
                break;
            case "Inspection":
                suggestion = "Inspection checklist activated. Say 'Check' or tap to mark items complete.";
                // Start Inspection Checklist
                isInspectionActivity = true;
                currentChecklistIndex = 0;
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
                break;
            case "Search":
                suggestion = "Let's search this area systematically. First, search the area around marker 1.";
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
                break;
            default:
                // No specific suggestion beyond default view
                break;
        }

        SuggestionText.GetComponent<TextMeshPro>().text = suggestion;

        await SaveCurrentActivity(activity, probability);

        if (!string.IsNullOrEmpty(gazeDataCsv))
        {
            await SaveGazeData(gazeDataCsv);
        }
    }

    private void CacheTextOrientations()
    {
        if (textRotationsCached)
        {
            return;
        }

        CacheTextRotationsFromRoot(ActivityNotifyContainer);
        CacheTextRotationsFromRoot(ActivityText);
        CacheTextRotationsFromRoot(SuggestionText);
        CacheTextRotationsFromRoot(PersonNameText);
        CacheTextRotationsFromRoot(ActivityProbabilityText);
        CacheTextRotationsFromRoot(ActivityEndTimeText);
        CacheTextRotationsFromRoot(CountdownText);
        CacheTextRotationsFromRoot(ChecklistPanel);
        CacheTextRotationsFromRoot(SharePanel);
        CacheTextRotationsFromRoot(DebugLogContainer);

        if (SearchMarkers != null)
        {
            foreach (GameObject marker in SearchMarkers)
            {
                CacheTextRotationsFromRoot(marker);
            }
        }

        textRotationsCached = true;
    }

    private void RestoreTextOrientations()
    {
        if (!textRotationsCached)
        {
            CacheTextOrientations();
        }

        foreach (KeyValuePair<Transform, Quaternion> kvp in textInitialLocalRotations)
        {
            if (kvp.Key != null)
            {
                kvp.Key.localRotation = kvp.Value;
            }
        }
    }

    private void CacheTextRotationsFromRoot(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        foreach (TextMeshProUGUI tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            textInitialLocalRotations[tmp.transform] = tmp.transform.localRotation;
        }

        foreach (TextMeshPro tmp in root.GetComponentsInChildren<TextMeshPro>(true))
        {
            textInitialLocalRotations[tmp.transform] = tmp.transform.localRotation;
        }
    }
    // Test method to verify sharing functionality
    public async void TestSharing()
    {
        Debug.Log("Testing sharing functionality...");

        // Test contact loading
        LoadContacts();

        // Test current access display
        ShowCurrentAccess();

        Debug.Log("Sharing test completed. Check console for results.");
    }
}
