# Copilot instructions

When asked to create a new version, update all required version files to the requested version before finishing the task.

Required version files:

- `Api\Api.csproj`: update the `<Version>` value to the requested version.
- `CHANGELOG.md`: add a new release section for the requested version using the current date and summarize the release changes.

Keep `.github\workflows\release.yml` packing `Api\Api.csproj` so the published NuGet package uses the project version. The release workflow must use the matching `CHANGELOG.md` version section as the release notes for both NuGet and GitHub Releases.
