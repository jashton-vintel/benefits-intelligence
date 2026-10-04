# BenefitsIntelligence

Turns unstructured employee-benefit policy documents into structured, validated, queryable benefit data.

Policy PDFs are uploaded through an ASP.NET Core API and processed asynchronously by a Python document-intelligence service over RabbitMQ. The Python service extracts a canonical benefits model with an LLM, validates it with Pydantic and attaches source evidence to every extracted fact. The .NET side owns persistence, identity and all deterministic business logic, such as policy comparison and eligibility, so model output is never treated as the source of truth.

## Architecture

| Component | Technology | Responsibility |
| --- | --- | --- |
| `src/BenefitsIntelligence.Web` | Angular 21 | Upload, processing status and evidence-backed policy detail |
| `src/BenefitsIntelligence.Api` | ASP.NET Core (.NET 10) | Public API, authentication, orchestration |
| `src/BenefitsIntelligence.Application` | .NET class library | Use cases |
| `src/BenefitsIntelligence.Domain` | .NET class library | Domain model and deterministic rules |
| `src/BenefitsIntelligence.Infrastructure` | .NET class library | Persistence and messaging |
| `src/python` | Python 3.13, uv, FastAPI | Document parsing and LLM extraction (queue worker); comparison summaries and Q&A (internal HTTP API) |
| RabbitMQ | `rabbitmq:4-management` | Asynchronous document-processing pipeline |
| SQL Server | `mssql/server:2022` | System of record |

