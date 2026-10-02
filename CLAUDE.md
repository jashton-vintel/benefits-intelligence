# BenefitsIntelligence

## Build and test
- Build: `dotnet build`
- Test: `dotnet test`

## Projects
- `src/BenefitsIntelligence.Domain` - class library
- `tests/BenefitsIntelligence.Domain.Tests` - xUnit tests for Domain
- `src/BenefitsIntelligence.Api` - Web API (run: `dotnet run --project src/BenefitsIntelligence.Api`)

## Conventions
- Source in `src/`, tests in `tests/`.
- Shared settings live in `Directory.Build.props`; package versions live in `Directory.Packages.props` (central package management). Do not put `Version` on `PackageReference` items.
- Warnings are treated as errors. Style rules are in `.editorconfig`.