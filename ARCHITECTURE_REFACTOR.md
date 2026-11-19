# RiseUp — CRM-style Interface / Implementation Refactor

## Scope

This archive contains the **actual modified `RiseUp(1).zip` source**, not just a proposed folder tree.
The functional code, public type names, namespaces, service registrations, Razor views, migrations, database scripts, and tests are preserved. The principal change is that grouped interfaces and implementation classes have been separated into one public type per source file and stored in predictable folders.

## Project layout

```text
RiseUp/
├── RiseUp.slnx
├── src/
│   ├── RiseUp.Framework/        # Result<T>, constants, time and shared helpers
│   ├── RiseUp.Entities/         # Domain models and application port assembly
│   │   ├── Abstractions/        # ExternalServices.cs + shared query / error types
│   │   ├── Identity/            # UserAccount
│   │   ├── Startups/            # Startup
│   │   ├── Investors/           # InvestorProfile
│   │   ├── Social/              # StartupPost
│   │   ├── Connections/         # ConnectionRequest
│   │   ├── Conversations/       # Conversation / Message
│   │   └── ...
│   ├── RiseUp.DataAccess/
│   │   ├── Interface/           # 22 contracts, e.g. IPostRepository.cs
│   │   ├── Implementation/      # 21 classes, e.g. PostRepository.cs
│   │   ├── Configurations/      # EF Core entity mappings
│   │   ├── Migrations/          # Existing migrations (untouched)
│   │   ├── Services/            # Payment, AI, storage & password adapters
│   │   ├── Seed/                # Demo seeding
│   │   ├── RiseUpDbContext.cs
│   │   └── DependencyInjection.cs
│   ├── RiseUp.Facade/
│   │   ├── Interface/           # 21 interfaces, e.g. IPostFacade.cs
│   │   ├── Implementation/      # 24 classes, e.g. PostFacade.cs and MediaIntake.cs
│   │   ├── DTOs/                # 63 request / response records
│   │   ├── Validators/          # 5 FluentValidation validators
│   │   ├── Common/              # Actor, guards, helpers
│   │   └── DependencyInjection.cs
│   └── RiseUp.Web.UI/
│       ├── Controllers/         # MVC controllers, unchanged
│       ├── Views/               # Razor views, unchanged
│       ├── wwwroot/             # CSS / JS / video assets, unchanged
│       ├── Areas/Admin/
│       └── Program.cs
├── tests/
│   ├── RiseUp.Facade.Tests/
│   ├── RiseUp.Architecture.Tests/
│   └── RiseUp.Web.SmokeTests/
├── database/                    # Existing SQL scripts (unchanged)
├── STRUCTURE_VALIDATION.json     # Automated source-preservation audit
└── refactor_manifest.json       # Original-to-new file mapping
```

## Example: posts end to end

| Purpose | New path | Original namespace (preserved) |
| --- | --- | --- |
| Domain model | `src/RiseUp.Entities/Social/StartupPost.cs` | `RiseUp.Entities.Social` |
| Facade contract | `src/RiseUp.Facade/Interface/IPostFacade.cs` | `RiseUp.Facade.Posts` |
| Facade implementation | `src/RiseUp.Facade/Implementation/PostFacade.cs` | `RiseUp.Facade.Posts` |
| Post DTO | `src/RiseUp.Facade/DTOs/FeedItemDto.cs` | `RiseUp.Facade.Posts` |
| Input validator | `src/RiseUp.Facade/Validators/SubmitPostRequestValidator.cs` | `RiseUp.Facade.Posts` |
| Data repository contract | `src/RiseUp.DataAccess/Interface/IPostRepository.cs` | `RiseUp.Entities.Abstractions` |
| Data repository implementation | `src/RiseUp.DataAccess/Implementation/PostRepository.cs` | `RiseUp.DataAccess.Repositories` |
| Controller | `src/RiseUp.Web.UI/Controllers/WorkspaceController.cs` | unchanged |

The runtime request path remains:

`Controller -> IPostFacade / PostFacade -> IPostRepository / PostRepository -> RiseUpDbContext -> SQL Server`

Each repository remains registered in `RiseUp.DataAccess/DependencyInjection.cs`; each facade remains registered in `RiseUp.Facade/DependencyInjection.cs`. No constructor names, public method signatures or `using`-namespace contracts were intentionally changed.

## Important: why DataAccess/Interface files have the original Entities namespace

The project already followed a **dependency inversion** rule:

- `RiseUp.Facade` depends on `RiseUp.Entities` and `RiseUp.Framework`, **not** on DataAccess.
- `RiseUp.DataAccess` depends on `RiseUp.Entities` and `RiseUp.Framework`, **not** on Facade.
- `RiseUp.Web.UI` wires the layers together at startup.

Putting the repository contracts into the DataAccess **assembly** would force the Facade to reference infrastructure and break the existing architecture tests. To satisfy the requested CRM-style **on-disk** paths without that regression, this refactor uses a source-link:

- The repository interface **source files are located at** `RiseUp.DataAccess/Interface/I...Repository.cs`.
- `RiseUp.Entities.csproj` includes them as **linked compile items**. They continue to define `RiseUp.Entities.Abstractions.I...Repository` types *inside the Entities assembly*.
- `RiseUp.DataAccess.csproj` excludes those source files from its own compilation (and includes them as `None` for browsing) to avoid duplicate types.

**Do not remove those two `.csproj` entries** without also redesigning the dependency graph. This is a pragmatic layout compatibility technique; a future full architecture migration could instead put all contracts in a dedicated standalone `RiseUp.Contracts` assembly.

## Verification completed here

- Extracted the user's current archive (`RiseUp(1).zip`).
- Moved 158 public declarations / helper classes and checked each declaration's body against the original source (ignoring whitespace and documentation-comment repositioning): **158/158 preserved**.
- Confirmed every facade implementation has its `I...Facade.cs` file.
- Confirmed every concrete repository implementation has its `I...Repository.cs` file.
- Checked the C# project files for valid XML and correct contract-source link/exclude configuration.
- Checked the service-registration source for known implementations.
- Kept original database migrations and UI files.

**Not verified:** The `.NET 10 SDK` is not installed in this work environment, so an actual `dotnet build` and automated `dotnet test` could not be executed. This package is a source-preserving structural refactor; it is **not** a guarantee of compilation or production-readiness. Please build and run the tests in Visual Studio.

## Local build instructions

1. Extract the ZIP, preserving the directory structure.
2. Open `RiseUp/RiseUp.slnx` with Visual Studio with .NET 10 support, or use the current .NET 10 SDK.
3. Run:

```powershell
cd RiseUp
dotnet restore RiseUp.slnx
dotnet build RiseUp.slnx -c Debug
dotnet test RiseUp.slnx -c Debug
```

4. If Visual Studio shows stale metadata errors, close the IDE, remove only generated `bin/` and `obj/` folders, reopen, and rebuild.
5. Database connection strings and service-provider credentials must be configured in local user secrets or environment variables. Follow `README.md` and `DEPLOYMENT.md`.

## Limits of the change

This request was to **implement the Interface/Implementation project-folder blueprint**. The permission-management feature discussed earlier (`PermissionGroup`, `UserPermission`, etc.) has **not** been silently added to this build, because doing so requires a separate authorized data model, migration, policies, tests and UI workflows. Nor have we silently changed existing business rules.
