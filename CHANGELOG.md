# Changelog

All notable changes to this project are documented in this file.

## Next version

## [0.3.3.0] - August 23, 2026

- Added Roborock cloud room-name lookup so `RoborockCurrentRoom.Name` can return the friendly room name shown by Home Assistant.

## [0.3.2.0] - August 23, 2026

- Added current room resolution from RRMap robot position and room segment pixels through `RoborockCurrentRoom`, `GetCurrentRoomAsync()` and `RoborockMapImageWithMetadata.CurrentRoom`.
- Added rendered PNG coordinates to `RoborockMapPosition` so consumers can draw the vacuum on top of the map image.
- Changed map rendering to crop to known map bounds and use transparent outside-map pixels instead of a solid background.

## [0.3.1.0] - August 23, 2026

- Added a committed test configuration template with ignored local overrides for Roborock integration tests.
- Added typed multi-map metadata through `RoborockMapInfo` and `GetMultiMapsAsync()`.
- Added `GetMapImageWithMetadataAsync()` helpers to return rendered maps together with their Roborock map flag and friendly name.

## [0.3.0.0] - August 23, 2026

- Added Home Assistant-style cloud MQTT map retrieval through `RoborockCloudMapClient` for devices that do not return map payloads over the local TCP channel.
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
