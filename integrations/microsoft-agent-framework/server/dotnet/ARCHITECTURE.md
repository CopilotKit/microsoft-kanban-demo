# AG-UI .NET Implementation - AIAgent Architecture

## Overview

The AG-UI .NET implementation is now based on **Microsoft Agent Framework's `AIAgent`** abstraction, providing a clean, type-safe way to expose AI agents through the AG-UI protocol.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│  Frontend (Dojo/React)                                  │
│  - TypeScript AG-UI Client                             │
└──────────────────┬──────────────────────────────────────┘
                   │ HTTP POST with AGUIRequest
                   │ Response: Server-Sent Events
                   ↓
┌─────────────────────────────────────────────────────────┐
│  ASP.NET Core Endpoint                                  │
│  - MapAGUIAgent(pattern, agent)                         │
│  - Deserializes AGUIRequest                             │
│  - Streams SSE responses                                │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ↓
┌─────────────────────────────────────────────────────────┐
│  AGUIAgent (Abstract Base)                              │
│  - RunAsync(AGUIRequest) → IAsyncEnumerable<SseItem>    │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ↓
┌─────────────────────────────────────────────────────────┐
│  ChatClientAGUIAgent (Implementation)                   │
│  - Wraps Microsoft.Agents.AI.ChatClientAgent            │
│  - Converts agent responses to AG-UI events             │
│  - Yields: RUN_STARTED → TEXT_MESSAGE_* → RUN_FINISHED  │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ↓
┌─────────────────────────────────────────────────────────┐
│  ChatClientAgent (Microsoft Agent Framework)            │
│  - RunStreamingAsync(messages)                          │
│  - Built on IChatClient abstraction                     │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ↓
┌─────────────────────────────────────────────────────────┐
│  IChatClient Implementation                             │
│  - OpenAI, Azure OpenAI, Ollama, etc.                   │
│  - GitHub Models (gpt-4o)                               │
└─────────────────────────────────────────────────────────┘
```

## Key Components

### 1. AGUIRequest (Typed Payload)

**File**: `AGUI/src/AGUIRequest.cs`

Strongly-typed C# class for AG-UI requests:

```csharp
public class AGUIRequest
{
    public string? ThreadId { get; set; }
    public string? RunId { get; set; }
    public required string Message { get; set; }
    public string? SystemPrompt { get; set; }
}
```

### 2. AGUIAgent (Base Abstraction)

**File**: `Microsoft.Agents.AI.AGUI/src/AGUIAgent.cs`

Abstract base class for all AG-UI agents:

```csharp
public abstract class AGUIAgent
{
    public string? Name { get; protected set; }
    public string? Description { get; protected set; }
    
    public abstract IAsyncEnumerable<SseItem<string>> RunAsync(
        AGUIRequest request,
        CancellationToken cancellationToken = default);
}
```

### 3. ChatClientAGUIAgent (Implementation)

**File**: `Microsoft.Agents.AI.AGUI/src/ChatClientAGUIAgent.cs`

Wraps `ChatClientAgent` from Microsoft Agent Framework:

```csharp
public class ChatClientAGUIAgent : AGUIAgent
{
    private readonly ChatClientAgent _chatClientAgent;
    
    public ChatClientAGUIAgent(ChatClientAgent chatClientAgent)
    {
        _chatClientAgent = chatClientAgent;
        Name = chatClientAgent.Name;
        Description = chatClientAgent.Description;
    }
    
    // Factory method for convenience
    public static ChatClientAGUIAgent FromChatClient(
        IChatClient chatClient,
        string? instructions = null,
        string? name = null,
        string? description = null);
    
    public override async IAsyncEnumerable<SseItem<string>> RunAsync(
        AGUIRequest request,
        CancellationToken cancellationToken = default)
    {
        // Yields AG-UI events: RUN_STARTED, TEXT_MESSAGE_*, RUN_FINISHED
    }
}
```

### 4. ASP.NET Core Extensions

**File**: `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore/src/AGUIExtensions.cs`

Extension methods for easy endpoint mapping:

```csharp
public static RouteHandlerBuilder MapAGUIAgent(
    this IEndpointRouteBuilder endpoints,
    string pattern,
    AGUIAgent agent)
{
    // Maps POST endpoint
    // Deserializes AGUIRequest from JSON body
    // Calls agent.RunAsync()
    // Streams SSE responses
}
```

## Usage Example

### SimpleChat.API/Program.cs

```csharp
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.AGUI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Get GitHub token from user secrets
var githubToken = builder.Configuration["GitHubToken"] 
    ?? throw new InvalidOperationException("GitHubToken not configured");

// Register AGUIAgent
builder.Services.AddSingleton<AGUIAgent>(serviceProvider =>
{
    // Create OpenAI client
    var openAiClient = new OpenAIClient(
        new System.ClientModel.ApiKeyCredential(githubToken), 
        new OpenAIClientOptions
        {
            Endpoint = new Uri("https://models.inference.ai.azure.com")
        });

    // Get chat client
    var chatClient = openAiClient.GetChatClient("gpt-4o").AsIChatClient();
    
    // Create ChatClientAgent with instructions
    var chatClientAgent = new ChatClientAgent(
        chatClient, 
        instructions: "You are a helpful assistant.",
        name: "SimpleChat",
        description: "A simple chat agent using GPT-4o");
    
    // Wrap in AGUIAgent
    return new ChatClientAGUIAgent(chatClientAgent);
});

var app = builder.Build();

// Map AG-UI endpoint
var agent = app.Services.GetRequiredService<AGUIAgent>();
app.MapAGUIAgent("/", agent);

