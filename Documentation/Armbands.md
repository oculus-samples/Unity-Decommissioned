# Player Armbands

## Implementation Overview

This project introduces a diegetic method for users to adjust settings and perform tasks typically found in a user menu. We implemented an armband that automatically fits any user's avatar and contains necessary settings. This armband integrates user settings into an interactable object on the avatar, eliminating traditional UI menus and enhancing user immersion.

## Involved Scripts

- **[PlayerArmband.cs](../Assets/Decommissioned/Scripts/Player/PlayerArmband.cs):** Manages armband functionality, scaling to the user's avatar, and handling interactions with settings.
- **[ExternallyScaledObject.cs](../Assets/Decommissioned/Scripts/Utilities/ExternallyScaledObject.cs):** Manages the scaling of armband straps, using external transforms to scale the strap to always touch the given position.
- **[AvatarMeshCache.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarMeshUtils/AvatarMeshCache.cs):** Handles avatar data, recording vertices, skeleton, and vertex weights, crucial for armband scaling.
- **[AvatarMeshQuery.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarMeshUtils/AvatarMeshQuery.cs):** Provides methods to retrieve values from an avatar mesh, including vertices, joint transforms, and arm plane points for armband positioning.
- **[AvatarMeshArmInfo.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarMeshUtils/AvatarMeshArmInfo.cs):** Offers extension methods for AvatarMeshQuery to obtain accurate arm mesh values for armband scaling.

## How We Use The Avatar Mesh

Here's how the mesh is captured and used for the armband:

- **AvatarMeshCache** listens for the __OnAvatarMeshLoaded__ event, triggered when an avatar's mesh loads via OvrAvatarManager. This event provides the __OvrAvatarPrimitive__, which offers bone weights and mesh data, stored internally for later access.
- The user and armband are created, attempting to scale with the avatar mesh. If no avatar exists, default scale values for a grey avatar are used. Otherwise, the armband disables avatar tracking temporarily to request arm mesh details via __AvatarMeshArmInfo__. It uses upper and lower arm points and arm radius at two locations.
  - Upper arm points and radius position and rotate the armband to match arm positioning and curvature. Lower arm points scale the straps to fit without clipping or being oversized.
- Previous values are saved into NetworkVariables, synced to other clients, and updated to reflect new scale and positioning. Avatar tracking is then re-enabled.

### Why Is The Avatar's Tracking Disabled During Scaling?

Initially, avatar tracking wasn't disabled during scaling, causing inconsistencies. For example, if a user raised their hand while scaling, the armband would misalign. Disabling tracking forces the avatar into bind pose, aligning with mesh capture and resolving scaling issues.

## Armband Interactions

The armband features two interaction types for player settings and actions, replacing a flat 2D menu:

- **Buttons:** Trigger actions like opening/closing armband shutters (similar to using a keyboard key to open a pause menu), accessing help tablets, and leaving the game.
- **Sliders:** Control sound channel volumes, including Music, Sound, and Voice.

### Armband Shutters

Armband shutters indicate whether the armband is "activated" or "deactivated." Pressing the open button animates the shutters open, enabling interactions. Pressing again closes them, disabling interactions. Shutter states are networked via a NetworkVariable, allowing other clients to see changes. Interactions are disabled when closed to prevent accidental button presses, such as unintentionally leaving the game. The leave game button is disabled once the game starts to prevent mid-match exits to avoid degrading the state of the game for the other players.

### How Do The Sliders Work?

Armband sliders lock local Y and Z positions, allowing movement along the X axis, stopping it at a certain point. The X position sets the volume. Once released, the setting is saved in Unity PlayerPrefs and loaded when the armband initializes, setting the slider to the correct position.

## The Challenge Of Interactables On The Arm

Placing interactable objects on the user's virtual arm presents challenges. Moving a slider with hand tracking can obscure the other hand, causing tracking issues. To address this, grabbing a slider locks the user's wrist pose, improving hand tracking accuracy and stabilizing the arm during adjustments.
