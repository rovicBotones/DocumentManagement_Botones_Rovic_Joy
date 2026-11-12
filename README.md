# Document Management API

## Overview

An ASP.NET Core Web API on .NET 10 for uploading, listing, retrieving, downloading, and deleting documents. Swagger UI is enabled in Development.

## Prerequisites and setup

- .NET 10 SDK.
- SQL Server LocalDB or a SQL Server instance accessible with Windows Authentication.
- EF Core CLI (`dotnet-ef`) version 10. Install it if needed with `dotnet tool install --global dotnet-ef --version 10.0.15`.

From the repository root, restore packages:

```sh
dotnet restore DocumentManagement_Botones_Rovic_Joy.slnx
```

## Database configuration

The app uses EF Core code-first with SQL Server. `ConnectionStrings:DefaultConnection` in `DocumentManagement_Botones_Rovic_Joy/appsettings.json` is configured for Windows Authentication and LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=DocumentManagementDB;Trusted_Connection=True;TrustServerCertificate=True;
```

Change the server/instance name if needed, or override the setting with the `ConnectionStrings__DefaultConnection` environment variable. Start the database, then apply the included migration:

```sh
dotnet ef database update --project DocumentManagement_Botones_Rovic_Joy --startup-project DocumentManagement_Botones_Rovic_Joy
```

The app does not apply migrations automatically. The `Documents` table stores the ID, generated stored filename, size, and upload time. `DocumentMetadata` is a one-to-one table keyed by document ID and stores the original filename and content type.

## Run the application

```sh
dotnet build DocumentManagement_Botones_Rovic_Joy.slnx
dotnet run --project DocumentManagement_Botones_Rovic_Joy --launch-profile http
```

The HTTP profile listens at `http://localhost:5085`; open Swagger UI at `http://localhost:5085/swagger`. The `https` profile uses `https://localhost:7148` and `http://localhost:5085`. Swagger and OpenAPI endpoints are enabled only in Development.

## Implemented endpoints

| Method and route | Behavior | Main responses |
| --- | --- | --- |
| `POST /api/documents` | Upload a multipart form field named `file`. | `201`, `400`, `413`, `415` |
| `GET /api/documents` | List document metadata. | `200` |
| `GET /api/documents/{id}` | Get metadata by GUID. | `200`, `404` |
| `GET /api/documents/{id}/download` | Download document bytes by GUID. | `200`, `404` |
| `DELETE /api/documents/{id}` | Delete document and metadata by GUID. | `204`, `404` |

Uploads are limited to 10 MiB per file. Accepted types are PDF (`application/pdf`), DOCX (`application/vnd.openxmlformats-officedocument.wordprocessingml.document`), XLSX (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`), PPTX (`application/vnd.openxmlformats-officedocument.presentationml.presentation`), TXT (`text/plain`), and CSV (`text/csv`). These cover portable records, editable Office documents and simple text/tabular data. The validator checks extension and MIME agreement; it checks PDF headers and required Office package entries. MIME type alone does not establish file safety.

## Architecture and storage

`DocumentsController` handles HTTP routes and responses. `DocumentUploadValidator` holds upload rules, and `IDocumentStorage` separates controller behavior from persistence. `LocalDocumentStorage` writes document bytes to `App_Data/Documents` under a generated GUID-based name and uses EF Core to save metadata. User-provided filenames are stored only as display metadata. EF Core entities in `Data/Entities` map to the `Documents` and `DocumentMetadata` tables; schema changes are tracked in `Data/Migrations`.

## Automated tests

The xUnit project `DocumentManagement_Botones_Rovic_Joy.Tests` tests controller actions and upload validation using an in-memory fake storage implementation; tests do not require SQL Server. Run from the repository root:

```sh
dotnet test DocumentManagement_Botones_Rovic_Joy.slnx
```

## Limitations and assumptions

- Authentication and authorization are not implemented; do not expose these endpoints to untrusted clients as-is.
- Filesystem writes and SQL changes cannot share a transaction. A failure during deletion can leave an orphaned file; there is no orphan cleanup job.
- Storage is local to one app host. Production deployments need private persistent storage, backups, access controls, and a retention policy.
- Type checks are not antivirus scanning or complete semantic validation of every accepted file format.
- Tests cover controller behavior with fake storage, not SQL Server integration, migration application, or the live HTTP hosting pipeline.
- The default connection assumes LocalDB and Windows Authentication; configure it for the deployment environment before using database-backed endpoints.

## AI tools and development disclosure

OpenAI Codex was used as an AI coding assistant during development. It inspected the existing project, proposed and edited the document API, upload validation, EF Core entities/context/migration, Swagger setup, xUnit tests, and project documentation. It also ran builds and the xUnit suite and used the resulting output to correct implementation issues.

The AI-assisted review and modifications covered the API controller, validation and storage services, EF Core configuration and migration, Swagger registration, test project, README, and contributor guide. The least-confident areas are failure recovery across the separate filesystem and database, and the depth of file-format validation: current checks do not provide malware scanning or full parsing of all accepted formats.
