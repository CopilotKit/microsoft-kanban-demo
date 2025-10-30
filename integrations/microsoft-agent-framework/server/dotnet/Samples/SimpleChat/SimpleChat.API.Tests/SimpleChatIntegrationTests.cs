using System.Net.Http.Json;
using AGUI;
using AGUI.Events;
using AGUI.Messages;
using Xunit;

#pragma warning disable CA1859 // Use concrete types when possible for improved performance
// We use AGUIAgent interface intentionally for these tests

namespace SimpleChat.API.Tests;

public class SimpleChatIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SimpleChatIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Endpoint_Returns_TextEventStream_ContentType()
    {
        // Arrange
        var client = _factory.CreateClient();
        var input = new RunAgentInput
        {
            ThreadId = "test-thread-1",
            RunId = "test-run-1",
            Messages = [new UserMessage { Id = "msg-1", Content = "Hello!" }]
        };

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = JsonContent.Create(input, options: AGUIJsonSerializerContext.Default.Options)
        };
        var response = await client.SendAsync(request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task HttpClientAGUIAgent_Receives_Complete_Event_Sequence()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var agent = new HttpClientAGUIAgent(httpClient, "/");
        
        var input = new RunAgentInput
        {
            ThreadId = "test-thread-2",
            RunId = "test-run-2",
            Messages = [new UserMessage { Id = "msg-2", Content = "Hello!" }]
        };

        // Act
        var events = await CollectEventsAsync(agent.RunAsync(input));

        // Assert
        Assert.NotEmpty(events);
        
        // Verify event sequence
        Assert.Contains(events, e => e is RunStartedEvent);
        Assert.Contains(events, e => e is TextMessageStartEvent);
        Assert.Contains(events, e => e is TextMessageContentEvent);
        Assert.Contains(events, e => e is TextMessageEndEvent);
        Assert.Contains(events, e => e is RunFinishedEvent);
    }

    [Fact]
    public async Task HttpClientAGUIAgent_Returns_Expected_Text_Content()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var agent = new HttpClientAGUIAgent(httpClient, "/");
        
        var input = new RunAgentInput
        {
            ThreadId = "test-thread-3",
            RunId = "test-run-3",
            Messages = [new UserMessage { Id = "msg-3", Content = "Hello!" }]
        };

        // Act
        var events = await CollectEventsAsync(agent.RunAsync(input));
        var contentEvents = events.OfType<TextMessageContentEvent>().ToList();

        // Assert
        Assert.NotEmpty(contentEvents);
        
        var fullContent = string.Join("", contentEvents.Select(e => e.Delta));
        Assert.Equal("Hello! I'm a test response.", fullContent);
    }

    [Fact]
    public async Task HttpClientAGUIAgent_Handles_Multiple_Message_Exchanges()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var agent = new HttpClientAGUIAgent(httpClient, "/");
        
        var input = new RunAgentInput
        {
            ThreadId = "test-thread-4",
            RunId = "test-run-4",
            Messages =
            [
                new UserMessage { Id = "msg-4a", Content = "First message" },
                new AssistantMessage { Id = "msg-4b", Content = "Response 1" },
                new UserMessage { Id = "msg-4c", Content = "Second message" }
            ]
        };

        // Act
        var events = await CollectEventsAsync(agent.RunAsync(input));

        // Assert
        Assert.Contains(events, e => e is RunStartedEvent);
        Assert.Contains(events, e => e is TextMessageContentEvent);
        Assert.Contains(events, e => e is RunFinishedEvent);
    }

    [Fact]
    public async Task HttpClientAGUIAgent_Respects_CancellationToken()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var agent = new HttpClientAGUIAgent(httpClient, "/");
        
        var input = new RunAgentInput
        {
            ThreadId = "test-thread-5",
            RunId = "test-run-5",
            Messages = [new UserMessage { Id = "msg-5", Content = "Hello!" }]
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in agent.RunAsync(input, cts.Token))
            {
                await Task.Delay(100, CancellationToken.None); // Delay to ensure cancellation happens
            }
        });
    }

    [Fact]
    public async Task Integration_Test_Runs_Without_External_Dependencies()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var agent = new HttpClientAGUIAgent(httpClient, "/");
        
        var input = new RunAgentInput
        {
            ThreadId = "test-thread-6",
            RunId = "test-run-6",
            Messages = [new UserMessage { Id = "msg-6", Content = "Test message" }]
        };

        // Act
        var events = await CollectEventsAsync(agent.RunAsync(input));

        // Assert
        // This test verifies the entire pipeline works:
        // HttpClientAGUIAgent → HTTP POST → ASP.NET Core → ChatClientAGUIAgent → TestChatClient → AG-UI events → SSE → back to HttpClientAGUIAgent
        Assert.NotEmpty(events);
        Assert.All(events, e => Assert.NotNull(e));
    }

    [Fact]
    public async Task Event_Sequence_Maintains_Correct_Order()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var agent = new HttpClientAGUIAgent(httpClient, "/");
        
        var input = new RunAgentInput
        {
            ThreadId = "test-thread-7",
            RunId = "test-run-7",
            Messages = [new UserMessage { Id = "msg-7", Content = "Hello!" }]
        };

        // Act
        var events = await CollectEventsAsync(agent.RunAsync(input));

        // Assert
        var runStartedIndex = events.FindIndex(e => e is RunStartedEvent);
        var textMessageStartIndex = events.FindIndex(e => e is TextMessageStartEvent);
        var textMessageContentIndex = events.FindIndex(e => e is TextMessageContentEvent);
        var textMessageEndIndex = events.FindIndex(e => e is TextMessageEndEvent);
        var runFinishedIndex = events.FindIndex(e => e is RunFinishedEvent);

        Assert.True(runStartedIndex >= 0, "RunStartedEvent should be present");
        Assert.True(textMessageStartIndex > runStartedIndex, "TextMessageStartEvent should come after RunStartedEvent");
        Assert.True(textMessageContentIndex > textMessageStartIndex, "TextMessageContentEvent should come after TextMessageStartEvent");
        Assert.True(textMessageEndIndex > textMessageContentIndex, "TextMessageEndEvent should come after TextMessageContentEvent");
        Assert.True(runFinishedIndex > textMessageEndIndex, "RunFinishedEvent should come after TextMessageEndEvent");
    }

    [Fact]
    public async Task TextMessageStartEvent_Contains_Valid_MessageId()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var agent = new HttpClientAGUIAgent(httpClient, "/");
        
        var input = new RunAgentInput
        {
            ThreadId = "test-thread-8",
            RunId = "test-run-8",
            Messages = [new UserMessage { Id = "msg-8", Content = "Hello!" }]
        };

        // Act
        var events = await CollectEventsAsync(agent.RunAsync(input));
        var textMessageStart = events.OfType<TextMessageStartEvent>().FirstOrDefault();

        // Assert
        Assert.NotNull(textMessageStart);
        Assert.False(string.IsNullOrWhiteSpace(textMessageStart.MessageId));
    }

    [Fact]
    public async Task TextMessageEndEvent_Has_Matching_MessageId()
    {
        // Arrange
        var httpClient = _factory.CreateClient();
        var agent = new HttpClientAGUIAgent(httpClient, "/");
        
        var input = new RunAgentInput
        {
            ThreadId = "test-thread-9",
            RunId = "test-run-9",
            Messages = [new UserMessage { Id = "msg-9", Content = "Hello!" }]
        };

        // Act
        var events = await CollectEventsAsync(agent.RunAsync(input));
        var textMessageStart = events.OfType<TextMessageStartEvent>().FirstOrDefault();
        var textMessageEnd = events.OfType<TextMessageEndEvent>().FirstOrDefault();

        // Assert
        Assert.NotNull(textMessageStart);
        Assert.NotNull(textMessageEnd);
        Assert.Equal(textMessageStart.MessageId, textMessageEnd.MessageId);
    }

    private static async Task<List<BaseEvent>> CollectEventsAsync(IAsyncEnumerable<BaseEvent> events)
    {
        var result = new List<BaseEvent>();
        await foreach (var @event in events)
        {
            result.Add(@event);
        }
        return result;
    }
}
