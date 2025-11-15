# SportsBetting.Services

Application services layer containing business logic and orchestration.

## Structure

- **Interfaces/** - Service contracts (interfaces)
- **Implementations/** - Service implementations

## Purpose

This layer sits between the API and Domain:
- Orchestrates domain operations
- Handles application-specific logic (not domain logic)
- Can be consumed by API, background workers, webhooks, etc.

## Dependencies

- SportsBetting.Domain (for entities, value objects, domain services)

## Examples of Services

- BetPlacementService - Handles bet creation, validation, balance checks
- OddsService - Manages odds updates, odds feeds
- EventManagementService - Creates and manages events
- UserAccountService - User registration, balance management (if not outsourced)
