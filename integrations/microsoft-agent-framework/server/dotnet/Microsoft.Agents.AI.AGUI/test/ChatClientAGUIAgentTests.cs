using AGUI;
using AGUI.Events;
using AGUI.Messages;
using Microsoft.Extensions.AI;

namespace Microsoft.Agents.AI.AGUI.Tests;

/// <summary>
/// Test double for IChatClient that returns canned streaming responses.
/// </summary>
class TestChatClient : IChatClient
{
    private readonly Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>> _streamingFunc;
    private readonly Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>> _completeFunc;

    public ChatClientMetadata Metadata { get; }

    public TestChatClient(
        Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? streamingFunc = null,
        Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>? completeFunc = null,
        ChatClientMetadata? metadata = null)
    {
        _streamingFunc = streamingFunc ?? DefaultStreamingFunc;
        _completeFunc = completeFunc ?? DefaultCompleteFunc;
        Metadata = metadata ?? new ChatClientMetadata("TestClient");
    }

    public IEnumerable<ChatMessage>? LastMessages { get; private set; }
    public ChatOptions? LastOptions { get; private set; }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastMessages = chatMessages;
        LastOptions = options;

        await foreach (var update in _streamingFunc(chatMessages, options, cancellationToken))
        {
            yield return update;
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        LastMessages = chatMessages;
        LastOptions = options;
        return _completeFunc(chatMessages, options, cancellationToken);
    }

    public void Dispose() { }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public TService? GetService<TService>(object? key = null) where TService : class => this.GetService(typeof(TService), key) as TService;

    private static async IAsyncEnumerable<ChatResponseUpdate> DefaultStreamingFunc(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, "Hello");
        await Task.CompletedTask;
    }

    private static Task<ChatResponse> DefaultCompleteFunc(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "Hello")]));
    }

    public static TestChatClient CreateWithStreamingResponses(params string[] textChunks)
    {
        async IAsyncEnumerable<ChatResponseUpdate> StreamFunc(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            foreach (var chunk in textChunks)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
                await Task.Yield();
            }
        }

        return new TestChatClient(streamingFunc: StreamFunc);
    }

    public static TestChatClient CreateWithError(Exception exception)
    {
        async IAsyncEnumerable<ChatResponseUpdate> StreamFunc(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.Yield();
            throw exception;
#pragma warning disable CS0162 // Unreachable code detected
            yield break;
#pragma warning restore CS0162
        }

        return new TestChatClient(streamingFunc: StreamFunc);
    }

    public static TestChatClient CreateWithCancellationSupport()
    {
        async IAsyncEnumerable<ChatResponseUpdate> StreamFunc(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            for (int i = 0; i < 100; i++)
            {
                ct.ThrowIfCancellationRequested();
                yield return new ChatResponseUpdate(ChatRole.Assistant, $"Chunk {i}");
                await Task.Delay(10, ct);
            }
        }

        return new TestChatClient(streamingFunc: StreamFunc);
    }

    public static TestChatClient CreateWithEmptyResponse()
    {
        async IAsyncEnumerable<ChatResponseUpdate> StreamFunc(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }

        return new TestChatClient(streamingFunc: StreamFunc);
    }
}

