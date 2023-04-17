# Player Armbands

## Implementation Overview
In this project, we wanted to have a diegetic way for the user to adjust their settings and do other tasks that would traditionally go into a user menu. We decided to implement an armband that would programmatically adjust itself to fit on any user's avatar and contain any settings that the player may need. This armband keeps all of the user settings in an interactable object on the user's avatar without using UI menus, keeping the user immersed in the experience.

## Involved Scripts

- [PlayerArmband.cs](../Assets/Decommissioned/Scripts/Player/PlayerArmband.cs)
  - This script handles the functionality of the armband. It scales itself to the user's avatar and handles all interactions with the settings contained within the armband.
- [ExternallyScaledObject.cs](../Assets/Decommissioned/Scripts/Utilities/ExternallyScaledObject.cs)
  - This script handles the scaling of the straps on the armband. It uses external transforms to scale the strap to always touch the given position.
- [AvatarMeshCache.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarMeshUtils/AvatarMeshCache.cs)
  - The Avatar Mesh Cache script handles data from the user's avatar. It records the vertices of the avatar as well as the avatar's skeleton and vertex weights with the skeleton. This is a key component to scaling the armband to the user's arm.
- [AvatarMeshQuery.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarMeshUtils/AvatarMeshQuery.cs)
  - This script contains many methods that help a developer get values from an avatar mesh. This script can get mesh vertices, joint transforms, and calculate points along the arm plane for positioning the armband.
- [AvatarMeshArmInfo.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarMeshUtils/AvatarMeshArmInfo.cs)
  - This script contains many extension methods for AvatarMeshQuery to help the armband script grab proper values from the user's arm mesh in order for the armband to scale to the arm.

## How We Use The Avatar Mesh
Let's see the order of operations on how the mesh is captured and used for the armband:

- First, the AvatarMeshCache listens to an event called __OnAvatarMeshLoaded__ that fires when an avatar's mesh is loaded by the OvrAvatarManager. This event gives us the __OvrAvatarPrimitive__ that we can use to get info from this avatar mesh such as bone weights, flags, and so on. We use this primitive to get the bone weights. This event also gives us the MeshData for this loaded avatar, which is what we use to get the vertices of the mesh. This data is stored in an internal dictionary for access later.
- Next, the user and their armband is created, which then attempts to scale itself with the avatar mesh. If the user has no avatar, then the armband will use default scale values that are catered to the default grey avatar. Otherwise, the armband disables the user's avatar tracking for a frame while it requests arm mesh details using the __AvatarMeshArmInfo__ script. It uses the upper and lower points of the arm and the arm's radius at two different places along the arm.
  - The upper points along the arm and the radius are used to position and rotate the entire armband to match the user's arm positioning and curvature, as many avatars have clothing that extend to the wrist while other do not. The lower points along the arm are used to scale the straps to the bottom of the arm without them clipping into the arm or being too big for the arm.
- It then saves the previous values into NetworkVariables that are then synced to other clients and updated on their game to reflect the new scale and positioning. The user's avatar tracking is then re-enabled.

### Why Is The Avatar's Tracking Disabled During Scaling?
For a while the avatar's tracking was not disabled during scaling and eventually we noticed inconsistencies with scaling. For example, if the user held their hand straight up while their armband was scaling, the armband would be incorrectly scaled for the rest of the game. Turning off the tracking for the avatar will force them into bind pose, which is the pose the avatar's mesh is captured in as well. Keeping these two things consistent removed the previous issue and allowed the scaling to line up with the mesh measurements.

## Armband Interactions
The armband contains two different kinds interactions that house the different player settings and actions that would traditionally be in a flat 2D menu:

- __Buttons__
  - Buttons are used for actions that the player can activate such as opening and closing the armband shutters (similar to using a keyboard key to open a pause menu), opening and closing the help tablets located at each station, and leaving the game.
- __Sliders__
  - Sliders are used to control volumes of different sound channels. The armband contains sliders for Music, Sound, and Voice.

### Armband Shutters
The armband shutters indicate when the armband is open or closed, or rather "activated" and "deactivated". When the user presses the open button, the shutters animate open and the interactions contained within the armband are enabled. When the user presses the button while the shutters are open, they will animate closed and all interactions are disabled. The state of the shutters are networked using a NetworkVariable so that other clients may see the shutters opening and closing. Interactions are disabled when the armband is closed to prevent accidental button presses when the user is playing the game. Otherwise, a user might accidentally press the leave game button when it wasn't intended.

The leave game button is disabled once the game starts in order to prevent users from purposefully leaving the game mid-match which will degrade the state of the game for the other players.

### How Do The Sliders Work?
The sliders on the armband work by locking the local position of the slider on the Y and Z axis and allowing the user to move it along the X axis, stopping it at a certain point. The position along the X axis is the value that the slider uses to set the volume. Once the user lets go of the slider, the setting it controls is saved into Unity PlayerPrefs. The setting is loaded when the armband is loaded, and the slider is set to the proper position.

## The Challenge Of Interactables On The Arm
Having an interactable object on the user's virtual arm posed many challenges. When a user is trying to move a slider on their arm with hand tracking, naturally this obscures the users other hand and causes tracking issues. To combat this, when grabbing a slider it will lock the user's wrist pose for the duration of them holding on to the slider to aid in hand tracking accuracy and to keep the arm steady while the user adjusts their setting.
