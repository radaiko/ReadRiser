# ReadRiser

A self-hosted ebook library and management server built with .NET 9.

## Features

- File-based ebook storage and management
- User management with multi-user support
- REST API for ebook operations
- Package/library metadata tracking
- Web-based interface

## Architecture

- `RR.App` — ASP.NET Core web host
- `RR.Core` — Business logic, services, and data access
- `RR.DTO` — Data transfer objects and API contracts
- `RR.Http` — HTTP client layer

## Tech Stack

- C# / .NET 9
- ASP.NET Core
- File-based database (JSON storage)
