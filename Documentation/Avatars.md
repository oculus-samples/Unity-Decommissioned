# Avatars

We integrated Meta Avatars into this project to enhance user identity and social interaction. Reusing the platform Avatar ensures continuity, allowing users to recognize each other across different applications.

The Meta Avatar SDK is located in the [Assets/Oculus/Avatar2](../Assets/Oculus/Avatar2/) directory, downloaded from the [developer website](https://developers.meta.com/horizon/downloads/package/meta-avatars-sdk/).

For integration, we followed the guidelines on the [developer website](https://developers.meta.com/horizon/documentation/unity/meta-avatars-overview/). The [AvatarEntity.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarEntity.cs) file shows how we set up the Avatar for body, lipsync, face, and eye tracking. This setup is linked to the [PlayerAvatarEntity Prefab](../Assets/Decommissioned/Prefabs/Player/AvatarEntity.prefab), which includes all behaviors and settings for in-game Avatar use. We track the Camera Rig root to keep the avatar synchronized with the user's position.

More information on face and eye tracking is available [here](https://developers.meta.com/horizon/documentation/unity/meta-avatars-face-eye-pose/).

## Networking

For our multiplayer game, we implemented a networking solution for the Avatar. This is handled in [PhotonVoiceAvatarNetworking.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/PhotonVoiceAvatarNetworking.cs). We use the `RecordStreamData` function on the avatar entity to stream data over the network. The data is sent via RPC and received by other clients. On the receiving end, the `ApplyStreamData` function applies the Avatar's state. We also implemented a frequency to send different levels of detail (LOD) to reduce bandwidth while maintaining Avatar motion fidelity.
