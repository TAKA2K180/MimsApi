# MimsApi (API Host)

This README describes how to configure, run and use the MimsApi host project (MimsApi.Main). It covers setup steps, configuration, authentication examples, logging behavior and available API endpoints (V1/V2).

Prerequisites
- .NET 8 SDK
- SQL Server instance reachable from the host (This time using Azure SQL server)
- (Optional) Visual Studio 2022 / VS Code

Quick configuration
1. Set the database connection string.
   Set - Use secrets to set the connection string for the Data layer.

2. Configure JWT settings (minimum requirements)
   - Key: `JwtSettings:SecretKey` (must be at least 32 chars)
   - `JwtSettings:Issuer` and `JwtSettings:Audience`
   - `JwtSettings:ExpirationMinutes`

3. Apply database schema and sample data
   - Option A: Start the application; EF Core migrations are applied automatically on startup.
   - Option B: Execute SQL scripts in `MimsApi.Data/SQL` (recommended for production-ready schema and stored procedures).

Run the API
- From solution root:
  dotnet run --project MimsApi.Main
- In Development environment Swagger/OpenAPI UI is exposed at `/swagger`.

Logging
- Serilog is configured in `MimsApi.Main/Logging/LoggingConfiguration.cs`.
- Console output and rolling files are enabled.
- Log files are created under the application base directory in `logs/`:
  - `application-*.txt` (general logs)
  - `errors-*.txt` (errors only)
  - `authentication-*.txt` (auth events)
- Request/response middleware logs HTTP method, path, status and duration. Authenticated user id (if present) is included.

Authentication
- JWT Bearer tokens are used.
- Public endpoints:
  - POST `/api/v1/auth/register` — register a new user
  - POST `/api/v1/auth/login` — login and receive JWT
- Protected endpoints require header:
  Authorization: Bearer (YOUR_TOKEN)
- JWT settings validated on startup (SecretKey length, issuer/audience). If missing or too short the app will fail to start.

API Endpoints (summary)
- V1: `/api/v1/products` — basic product CRUD (GET, POST, PUT, DELETE), paged listing `/paged`
- V2: `/api/v2/products` — returns products with full packaging hierarchy
- Packaging endpoints (historical/deprecated file exists) have been consolidated into the Products controllers; refer to `/api/v2/products` for hierarchy

Examples
1) Register
POST /api/v1/auth/register
{
  "username": "demo",
  "password": "P@ssw0rd123",
  "fullName": "Demo User",
  "email": "demo@example.com"
}

2) Login
POST /api/v1/auth/login
{
  "username": "demo",
  "password": "P@ssw0rd123"
}
Response contains `token` to use in `Authorization: Bearer <token>` header.

3) Get products (protected)
GET /api/v1/products
Header: Authorization: Bearer <token>

4) Get product with hierarchy (V2)
GET /api/v2/products/1
Header: Authorization: Bearer <token>

Database-first SQL support
- The repository includes SQL scripts (MimsApi.Data/SQL) with tables, views and stored procedures used by the Data layer.
- Use these scripts when you prefer a database-first deployment (recommended for deterministic schema & stored-proc performance).

Security notes
- Do not commit secrets to the repository. Use User Secrets locally and environment variables in CI/CD.
- Use a strong JWT secret (>=32 chars).
- Ensure TLS/HTTPS in production.

Support and documentation
- SQL schema docs: `MimsApi.Data/SQL/README.md`
- Packaging and hierarchy docs: `README_HIERARCHICAL_PACKAGING.md`, `PACKAGING_HIERARCHY_DOCUMENTATION.md`
- Logging guide: `LOGGING_GUIDE.md`

Database setup: (For minimal setup)
Database is deployed at Azure @ mimsdb.database.windows.net with creds Username: DbAd, password:
Database name: MimsDatabase
