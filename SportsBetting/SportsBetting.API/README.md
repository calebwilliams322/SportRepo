# SportsBetting.API

REST API for the sports betting platform.

## Structure

- **Controllers/** - API endpoints
- **Program.cs** - Application startup and DI configuration
- **appsettings.json** - Configuration

## Purpose

Thin HTTP layer that:
- Exposes REST endpoints
- Handles HTTP concerns (routing, validation, status codes)
- Delegates business logic to Services layer
- Configures Dependency Injection

## Dependencies

- SportsBetting.Services (application services)
- SportsBetting.Domain (indirectly through Services)

## Running

```bash
dotnet run --project SportsBetting.API
```

Default: https://localhost:5001
