# Project Configuration
In order for this project to be functional in editor and on device there is some initial setup that needs to be done.

## Application Configuration
In order to run the project and use the platform services we need to create an application on the [Meta Quest Developer Center](https://developers.meta.com/horizon/).

To run on device you will need a Quest application, and to run in editor you will need a Rift application. The following sections will describe the configuration required for the application to run.

### Data Use Checkup
To use the features from the Platform we need to request which kind of data is required for the application. This can be found in the _Data Use Checkup_ section of the application.

<img alt="Data use Checkup" src="./Media/dashboard/datausecheckup.png" height="400px" />

And configure the required Data Usage:

* **User Id**: Avatars, Destinations, Multiplayer, Oculus Username, Friends Invites, User Invite Others
* **User Profile**: Avatars
* **Avatars**: Avatars
* **Deep Linking**: Destinations
* **Friends**: Multiplayer
* **Invites**: Multiplayer, Friends Invite, User Invite Others

### Destinations
This application uses Destination configuration to enable users to invite friends in the same arenas and launch the application together.

First we need to open the Destinations from the Platform Services:

<img alt="Platform Services Destinations" src="./Media/dashboard/dashboard_destinations_platformservices.png" width="400px" />

Then we need to setup the different destinations. Here is a table for destinations settings:

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
We then need to the set the application ID in our project in Unity.

The identifier (**App ID**) can be found in the _API_ section.

<img alt="Application API" src="./Media/dashboard/dashboard_api.png" width="400px" />

Then it needs to be placed in the [OculusPlatformSettings](../Assets/Resources/OculusPlatformSettings.asset).

<img alt="Oculus Platform Settings Menu" src="./Media/editor/oculusplatformsettings_menu.png" width="400px" />

<img alt="Oculus Platform Settings" src="./Media/editor/oculusplatformsettings.png" width="400px" />

## Photon Configuration

To get the sample working, you will need to configure Photon with your own account and applications. The Photon base plan is free.

* Visit [photonengine.com](https://www.photonengine.com) and [create an account](https://doc.photonengine.com/en-us/realtime/current/getting-started/obtain-your-app-id)
* From your Photon dashboard, click "Create A New App"
  * We will create 2 apps, "Realtime" and "Voice"
* First fill out the form making sure to set type to "Photon Realtime". Then click Create.
* Second fill out the form making sure to set type to "Photon Voice". Then click Create.

Your new app will now show on your Photon dashboard. Click the App ID to reveal the full string and copy the value for each app.

Open your unity project and paste your Realtime App ID in [PhotonAppSettings](../Assets/Photon/Resources/PhotonAppSettings.asset).

<table>
 <tr>
  <td>
   <img alt="Photon App Settings Location" src="./Media/editor/photonappsettings_location.png" width="400px" />
  </td>
  <td>
   <img alt="Photon App Settings" src="./Media/editor/photonappsettings.png" width="400px" />
  </td>
 </tr>
</table>

Set the Voice App Id on the [VoiceRecorder](../Assets/Decommissioned/Prefabs/Audio/VoipRecorder.prefab) prefab:

<img alt="Photon Voice Settings" src="./Media/editor/photonvoicesetting.png" width="400px" />

## Upload to release channel
To use the platform features, you will first need to upload an initial build to a release channel.
For instructions you can go to the [developer center](https://developers.meta.com/horizon/resources/publish-release-channels-upload/). Then to be able to test with other users you will need to add them to the channel, more information in the [Add Users to Release Channel](https://developers.meta.com/horizon/resources/publish-release-channels-add-users/) topic.

Once the initial build is uploaded you will be able to use any development build with the same application Id, no need to upload every build to test local changes.
