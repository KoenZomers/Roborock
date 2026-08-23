# Copilot instructions

When making notable changes that are not part of an explicitly requested version release, add the release note under `CHANGELOG.md` section `## Next version`. Do not add unreleased changes to the latest numbered version section.

When asked to create a new version, update all required version files to the requested version before finishing the task.

Required version files:

- `Api\Api.csproj`: update the `<Version>` value to the requested version.
- `CHANGELOG.md`: create a new release section for the requested version using the current date, move the notes from `## Next version` into that new version section, and leave a fresh `## Next version` section for future unreleased changes.

Keep `.github\workflows\release.yml` packing `Api\Api.csproj` so the published NuGet package uses the project version. The release workflow must use the matching `CHANGELOG.md` version section as the release notes for both NuGet and GitHub Releases.
