# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 2023-04-17

### Added

- `AvatarMeshQuery` and related classes, used for analyzing an Avatar's mesh.
- `PhotonVoiceAvatarNetworking` and related classes, used for networking an Avatar through Photon Voice 2.
- `PlayerDisplacer` component for keeping players out of each other's faces.
- `NetworkArray`, `NetworkEvents`, `NetworkSingleton`, `NetworkMultiton`, `NetworkReference`, and `NetworkTimer` components.
- `PlayerId` and related classes, used for identifying players uniquely and persistently (without being affected by players disconnecting).

### Changed

- `AvatarEntity` no longer relies on `AvatarNetworking`.
- `VoipController` now works with the latest Photon Voice 2.
