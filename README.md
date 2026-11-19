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
2. Set `ConnectionStrings:Erp` to a SQL Server 2019 database. The checked-in development default uses LocalDB and Windows authentication; it contains no username or password. For another server use an environment variable or User Secrets rather than committing credentials:

   ```powershell
   dotnet user-secrets init --project MyERP.Web
   dotnet user-secrets set "ConnectionStrings:Erp" "Server=YOUR_SERVER;Database=MyERP;Trusted_Connection=True;TrustServerCertificate=True" --project MyERP.Web
   dotnet user-secrets set "Database:InitializeOnStartup" "true" --project MyERP.Web
   dotnet user-secrets set "ERP_ADMIN_EMAIL" "admin@your-company.example" --project MyERP.Web
   dotnet user-secrets set "ERP_ADMIN_PASSWORD" "<choose-a-unique-password-of-at-least-12-characters>" --project MyERP.Web
   ```

   Replace the server and example email/password with values for your environment. For SQL authentication, keep the full connection string in User Secrets or a secure deployment secret store.
3. Run `MyERP.Web` with the Development environment. When `Database:InitializeOnStartup` is enabled, the app creates the schema and seeds the permission catalog and navigation. If both `ERP_ADMIN_EMAIL` and `ERP_ADMIN_PASSWORD` are supplied, it creates that initial Administrator account with a salted ASP.NET Core password hash. No default administrator password is shipped.
4. Sign in with the configured initial administrator. Create a company and branch, then configure additional users, roles, permissions, suppliers, customers, and items.

`EnsureCreated` is intended for a fresh database and local evaluation. It does not update an existing schema or work alongside EF migrations; back up data and introduce EF Core migrations before evolving a deployed database. For production, disable `Database:InitializeOnStartup`, provision/update the schema through a reviewed database deployment, use HTTPS and a managed secret store, and run a currently supported .NET/EF Core version. .NET 8 support ends in November 2026; move to .NET 10 when updating the framework.

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
dotnet build MyERP.sln --no-restore
dotnet test MyERP.sln --no-build
```

The solution targets .NET 8 with EF Core 8, which is supported by Visual Studio 2022. Upgrade the target framework and EF Core packages together to .NET 10 before beginning a new production deployment after .NET 8 support ends.
