# Changelog

All notable changes to this project are documented in this file.

## Next version

## [0.3.0.0] - August 23, 2026

- Added typed `get_status` diagnostics for mop attachment, water box attachment, water shortage, in-cleaning state and the current map flag.
- Added typed `get_room_mapping` parsing through `RoborockRoomMapping` and `GetRoomMappingsAsync()`.
- Documented that Home Assistant derives the current room from parsed map content plus room mappings, and that its map content retrieval uses the cloud MQTT map RPC channel rather than the local TCP command path.
- Added a method to `RoborockMapImage` for saving rendered map images to disk.

## [0.2.2.0] - August 23, 2026

- Added a downloadable binary zip asset to GitHub Releases for users who cannot consume the NuGet package directly.

## [0.2.1.0] - August 23, 2026

- Updated the release workflow to publish packages to GitHub Packages and create GitHub Releases using the matching changelog section as release notes.

## [0.2.0.0] - August 23, 2026

- Changed the namespace and library name to use the full solution name with API to align it with my other projects

## [0.1.0.0] - August 22, 2026

- Inital version
