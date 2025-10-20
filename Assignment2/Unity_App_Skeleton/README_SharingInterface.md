# Solid Pod Activity Sharing Interface

## Overview
The Unity app now includes a comprehensive sharing interface that allows you to grant access to your `gazeData/currentActivity.ttl` resource to colleagues. The interface features:

- **Contact Management**: Reads contacts from your FOAF profile or provides manual input
- **Access Control**: Grant Read, Write, or ReadWrite permissions
- **Visual Interface**: Mixed Reality UI for easy interaction

## UI Setup Requirements

### Required UI Elements (Add to Unity Scene)
1. **SharePanel**: Main panel containing all sharing controls
2. **WebIdInputField**: TMP_InputField for entering WebID manually
3. **AccessTypeDropdown**: TMP_Dropdown with options: Read, Write, ReadWrite
4. **ContactListPanel**: Panel to display available contacts
5. **ContactListContent**: Scrollable content area for contact buttons
6. **ContactButtonPrefab**: Button prefab for each contact (with TextMeshProUGUI child)

### Inspector Assignments
In the ActivityReceiver component, assign:
- `SharePanel`: The main sharing panel GameObject
- `WebIdInputField`: The WebID input field
- `AccessTypeDropdown`: The access type dropdown
- `ContactListPanel`: The contact list panel
- `ContactListContent`: The scrollable content area
- `ContactButtonPrefab`: The contact button prefab

## Usage

### Opening the Share Interface
Call `activityReceiver.ShowSharePanel()` from a button or voice command.

### Granting Access
1. Select a contact from the list or enter a WebID manually
2. Choose access type (Read/Write/ReadWrite)
3. Click "Grant Access" button

### Revoking Access
1. Enter the WebID of the person to revoke access from
2. Click "Revoke Access" button

### Viewing Current Permissions
Click "Show Current Access" to log current permissions to the console.

## Contact Management

### Automatic Contact Loading
The app attempts to read contacts from your FOAF profile at:
```
{your-pod-url}/profile/card
```

It looks for `foaf:knows` relationships and extracts WebIDs.

### Fallback Contacts
If no FOAF contacts are found, the app provides sample contacts:
- Alice
- Bob  
- Charlie

### Manual WebID Entry
Users can always enter WebIDs manually using the input field.

## Technical Implementation

### Methods Added
- `ShowSharePanel()`: Displays the sharing interface
- `LoadContacts()`: Reads contacts from FOAF profile
- `GrantAccess()`: Grants access to the activity file
- `RevokeAccess()`: Revokes access from a WebID
- `ShowCurrentAccess()`: Displays current access permissions

### Access Types
- **Read**: Allows viewing the activity data
- **Write**: Allows modifying the activity data
- **ReadWrite**: Full read and write access

### File Access Control
The sharing controls access to:
```
{your-pod-url}/gazeData/currentActivity.ttl
```

This file contains your classified activity information in semantic TTL format.

## Integration with Voice Commands

Add voice command handling in `OnVoiceCommand()`:
```csharp
if (command.ToLower().Contains("share activity"))
{
    ShowSharePanel();
}
```

## Security Considerations

- Only grant access to trusted colleagues
- Use Read-only access when possible
- Regularly review granted permissions using "Show Current Access"
- Access can be revoked at any time

## Troubleshooting

### Contacts Not Loading
- Check that your FOAF profile exists and contains `foaf:knows` relationships
- Manual WebID entry is always available as fallback

### Access Granting Fails
- Verify the target WebID is valid
- Ensure you have Control permissions on the resource
- Check Solid pod connectivity

### UI Elements Not Working
- Verify all UI elements are assigned in the Inspector
- Ensure TextMeshProUGUI components are present on text objects
- Check that buttons have the correct OnClick handlers</content>
<parameter name="filePath">d:\projects\2025-HS-MCS-UbiComp-Public\Assignment2\Unity_App_Skeleton\README_SharingInterface.md