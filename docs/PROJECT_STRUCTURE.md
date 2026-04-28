# Project Structure

This solution uses a layered architecture for a desktop mall application built on .NET 8.

For documentation:
- Beginner guide (Chinese): `docs/PROJECT_GUIDE_CN.md`
- Long-term maintenance guide (Chinese): `docs/PROJECT_MAINTENANCE_CN.md`

## Current Status

- Multi-project layered solution is in place.
- `MainWindow` works as a shell host with a full-screen `ContentControl`.
- First-batch 7 UI pages have been scaffolded with design-placeholder layouts.

## Root layout

```text
shop/
  src/
    Shop.Desktop/        # WPF shell, views, view-models
    Shop.Domain/         # Domain entities, value objects, enums
    Shop.Application/    # Use-case contracts and application abstractions
    Shop.Infrastructure/ # Data access and external implementations
    Shop.Contracts/      # Shared DTOs between desktop/backend
    Shop.Backend/        # ASP.NET Core Web API (future backend)
  tests/
    Shop.Domain.Tests/
    Shop.Application.Tests/
  docs/
    PROJECT_STRUCTURE.md
    PROJECT_GUIDE_CN.md
    PROJECT_MAINTENANCE_CN.md
```

## Layer responsibility

- `Shop.Domain`: Business core with no infrastructure dependencies.
- `Shop.Application`: Business use cases and interfaces used by UI/API.
- `Shop.Infrastructure`: Concrete repository implementations and integrations.
- `Shop.Contracts`: Stable boundary objects for data exchange.
- `Shop.Desktop`: WPF desktop host and presentation layer.
- `Shop.Backend`: HTTP API host for admin/mobile/web integration later.

## Suggested next modules

- Page switching shell (preview all pages quickly).
- Product flow: list -> detail -> 3D preview.
- Data binding and ViewModel extraction per page.
- Backend integration and persistence upgrade.
