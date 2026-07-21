# Blue

Single-project .NET global tool (`dotnet tool`). Published on NuGet as `Blue`.

## Build & Run

```powershell
dotnet build
dotnet run -- version
dotnet run -- new owner/repo -n MyProject --output ./MyProject
```

## Pack (producing NuGet)

```powershell
dotnet pack -c Release -o artifacts
dotnet tool install --global --add-source ./artifacts Blue
```

## Tests

No tests exist. If added — `dotnet test` from repo root.

## SDK

Requires .NET SDK `10.0.100` (`global.json`). Available via `dotnet` in PATH.

## Entrypoint & Commands

`src/Blue/Program.cs` — top-level statements, routes `args[0]` via switch:

- `version` → prints Blue, .NET SDK, and Git versions
- `new <owner/repo>` → `git clone --depth 1` from GitHub, applies `.template.config/template.json`, runs `dotnet new install`, calls `dotnet new`, then cleans up temp dirs

## CI/CD

`.github/workflows/ci.yml`:
- **build** on every push
- **publish** on tags matching `v*` (validated as semver): builds, packs, pushes `.nupkg` to NuGet.org via OIDC (NuGet login as `Chakrygin`)

## Style

Enforced via `.editorconfig` only. No `.roslynatorconfig`, no `StyleCop`, no `dotnet format` in CI. Uses JetBrains Rider (`.idea/`). Central Package Management enabled (`Directory.Packages.props`) though no external packages yet.
