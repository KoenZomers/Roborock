# Copilot instructions

When asked to create a new version, update all required version files to the requested version before finishing the task.

Required version files:

- `Api\Api.csproj`: update the `<Version>` value to the requested version.
- `CHANGELOG.md`: add a new release section for the requested version using the current date and summarize the release changes.

Keep `.github\workflows\publish-nuget.yml` packing `Api\Api.csproj` so the published NuGet package uses the project version unless a workflow dispatch version override is supplied.
