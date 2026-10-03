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

Copy the environment template and set real values first:

```powershell
Copy-Item .env.example .env
```

### Run everything in Docker

Builds and starts the API, the processing worker, RabbitMQ and SQL Server. The database schema is created on first start.

```powershell
docker compose --profile app up --build
```

| Service | Address |
| --- | --- |
| API reference (Scalar) | http://localhost:8080/scalar |
| RabbitMQ management UI | http://localhost:15672 |
| SQL Server | `localhost,1433` (login `sa`) |

The API and worker share uploaded documents through the `documents` volume, mounted at `/data` in both containers.

### Develop locally

`docker compose up -d` without the profile starts only RabbitMQ and SQL Server, leaving the API and worker to run from the IDE.

**.NET** (connection details come from user secrets):

```powershell
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:BenefitsIntelligence" "Server=localhost,1433;Database=BenefitsIntelligence;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True" --project src/BenefitsIntelligence.Api
dotnet user-secrets set "RabbitMq:UserName" "<RABBITMQ_USER>" --project src/BenefitsIntelligence.Api
dotnet user-secrets set "RabbitMq:Password" "<RABBITMQ_PASSWORD>" --project src/BenefitsIntelligence.Api
dotnet tool run dotnet-ef database update --project src/BenefitsIntelligence.Infrastructure --startup-project src/BenefitsIntelligence.Api
dotnet run --project src/BenefitsIntelligence.Api --launch-profile http
```

The API listens on http://localhost:5209 (Scalar at `/scalar`). Uploaded documents are written to `data/` at the repository root.

**Python worker** (reads `.env` from the repository root):

```powershell
cd src/python
uv sync
uv run policy-worker
```

### Tests

```powershell
dotnet test
cd src/python
uv run pytest
uv run ruff check .
```

### Trying it out

Upload a PDF from Scalar (`POST /api/policies`) or with curl:

```powershell
curl.exe -X POST http://localhost:8080/api/policies -F "name=Current policy" -F "file=@CurrentHealthPolicy.pdf;type=application/pdf"
```

The response is `202 Accepted` with the policy and correlation IDs. `GET /api/policies/{id}` shows the processing status moving to `Completed` once the worker has handled the document.

## Conventions

- Package versions are managed centrally in `Directory.Packages.props`. Do not put `Version` on `PackageReference` items.
- Shared compiler settings live in `Directory.Build.props`. Warnings are treated as errors.
- .NET code style is enforced by `.editorconfig`. Python linting, formatting and type checking are configured in `src/python/pyproject.toml`.
- Python dependencies are locked in `src/python/uv.lock`. Add them with `uv add` (or `uv add --dev`) rather than editing the file by hand.
- Secrets are supplied through `.env`, which is never committed. `.env.example` lists the required keys.
