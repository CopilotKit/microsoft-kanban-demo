# Project Structure, Architecture & Layering

Status: Draft
Scope: Define solution/projects, folders, high-level dependencies. (No implementation details or class/member design.)

## Goals
- Clear separation of concerns
- Minimal coupling between protocol core, integration glue, hosting, and UI
- Allow independent packaging (NuGet) and versioning alignment
- Support future extensibility (additional transports, UI frameworks, alternative runtimes)

## High-Level Layers
1. Core Protocol (Models + Events + Serialization)
2. Framework Integration (Adapter layer to Microsoft Agent Framework & Microsoft.Extensions.AI)
3. Hosting (ASP.NET Core server, transports, connection/session management)
4. UI Components (Blazor UI & client state handling)
5. Samples (Reference applications demonstrating vertical slices)
6. Tests (Unit, integration, component) – colocated per library

Dependency Direction (allowed arrows):
Core  -> (none)
Integration -> Core
Hosting -> Core, Integration
UI -> Core (read-only protocol types)
Samples -> Core, Integration, Hosting, UI
Tests (per library) -> Library under test (and its direct dependencies)

No reverse dependencies permitted (e.g., Core must not depend on Hosting/UI).

## Solution & Project Layout
Root for .NET work (relative to repo):
`typescript-sdk/integrations/microsoft-agent-framework/server/dotnet/`

Each library folder contains exactly two top-level subfolders:
* `src/` – the production library project (.csproj) and its code
* `test/` – the companion test project using xUnit

No central monolithic `Tests/` directory; tests are colocated for locality and simpler refactoring.

```
dotnet/
├── AGUI/                                        # Core protocol & primitives
│   ├── src/
│   │   ├── AGUI.csproj
│   │   ├── Events/
│   │   ├── Messages/
│   │   ├── Models/
│   │   ├── Serialization/
│   │   └── Abstractions/
│   └── test/
│       ├── AGUI.Tests.csproj
│       └── (unit tests)
├── Microsoft.Agents.AI.AGUI/                    # Integration layer
│   ├── src/
│   │   ├── Microsoft.Agents.AI.AGUI.csproj
│   │   ├── Adapters/
│   │   ├── Integration/
│   │   ├── Extensions/
│   │   └── Configuration/
│   └── test/
│       ├── Microsoft.Agents.AI.AGUI.Tests.csproj
│       └── (unit tests)
├── Microsoft.Agents.AI.Hosting.AGUI.AspNetCore/  # Hosting & transports
│   ├── src/
│   │   ├── Microsoft.Agents.AI.Hosting.AGUI.AspNetCore.csproj
│   │   ├── Middleware/
│   │   ├── Hubs/
│   │   ├── Encoders/
│   │   ├── Services/
│   │   ├── Extensions/
│   │   └── Configuration/
│   └── test/
│       ├── Microsoft.Agents.AI.Hosting.AGUI.AspNetCore.Tests.csproj
│       └── (unit tests)
├── Microsoft.AspNetCore.Components.AI/           # Blazor UI components
│   ├── src/
│   │   ├── Microsoft.AspNetCore.Components.AI.csproj
│   │   ├── Components/
│   │   ├── Services/
│   │   ├── JavaScript/
│   │   └── wwwroot/
│   └── test/
│       ├── Microsoft.AspNetCore.Components.AI.Tests.csproj
│       └── (unit tests)
├── Samples/                                     # End-to-end runnable sample apps
│   └── SimpleChat/
│       ├── SimpleChat/                          # Blazor Server interactivity frontend
│       └── SimpleChat.API/                      # Companion API backend
├── AGUI.sln
└── README.md
```

### Test Project Pattern
For each library `X`:
* Production: `X/src/X.csproj`
* Tests: `X/test/X.Tests.csproj`
* Tests reference only the public surface of `X` (Internals visible only if justified via `[InternalsVisibleTo]`).
* xUnit is the default framework; FluentAssertions may be added later (tracked separately).

### Sample Applications
Placed under `Samples/` (NOT published). `SimpleChat` ships as a pair of projects: a Blazor app using server interactivity plus a companion API (`SimpleChat.API`) that hosts the backend endpoints. Both reference the shared libraries to exercise end-to-end protocol flows. Shared helpers (if any) live under `Samples/_shared/` (added only when duplication emerges).

## Packaging / NuGet Identity (Proposed)
- AGUI (Core)
- Microsoft.Agents.AI.AGUI (Integration)
- Microsoft.Agents.AI.Hosting.AGUI.AspNetCore (Hosting)
- Microsoft.AspNetCore.Components.AI (UI)
- (No NuGet for Samples; each sample has its own project file, excluded from publish pipeline)

## Layer Responsibilities (Concise)
- Core: Data contracts, event & message shapes, serialization helpers.
- Integration: Convert/bridge Agent Framework events & chat abstractions to Core events.
- Hosting: Expose HTTP/SSE, WebSocket/SignalR, (future) binary transport; session & connection orchestration.
- UI: Render protocol artifacts; manage client state synchronization & streaming presentation.
- Samples: Demonstrate vertical usage patterns (no reusable logic kept here).
- Tests: Validation of contracts, adapters, streaming behaviors, component rendering (colocated per library).

## Folder Conventions
- Singular conceptual folders (Events, Messages, Models) at Core level.
- Adapters vs Integration: "Adapters" = object-to-object transformations; "Integration" = orchestrators & higher level coordination.
- Extensions: Only extension methods (no core logic) to keep discoverability high.
- Services: Long-lived or DI-managed runtime objects.
- Encoders: Transport formatting boundaries.
- Components: Razor UI primitives (one component per file, co-located partial classes if needed).

## Naming & Guidelines
- Public surface: PascalCase types, suffix with intent (e.g., *Event, *Options, *Service, *Middleware, *Adapter, *Encoder).
- Internals: Prefer internal visibility unless required externally.
- Async streaming: IAsyncEnumerable<T> for event pipelines.
- No business logic leaks across layers—only contracts move upward.

## Open Decisions (Deferred to separate doc)
- Binary encoder shape & framing specifics
- Transport negotiation strategy
- Optional Native AOT trimming constraints

## Reference Package Baselines
- Microsoft.Extensions.AI `9.10.0` (latest prerelease-capable release, published 2025-10-14)
- Microsoft.Agents.AI `1.0.0-preview.251009.1` (prerelease, published 2025-10-09)

(End of document)
