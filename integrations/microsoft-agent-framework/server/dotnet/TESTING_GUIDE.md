# AG-UI .NET Implementation - Testing Guide

This document provides comprehensive guidance for writing tests for the AG-UI .NET implementation.

## Test Projects Created

1. **AGUI.Tests** - Tests for core AG-UI protocol library
2. **Microsoft.Agents.AI.AGUI.Tests** - Tests for Microsoft Agent Framework integration
3. **Microsoft.Agents.AI.Hosting.AGUI.AspNetCore.Tests** - Tests for ASP.NET Core hosting
4. **SimpleChat.API.Tests** - Integration tests for the sample application

## Important Note on Message Construction

All Message classes (UserMessage, AssistantMessage, SystemMessage, DeveloperMessage, ToolMessage) use **required init properties**. They must be constructed using object initializer syntax:

```csharp
// ✅ CORRECT
var message = new UserMessage { Content = "Hello" };
var toolMsg = new ToolMessage 
{ 
    Content = "Result", 
    ToolCallId = "call-123" 
};

// ❌ INCORRECT
var message = new UserMessage("Hello");  // Does not compile!
```

## Test Structure Examples

### 1. AGUI.Tests - JSON Serialization Tests

```csharp
[Fact]
public void SerializeEvent_ShouldUseCamelCase()
{
    var options = new JsonSerializerOptions
    {
        TypeInfoResolver = AGUIJsonSerializerContext.Default
    };
    
    var evt = new RunStartedEvent("thread-1", "run-1");
    var json = JsonSerializer.Serialize(evt, options);
    
    Assert.Contains("\"threadId\":\"thread-1\"", json);
    Assert.Contains("\"type\":\"RUN_STARTED\"", json);
}
```

### 2. AGUI.Tests - HttpClientAGUIAgent Tests

**Note:** HttpClientAGUIAgent requires mocking HTTP responses. Use `RichardSzalay.MockHttp` package:

```csharp
[Fact]
public async Task RunAsync_ShouldParseSSEStream()
{
    var mockHttp = new MockHttpMessageHandler();
    var sseResponse = "event: RUN_STARTED\ndata: {\"type\":\"RUN_STARTED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n\n";
    
    mockHttp.When(HttpMethod.Post, "http://test.com/agent")
        .Respond("text/event-stream", sseResponse);
    
    var httpClient = mockHttp.ToHttpClient();
    httpClient.BaseAddress = new Uri("http://test.com");
    
    var agent = new HttpClientAGUIAgent(httpClient, "/agent");
    var input = new RunAgentInput
    {
        ThreadId = "t1",
        RunId = "r1",
        Messages = [new UserMessage { Content = "Test" }]
    };
    
    var events = new List<BaseEvent>();
    await foreach (var evt in agent.RunAsync(input))
    {
        events.Add(evt);
    }
    
    Assert.Contains(events, e => e is RunStartedEvent);
}
```

### 3. Microsoft.Agents.AI.AGUI.Tests - ChatClientAGUIAgent Tests

**Note:** Use Moq to mock IChatClient:

```csharp
[Fact]
public async Task RunAsync_ShouldConvertMessagesCorrectly()
{
    var mockChatClient = new Mock<IChatClient>();
    var capturedMessages = new List<ChatMessage>();
    
    mockChatClient
        .Setup(x => x.CompleteStreamingAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<ChatOptions>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync((IList<ChatMessage> messages, ChatOptions options, CancellationToken ct) =>
        {
            capturedMessages.AddRange(messages);
            return CreateAsyncEnumerable<StreamingChatCompletionUpdate>();
        });
    
    var chatClientAgent = new ChatClientAgent(mockChatClient.Object);
    var agent = new ChatClientAGUIAgent(chatClientAgent);
    
    var input = new RunAgentInput
    {
        ThreadId = "t1",
        RunId = "r1",
        Messages = [
            new UserMessage { Content = "User msg" },
            new AssistantMessage { Content = "Assistant msg" }
        ]
    };
    
    await foreach (var evt in agent.RunAsync(input))
    {
        // Consume events
    }
    
    Assert.Equal(2, capturedMessages.Count);
    Assert.Equal(ChatRole.User, capturedMessages[0].Role);
    Assert.Equal(ChatRole.Assistant, capturedMessages[1].Role);
}

// Helper method
private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(params T[] items)
{
    foreach (var item in items)
    {
        await Task.Yield();
        yield return item;
    }
}
```

### 4. Microsoft.Agents.AI.Hosting.AGUI.AspNetCore.Tests - MapAGUIAgent Tests

**Note:** Use `WebApplicationFactory` from `Microsoft.AspNetCore.Mvc.Testing` package:

