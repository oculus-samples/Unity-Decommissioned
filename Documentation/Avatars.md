# Avatars
To have a greater sense of self and engage more of a social feeling we integrated the Meta Avatars in this project.
Being able to reuse the platform Avatar creates a continuity on the platform where users can recognize each other between different applications.

You will find the Meta Avatar SDK in the [Assets/Oculus/Avatar2](../Assets/Oculus/Avatar2/) directory. It was downloaded on the [developer website](https://developer.oculus.com/downloads/package/meta-avatars-sdk).

For the integration, we followed the information highlighted on the [developer website](https://developer.oculus.com/documentation/unity/meta-avatars-overview/). The [AvatarEntity.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/AvatarEntity.cs) implementation is where you will see how we setup the Avatar for body, lipsync, face and eye tracking. This setup is also associated with the [PlayerAvatarEntity Prefab](../Assets/Decommissioned/Prefabs/Player/AvatarEntity.prefab), which contains all the behaviours and settings on how we use the Avatar in game. To keep the avatar in sync with the user position, we track the Camera Rig root.

More information on Face and Eye tracking can be found [here](https://developer.oculus.com/documentation/unity/meta-avatars-face-eye-pose/).

## Networking
Since we are building a multiplayer game, it is necessary that we implement a networking solution for the Avatar.
This is done in [PhotonVoiceAvatarNetworking.cs](../Packages/com.meta.multiplayer.netcode-photon/Avatar/PhotonVoiceAvatarNetworking.cs). In this implementation we use the `RecordStreamData` function on the avatar entity to get the data to stream over the network. We then send it via rpc, which is then received by each other clients.
On the receiving end, we apply the data using the `ApplyStreamData` function which will properly apply the state of the Avatar. Additionally, we implemented a frequency to send different level of details (LOD) so that we can reduce the bandwidth while still keeping a good fidelity of the Avatar motion.
