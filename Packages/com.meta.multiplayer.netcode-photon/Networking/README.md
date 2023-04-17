# Networking

This directory contains utilities and components that assist in developing the networking portion of a project.

|Utility|Description|
|-|-|
|[NetcodeHelpers](./NetcodeHelpers.cs)|Encapsulates helper methods to assist with networking development, namely generating RPC parameters.|
|[NetworkLayer](./NetworkLayer.cs)|This is the core for handling the networking state. It handles connection as Host or Client, disconnection and reconnection flows. It supplies multiple callback for different state changes that can be handled at the application implementation level, keeping this agnostic from the application implementation.|
|[NetworkSession](./NetworkSession.cs)|A network behaviour spawned by the host to sync information about the current session. It syncs the photon voice room name and contains logic to detect and sync which client would be the fallback host if the host disconnects.|
|[NetworkEvents](./NetworkEvents.cs)|Provides callbacks for various network events, such as host changes, client connections, and players entering or exiting a photon room.|
|[NetworkReference](./NetworkReference.cs)|Generic wrapper for `NetworkBehaviourReference`. This allows you to use `NetworkVariable`s that reference Unity `Object`s, while maintaining type-safety. For example: `NetworkVariable<NetworkReference<MyBehaviour>>`|
|[NetworkArray](./NetworkArray.cs)|This class is essentially a dynamically sized `NetworkVariable`. It's similar to `NetworkList`, but instead of having several events for different list manipulations, it only has one `OnValuesChanged` event.|
|[NetworkSingleton](./NetworkSingleton.cs)|[`Singleton`](../../com.meta.utilities/Singleton.cs) but derived from `NetworkBehaviour`.|
|[NetworkMultiton](./NetworkMultiton.cs)|[`Multiton`](../../com.meta.utilities/Multiton.cs) but derived from `NetworkBehaviour`.|
|[NetworkTimer](./NetworkTimer.cs)|Network-synchronized timer for countdowns and game phase synchronization.|
