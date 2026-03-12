# MaintOps
**Industrial Maintenance Scheduling Optimizer**

## Tech Stack
- **Runtime**: .NET 8 / ASP.NET Core Minimal APIs
- **Architecture**: Vertical Slice Architecture (VSA) + Domain-Driven Design (DDD)
- **Mediation**: MediatR + Carter
- **Database**: PostgreSQL via EF Core (Npgsql)
- **Validation**: FluentValidation (MediatR pipeline)
- **Security**: JWT Bearer Authentication + ASP.NET Core Rate Limiting
- **Testing**: xUnit + NSubstitute + FluentAssertions + Testcontainers

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for local PostgreSQL or full compose stack)

### Run with Docker Compose
```bash
docker compose up --build
```
The API will be available at `http://localhost:8080`.
Health check: `http://localhost:8080/health`

### Run locally
```bash
# Start PostgreSQL
docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:16-alpine

# Apply migrations
cd Industriall.MaintOps.Api
dotnet ef database update

# Run
dotnet run
```

## Project Structure
```
MaintOps/
├── Industriall.MaintOps.Api/
│   ├── Domain/
│   │   ├── Entities/       # WorkOrder (Aggregate Root), Equipment
│   │   ├── ValueObjects/   # MaintenanceSchedule
│   │   ├── Enums/          # WorkOrderStatus, CriticalityLevel
│   │   └── Common/         # Result<T> pattern
│   ├── Features/
│   │   └── WorkOrders/
│   │       ├── SubmitWorkOrder/     # POST  /work-orders
│   │       ├── ScheduleWorkOrder/   # PATCH /work-orders/{id}/schedule
│   │       ├── CompleteWorkOrder/   # PATCH /work-orders/{id}/complete
│   │       └── GetWorkOrders/       # GET   /work-orders
│   ├── Infrastructure/
│   │   ├── Database/       # ApplicationDbContext, EF Configurations
│   │   └── Security/       # JwtTokenGenerator, PasswordHasher
│   └── Common/
│       ├── Behaviors/      # ValidationBehavior, LoggingBehavior
│       └── Exceptions/     # GlobalExceptionHandler, custom exceptions
└── Industriall.MaintOps.Tests/
    ├── Domain/             # Unit tests – domain entity state machines
    ├── Handlers/           # Unit tests – handler logic (NSubstitute mocks)
    └── Integration/        # Integration tests – Testcontainers + WebApplicationFactory
```

## Domain Rules
| # | Rule |
|---|------|
| 1 | A `WorkOrder` in `Pending` status cannot be completed directly. |
| 2 | `MaintenanceSchedule` enforces `StartDate < EndDate`. |
| 3 | Scheduling is blocked if the same equipment already has a `High`-criticality `WorkOrder` with overlapping dates. |
