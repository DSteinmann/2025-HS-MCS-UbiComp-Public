# Solid Pod Activity Reader

This project demonstrates reading classified activity data from a Solid pod using semantic web technologies.

## Features

### Unity App (ActivityReceiver.cs)
- **Automatic Storage**: Saves activity classifications and gaze data to Solid pod when detected
- **Manual Reading**: Button to read and display stored activity information from the pod
- **UI Display**: Shows person name, activity probability, and end time

### Standalone Reader (SolidActivityReader.cs)
- **Command-line Tool**: Reads activity data from Solid pod without Unity
- **Parsed Output**: Displays structured information about the stored activity

# Solid Pod Activity Reader

This project demonstrates reading classified activity data from a Solid pod using semantic web technologies and includes a comprehensive sharing interface.

## Features

### Unity App (ActivityReceiver.cs)
- **Automatic Storage**: Saves activity classifications and gaze data to Solid pod when detected
- **Manual Reading**: Button to read and display stored activity information from the pod
- **Sharing Interface**: Grant/revoke access to activity data for colleagues
- **Contact Management**: Load contacts from FOAF profile or manual WebID entry
- **UI Display**: Shows person name, activity probability, and end time

### Standalone Reader (SolidActivityReader.cs)
- **Command-line Tool**: Reads activity data from Solid pod without Unity
- **Parsed Output**: Displays structured information about the stored activity

## Unity App Setup

### Required UI Elements (assign in Inspector)
1. **Activity Display**:
   - `PersonNameText`: TextMeshProUGUI for person name
   - `ActivityProbabilityText`: TextMeshProUGUI for probability
   - `ActivityEndTimeText`: TextMeshProUGUI for end time

2. **Sharing Interface**:
   - `SharePanel`: Main sharing panel GameObject
   - `WebIdInputField`: TMP_InputField for WebID entry
   - `AccessTypeDropdown`: TMP_Dropdown (auto-populated with Read/Write/ReadWrite)
   - `ContactListPanel`: Panel for contact display
   - `ContactListContent`: Scrollable content area for contacts
   - `ContactButtonPrefab`: Button prefab for each contact

### Credentials Setup
Set in ActivityReceiver Inspector:
- `solidEmail`: Your Solid pod email
- `solidPassword`: Your Solid pod password

## Usage

### Reading Activities
- Activities are automatically saved when detected
- Call `DisplayStoredActivity()` to read and display stored activity

### Sharing Activities
- Call `ShowSharePanel()` to open sharing interface
- Voice commands: "share activity", "hide share"
- Select contacts from FOAF profile or enter WebID manually
- Choose access type (Read/Write/ReadWrite) and grant permissions
- View current access permissions with `ShowCurrentAccess()`

### Standalone Reader
Run the PowerShell script:
```powershell
.\ReadSolidActivity.ps1 -Email "your@email.com" -Password "yourpassword"
```

Or run directly:
```bash
dotnet run --project SolidActivityReader.csproj <serverUrl> <webId> <email> <password>
```

## Data Format

Activities are stored in Turtle (TTL) format using:
- **PROV Ontology**: For activity provenance and temporal information
- **FOAF Ontology**: For person/agent identification
- **Schema.org**: For activity type classification
- **Custom BIMERR**: For probability metadata

Example stored data:
```turtle
@prefix xsd: <http://www.w3.org/2001/XMLSchema#> .
@prefix foaf: <http://xmlns.com/foaf/0.1/> .
@prefix prov: <http://www.w3.org/ns/prov#> .
@prefix schema: <https://schema.org/> .

<> a prov:Activity, schema:ReadAction ;
    schema:name "Reading action"^^xsd:string ;
    prov:wasAssociatedWith <https://example.com/profile/card#me> ;
    prov:endedAtTime "2025-10-19T10:30:00Z"^^xsd:dateTime ;
    bm:probability "0.85"^^xsd:float .

<https://example.com/profile/card#me> a foaf:Person, prov:Agent ;
    foaf:name "Dominik"^^xsd:string ;
    foaf:mbox <mailto:dominik@ubicomp2025.unisg.ch> .
```</content>
<parameter name="filePath">d:\projects\2025-HS-MCS-UbiComp-Public\Assignment2\Unity_App_Skeleton\README_SolidActivity.md