/// <summary>
/// Tests for ChatClientAGUIAgent which wraps Microsoft Agent Framework's ChatClientAgent.
/// </summary>
public class ChatClientAGUIAgentTests
{
    // Helper method to convert IAsyncEnumerable to List
    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
        {
            list.Add(item);
        }
        return list;
    }
    [Fact]
    public void Constructor_WithNullChatClientAgent_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ChatClientAGUIAgent(null!));
    }

    [Fact]
    public void Constructor_WithValidChatClientAgent_SetsProperties()
    {
        // Arrange
        var chatClient = new TestChatClient();
        var chatClientAgent = new ChatClientAgent(
            chatClient,
            instructions: "Test instructions",
            name: "TestAgent",
            description: "Test Description");

        // Act
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        // Assert
        Assert.Equal("TestAgent", agent.Name);
        Assert.Equal("Test Description", agent.Description);
    }

    [Fact]
    public void FromChatClient_WithMinimalParameters_CreatesAgent()
    {
        // Arrange
        var chatClient = new TestChatClient();

        // Act
        var agent = ChatClientAGUIAgent.FromChatClient(chatClient);

        // Assert
        Assert.NotNull(agent);
    }

    [Fact]
    public void FromChatClient_WithAllParameters_CreatesAgentWithProperties()
    {
        // Arrange
        var chatClient = new TestChatClient();

        // Act
        var agent = ChatClientAGUIAgent.FromChatClient(
            chatClient,
            instructions: "Test instructions",
            name: "TestAgent",
            description: "Test Description");

        // Assert
        Assert.Equal("TestAgent", agent.Name);
        Assert.Equal("Test Description", agent.Description);
    }

    [Fact]
    public async Task RunAsync_WithSingleUserMessage_EmitsCorrectEventSequence()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithStreamingResponses("Hello", " there!");
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "thread-1",
            RunId = "run-1",
            Messages =
            [
                new UserMessage { Id = "msg-1", Content = "Hi" }
            ]
        };

        // Act
        var events = new List<BaseEvent>();
        await foreach (var evt in agent.RunAsync(input))
        {
            events.Add(evt);
        }

        // Assert
        Assert.Equal(6, events.Count);

        // RUN_STARTED
        var runStarted = Assert.IsType<RunStartedEvent>(events[0]);
        Assert.Equal("thread-1", runStarted.ThreadId);
        Assert.Equal("run-1", runStarted.RunId);

        // TEXT_MESSAGE_START
        var msgStart = Assert.IsType<TextMessageStartEvent>(events[1]);
        Assert.Equal("assistant", msgStart.Role);
        Assert.NotNull(msgStart.MessageId);

        // TEXT_MESSAGE_CONTENT (first chunk)
        var msgContent1 = Assert.IsType<TextMessageContentEvent>(events[2]);
        Assert.Equal(msgStart.MessageId, msgContent1.MessageId);
        Assert.Equal("Hello", msgContent1.Delta);

        // TEXT_MESSAGE_CONTENT (second chunk)
        var msgContent2 = Assert.IsType<TextMessageContentEvent>(events[3]);
        Assert.Equal(msgStart.MessageId, msgContent2.MessageId);
        Assert.Equal(" there!", msgContent2.Delta);

        // TEXT_MESSAGE_END
        var msgEnd = Assert.IsType<TextMessageEndEvent>(events[4]);
        Assert.Equal(msgStart.MessageId, msgEnd.MessageId);

        // RUN_FINISHED
        var runFinished = Assert.IsType<RunFinishedEvent>(events[5]);
        Assert.Equal("thread-1", runFinished.ThreadId);
        Assert.Equal("run-1", runFinished.RunId);
    }

    [Fact]
    public async Task RunAsync_ConvertsAGUIMessagesToChatMessages()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithStreamingResponses("Response");
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "thread-1",
            RunId = "run-1",
            Messages =
            [
                new DeveloperMessage { Id = "msg-1", Content = "System prompt" },
                new UserMessage { Id = "msg-2", Content = "Hello" },
                new AssistantMessage { Id = "msg-3", Content = "Hi there!" },
                new UserMessage { Id = "msg-4", Content = "How are you?" }
            ]
        };

        // Act
        var events = await ToListAsync(agent.RunAsync(input));

        // Assert
        Assert.NotNull(chatClient.LastMessages);
        var messagesList = chatClient.LastMessages.ToList();
        Assert.Equal(4, messagesList.Count);
        Assert.Equal(ChatRole.System, messagesList[0].Role);
        Assert.Equal("System prompt", messagesList[0].Text);
        Assert.Equal(ChatRole.User, messagesList[1].Role);
        Assert.Equal("Hello", messagesList[1].Text);
        Assert.Equal(ChatRole.Assistant, messagesList[2].Role);
        Assert.Equal("Hi there!", messagesList[2].Text);
        Assert.Equal(ChatRole.User, messagesList[3].Role);
        Assert.Equal("How are you?", messagesList[3].Text);
    }

    [Fact]
    public async Task RunAsync_WithMultipleTextDeltas_EmitsMultipleContentEvents()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithStreamingResponses(
            "The", " quick", " brown", " fox", " jumps");
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = [new UserMessage { Id = "m1", Content = "Test" }]
        };

        // Act
        var events = await ToListAsync(agent.RunAsync(input));

        // Assert
        var contentEvents = events.OfType<TextMessageContentEvent>().ToList();
        Assert.Equal(5, contentEvents.Count);
        Assert.Equal("The", contentEvents[0].Delta);
        Assert.Equal(" quick", contentEvents[1].Delta);
        Assert.Equal(" brown", contentEvents[2].Delta);
        Assert.Equal(" fox", contentEvents[3].Delta);
        Assert.Equal(" jumps", contentEvents[4].Delta);

        // All content events should have the same messageId
        Assert.True(contentEvents.All(e => e.MessageId == contentEvents[0].MessageId));
    }

    [Fact]
    public async Task RunAsync_WithEmptyResponse_OnlyEmitsRunEvents()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithEmptyResponse();
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = [new UserMessage { Id = "m1", Content = "Test" }]
        };

        // Act
        var events = await ToListAsync(agent.RunAsync(input));

        // Assert
        Assert.Equal(2, events.Count);
        Assert.IsType<RunStartedEvent>(events[0]);
        Assert.IsType<RunFinishedEvent>(events[1]);
    }

    [Fact]
    public async Task RunAsync_WithSystemMessage_ConvertsToChatRole()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithStreamingResponses("Response");
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages =
            [
                new SystemMessage { Id = "m1", Content = "System message" }
            ]
        };

        // Act
        await ToListAsync(agent.RunAsync(input));

        // Assert
        Assert.NotNull(chatClient.LastMessages);
        var messagesList = chatClient.LastMessages.ToList();
        Assert.Single(messagesList);
        Assert.Equal(ChatRole.System, messagesList[0].Role);
        Assert.Equal("System message", messagesList[0].Text);
    }

    [Fact]
    public async Task RunAsync_WithToolMessage_ConvertsToChatRole()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithStreamingResponses("Response");
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages =
            [
                new ToolMessage { Id = "m1", Content = "Tool result", ToolCallId = "tool-1" }
            ]
        };

        // Act
        await ToListAsync(agent.RunAsync(input));

        // Assert
        Assert.NotNull(chatClient.LastMessages);
        var messagesList = chatClient.LastMessages.ToList();
        Assert.Single(messagesList);
        Assert.Equal(ChatRole.Tool, messagesList[0].Role);
        Assert.Equal("Tool result", messagesList[0].Text);
    }

    [Fact]
    public async Task RunAsync_WithNullMessageContent_SkipsMessage()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithStreamingResponses("Response");
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages =
            [
                new UserMessage { Id = "m1", Content = "Valid message" },
                new UserMessage { Id = "m2", Content = null! }
            ]
        };

        // Act
        await ToListAsync(agent.RunAsync(input));

        // Assert
        Assert.NotNull(chatClient.LastMessages);
        var messagesList = chatClient.LastMessages.ToList();
        Assert.Single(messagesList);
        Assert.Equal("Valid message", messagesList[0].Text);
    }

    [Fact]
    public async Task RunAsync_WithCancellationToken_PropagatesCancellation()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithCancellationSupport();
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = [new UserMessage { Id = "m1", Content = "Test" }]
        };

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var evt in agent.RunAsync(input, cts.Token))
            {
                // Consume events until cancellation
            }
        });
    }

    [Fact]
    public async Task RunAsync_EachMessageHasUniqueMessageId()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithStreamingResponses("Hello");
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input1 = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = [new UserMessage { Id = "m1", Content = "First" }]
        };

        var input2 = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r2",
            Messages = [new UserMessage { Id = "m2", Content = "Second" }]
        };

        // Act
        var events1 = await ToListAsync(agent.RunAsync(input1));
        var events2 = await ToListAsync(agent.RunAsync(input2));

        // Assert
        var msgStart1 = events1.OfType<TextMessageStartEvent>().First();
        var msgStart2 = events2.OfType<TextMessageStartEvent>().First();

        Assert.NotEqual(msgStart1.MessageId, msgStart2.MessageId);
    }

    [Fact]
    public async Task RunAsync_WithEmptyMessageList_StillCallsAgent()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithStreamingResponses("Hello");
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = []
        };

        // Act
        var events = await ToListAsync(agent.RunAsync(input));

        // Assert
        Assert.NotNull(chatClient.LastMessages);
        Assert.Empty(chatClient.LastMessages.ToList());
        Assert.True(events.Count > 0); // Should still emit run events
    }

    [Fact]
    public async Task RunAsync_StreamsEventsAsTheyArrive()
    {
        // Arrange
        async IAsyncEnumerable<ChatResponseUpdate> DelayedStreamFunc(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, "First");
            await Task.Delay(50, ct);
            yield return new ChatResponseUpdate(ChatRole.Assistant, "Second");
        }

        var delayedChatClient = new TestChatClient(streamingFunc: DelayedStreamFunc);
        var chatClientAgent = new ChatClientAgent(delayedChatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages = [new UserMessage { Id = "m1", Content = "Test" }]
        };

        // Act
        var events = new List<BaseEvent>();
        var timestamps = new List<DateTime>();
        
        await foreach (var evt in agent.RunAsync(input))
        {
            events.Add(evt);
            timestamps.Add(DateTime.UtcNow);
        }

        // Assert - verify events arrived over time, not all at once
        Assert.True(events.Count >= 4); // RUN_STARTED, TEXT_MESSAGE_START, at least 2 content events
        
        // The time between first and last event should reflect the delay
        var duration = timestamps.Last() - timestamps.First();
        Assert.True(duration.TotalMilliseconds >= 40); // Allow for some timing variance
    }

    [Fact]
    public async Task RunAsync_WithDeveloperRole_MapsToSystemRole()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithStreamingResponses("Response");
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages =
            [
                new DeveloperMessage { Id = "m1", Content = "Developer instructions" }
            ]
        };

        // Act
        await ToListAsync(agent.RunAsync(input));

        // Assert
        Assert.NotNull(chatClient.LastMessages);
        var messagesList = chatClient.LastMessages.ToList();
        Assert.Single(messagesList);
        Assert.Equal(ChatRole.System, messagesList[0].Role);
        Assert.Equal("Developer instructions", messagesList[0].Text);
    }

    [Fact]
    public async Task RunAsync_PreservesMessageOrder()
    {
        // Arrange
        var chatClient = TestChatClient.CreateWithStreamingResponses("Response");
        var chatClientAgent = new ChatClientAgent(chatClient);
        var agent = new ChatClientAGUIAgent(chatClientAgent);

        var input = new RunAgentInput
        {
            ThreadId = "t1",
            RunId = "r1",
            Messages =
            [
                new UserMessage { Id = "m1", Content = "First" },
                new AssistantMessage { Id = "m2", Content = "Second" },
                new UserMessage { Id = "m3", Content = "Third" },
                new AssistantMessage { Id = "m4", Content = "Fourth" }
            ]
        };

        // Act
        await ToListAsync(agent.RunAsync(input));

        // Assert
        Assert.NotNull(chatClient.LastMessages);
        var messagesList = chatClient.LastMessages.ToList();
        Assert.Equal(4, messagesList.Count);
        Assert.Equal("First", messagesList[0].Text);
        Assert.Equal("Second", messagesList[1].Text);
        Assert.Equal("Third", messagesList[2].Text);
        Assert.Equal("Fourth", messagesList[3].Text);
    }
}
