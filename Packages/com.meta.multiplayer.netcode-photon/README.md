# Meta Multiplayer for Netcode and Photon Package

This package contains core implementation to start a multiplayer project while using Netcode for gameobject and Photon as the transport layer.

You can integrate this package into your own project by using the Package Manager to [add the following Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

```txt
https://github.com/oculus-samples/Unity-Decommissioned.git?path=/Packages/com.meta.multiplayer.netcode-photon
```

## Core
|Script|Description|
|-|-|
|[BlockUserManager](./Core/BlockUserManager.cs)|Handles the [platform blocking APIs](https://developer.oculus.com/documentation/unity/ps-blockingsdk/). On initialize you get the list of blocked users and it centralizes the logic to block, unblock and query if a user is blocked.|
|[CameraRigRef](./Core/CameraRigRef.cs)|Singleton that keeps a reference to the camera rig and OvrAvatarInputManager for easy access through the application.|
|[ClientNetworkTransform](./Core/ClientNetworkTransform.cs)|Based on the [netcode networktransform documentation](https://docs-multiplayer.unity3d.com/netcode/current/components/networktransform/index.html#clientnetworktransform), it handles client authoritative transform synchronization.|
|[GroupPresenceState](./Core/GroupPresenceState.cs)|Handles the [platform GroupPresence API](https://developer.oculus.com/documentation/unity/ps-group-presence-overview/) and keeps track of the user presence state. This is used for social platform functionalities like invites, rosters and join.|
|[SceneLoader](./Core/SceneLoader.cs)|Handles loading the scene based on the networking context. It also keeps track of which scene is loaded and when the load is completed.|
|[VoipController](./Core/VoipController.cs)|Controls the creation of the speaker(remote) or the recorder(local) as well as microphone permissions. It also connects the voip to the right room when it is set.|
|[VoipHandler](./Core/VoipHandler.cs)|Keeps a reference of the speaker or recorder and handles muting the right component.|
|[PlayerDisplacer](./Core/PlayerDisplacer.cs)|"Safety bubble" to keep players from getting too close to eachothers' faces.|
|[PlayerId/PlayerManager/PlayerObject](./PlayerId/README.md)|System for referencing players by a GUID that persists between game sessions.|

## Networking
Information about the Networking utilities can be [found here](./Networking/README.md).

## Avatar
|Script|Description|
|-|-|
|[AvatarEntity](./Avatar/AvatarEntity.cs)|Implementation of the OvrAvatarEntity that sets up the avatar based on the user ID, integrates the body tracking, events on joints loaded, hide and show avatar, tracks camera rig, as well as local and remote setup.|
|[AvatarNetworking](./Avatar/AvatarNetworking.cs)|Combined with the AvatarEntity, this script handles the networked updates of the avatar state using **Netcode for GameObjects**. For a local avatar it will send the data to other players and for a remote avatar it will receive and apply the updates. You can send the data LOD frequency based on your needs.|
|[PhotonVoiceAvatarNetworking](./Avatar/PhotonVoiceAvatarNetworking.cs)|This is an alternative to `AvatarNetworking` that uses **Photon Voice** as the network transport. This method allows the Avatar animation to be synchronized with their players' voices. It is also more efficient than `AvatarNetworking`, since Netcode does not support peer-to-peer multicasting. The class works by setting up the [`PhotonVoiceAvatarSender`](./Avatar/PhotonVoiceAvatarSender.cs) class to encode the Avatar packets into voice data, then using the [`PhotonVoiceAvatarReceiver`](./Avatar/PhotonVoiceAvatarReceiver.cs) class to decode it on the other clients.|
|[AvatarMeshUtils](./Avatar/AvatarMeshUtils/README.md)|Information about the Avatar mesh querying system can be found here.|
