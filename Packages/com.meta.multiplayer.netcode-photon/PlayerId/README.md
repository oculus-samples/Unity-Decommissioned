# Player ID & Player Objects

This directory contains scripts that allow developers to access and distinguish the data of specific players in a networked game.

|Script|Description|
|-|-|
|[PlayerId](./PlayerId.cs)|A unique 64-byte string that is assigned to each player. These IDs persist between game sessions, and they can be used to identify players and retrieve their components. Using this class, you can retrieve any given player's corresponding PlayerObject or the client ID associated with their [NetworkObject](../../com.unity.netcode.gameobjects/Runtime/Core/NetworkObject.cs).|
|[PlayerManager](./PlayerManager.cs)|This singleton initializes and stores the local player's PlayerID and username as well as listens for important network events (such as client connection/disconnection). It also stores the PlayerObjects and IDs of all connected players and allows us to retrieve them directly or by using client IDs.|
|[PlayerObject](./PlayerObject.cs)|A [NetworkMultiton](../Networking/NetworkMultiton.cs) that keeps track of the id and username of a player and allows us to easily access them using player and/or client ids. Any object representing a player should have a PlayerObject component attached to it.|
