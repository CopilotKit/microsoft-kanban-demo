# AG-UI .NET Implementation Plan - Agentic Chat Scenario

Status: Draft
Scope: Minimal implementation to support agentic_chat scenario in the dojo

## Scenario Overview

The `agentic_chat` scenario provides a basic chat interface where:
- User sends messages to an AI agent
- Agent responds with streaming text responses
- Supports simple frontend tools (like `change_background`) handled by the frontend
- Uses Server-Sent Events (SSE) for real-time streaming

## Implementation Steps

### Step 1: Map agentic_chat endpoint in SimpleChat.API
- Configure SimpleChat.API to run on port 5018 (update `Properties/launchSettings.json`)
- Add POST `/` endpoint that returns `ServerSentEventsResult` from ASP.NET Core
- Use built-in `Results.Sse()` method for streaming responses
- Handle AG-UI request parsing and agent routing

### Step 2: Define AGUI hosting extension methods
- Create extension method in `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` project
- Method: `IServiceCollection.AddAGUI()` for DI registration
- Method: `WebApplication.MapAGUIAgent(string name, Func<> agentFactory)` for routing
- Leverage ASP.NET Core's built-in SSE infrastructure

### Step 3: Define AG-UI event models and parsing
- Create core AG-UI event types in `AGUI` core project
- Add `System.Net.ServerSentEvents` package reference to AGUI project
- Focus on event model definitions and JSON serialization
- Remove custom SSE serialization logic (use built-in instead)

### Step 4: Define AG-UI to Microsoft.Agents.AI adapter
- Create agent adapter in `Microsoft.Agents.AI.AGUI` project
- Convert `Microsoft.Agents.AI.IChatClient` responses to AG-UI events
- Use `IAsyncEnumerable<SseItem>` for streaming event generation
- Map conversation history between AG-UI messages and Microsoft.Agents.AI format

### Step 5: Wire Microsoft Agent Framework chat client
- Configure `Microsoft.Agents.AI.IChatClient` in SimpleChat.API
- Create basic agent that responds to user messages
- Generate `IAsyncEnumerable<SseItem>` with AG-UI events
- Return via `Results.Sse(eventStream)` from endpoint

## Required AG-UI Events for Agentic Chat

The minimal set of events needed:
1. `RUN_STARTED` - Indicates the agent has started processing
2. `TEXT_MESSAGE_START` - Begins a new text message from the agent
3. `TEXT_MESSAGE_CONTENT` - Streaming text deltas for the message
4. `TEXT_MESSAGE_END` - Completes the text message
5. `RUN_FINISHED` - Indicates the agent has finished processing

## Technical Decisions

- **Use `System.Net.ServerSentEvents` package** instead of custom SSE implementation
- **Leverage `ServerSentEventsResult`** from ASP.NET Core for streaming
- **Focus AGUI core on event models** rather than transport serialization
- **Generate `IAsyncEnumerable<SseItem>`** for event streaming
- **Remove custom EventStreamWriter** dependency

## Package Dependencies

- `AGUI` project: Add `System.Net.ServerSentEvents` package reference
- `SimpleChat.API`: Use built-in ASP.NET Core SSE support (no additional packages)
- `Microsoft.Agents.AI.AGUI`: Reference AGUI core for event models

## Success Criteria

- [ ] AGUI project generates proper AG-UI event models
- [ ] Microsoft.Agents.AI adapter converts responses to `IAsyncEnumerable<SseItem>`
- [ ] SimpleChat.API endpoint returns `Results.Sse(eventStream)`
- [ ] Dojo receives properly formatted SSE events
- [ ] Chat interface works end-to-end

## Out of Scope

- Tool calling (backend tools)
- State management
- Human-in-the-loop workflows
- Complex project structure beyond existing layout
- Protocol Buffers support
- WebSocket transport
- Multiple agent support

## Next Steps

This plan provides the foundation for implementing the minimal agentic_chat scenario. The implementation should focus on getting basic chat functionality working with Microsoft Agent Framework integration before adding any additional features.