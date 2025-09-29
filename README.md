![Decommissioned Banner](./Documentation/Media/banner.png "Decommissioned")

# Decommissioned

Decommissioned is a social deduction VR game that showcases the use of the [Meta XR SDK](https://developers.meta.com/horizon/downloads/package/meta-xr-core-sdk/), the [Meta Interaction SDK](https://developers.meta.com/horizon/documentation/unity/unity-isdk-interaction-sdk-overview/), and the [Meta Avatars SDK](https://developers.meta.com/horizon/documentation/unity/meta-avatars-overview/) to create a social VR gaming experience.

This codebase serves as a reference and template for multiplayer VR games. You can play it on Meta Quest via the [Horizon Store - Decommissioned](https://www.meta.com/en-gb/experiences/decommissioned/5756827011021749/).

## Project Description

This project is a social deduction game for Meta Quest devices, playable with friends or strangers. It demonstrates how to connect users in random games or private rooms and invite friends to matches using the [Meta Platform SDK](https://developers.meta.com/horizon/documentation/unity/ps-platform-intro/).

Players use [Meta Avatars](https://developers.meta.com/horizon/documentation/unity/meta-avatars-overview/) to represent their VR personas, and [Photon Voice](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) for communication. The game uses [Application SpaceWarp](https://developers.meta.com/horizon/documentation/unity/unity-asw/) to maintain a stable framerate.

## How to Run the Project in Unity

1. [Configure the project](./Documentation/Configuration.md) with Meta Quest and Photon.
2. Use *Unity 2022.3.32f1* or newer.
3. Load the [Assets/Decommissioned/Scenes/Startup](./Assets/Decommissioned/Scenes/Startup.unity) scene.
4. To test in the Editor, choose one of the following:

    <details>
      <summary><b>Quest Link</b></summary>

    - Enable Quest Link:
        - Put on your headset, go to "Quick Settings", and select "Quest Link" (or "Quest Air Link" if using Air Link).
        - Select your desktop from the list, then select "Launch". This opens the Quest Link app, allowing desktop control from your headset.
    - With the headset on, select "Desktop" from the control panel in front of you. You should see your desktop in VR.
    - Navigate to Unity and press "Play"; the application should launch on your headset automatically.
    </details>
    <details>
      <summary><b>XR FPS Simulator</b></summary>

    - In Unity, press "Play" and enjoy the simulated XR controls!
    - Review the [XR FPS Simulator documentation](./Packages/com.meta.utilities.input/README.md#xr-device-fps-simulator) for more information.
        - Note: The mouse is [captured by the simulator](./Packages/com.meta.utilities.input/README.md#mouse-capture) when in play mode. To use the mouse in-game (such as to interact with menus), hold Left Alt.
    </details>

## Dependencies

This project uses the following plugins and software:

- [Unity](https://unity.com/download) 6000.0.50f1
- [Meta Avatars SDK](https://assetstore.unity.com/packages/tools/integration/meta-avatars-sdk-271958)
- [Meta XR Audio SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-audio-sdk-264557)
- [Meta XR Core](https://assetstore.unity.com/packages/tools/integration/meta-xr-core-sdk-269169)
- [Meta XR Integration SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-interaction-sdk-265014)
- [Meta XR Platform SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-platform-sdk-262366)
- [Unity Universal Render Pipeline (ASW fork)](https://developers.meta.com/horizon/documentation/unity/unity-asw/#how-to-enable-appsw-in-app)
- [ParrelSync](https://github.com/brogan89/ParrelSync)
- [Photon Realtime for Netcode](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.photon-realtime)
- [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518)
- [Unity Netcode for GameObjects](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects)
- [Unity Toolbar Extender](https://github.com/marijnz/unity-toolbar-extender.git)
- [ScriptableObject-Architecture](https://github.com/DanielEverland/ScriptableObject-Architecture)
- [NaughtyAttributes](https://github.com/dbrizov/NaughtyAttributes)

To test this project within Unity, you need:

- [The Meta Quest App](https://www.meta.com/quest/setup/)

## Getting the Code

First, ensure you have Git LFS installed by running:

```sh
git lfs install
```

Then, clone this repository using the "Code" button above or this command:

```sh
git clone https://github.com/oculus-samples/Unity-Decommissioned.git
```

## Documentation

More information is available in the [Documentation](./Documentation) section of this project.

- [Armbands](./Documentation/Armbands.md)
- [Avatars](./Documentation/Avatars.md)
- [Code Structure](./Documentation/CodeStructure.md)
- [Configuration](./Documentation/Configuration.md)
- [Multiplayer](./Documentation/Multiplayer.md)

Custom Packages:

- [Meta Utilities](./Packages/com.meta.utilities/) - General utilities for game development.
- [Meta Input Utilities](./Packages/com.meta.utilities.input/) - XR FPS Simulator and other input-related utilities.
- [Meta Multiplayer for Netcode and Photon](./Packages/com.meta.multiplayer.netcode-photon/) - Components for easily bootstrapping a multiplayer VR game, including networked Avatars.
- [Watch Window](./Packages/com.meta.utilities.watch-window/) - Editor tool to help track and visualize data while in Play or Edit mode.

## Meta Avatar SDK and Photon Packages

The [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) package is stored in the [Packages](./Packages) folder. To update them, import their updated Asset Store packages, then copy them into their respective `Packages` folders.

We use the [Ultimate GloveBall fork of Photon Realtime for Netcode](https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/d48bb48d3232fd3e9e753f127b3d49b04ae4885f/Packages/com.community.netcode.transport.photon-realtime%40b28923aa5d).

## License

Most of Decommissioned is licensed under the [MIT LICENSE](./LICENSE). However, files from [Text Mesh Pro](https://unity.com/legal/licenses/unity-companion-license), and [Photon Voice](./Packages/Photon/Photon/license.txt) are licensed under their respective terms.

The [MIT License](./Assets/Decommissioned/LICENSE) applies to only certain, clearly marked documents. If an individual file does not indicate which license it is subject to, then the MIT License applies.

## Contribution

See the [CONTRIBUTING](./CONTRIBUTING.md) file for information on how to contribute.
