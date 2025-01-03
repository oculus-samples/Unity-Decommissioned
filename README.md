![Decommissioned Banner](./Documentation/Media/banner.png "Decommissioned")

# Decommissioned
Decommissioned was built to demonstrate how to use the [Oculus Integration SDK](https://developer.oculus.com/downloads/package/unity-integration/), the [Meta Interaction SDK](https://developer.oculus.com/documentation/unity/unity-isdk-interaction-sdk-overview/), and the [Meta Avatars SDK](https://developer.oculus.com/documentation/unity/meta-avatars-overview/) to create a social gaming experience in VR.

This codebase is available both as a reference and as a template for multiplayer VR games. The [Oculus License](./LICENSE) applies to the SDK and supporting material. The [MIT License](./Assets/Decommissioned/LICENSE) applies to only certain, clearly marked documents. If an individual file does not indicate which license it is subject to, then the Oculus License applies.

The majority of Decommissioned is licensed under [MIT LICENSE](./LICENSE), however files from [Text Mesh Pro](http://www.unity3d.com/legal/licenses/Unity_Companion_License), [Photon SDK](./Assets/Photon/LICENSE), and [Photon Voice](./Packages/Photon/Photon/license.txt), are licensed under their respective licensing terms.


You can play it on Meta Quest through the Store - [Decommissioned](https://www.meta.com/en-gb/experiences/decommissioned/5756827011021749).

## Project Description
This project is an application for the Meta Quest devices that demonstrates a social deduction game that can be played with friends or strangers. It shows how to integrate connection between users joining random games or specific private rooms, and how to invite friends to specific matches using the [Oculus Platform API](https://developer.oculus.com/documentation/unity/ps-platform-intro/).

[Meta Avatars](https://developer.oculus.com/documentation/unity/meta-avatars-overview/) are integrated for players to represent their VR persona and [Photon Voice](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) voice chat for easy communication. The game is rendered using [Application SpaceWarp](https://developer.oculus.com/documentation/unity/unity-asw/) to keep a stable framerate.

The project also contains the following reusable packages:

- [Meta Utilities](./Packages/com.meta.utilities/) - General utilities for game development.
- [Meta Input Utilities](./Packages/com.meta.utilities.input/) - XR FPS Simulator and other input-related utilities.
- [Meta Multiplayer for Netcode and Photon](./Packages/com.meta.multiplayer.netcode-photon/) - Components for easily bootstrapping a multiplayer VR game, including networked Avatars.
- [Watch Window](./Packages/com.meta.utilities.watch-window/) - Editor tool to help track and visualize data while in Play or Edit mode.

More information can be found in the [Documentation](./Documentation) section of this project.

## How to run the project in Unity
1. [Configure the project](./Documentation/Configuration.md) with Meta Quest and Photon
2. Make sure you're using  *Unity 2022.3.32f1* or newer.
3. Load the [Assets/Decommissioned/Scenes/Startup](./Assets/Decommissioned/Scenes/Startup.unity) scene.
4. There are two ways of testing in the editor:
    <details>
      <summary><b>Quest Link</b></summary>

      + Enable Quest Link:
        + Put on your headset and navigate to "Quick Settings"; select "Quest Link" (or "Quest Air Link" if using Air Link).
        + Select your desktop from the list and then select, "Launch". This will launch the Quest Link app, allowing you to control your desktop from your headset.
      + With the headset on, select "Desktop" from the control panel in front of you. You should be able to see your desktop in VR!
      + Navigate to Unity and press "Play" - the application should launch on your headset automatically.
    </details>
    <details>
      <summary><b>XR FPS Simulator</b></summary>

      + In Unity, press "Play" and enjoy the simulated XR controls!
      + Review the [XR FPS Simulator documentation](./Packages/com.meta.utilities.input/README.md#xr-device-fps-simulator) for more information.
        + Note: The mouse is [captured by the simulator](./Packages/com.meta.utilities.input/README.md#mouse-capture) when in play mode. In order to otherwise use the mouse in-game (such as to interact with menus), hold Left Alt.
    </details>



## Dependencies
This project makes use of the following plugins and software:
- [Unity](https://unity.com/download) 2022.3.32f1 or newer
- [Meta Avatars SDK](https://assetstore.unity.com/packages/tools/integration/meta-avatars-sdk-271958)
- [Meta XR Audio SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-audio-sdk-264557)
- [Meta XR Core](https://assetstore.unity.com/packages/tools/integration/meta-xr-core-sdk-269169)
- [Meta XR Integration SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-interaction-sdk-265014)
- [Meta XR Platform SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-platform-sdk-262366)
- [Unity Universal Render Pipeline (ASW fork)](https://developer.oculus.com/documentation/unity/unity-asw/#how-to-enable-appsw-in-app)
- [ParrelSync](https://github.com/brogan89/ParrelSync)
- [Photon Realtime for Netcode](https://github.com/Unity-Technologies/multiplayer-community-contributions/tree/main/Transports/com.community.netcode.transport.photon-realtime)
- [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518)
- [Unity Netcode for GameObjects](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects)
- [Unity Toolbar Extender](https://github.com/marijnz/unity-toolbar-extender.git)
- [ScriptableObject-Architecture](https://github.com/DanielEverland/ScriptableObject-Architecture)
- [NaughtyAttributes](https://github.com/dbrizov/NaughtyAttributes)

The following is required to test this project within Unity:
- [The Meta Link App](https://www.meta.com/quest/setup/)

---

## Getting The Code
First, ensure you have Git LFS installed by running this command:
```sh
git lfs install
```

Then, clone this repo using the "Code" button above, or this command:
```sh
git clone https://github.com/oculus-samples/Unity-Decommissioned.git
```

# Where are the Meta Avatar SDK and Photon packages?
In order to keep the project organized, the [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) package is stored in the [Packages](./Packages) folder. To update them, import their updated Asset Store packages, then copy them into their respective `Packages` folders.

Also, we are using the [Ultimate GloveBall fork of Photon Realtime for Netcode](https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/d48bb48d3232fd3e9e753f127b3d49b04ae4885f/Packages/com.community.netcode.transport.photon-realtime%40b28923aa5d).
