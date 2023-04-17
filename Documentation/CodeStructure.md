# Code Structure
This project is separated into 2 main structures. First is the [Meta Multiplayer for Netcode and Photon](../Packages/com.meta.multiplayer.netcode-photon) package, which is core reusable code that can easily be used to start a new project using similar configuration for a multiplayer game. Then there is the [Decommissioned](../Assets/Decommissioned) app, which uses the Meta Multiplayer base and implements the specific game logic.

We also have a package of common utility functionality that helped us speed up the implementation of our project. These utilities can be found in [Packages/com.meta.utilities](../Packages/com.meta.utilities).

We also needed to extend the functionality of Photon Realtime for Netcode, and to do so, we made a copy of the package in [Packages/com.community.netcode.transport.photon-realtime](https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Packages/com.community.netcode.transport.photon-realtime%40b28923aa5d).

# Meta Multiplayer for Netcode and Photon
Project agnostic logic that can be reused in any project. It is implementing different elements required for a networked multiplayer project. It also contains some key features implementation from our Platform Social API.

[BlockUserManager.cs](../Packages/com.meta.multiplayer.netcode-photon/Core/BlockUserManager.cs) implements the blocking flow API.

[GroupPresenceState.cs](../Packages/com.meta.multiplayer.netcode-photon/Core/GroupPresenceState.cs) implements the usage of group presence API which is the base for players to play together easily.

[NetworkLayer.cs](../Packages/com.meta.multiplayer.netcode-photon/Networking/NetworkLayer.cs) implements the network state for client/host connection flow and disconnection handling as well as host migration.

The implementation of the networked Avatar is key in integrating personality in a project and a good example on how avatars can easily be integrated in a project ([Avatars](../Packages/com.meta.multiplayer.netcode-photon/Avatar)).

# Decommissioned
This is the implementation of the specifics of the game. Some of the key components and systems will be highlighted here, but it is strongly encouraged for you to delve into the code yourself for the best understanding of its functionality.

## Application
The [Application.Core](../Assets/Decommissioned/Scripts/App/Application.Core.cs) and [Application.Oculus.cs](../Assets/Decommissioned/Scripts/App/Application.Oculus.cs) scripts encapsulate logic for setting the initial state of the app at launch: this includes initializing the Oculus Platform (i.e. user account, avatar, guardian boundary checks, etc.), initializing group presences, and handling connecting to lobbies on launch (if the app is started via an invitation to an existing match).

## Main Menu
The [MainMenu.cs](../Assets/Decommissioned/Scripts/App/MainMenu.cs) script gives us access to several API calls that control the user flow of the app, which are used in the [MainMenu scene](../Assets/Decommissioned/Scenes/MainMenu.unity) after startup is complete. This encapsulates public methods for launching a new match, joining an existing one, or exiting the application altogether.

## Game
The [Game directory](../Assets/Decommissioned/Scripts/Game) contains all of the gameplay logic used within an actual match (inside the [Lobby scene](../Assets/Decommissioned/Scenes/Lobby.unity)). We will highlight some of the major systems and components related to gameplay below.

### Game Manager
The [GameManager](../Assets/Decommissioned/Scripts/Game/GameManager/GameManager.cs) is a singleton that stores and controls the current state of the game. It determines the maximum number of players per match, how many rounds the game will last, and the conditons for player victory; by extension, it also controls the advancement through rounds and checking for conditons that may end a given match.

### Game Phases
A match of Deccommissioned proceeds through four different [phases](../Assets/Decommissioned/Scripts/Game/GamePhase/GamePhase.cs), each of which having its own objectives and directives for the players. The [GamePhase directory](../Assets/Decommissioned/Scripts/Game/GamePhase) contains components for managing and updating the current phase of the game.

### Role Manager
During a match, each player is assigned a specific [PlayerRole](../Assets/Decommissioned/Scripts/Player/PlayerRole.cs) that determines their goals during the game. The [RoleManager](../Assets/Decommissioned/Scripts/Game/GameManager/RoleManager.cs) is a singleton that determines each player's role during a match. Here, you can adjust various thresholds determining how many players can be assigned a certain role at once. 

### Voting
During the game, players will vote to determine who will assign players to certain Mini Games. The [Voting directory](../Assets/Decommissioned/Scripts/Game/Voting) contains components for managing and storing the state of each player's votes.

### Mini Games
The core gameplay of Decommissioned centers around 6 unique Mini Games, each of which having their own unique set of tasks to complete during every round. The [Minigames directory](../Assets/Decommissioned/Scripts/Game/Minigames) contains all scripts related to each of them.

The [Minigame](../Assets/Decommissioned/Scripts/Game/Minigames/MiniGame.cs) script is the core component managing the configuration and state of every minigame, and can be found somewhere within all [Minigame Prefabs](../Assets/Decommissioned/Prefabs/Game/Minigames).

### Location Manager
Players will be moved to specific [GamePositions](../Assets/Decommissioned/Scripts/Lobby/GamePosition.cs) throughout the scene as the game proceeds. The [LocationManager](../Assets/Decommissioned/Scripts/Game/GameManager/LocationManager.cs) component stores all of these locations and provides several methods for accessing them.

## Player
The [Player directory](../Assets/Decommissioned/Scripts/Player) contains all of the gameplay logic used to distinguish and manage players within a match. We will highlight some of the major components below.

### Player Status
Tracks a given player's status in the game; currently, we use the the [PlayerStatus](../Assets/Decommissioned/Scripts/Player/PlayerStatus.cs) component to determine whether or not a given player is the current Commander or not.

### Player Spawns
Players are always associated with a specific GamePosition during the match. The [PlayerSpawns](../Assets/Decommissioned/Scripts/Player/PlayerSpawns.cs) component allows us store and access each player's current location.

### Player Voip
The [PlayerVoip](../Assets/Decommissioned/Scripts/Player/PlayerVoip.cs) component is used to manage the state of the player's voice chat; we can use this component to mute or unmute specific players during the game.

## Player Armband
Upon connecting to a match. each player is given a color-coded armband with which they can change their audio settings, access help menus, and quit the game. The [PlayerArmband](../Assets/Decommissioned/Scripts/Player/PlayerArmband.cs) component manages the appearance and functionality of this object.