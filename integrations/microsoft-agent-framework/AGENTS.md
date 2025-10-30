# AG-UI .NET Implementation - Developer Guide

This guide provides practical tips and common operations for working on the Microsoft Agent Framework .NET integration for AG-UI.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Project Architecture](#project-architecture)
- [Development Workflow](#development-workflow)
- [Running the Samples](#running-the-samples)
- [Testing with the Dojo](#testing-with-the-dojo)
- [Common Operations](#common-operations)
- [Debugging Tips](#debugging-tips)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software
- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Node.js 18+** - Required for the TypeScript SDK and dojo
- **pnpm 10.13.1+** - Package manager for the monorepo
- **PowerShell 7+** (Windows) - For running scripts

### Optional Tools
- **Visual Studio 2022 17.13+** or **JetBrains Rider 2024.3+**
- **VS Code** with C# Dev Kit extension
- **@modelcontextprotocol/server-playwright** - For automated browser testing

### Verify Installation
```powershell
# Check .NET SDK
dotnet --version  # Should be 9.0.x or higher

# Check Node.js
node --version    # Should be v18.x or higher

# Check pnpm
pnpm --version    # Should be 10.13.1 or higher
```

---

## Project Architecture

The .NET implementation follows a layered architecture with clear separation of concerns:

### Solution Structure
```
server/dotnet/
├── AGUI.sln                                     # Main solution file
├── AGUI/                                        # Core protocol library
│   ├── src/                                     # Core types, events, messages
│   └── test/                                    # Unit tests
├── Microsoft.Agents.AI.AGUI/                    # Integration layer
│   ├── src/                                     # Adapters for Agent Framework
│   └── test/                                    # Unit tests
├── Microsoft.Agents.AI.Hosting.AGUI.AspNetCore/ # ASP.NET Core hosting
│   ├── src/                                     # Middleware, SSE, WebSockets
│   └── test/                                    # Unit tests
├── Microsoft.AspNetCore.Components.AI/          # Blazor UI components
│   ├── src/                                     # Razor components
│   └── test/                                    # Component tests
└── Samples/
    └── SimpleChat/
        ├── SimpleChat/                          # Blazor frontend
        └── SimpleChat.API/                      # Backend API
```

### Key Design Principles

1. **Dependency Flow**: Core → Integration → Hosting → UI (one-way only)
2. **Test Colocation**: Each library has a paired `test/` folder
3. **No Reverse Dependencies**: Core libraries never depend on higher layers
4. **Framework**: All projects target .NET 9.0

### Layer Responsibilities

- **AGUI (Core)**: Protocol definitions, event shapes, serialization contracts
- **Microsoft.Agents.AI.AGUI (Integration)**: Bridges Microsoft Agent Framework to AG-UI
- **Microsoft.Agents.AI.Hosting.AGUI.AspNetCore (Hosting)**: HTTP/SSE endpoints, SignalR hubs, connection management
- **Microsoft.AspNetCore.Components.AI (UI)**: Blazor components for rendering AG-UI streams

---

## Development Workflow

### Opening the Solution

**Using Visual Studio / Rider:**
```powershell
cd d:\work\ag-ui-dotnet\typescript-sdk\integrations\microsoft-agent-framework\server\dotnet
start AGUI.sln
```

**Using VS Code:**
```powershell
cd d:\work\ag-ui-dotnet\typescript-sdk\integrations\microsoft-agent-framework\server\dotnet
code .
```

### Building the Solution

**Build all projects:**
```powershell
cd d:\work\ag-ui-dotnet\typescript-sdk\integrations\microsoft-agent-framework\server\dotnet
dotnet build
```

**Build a specific project:**
```powershell
dotnet build AGUI/src/AGUI.csproj
dotnet build Microsoft.Agents.AI.AGUI/src/Microsoft.Agents.AI.AGUI.csproj
```

**Clean and rebuild:**
```powershell
dotnet clean
dotnet build
```

### Running Tests

**Run all tests:**
```powershell
dotnet test
```

**Run tests for a specific project:**
```powershell
dotnet test AGUI/test/AGUI.Tests.csproj
dotnet test Microsoft.Agents.AI.AGUI/test/Microsoft.Agents.AI.AGUI.Tests.csproj
```

**Run tests with detailed output:**
```powershell
dotnet test --logger "console;verbosity=detailed"
```

**Run tests with coverage (requires coverlet):**
```powershell
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Running the Samples

### SimpleChat.API (Backend)

The `SimpleChat.API` project is the backend server that implements the AG-UI protocol endpoints.

**Location:** `server/dotnet/Samples/SimpleChat/SimpleChat.API/`

#### Running from Command Line

```powershell
cd d:\work\ag-ui-dotnet\typescript-sdk\integrations\microsoft-agent-framework\server\dotnet\Samples\SimpleChat\SimpleChat.API

# Run with HTTP (port 5277)
dotnet run --launch-profile http

# Run with HTTPS (port 7210)
dotnet run --launch-profile https
```

#### Running from IDE

**Visual Studio / Rider:**
1. Set `SimpleChat.API` as the startup project
2. Select the launch profile (http or https)
3. Press F5 (Debug) or Ctrl+F5 (Run without debugging)

**VS Code:**
1. Open the `SimpleChat.API` folder
2. Use the Run and Debug panel (Ctrl+Shift+D)
3. Select ".NET Core Launch (web)" configuration

#### Verify the Server is Running

```powershell
# Check the weather forecast endpoint (default template)
Invoke-WebRequest -Uri http://localhost:5277/weatherforecast

# Or with curl
curl http://localhost:5277/weatherforecast
```

#### Port Configuration

The default ports are configured in `Properties/launchSettings.json`:
- HTTP: `http://localhost:5277`
- HTTPS: `https://localhost:7210` (with HTTP fallback on 5277)

To change ports, edit `launchSettings.json`:
```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:YOUR_PORT"
    }
  }
}
```

### SimpleChat (Blazor Frontend)

The `SimpleChat` project is a Blazor Server application that provides the UI.

**Location:** `server/dotnet/Samples/SimpleChat/SimpleChat/`

#### Running the Frontend

```powershell
cd d:\work\ag-ui-dotnet\typescript-sdk\integrations\microsoft-agent-framework\server\dotnet\Samples\SimpleChat\SimpleChat

dotnet run
```

The Blazor app will typically run on `http://localhost:5000` or `https://localhost:5001`.

---

## Testing with the Dojo

The **dojo** is a Next.js application that provides a visual testing environment for AG-UI integrations. It runs at `http://localhost:3000` and allows you to interactively test your .NET backend.

### Starting the Dojo

**From the typescript-sdk root:**
```powershell
cd d:\work\ag-ui-dotnet\typescript-sdk
pnpm dev
```

This command:
1. Installs all dependencies across the monorepo
2. Builds the AG-UI packages
3. Starts the dojo development server
4. Opens `http://localhost:3000` in your browser

### Dojo Configuration

The dojo is configured to connect to your .NET backend via:

**File:** `typescript-sdk/apps/dojo/src/agents.ts`

```typescript
export const agents: Agents = {
  'agentic_chat': {
    agentClass: MicrosoftAgentFrameworkAgent,
    endpoint: 'http://localhost:5018'  // Update this to match your backend
  },
  'backend_tool_rendering': {
    agentClass: MicrosoftAgentFrameworkAgent,
    endpoint: 'http://localhost:5018'
  }
}
```

**Important:** Make sure the `endpoint` URL matches your `SimpleChat.API` server port (default: 5277).

### Testing Workflow

1. **Start the .NET backend:**
   ```powershell
   cd server/dotnet/Samples/SimpleChat/SimpleChat.API
   dotnet run --launch-profile http
   ```

2. **Start the dojo** (in a new terminal):
   ```powershell
   cd d:\work\ag-ui-dotnet\typescript-sdk
   pnpm dev
   ```

3. **Open the dojo** at `http://localhost:3000`

4. **Select your integration** from the menu (Microsoft Agent Framework)

5. **Test your features** using the interactive UI

### Automated Browser Testing with Playwright MCP

For automated testing, we recommend using the **@modelcontextprotocol/server-playwright** MCP server.

#### Setup Playwright MCP

1. **Install the Playwright MCP server:**
   ```powershell
   npx @modelcontextprotocol/server-playwright
   ```

2. **Configure your AI assistant** to use the Playwright server for testing localhost:3000

3. **Write test scenarios** that interact with the dojo UI

#### Example Test Flow

```typescript
// Pseudocode for a typical test scenario
1. Navigate to http://localhost:3000
2. Select "Microsoft Agent Framework" from the integration menu
3. Click on "Agentic Chat" feature
4. Type a message: "Hello, what's the weather?"
5. Verify the response contains expected AG-UI events
6. Check that the UI renders correctly
```

The Playwright MCP server can automate these interactions, making it easy to:
- Verify protocol compliance
- Test streaming responses
- Validate UI rendering
- Perform regression testing

## Contributing

When making changes:

1. **Follow the architecture layers** - respect dependency flow
2. **Write tests** - add unit tests for new functionality
3. **Update documentation** - keep this guide and plan docs current
4. **Test with dojo** - verify integration works end-to-end
5. **Check build** - ensure `dotnet build` and `dotnet test` pass

For more details, see [CONTRIBUTING.md](../../CONTRIBUTING.md) in the repository root.
