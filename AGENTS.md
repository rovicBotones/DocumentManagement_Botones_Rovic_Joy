# Repository Guidelines

## Project Structure & Module Organization

The solution is `DocumentManagement_Botones_Rovic_Joy.slnx`. The ASP.NET Core Web API project lives in `DocumentManagement_Botones_Rovic_Joy/`; controllers are in `Controllers/`, EF Core context and migrations are in `Data/`, and database entities are in `Data/Entities/`. `Services/` contains upload validation and storage logic. The xUnit tests live in `DocumentManagement_Botones_Rovic_Joy.Tests/`. `Properties/launchSettings.json` configures local launch profiles. Build output is generated under `bin/` and `obj/`; do not commit generated files.

## Build, Test, and Development Commands

Run commands from the repository root:

- `dotnet restore DocumentManagement_Botones_Rovic_Joy.slnx` restores NuGet dependencies.
- `dotnet build DocumentManagement_Botones_Rovic_Joy.slnx` compiles the solution.
- `dotnet test DocumentManagement_Botones_Rovic_Joy.slnx` runs the xUnit controller and upload validation tests.
- `dotnet run --project DocumentManagement_Botones_Rovic_Joy` starts the API locally using its launch profile.
- `dotnet ef database update --project DocumentManagement_Botones_Rovic_Joy --startup-project DocumentManagement_Botones_Rovic_Joy` applies EF Core migrations after `DefaultConnection` is configured.
- `dotnet test DocumentManagement_Botones_Rovic_Joy.slnx` runs tests when test projects are added; none are currently included.

In Development, OpenAPI JSON and Swagger UI are exposed at `/openapi/v1.json` and `/swagger`.

## Coding Style & Naming Conventions

Use the existing C# conventions: four spaces for indentation, nullable reference types enabled, and implicit usings enabled. Use PascalCase for types, methods, and public properties; use descriptive names and group controllers under the `Controllers` namespace. Keep routes and HTTP verbs explicit with ASP.NET Core attributes. Follow the surrounding file style and avoid unrelated formatting changes.

## Testing Guidelines

Use xUnit `[Fact]` tests in the dedicated test project. Name tests for the action and expected behavior (for example, `Upload_UnsupportedExtension_ReturnsUnsupportedMediaType`). Run `dotnet test DocumentManagement_Botones_Rovic_Joy.slnx` when changing API behavior or upload validation.

For document uploads, keep validation in a reviewable function or service. Enforce a 10 MB maximum per file, validate required inputs and file types, return useful errors with appropriate HTTP status codes, and use controlled storage paths and server-generated names. Prevent filename collisions and path traversal; never trust a user-supplied filename as a filesystem path. Maintain test coverage for validation and controller outcomes.

## Commit & Pull Request Guidelines

Git metadata is unavailable in this checkout, so no repository-specific commit format could be verified. Use short imperative commit subjects (for example, `Add document upload endpoint`). Pull requests should explain the user-visible change, summarize implementation choices, link any related issue, and include request/response examples or screenshots when they clarify API behavior. Record build and test results in the PR description.

## Configuration & Security

Keep environment-specific settings in the appropriate `appsettings` file or local environment configuration. Set `ConnectionStrings__DefaultConnection` securely and do not commit real credentials, tokens, or other secrets. Review configuration changes for accidental exposure before submitting.
