# BenefitsIntelligence

> TODO: describe what this solution is for.

## Structure

- `src/BenefitsIntelligence.Domain` - class library
- `tests/BenefitsIntelligence.Domain.Tests` - xUnit tests for Domain
- `src/BenefitsIntelligence.Api` - Web API (run: `dotnet run --project src/BenefitsIntelligence.Api`)

Root-level configuration (`Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `global.json`) is listed under **Solution Items** in Visual Studio.

## Getting started

Requires the .NET SDK version pinned in `global.json`.

```powershell
dotnet build
dotnet test
```

## Conventions

- Package versions are managed centrally in `Directory.Packages.props`; do not put `Version` on `PackageReference` items.
- Shared compiler settings live in `Directory.Build.props`. Warnings are treated as errors.
- Code style is enforced by `.editorconfig`.