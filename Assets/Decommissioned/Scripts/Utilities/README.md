# Utilities

This directory contains general utilities for Unity development used throughout this project.

|Utility|Description|
|-|-|
|[CollisionEnterBehavior](./CollisionEnterBehavior.cs)|Allows us to set additional behavior upon an object entering a collision (i.e. playing sound effects). It can be configured to execture this behavior only when certain tags are detected.|
|[ExternallyScaledObject](./ExternallyScaledObject.cs)|Scales an object relative to the local position of a specified transform.|
|[NameTruncator](./NameTruncator.cs)| Truncates a string after a given a specific character (i.e. periods, commas, spaces). Truncates after spaces by default.|
|[PokeRadius](./PokeRadius.cs)|This component, usually attached to pokeable objects (such as buttons), ensures that their state is reset when interactors in the scene are a certain distance away from it.|
|[PositionSensitiveCollisionEnterBehavior](./PositionSensitiveCollisionEnterBehavior.cs)| Child class of [CollisionEnterBehavior](./CollisionEnterBehavior.cs) that only executes collision behavior for the player occupying a specific [GamePosition](../../Scripts/Lobby/GamePosition.cs). |
|[SnapToPosition](./SnapToPosition.cs)|When attached to an object, a specified object can "snap" to its position and rotation when brought within a certain distance of it.|
|[StationaryObject](./StationaryObject.cs)|When attached to an object, this component prevents it from being moved by any means.|
|[TriggerEnterBehavior](./TriggerEnterBehavior.cs)|Allows us to set additional behavior upon an object entering a trigger (i.e. playing sound effects). It can be configured to execture this behavior only when certain tags are detected.|