using System.Net;
using System.Text;
using AGUI;
using AGUI.Events;
using AGUI.Messages;

namespace AGUI.Tests;

/// <summary>
/// Test double for HttpMessageHandler that returns canned responses.
/// </summary>
class TestDelegatingHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responseFunc;

    public TestDelegatingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFunc)
    {
        _responseFunc = responseFunc;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = await _responseFunc(request);
        return response;
    }

    public static TestDelegatingHandler CreateWithSSE(string sseContent)
    {
        return new TestDelegatingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
        }));
    }

    public static TestDelegatingHandler CreateWithStatus(HttpStatusCode statusCode)
    {
        return new TestDelegatingHandler(_ => Task.FromResult(new HttpResponseMessage(statusCode)));
    }
}

/// <summary>
/// Tests for HttpClientAGUIAgent which communicates with remote AG-UI servers via HTTP and parses SSE events.
/// </summary>
public class HttpClientAGUIAgentTests
{
    [Fact]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new HttpClientAGUIAgent(null!, "http://localhost/agui"));
    }

    [Fact]
    public void Constructor_WithNullEndpoint_ThrowsArgumentNullException()
    {
        using var httpClient = new HttpClient();
        
        Assert.Throws<ArgumentNullException>(() =>
            new HttpClientAGUIAgent(httpClient, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsProperties()
    {
        using var httpClient = new HttpClient();
        
        var agent = new HttpClientAGUIAgent(
            httpClient, 
            "http://localhost/agui",
            name: "TestAgent",
            description: "Test Description");

        Assert.Equal("TestAgent", agent.Name);
        Assert.Equal("Test Description", agent.Description);
    }

    [Fact]
    public async Task RunAsync_WithSuccessfulResponse_ParsesSSEEvents()
    {
        // Arrange
        var sseResponse = "data: {\"type\":\"RUN_STARTED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n\ndata: {\"type\":\"TEXT_MESSAGE_START\",\"messageId\":\"m1\",\"role\":\"assistant\"}\n\ndata: {\"type\":\"TEXT_MESSAGE_CONTENT\",\"messageId\":\"m1\",\"delta\":\"Hello\"}\n\ndata: {\"type\":\"TEXT_MESSAGE_END\",\"messageId\":\"m1\"}\n\ndata: {\"type\":\"RUN_FINISHED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n\n";

        var handler = TestDelegatingHandler.CreateWithSSE(sseResponse);
        using var httpClient = new HttpClient(handler);
        var agent = new HttpClientAGUIAgent(httpClient, "http://localhost/agui");

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = []
        };

        // Act
        var events = new List<BaseEvent>();
        await foreach (var evt in agent.RunAsync(input))
        {
            events.Add(evt);
        }

        // Assert
        Assert.Equal(5, events.Count);
        Assert.IsType<RunStartedEvent>(events[0]);
        Assert.IsType<TextMessageStartEvent>(events[1]);
        Assert.IsType<TextMessageContentEvent>(events[2]);
        Assert.IsType<TextMessageEndEvent>(events[3]);
        Assert.IsType<RunFinishedEvent>(events[4]);
    }

    [Fact]
    public async Task RunAsync_ParsesRunStartedEvent_Correctly()
    {
        // Arrange
        var sseResponse = "data: {\"type\":\"RUN_STARTED\",\"threadId\":\"thread-123\",\"runId\":\"run-456\"}\n\n";

        var handler = TestDelegatingHandler.CreateWithSSE(sseResponse);
        using var httpClient = new HttpClient(handler);
        var agent = new HttpClientAGUIAgent(httpClient, "http://localhost/agui");

        var input = new RunAgentInput
        {
            ThreadId = "thread-123",
            RunId = "run-456",
            Messages = []
        };

        // Act
        var events = new List<BaseEvent>();
        await foreach (var evt in agent.RunAsync(input))
        {
            events.Add(evt);
        }

        // Assert
        var startEvent = Assert.IsType<RunStartedEvent>(events.Single());
        Assert.Equal("thread-123", startEvent.ThreadId);
        Assert.Equal("run-456", startEvent.RunId);
    }

    [Fact]
    public async Task RunAsync_ParsesTextMessageContentEvents_Correctly()
    {
        // Arrange
        var sseResponse = "data: {\"type\":\"TEXT_MESSAGE_CONTENT\",\"messageId\":\"m1\",\"delta\":\"Hello\"}\n\ndata: {\"type\":\"TEXT_MESSAGE_CONTENT\",\"messageId\":\"m1\",\"delta\":\" World\"}\n\ndata: {\"type\":\"TEXT_MESSAGE_CONTENT\",\"messageId\":\"m1\",\"delta\":\"!\"}\n\n";

        var handler = TestDelegatingHandler.CreateWithSSE(sseResponse);
        using var httpClient = new HttpClient(handler);
        var agent = new HttpClientAGUIAgent(httpClient, "http://localhost/agui");

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = []
        };

        // Act
        var events = new List<BaseEvent>();
        await foreach (var evt in agent.RunAsync(input))
        {
            events.Add(evt);
        }

        // Assert
        Assert.Equal(3, events.Count);
        var contentEvents = events.OfType<TextMessageContentEvent>().ToList();
        Assert.Equal(3, contentEvents.Count);
        Assert.Equal("Hello", contentEvents[0].Delta);
        Assert.Equal(" World", contentEvents[1].Delta);
        Assert.Equal("!", contentEvents[2].Delta);
        
        // All should have same message ID
        Assert.All(contentEvents, e => Assert.Equal("m1", e.MessageId));
    }

    [Fact]
    public async Task RunAsync_ParsesRunErrorEvent_Correctly()
    {
        // Arrange
        var sseResponse = "data: {\"type\":\"RUN_ERROR\",\"message\":\"Something went wrong\",\"code\":\"ERR_123\"}\n\n";

        var handler = TestDelegatingHandler.CreateWithSSE(sseResponse);
        using var httpClient = new HttpClient(handler);
        var agent = new HttpClientAGUIAgent(httpClient, "http://localhost/agui");

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = []
        };

        // Act
        var events = new List<BaseEvent>();
        await foreach (var evt in agent.RunAsync(input))
        {
            events.Add(evt);
        }

        // Assert
        var errorEvent = Assert.IsType<RunErrorEvent>(events.Single());
        Assert.Equal("Something went wrong", errorEvent.Message);
        Assert.Equal("ERR_123", errorEvent.Code);
    }

    [Fact]
    public async Task RunAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = TestDelegatingHandler.CreateWithStatus(HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler);
        var agent = new HttpClientAGUIAgent(httpClient, "http://localhost/agui");

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = []
        };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (var evt in agent.RunAsync(input))
            {
                // Should not get here
            }
        });
    }

    [Fact]
    public async Task RunAsync_SendsCorrectJsonPayload()
    {
        // Arrange
        string? capturedContent = null;
        var handler = new TestDelegatingHandler(async request =>
        {
            capturedContent = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("", Encoding.UTF8, "text/event-stream")
            };
        });

        using var httpClient = new HttpClient(handler);
        var agent = new HttpClientAGUIAgent(httpClient, "http://localhost/agui");

        var input = new RunAgentInput
        {
            ThreadId = "thread-abc",
            RunId = "run-xyz",
            Messages = 
            [
                new UserMessage { Id = "m1", Content = "Hello" }
            ]
        };

        // Act
        await foreach (var evt in agent.RunAsync(input))
        {
            // Consume stream
        }

        // Assert
        Assert.NotNull(capturedContent);
        Assert.Contains("\"threadId\":\"thread-abc\"", capturedContent);
        Assert.Contains("\"runId\":\"run-xyz\"", capturedContent);
        Assert.Contains("\"Hello\"", capturedContent);
    }

    [Fact]
    public async Task RunAsync_WithCancellationToken_StopsProcessing()
    {
        // Arrange
        // Create a long SSE stream
        var longStream = string.Join("\n\n", Enumerable.Range(0, 100).Select(i =>
            $"data: {{\"type\":\"TEXT_MESSAGE_CONTENT\",\"messageId\":\"m1\",\"delta\":\"Word {i}\"}}")); 

        var handler = TestDelegatingHandler.CreateWithSSE(longStream);
        using var httpClient = new HttpClient(handler);
        var agent = new HttpClientAGUIAgent(httpClient, "http://localhost/agui");        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = []
        };

        using var cts = new CancellationTokenSource();
        var events = new List<BaseEvent>();

        // Act
        try
        {
            await foreach (var evt in agent.RunAsync(input, cts.Token))
            {
                events.Add(evt);
                
                // Cancel after first event
                if (events.Count == 1)
                {
                    await cts.CancelAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        // Assert - should have stopped processing
        Assert.True(events.Count < 100);
    }

    [Fact]
    public async Task RunAsync_WithUnknownEventType_SkipsEvent()
    {
        // Arrange
        var sseResponse = "data: {\"type\":\"RUN_STARTED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n\ndata: {\"type\":\"UNKNOWN_EVENT_TYPE\",\"someData\":\"ignored\"}\n\ndata: {\"type\":\"RUN_FINISHED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n\n";

        var handler = TestDelegatingHandler.CreateWithSSE(sseResponse);
        using var httpClient = new HttpClient(handler);
        var agent = new HttpClientAGUIAgent(httpClient, "http://localhost/agui");

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = []
        };

        // Act
        var events = new List<BaseEvent>();
        await foreach (var evt in agent.RunAsync(input))
        {
            events.Add(evt);
        }

        // Assert - unknown event should be skipped
        Assert.Equal(2, events.Count);
        Assert.IsType<RunStartedEvent>(events[0]);
        Assert.IsType<RunFinishedEvent>(events[1]);
    }

    [Fact]
    public async Task RunAsync_WithMalformedJson_SkipsEvent()
    {
        // Arrange
        var sseResponse = "data: {\"type\":\"RUN_STARTED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n\ndata: {invalid json here}\n\ndata: {\"type\":\"RUN_FINISHED\",\"threadId\":\"t1\",\"runId\":\"r1\"}\n\n";

        var handler = TestDelegatingHandler.CreateWithSSE(sseResponse);
        using var httpClient = new HttpClient(handler);
        var agent = new HttpClientAGUIAgent(httpClient, "http://localhost/agui");

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = []
        };

        // Act
        var events = new List<BaseEvent>();
        await foreach (var evt in agent.RunAsync(input))
        {
            events.Add(evt);
        }

        // Assert - malformed event should be skipped
        Assert.Equal(2, events.Count);
        Assert.IsType<RunStartedEvent>(events[0]);
        Assert.IsType<RunFinishedEvent>(events[1]);
    }

    [Fact]
    public async Task RunAsync_WithCompleteConversation_ParsesAllEventTypes()
    {
        // Arrange
        var sseResponse = "data: {\"type\":\"RUN_STARTED\",\"threadId\":\"conv-1\",\"runId\":\"run-1\"}\n\ndata: {\"type\":\"TEXT_MESSAGE_START\",\"messageId\":\"msg-1\",\"role\":\"assistant\"}\n\ndata: {\"type\":\"TEXT_MESSAGE_CONTENT\",\"messageId\":\"msg-1\",\"delta\":\"Hello\"}\n\ndata: {\"type\":\"TEXT_MESSAGE_CONTENT\",\"messageId\":\"msg-1\",\"delta\":\", how\"}\n\ndata: {\"type\":\"TEXT_MESSAGE_CONTENT\",\"messageId\":\"msg-1\",\"delta\":\" can I help?\"}\n\ndata: {\"type\":\"TEXT_MESSAGE_END\",\"messageId\":\"msg-1\"}\n\ndata: {\"type\":\"RUN_FINISHED\",\"threadId\":\"conv-1\",\"runId\":\"run-1\"}\n\n";

        var handler = TestDelegatingHandler.CreateWithSSE(sseResponse);
        using var httpClient = new HttpClient(handler);
        var agent = new HttpClientAGUIAgent(httpClient, "http://localhost/agui");

        var input = new RunAgentInput
        {
            ThreadId = "conv-1",
            RunId = "run-1",
            Messages = 
            [
                new UserMessage { Id = "m0", Content = "Hi" }
            ]
        };

        // Act
        var events = new List<BaseEvent>();
        await foreach (var evt in agent.RunAsync(input))
        {
            events.Add(evt);
        }

        // Assert
        Assert.Equal(7, events.Count);
        
        // Validate sequence
        var runStart = Assert.IsType<RunStartedEvent>(events[0]);
        Assert.Equal("conv-1", runStart.ThreadId);
        
        var msgStart = Assert.IsType<TextMessageStartEvent>(events[1]);
        Assert.Equal("msg-1", msgStart.MessageId);
        
        var contents = events.Skip(2).Take(3).OfType<TextMessageContentEvent>().ToList();
        Assert.Equal(3, contents.Count);
        Assert.Equal("Hello", contents[0].Delta);
        Assert.Equal(", how", contents[1].Delta);
        Assert.Equal(" can I help?", contents[2].Delta);
        
        var msgEnd = Assert.IsType<TextMessageEndEvent>(events[5]);
        Assert.Equal("msg-1", msgEnd.MessageId);
        
        var runFinish = Assert.IsType<RunFinishedEvent>(events[6]);
        Assert.Equal("conv-1", runFinish.ThreadId);
    }
}
