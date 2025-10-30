# AG-UI .NET - SimpleChat API

A minimal implementation of the AG-UI protocol using Microsoft Agent Framework and OpenAI via GitHub Models.

## Features

- **AG-UI Protocol Support**: Implements core AG-UI events (RUN_STARTED, TEXT_MESSAGE_*, RUN_FINISHED)
- **Streaming Responses**: Real-time Server-Sent Events (SSE) for agent responses
- **Microsoft Agent Framework**: Built on Microsoft.Extensions.AI abstractions
- **GitHub Models Integration**: Uses GPT-4o via GitHub Models API

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- GitHub CLI (`gh`) installed
- Active GitHub account with access to GitHub Models

### 1. Get Your GitHub Token

```bash
gh auth token
```

### 2. Configure User Secrets

Navigate to the SimpleChat.API directory and set your GitHub token:

```bash
cd typescript-sdk/integrations/microsoft-agent-framework/server/dotnet/Samples/SimpleChat/SimpleChat.API
dotnet user-secrets set "GitHubToken" "<your-github-token>"
```

### 3. Run the Server

```bash
dotnet run
```

The server will start on `http://localhost:5018`

### 4. Test with the Dojo

The server is configured to work with the AG-UI dojo. Start the dojo from the root:

```bash
cd typescript-sdk/integrations/microsoft-agent-framework
npm install
npm run dev
```

Then open http://localhost:3000 (or the port shown) and select the "agentic_chat" scenario.

## API Endpoint

### POST /

Send a chat message and receive streaming AG-UI events.

**Request Body:**
```json
{
  "message": "Tell me a joke",
  "systemPrompt": "You are a helpful assistant",
  "threadId": "optional-thread-id",
  "runId": "optional-run-id"
}
```

**Response:**
Server-Sent Events stream with AG-UI formatted events:
- `RUN_STARTED`: Indicates agent has begun processing
- `TEXT_MESSAGE_START`: Begins a new message
- `TEXT_MESSAGE_CONTENT`: Streaming text chunks
- `TEXT_MESSAGE_END`: Completes the message
- `RUN_FINISHED`: Indicates agent has completed
- `RUN_ERROR`: Error information (if applicable)

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      SimpleChat.API                          │
│  (ASP.NET Core + AG-UI Endpoint)                            │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│         Microsoft.Agents.AI.Hosting.AGUI.AspNetCore         │
│  (Extension methods: MapAGUIAgent)                          │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│            Microsoft.Agents.AI.AGUI                         │
│  (ChatClientAGUIAdapter - converts IChatClient to events)   │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    AGUI Core                                │
│  (Event Models + SSE Serialization)                         │
└─────────────────────────────────────────────────────────────┘
```

## Project Structure

- **AGUI**: Core event models and SSE serialization utilities
- **Microsoft.Agents.AI.AGUI**: Adapter between Microsoft.Extensions.AI.IChatClient and AG-UI events
- **Microsoft.Agents.AI.Hosting.AGUI.AspNetCore**: ASP.NET Core hosting extensions
- **SimpleChat.API**: Sample application demonstrating AG-UI integration

## Event Flow

1. Client sends POST request to `/` with user message
2. `MapAGUIAgent` endpoint handler receives request
3. `ChatClientAGUIAdapter` creates event stream:
   - Emits `RUN_STARTED`
   - Streams `IChatClient` responses as `TEXT_MESSAGE_*` events
   - Emits `RUN_FINISHED` (or `RUN_ERROR` on failure)
4. Events serialized to SSE format via `AGUISerializer`
5. Client receives and renders streaming events in real-time

## Configuration

The application uses ASP.NET Core configuration system. You can set configuration via:

- **User Secrets** (recommended for development):
  ```bash
  dotnet user-secrets set "GitHubToken" "your-token"
  ```

- **Environment Variables**:
  ```bash
  export GitHubToken="your-token"
  dotnet run
  ```

- **appsettings.json** (not recommended for tokens):
  ```json
  {
    "GitHubToken": "your-token"
  }
  ```

## Development

### Build

```bash
dotnet build
```

### Test

```bash
# Test with curl
curl -X POST http://localhost:5018/ \
  -H "Content-Type: application/json" \
  -d '{"message":"Hello, world!"}'
```

## Troubleshooting

### "GitHubToken not found"

Make sure you've set the user secret:
```bash
dotnet user-secrets set "GitHubToken" "$(gh auth token)"
```

### CORS errors from dojo

The server is configured to allow requests from `localhost:3000` and `localhost:5173`. If your dojo runs on a different port, update the CORS policy in `Program.cs`.

### Connection refused

Make sure the server is running on port 5018. Check `Properties/launchSettings.json`.

## Next Steps

- Add support for tool calling (frontend tools)
- Implement conversation history management
- Add support for additional event types (STEP_STARTED, STEP_FINISHED)
- Add authentication/authorization

## License

See LICENSE file in the root directory.