```csharp
[Fact]
public async Task MapAGUIAgent_ShouldReturnEventStream()
{
    using var host = await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
            webBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapAGUIAgent("/", request =>
                        {
                            return new TestAGUIAgent();
                        });
                    });
                });
        })
        .StartAsync();
    
    var client = host.GetTestClient();
    var input = new RunAgentInput
    {
        ThreadId = "t1",
        RunId = "r1",
        Messages = [new UserMessage { Content = "Test" }]
    };
    
    var json = JsonSerializer.Serialize(input, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    
    var response = await client.PostAsync("/", content);
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("text/event-stream; charset=utf-8", 
        response.Content.Headers.ContentType?.ToString());
}

// Test agent implementation
private class TestAGUIAgent : AGUIAgent
{
    public override async IAsyncEnumerable<BaseEvent> RunAsync(
        RunAgentInput input,
        [System.Runtime.CompilerServices.EnumeratorCancellation] 
        CancellationToken cancellationToken = default)
    {
        yield return new RunStartedEvent(input.ThreadId, input.RunId);
        await Task.Yield();
        yield return new RunFinishedEvent(input.ThreadId, input.RunId);
    }
}
```

### 5. SimpleChat.API.Tests - Integration Tests

**Note:** First, add to `Program.cs`:

```csharp
app.Run();

// Make Program accessible to tests
public partial class Program { }
```

Then create `WebApplicationFactory`:

```csharp
public class SimpleChatWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace real AGUIAgent with mock
            services.AddSingleton<AGUIAgent>(sp => new MockAGUIAgent());
        });
    }
}

[Collection("SimpleChat")]
public class SimpleChatAPIIntegrationTests : IClassFixture<SimpleChatWebApplicationFactory>
{
    private readonly SimpleChatWebApplicationFactory _factory;
    
    public SimpleChatAPIIntegrationTests(SimpleChatWebApplicationFactory factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task Server_ShouldAcceptPostRequest()
    {
        var client = _factory.CreateClient();
        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = [new UserMessage { Content = "Hello" }]
        };
        
        var json = JsonSerializer.Serialize(input, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync("/", content);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

## Running All Tests

```powershell
# Build all projects
cd d:\work\ag-ui-dotnet\typescript-sdk\integrations\microsoft-agent-framework\server\dotnet
dotnet build

# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Run specific test project
dotnet test AGUI/test/AGUI.Tests.csproj
```

## Test Coverage Goals

### AGUI.Tests
- [x] JSON serialization with camelCase
- [ ] All event types serialize correctly
- [ ] RunAgentInput deserialization
- [ ] Null value handling (ignored in JSON)
- [ ] Role enum serialization
- [ ] HttpClientAGUIAgent HTTP request
- [ ] HttpClientAGUIAgent SSE parsing
- [ ] HttpClientAGUIAgent error handling
- [ ] HttpClientAGUIAgent cancellation

### Microsoft.Agents.AI.AGUI.Tests
- [ ] ChatClientAGUIAgent message conversion (User, Assistant, System, Developer, Tool)
- [ ] Event sequence (RUN_STARTED → TEXT_MESSAGE_* → RUN_FINISHED)
- [ ] Text delta streaming
- [ ] Error handling
- [ ] Cancellation token propagation
- [ ] Multi-turn conversations
- [ ] Empty content handling

### Microsoft.Agents.AI.Hosting.AGUI.AspNetCore.Tests
- [ ] POST endpoint mapping
- [ ] Content-Type: text/event-stream
- [ ] SSE format validation
- [ ] Request body deserialization
- [ ] Agent factory invocation
- [ ] Error handling (400, 500)
- [ ] Cancellation on client disconnect
- [ ] Source-generated JSON serialization

### SimpleChat.API.Tests
- [ ] Server startup
- [ ] Endpoint availability
- [ ] Event stream format
- [ ] Multiple concurrent requests
- [ ] Conversation history handling
- [ ] Round-trip with HttpClientAGUIAgent
- [ ] Large response handling

## Common Pitfalls

1. **Message Construction**: Always use object initializer syntax
2. **Async Enumerables**: Use `await foreach` and `yield return`
3. **Cancellation**: Test with `CancellationTokenSource`
4. **SSE Format**: Events must be `event: TYPE\ndata: JSON\n\n`
5. **JSON Options**: Use `AGUIJsonSerializerContext.Default` for source generation

## Debugging Tests

```powershell
# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~SerializeEvent"

# Debug in VS Code
# Add breakpoint, press F5, select ".NET Core Attach"
```

## Next Steps

1. Implement remaining test cases listed above
2. Ensure all tests pass
3. Verify code coverage reaches >80%
4. Test with real OpenAI/GitHub Models integration
5. Load test with multiple concurrent requests
