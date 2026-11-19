# MyERP

MyERP is an ASP.NET Core MVC ERP starter for Visual Studio 2022 and SQL Server 2019. It includes cookie-based sign-in, permission-filtered navigation, company/branch and access setup, supplier/customer/item maintenance, purchasing approvals, goods receipts, sales dispatch, stock adjustments, and a stock-on-hand dashboard/report.

## Solution structure

- `MyERP.Domain` — ERP entities and workflow statuses.
- `MyERP.Application` — persistence abstraction and purchasing/inventory workflow rules.
- `MyERP.Infrastructure` — EF Core 8 SQL Server provider and SQL Server model configuration.
- `MyERP.Web` — MVC controllers, Razor views, cookie authentication, AngularJS 1.x interactions, and Bootstrap 5 UI.
- `MyERP.Tests` — focused workflow tests using EF Core's in-memory provider.

## Requirements

- Visual Studio 2022 with the ASP.NET and web development workload.
- .NET 8 SDK/runtime to build and run the checked-in projects in Visual Studio 2022.
- SQL Server 2019 or SQL Server 2019 LocalDB. SQL Server Express/Developer can also be used by changing the connection string.

## Configure and run

1. Open `MyERP.sln` in Visual Studio 2022 and restore NuGet packages.
2. The checked-in default targets the configured SQL Server 2019 instance `L-PANKAJ\PKSQLEXP` and database `ITES_ERP` using Windows Authentication; it contains no username or password. Run the application as a Windows user that has permission to connect to the instance. For a different machine or database, override `ConnectionStrings:Erp` through User Secrets or environment variables instead of committing machine-specific credentials:

   ```powershell
   dotnet user-secrets init --project MyERP.Web
   dotnet user-secrets set "ConnectionStrings:Erp" "Server=YOUR_SERVER;Database=YOUR_DATABASE;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True" --project MyERP.Web
   dotnet user-secrets set "Database:InitializeOnStartup" "true" --project MyERP.Web
   dotnet user-secrets set "ERP_ADMIN_EMAIL" "admin@your-company.example" --project MyERP.Web
   dotnet user-secrets set "ERP_ADMIN_PASSWORD" "<choose-a-unique-password-of-at-least-12-characters>" --project MyERP.Web
   ```

   Replace the server and example email/password with values for your environment. For SQL authentication, keep the full connection string in User Secrets or a secure deployment secret store.
3. Keep `Database:InitializeOnStartup` disabled unless you explicitly intend to apply migrations to the configured database. When enabled, the app applies pending EF Core migrations and seeds the permission catalog and navigation. If both `ERP_ADMIN_EMAIL` and `ERP_ADMIN_PASSWORD` are supplied, it creates that initial Administrator account with a salted ASP.NET Core password hash. No default administrator password is shipped.
4. Sign in with the configured initial administrator. Create a company and branch, then configure additional users, roles, permissions, suppliers, customers, and items.

The SQL Server target `ITES_ERP` was found to exist during read-only validation. Do not initialize it or apply the first migration until its owner explicitly approves changing its schema. `database\setup-ites-erp.ps1` creates the database if it is absent and applies the generated idempotent migration script only when run with `-Apply`; it refuses an existing schema that is not tracked by EF migrations. For a new environment, review the script and get database-owner approval before running it. For production, keep startup migration disabled and run the reviewed migration script through your database deployment process. Use HTTPS and a managed secret store. .NET 8 support ends in November 2026; move to .NET 10 when updating the framework.

## Database migrations and setup

The application schema is versioned in `MyERP.Infrastructure\Migrations`. The committed SQL migration script is idempotent with respect to the EF Core migration history. For an empty/new database, from the repository root:

```powershell
dotnet tool restore
dotnet ef migrations script --idempotent --project MyERP.Infrastructure --startup-project MyERP.Web --output database\migrations\ites-erp-idempotent.sql
.\database\setup-ites-erp.ps1 -Apply
```

The setup script uses Windows Authentication and the configured SQL Server 2019 instance. It creates `ITES_ERP` only if it does not exist, then applies the idempotent migration script. It deliberately stops if an existing database has user tables but no EF migration history. An idempotent migration script is not a schema-diff or a baseline for databases initialized with `EnsureCreated`; have an existing schema reviewed and baselined separately before using migrations. Generating the migration/script does not connect to or change the database.

## Included workflows

- Purchase requests are submitted and approved/rejected. An approved request can create one purchase order; purchase orders have a separate approval step.
- Goods receipts reference an approved purchase order. Receipt approval validates the outstanding ordered quantities and posts inventory transactions.
- Sales orders reserve no stock until dispatch. Dispatch checks branch-level on-hand quantity and posts stock outflow.
- Stock adjustments require a reason and cannot make stock negative. Reports aggregate the posted inventory ledger by branch and item.
- Users with an assigned branch can transact only at that branch. Users without a branch assignment can select any active branch.
- The menu is rendered from active menu records only when the signed-in user has the corresponding permission claim. Configure roles and permissions under **Company & access**.

AngularJS 1.x and Bootstrap 5 are loaded from pinned public CDNs in the MVC layout; an internet connection is required for those browser assets. The server-side MVC forms remain functional without AngularJS, but dynamic line-entry controls will not be available.

## Build

```powershell
dotnet restore MyERP.sln
dotnet tool restore
dotnet build MyERP.sln --no-restore
dotnet test MyERP.sln
```

The solution targets .NET 8 with EF Core 8, which is supported by Visual Studio 2022. Upgrade the target framework and EF Core packages together to .NET 10 before beginning a new production deployment after .NET 8 support ends.