Dependencies point inwards: `Api → Application → Domain`, with `Infrastructure` implementing the `Application` abstractions.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (version pinned in `global.json`)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [uv](https://docs.astral.sh/uv/getting-started/installation/), which also provisions the pinned Python version
- [Node.js](https://nodejs.org/) 22.12 or later, for the web front end

## Getting started

Copy the environment template and set real values first:

```powershell
Copy-Item .env.example .env
```

### Run everything in Docker

Builds and starts the web front end, the API, the processing worker, the internal AI API, RabbitMQ and SQL Server. The database schema is created on first start.

```powershell
docker compose --profile app up --build
```

| Service | Address |
| --- | --- |
| Web front end | http://localhost:8081 |
| API reference (Scalar) | http://localhost:8080/scalar |
| RabbitMQ management UI | http://localhost:15672 |
| SQL Server | `localhost,1433` (login `sa`) |

The API and worker share uploaded documents through the `documents` volume, mounted at `/data` in both containers. The front end is served by nginx, which also proxies `/api` to the API, so the browser talks to a single origin and the API needs no CORS configuration.

### Develop locally

`docker compose up -d` without the profile starts only RabbitMQ and SQL Server, leaving the API and worker to run from the IDE.

**.NET** (connection details come from user secrets):

```powershell
dotnet tool restore
dotnet user-secrets set "ConnectionStrings:BenefitsIntelligence" "Server=localhost,1433;Database=BenefitsIntelligence;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True" --project src/BenefitsIntelligence.Api
dotnet user-secrets set "RabbitMq:UserName" "<RABBITMQ_USER>" --project src/BenefitsIntelligence.Api
dotnet user-secrets set "RabbitMq:Password" "<RABBITMQ_PASSWORD>" --project src/BenefitsIntelligence.Api
dotnet user-secrets set "PythonApi:ApiKey" "<INTERNAL_API_KEY>" --project src/BenefitsIntelligence.Api
dotnet tool run dotnet-ef database update --project src/BenefitsIntelligence.Infrastructure --startup-project src/BenefitsIntelligence.Api
dotnet run --project src/BenefitsIntelligence.Api --launch-profile http
```

The API listens on http://localhost:5209 (Scalar at `/scalar`). Uploaded documents are written to `data/` at the repository root.

**Python worker and internal API** (both read `.env` from the repository root; run each in its own terminal):

```powershell
cd src/python
uv sync
uv run policy-worker
uv run policy-api
```

The internal API listens on http://127.0.0.1:8000, with interactive docs at `/docs`. It is only called by the .NET API, using the `INTERNAL_API_KEY` shared secret.

**Web front end** (proxies `/api` to the API on port 5209, see `src/proxy.conf.json`):

```powershell
cd src/BenefitsIntelligence.Web
npm install
npm start
```

The app is served at http://localhost:4200.

### Tests

```powershell
dotnet test
cd src/python
uv run pytest
uv run ruff check .
cd ../BenefitsIntelligence.Web
npm test
```

`uv run pytest -m live` runs extraction against the sample policies using the OpenAI key in `.env`. These tests call the API, so they are excluded from the default run.

### Trying it out

Upload a PDF from the web front end, from Scalar (`POST /api/policies`) or with curl:

```powershell
curl.exe -X POST http://localhost:8080/api/policies -F "name=Current policy" -F "file=@CurrentHealthPolicy.pdf;type=application/pdf"
```

The response is `202 Accepted` with the policy and correlation IDs. The policy page in the front end refreshes itself until processing finishes. `GET /api/policies/{id}` shows the processing status moving to `Completed` once the worker has handled the document, followed by every extracted fact with its confidence, the page and quote it was read from, and whether it needs review.

## Design decisions

**Document text pipeline.** PDFs are parsed page by page with PyMuPDF, normalised, and split into sentence-aligned chunks. Page boundaries are tracked through every stage so any extracted fact can be traced back to the page it came from.

- **ftfy** repairs extracted text (ligatures, mis-decoded characters, curly quotes). It was preferred over Unicode NFKC normalisation, which also rewrites symbols such as `½` and `m²`.
- **tiktoken** sizes chunks in model tokens rather than characters, so chunk budgets match how the model is limited and billed.
- **Running header and footer removal, and page-offset mapping, are implemented in-house.** No mainstream library provides page-accurate evidence location, and it is central to the product.
- **LangChain text splitters and frameworks such as LlamaIndex and unstructured were not used.** They add significant dependency weight and do not track page provenance.
- **PyMuPDF is AGPL-licensed.** A production deployment would need a commercial licence or a permissively licensed alternative such as pdfplumber.

**Extraction and review.** The model returns each value with the passage it was read from; everything else is checked in code. Each quote is located in the document text, which is where the cited pages come from, so a quote the document does not contain produces no evidence. Matching tolerates case, punctuation and spacing differences only: measured against the samples, a fabricated quote that reverses a clause's meaning scores close to a genuinely misquoted one, so a looser threshold would accept it. Amounts and dates must also appear in their own quote. The worker reports confidence and any problems found; the API decides what needs review (`Review:ConfidenceThreshold`), so the review policy can change without re-extracting documents. In practice the model reports near-certain confidence even for ambiguous clauses, so the deterministic checks carry most of the weight.

- **gpt-4.1 is the default model.** gpt-4.1-mini intermittently corrupted the `£` sign in structured output, changing the digits that followed it. Model text containing control characters is now rejected rather than stored.
- **rapidfuzz** provides the tolerant quote matching.

**Comparison.** Two policies are compared field by field in the .NET domain (`PolicyComparer`): amounts and counts as increases or decreases with their size, other values as changed or unchanged, and anything missing on either side as not comparable rather than guessed. Differences that rely on a fact needing review are flagged. The written summary is optional and produced by the internal Python API from the calculated differences, never from the documents. Every figure is formatted in code before the model sees it, and a summary is rejected if it uses evaluative language ("better", "recommend") or any figure not in the comparison. If no summary can be produced, the comparison is returned without one. The UI deliberately shows every change in the same neutral style, since colouring one as good and another as bad would itself be a judgement.

**Queue or HTTP.** Work that takes seconds to minutes, benefits from retries and does not need an immediate answer, such as document processing, goes through RabbitMQ. Requests a user is waiting on and that are quick, such as comparison summaries, use HTTP to the internal API, with a timeout and a fallback.

**Sample data.** `sample-data/policies` holds the wording of two synthetic policies, rendered to PDF by `src/python/scripts/render_sample_policies.py`. `sample-data/expected` records the known answers, the evidence for each, and deliberately planted edge cases: conflicting limits, eligibility defined across sections, an ambiguous clause and an embedded prompt-injection attempt. Tests check that every expected evidence quote can be located in the rendered documents.

## Conventions

- Package versions are managed centrally in `Directory.Packages.props`. Do not put `Version` on `PackageReference` items.
- Shared compiler settings live in `Directory.Build.props`. Warnings are treated as errors.
- .NET code style is enforced by `.editorconfig`. Python linting, formatting and type checking are configured in `src/python/pyproject.toml`.
- Python dependencies are locked in `src/python/uv.lock`. Add them with `uv add` (or `uv add --dev`) rather than editing the file by hand.
- Secrets are supplied through `.env`, which is never committed. `.env.example` lists the required keys.
