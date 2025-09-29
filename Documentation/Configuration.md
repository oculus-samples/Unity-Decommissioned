# Project Configuration

To make this project functional in both the editor and on a device, some initial setup is required.

## Application Configuration

To run the project and use platform services, create an application on the [Meta Quest Developer Center](https://developers.meta.com/horizon/).

For device operation, you need a Quest application. For editor operation, you need a Rift application. The following sections describe the necessary configuration for the application to run.

### Data Use Checkup

To use platform features, request the necessary data for the application in the _Data Use Checkup_ section.

<img alt="Data use Checkup" src="./Media/dashboard/datausecheckup.png" height="400px" />

Configure the required Data Usage:

* **User Id**: Avatars, Destinations, Multiplayer, Oculus Username, Friends Invites, User Invite Others
* **User Profile**: Avatars
* **Avatars**: Avatars
* **Deep Linking**: Destinations
* **Friends**: Multiplayer
* **Invites**: Multiplayer, Friends Invite, User Invite Others

### Destinations

This application uses Destination configuration to let users invite friends to the same arenas and launch the application together.

First, open the Destinations under the Engagement section:

![Platform Services Destinations](./Media/dashboard/dashboard_destinations_platformservices.png "Platform Services Destinations")

Then, set up the different destinations. Here is a table for destination settings:

<table>
<tr>
    <th>Destination</th>
    <th>API Name</th>
    <th>Deeplink Type</th>
</tr>
<tr>
  <td>Main Menu</td>
 <td>main-menu</td>
 <td>DISABLED</td>
</tr>
<tr>
  <td>Lobby</td>
 <td>Lobby</td>
 <td>ENABLED</td>
</tr>
<tr>
  <td>In Game</td>
 <td>in-game</td>
 <td>DISABLED</td>
</tr>
</table>

### Set the Application ID

Set the application ID in your Unity project.

Find the identifier (**App ID**) in the _API_ section.

![Application API](./Media/dashboard/dashboard_api.png "Application API")

Place it in [Assets/Resources/OculusPlatformSettings.asset](../Assets/Resources/OculusPlatformSettings.asset).

![Oculus Platform Settings Menu](./Media/editor/oculusplatformsettings_menu.png "Oculus Platform Settings Menu")

![Oculus Platform Settings](./Media/editor/oculusplatformsettings.png "Oculus Platform Settings")

## Photon Configuration

To get the sample working, configure Photon with your account and applications. The Photon base plan is free.
- Visit [photonengine.com](https://www.photonengine.com) and [create an account](https://doc.photonengine.com/realtime/current/getting-started/obtain-your-app-id).
- From your Photon dashboard, click "Create A New App."
  - Create 2 apps: "Realtime" and "Voice."
- Fill out the form, setting the type to "Photon Realtime," then click Create.
- Fill out the form, setting the type to "Photon Voice," then click Create.

Your new app will now appear on your Photon dashboard. Click the App ID to reveal the full string and copy the value for each app.

Open your Unity project and paste your Realtime App ID in [Assets/Photon/Resources/PhotonAppSettings.asset](../Assets/Photon/Resources/PhotonAppSettings.asset).

![Photon App Settings Location](./Media/editor/photonappsettings_location.png "Photon App Settings Location")

![Photon App Settings](./Media/editor/photonappsettings.png "Photon App Settings")

Set the Voice App ID on the [Assets/Decommissioned/Prefabs/Audio/VoipRecorder.prefab](../Assets/Decommissioned/Prefabs/Audio/VoipRecorder.prefab).

![Photon Voice Settings](./Media/editor/photonvoicesetting.png "Photon Voice Settings")

## Upload to Release Channel

To use platform features, upload an initial build to a release channel. For instructions, visit the [developer center](https://developers.meta.com/horizon/resources/publish-release-channels-upload/). To test with other users, add them to the channel. More information is available in the [Add Users to Release Channel](https://developers.meta.com/horizon/resources/publish-release-channels-add-users/) topic.

Once the initial build is uploaded, you can use any development build with the same application ID. There's no need to upload every build to test local changes.
