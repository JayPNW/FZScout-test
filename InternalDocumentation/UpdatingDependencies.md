# Updating Dependencies

| Dependency | Where it's pinned | Who updates it |
| --- | --- | --- |
| NuGet packages | [Directory.Packages.props](/Directory.Packages.props) | Dependabot |
| GitHub Actions | [.github/workflows/](/.github/workflows/) | Dependabot |
| .NET tools | [dotnet-tools.json](/dotnet-tools.json) | Us |
| .NET SDK | [global.json](/global.json) | Us |

## What Dependabot does

[dependabot.yml](/.github/dependabot.yml) keeps NuGet packages and GitHub Actions up to date.

## What we do

### .NET tools

`vpk` must match the version of the `Velopack` NuGet package. Dependabot bumps the package but not the tool, so when a Velopack PR lands, update the tool to the same version:

```
dotnet tool update vpk --version <same version as Velopack>
```

### .NET SDK

[global.json](/global.json) pins an exact SDK version. CI installs exactly that version via `actions/setup-dotnet`.