app.Run();
```

## Benefits of AIAgent-Based Architecture

### 1. **Type Safety**
- `AGUIRequest` provides compile-time checking
- No manual JSON parsing in endpoint handler
- Clear contract between client and server

### 2. **Agent Framework Integration**
- Full access to Agent Framework features:
  - Function calling / tool use
  - Multi-turn conversations
  - Thread management
  - Structured output
- Works with any `AIAgent` implementation

### 3. **Separation of Concerns**
- `AGUIAgent` focuses on protocol conversion
- `ChatClientAgent` handles AI interaction
- Endpoint handler only deals with HTTP/SSE

### 4. **Extensibility**
Create custom AG-UI agents:

```csharp
public class MyCustomAGUIAgent : AGUIAgent
{
    public override async IAsyncEnumerable<SseItem<string>> RunAsync(
        AGUIRequest request,
        CancellationToken cancellationToken = default)
    {
        // Custom implementation
        // Can wrap any AIAgent: CopilotStudioAgent, custom agents, etc.
    }
}
```

### 5. **Testability**
- Mock `AGUIAgent` for unit tests
- Test endpoint handler separately
- Test agent logic independently

## Request/Response Flow

### Request

**HTTP POST to `/`**

```json
{
  "threadId": "thread-123",
  "runId": "run-456",
  "message": "Hello!",
  "systemPrompt": "You are a helpful assistant."
}
```

### Response

**Content-Type: `text/event-stream`**

```
event: RUN_STARTED
data: {"type":"RUN_STARTED","threadId":"thread-123","runId":"run-456"}

event: TEXT_MESSAGE_START
data: {"type":"TEXT_MESSAGE_START","messageId":"msg-789","role":"assistant"}

event: TEXT_MESSAGE_CONTENT
data: {"type":"TEXT_MESSAGE_CONTENT","messageId":"msg-789","delta":"Hello"}

event: TEXT_MESSAGE_CONTENT
data: {"type":"TEXT_MESSAGE_CONTENT","messageId":"msg-789","delta":"!"}

event: TEXT_MESSAGE_END
data: {"type":"TEXT_MESSAGE_END","messageId":"msg-789"}

event: RUN_FINISHED
data: {"type":"RUN_FINISHED","threadId":"thread-123","runId":"run-456"}
```

## Testing

### 1. PowerShell Script

```powershell
.\test-endpoint.ps1
```

### 2. Curl

```bash
curl -X POST http://localhost:5018/ \
  -H "Content-Type: application/json" \
  -d '{"message": "Hello!"}'
```

### 3. Dojo Frontend

```bash
cd typescript-sdk/apps/dojo
npm install
npm run dev
# Navigate to http://localhost:3000
```

## Dependencies

### Core Packages
- `Microsoft.Agents.AI` (v1.0.0-preview.251009.1) - Agent Framework
- `Microsoft.Extensions.AI` (v9.10.0) - AI abstractions
- `Microsoft.Extensions.AI.OpenAI` (v9.10.0-preview.1) - OpenAI integration
- `OpenAI` (v2.5.0) - OpenAI SDK
- `System.Net.ServerSentEvents` (v9.0.0) - SSE support

### Project Structure
```
AGUI (Core Protocol)
  ├─ Events (RUN_STARTED, TEXT_MESSAGE_*, etc.)
  ├─ AGUISerializer (Event → SSE conversion)
  └─ AGUIRequest (Typed request payload)

Microsoft.Agents.AI.AGUI (Agent Adapter)
  ├─ AGUIAgent (Abstract base)
  ├─ ChatClientAGUIAgent (ChatClientAgent wrapper)
  └─ [Future: Other agent wrappers]

Microsoft.Agents.AI.Hosting.AGUI.AspNetCore (Web Integration)
  └─ AGUIExtensions (MapAGUIAgent methods)

SimpleChat.API (Sample Application)
  └─ Program.cs (Configuration & setup)
```

## Migration from IChatClient-based Implementation

### Old Approach (Direct IChatClient)

```csharp
builder.Services.AddSingleton<IChatClient>(sp => chatClient);
app.MapAGUIAgent("/", chatClient);
```

### New Approach (AIAgent-based)

```csharp
builder.Services.AddSingleton<AGUIAgent>(sp => 
{
    var chatClientAgent = new ChatClientAgent(chatClient, instructions);
    return new ChatClientAGUIAgent(chatClientAgent);
});
app.MapAGUIAgent("/", app.Services.GetRequiredService<AGUIAgent>());
```

## Future Enhancements

### 1. Additional Agent Types
```csharp
public class CopilotStudioAGUIAgent : AGUIAgent
{
    private readonly CopilotStudioAgent _agent;
    // Wrap CopilotStudio agents
}
```

### 2. Custom Response Types
```csharp
public class AGUIResponse
{
    public string? Error { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
```

### 3. Middleware Support
```csharp
public class AGUIMiddleware : AGUIAgent
{
    public override IAsyncEnumerable<SseItem<string>> RunAsync(...)
    {
        // Pre-processing
        await foreach (var item in _innerAgent.RunAsync(...))
        {
            // Transform/augment events
            yield return item;
        }
        // Post-processing
    }
}
```

## Conclusion

The new AIAgent-based architecture provides:
- ✅ **Type safety** with `AGUIRequest`
- ✅ **Agent Framework integration** via `ChatClientAgent`
- ✅ **Clean abstraction** with `AGUIAgent` base class
- ✅ **Easy extensibility** for new agent types
- ✅ **Production-ready** SSE streaming
- ✅ **Dojo-compatible** event format

This architecture is ready for production use and can easily accommodate future AG-UI protocol enhancements.
