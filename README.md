# BenefitsIntelligence

Turns unstructured employee-benefit policy documents into structured, validated, queryable benefit data.

Policy PDFs are uploaded through an ASP.NET Core API and processed asynchronously by a Python document-intelligence service over RabbitMQ. The Python service extracts a canonical benefits model with an LLM, validates it with Pydantic and attaches source evidence to every extracted fact. The .NET side owns persistence, identity and all deterministic business logic, such as policy comparison and eligibility, so model output is never treated as the source of truth.

## Architecture

| Component | Technology | Responsibility |
| --- | --- | --- |
| `src/BenefitsIntelligence.Api` | ASP.NET Core (.NET 10) | Public API, authentication, orchestration |
| `src/BenefitsIntelligence.Application` | .NET class library | Use cases |
| `src/BenefitsIntelligence.Domain` | .NET class library | Domain model and deterministic rules |
| `src/BenefitsIntelligence.Infrastructure` | .NET class library | Persistence and messaging |
| `src/python` | Python 3.13, uv | Document parsing, LLM extraction, evidence-backed Q&A |
| RabbitMQ | `rabbitmq:4-management` | Asynchronous document-processing pipeline |
| SQL Server | `mssql/server:2022` | System of record |

Dependencies point inwards: `Api → Application → Domain`, with `Infrastructure` implementing the `Application` abstractions.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version pinned in `global.json`)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [uv](https://docs.astral.sh/uv/getting-started/installation/), which also provisions the pinned Python version

## Getting started

### 1. Infrastructure

```powershell
Copy-Item .env.example .env   # then set real values
docker compose up -d
docker compose ps             # wait for both services to report "healthy"
```

| Service | Address |
| --- | --- |
| RabbitMQ (AMQP) | `localhost:5672` |
| RabbitMQ management UI | http://localhost:15672 |
| SQL Server | `localhost,1433` (login `sa`) |

### 2. .NET

```powershell
dotnet build
dotnet test
dotnet run --project src/BenefitsIntelligence.Api
```

Health check: http://localhost:5209/health

### 3. Python

```powershell
cd src/python
uv sync
uv run pytest
uv run ruff check .
```

## Conventions

- Package versions are managed centrally in `Directory.Packages.props`. Do not put `Version` on `PackageReference` items.
- Shared compiler settings live in `Directory.Build.props`. Warnings are treated as errors.
- .NET code style is enforced by `.editorconfig`. Python linting, formatting and type checking are configured in `src/python/pyproject.toml`.
- Python dependencies are locked in `src/python/uv.lock`. Add them with `uv add` (or `uv add --dev`) rather than editing the file by hand.
- Secrets are supplied through `.env`, which is never committed. `.env.example` lists the required keys.
