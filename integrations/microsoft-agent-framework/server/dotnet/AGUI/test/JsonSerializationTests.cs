using System.Text.Json;
using AGUI;
using AGUI.Events;
using AGUI.Messages;

namespace AGUI.Tests;

/// <summary>
/// Tests for JSON serialization and deserialization of AG-UI protocol types using AGUIJsonSerializerContext.
/// Validates camelCase naming conventions and round-trip serialization.
/// </summary>
public class JsonSerializationTests
{
    private readonly JsonSerializerOptions _options;

    public JsonSerializationTests()
    {
        _options = AGUIJsonSerializerContext.Default.Options;
    }

    #region Event Serialization Tests

    [Fact]
    public void SerializeRunStartedEvent_ProducesCorrectJson()
    {
        var evt = new RunStartedEvent { ThreadId = "thread-1", RunId = "run-1" };

        // Serialize as BaseEvent to trigger polymorphic serialization
        var json = JsonSerializer.Serialize<BaseEvent>(evt, _options);
               
        // Verify round-trip deserialization works
        var deserialized = JsonSerializer.Deserialize(json, AGUIJsonSerializerContext.Default.BaseEvent);
        Assert.NotNull(deserialized);
        Assert.IsType<RunStartedEvent>(deserialized);
        var runStarted = (RunStartedEvent)deserialized;
        Assert.Equal("thread-1", runStarted.ThreadId);
        Assert.Equal("run-1", runStarted.RunId);
        Assert.Equal("RUN_STARTED", runStarted.Type);
    }

    [Fact]
    public void SerializeRunFinishedEvent_ProducesCorrectJson()
    {
        var evt = new RunFinishedEvent { ThreadId = "thread-1", RunId = "run-1" };

        var json = JsonSerializer.Serialize<BaseEvent>(evt, _options);

        var deserialized = JsonSerializer.Deserialize(json, AGUIJsonSerializerContext.Default.BaseEvent);
        Assert.NotNull(deserialized);
        Assert.IsType<RunFinishedEvent>(deserialized);
        var runFinished = (RunFinishedEvent)deserialized;
        Assert.Equal("thread-1", runFinished.ThreadId);
        Assert.Equal("run-1", runFinished.RunId);
        Assert.Equal("RUN_FINISHED", runFinished.Type);
    }

    [Fact]
    public void SerializeRunErrorEvent_ProducesCorrectJson()
    {
        var evt = new RunErrorEvent { Message = "Something went wrong", Code = "ERROR_CODE" };

        var json = JsonSerializer.Serialize<BaseEvent>(evt, _options);

        var deserialized = JsonSerializer.Deserialize(json, AGUIJsonSerializerContext.Default.BaseEvent);
        Assert.NotNull(deserialized);
        Assert.IsType<RunErrorEvent>(deserialized);
        var runError = (RunErrorEvent)deserialized;
        Assert.Equal("Something went wrong", runError.Message);
        Assert.Equal("ERROR_CODE", runError.Code);
        Assert.Equal("RUN_ERROR", runError.Type);
    }

    [Fact]
    public void SerializeTextMessageStartEvent_ProducesCorrectJson()
    {
        var evt = new TextMessageStartEvent { MessageId = "msg-123", Role = "assistant" };

        var json = JsonSerializer.Serialize<BaseEvent>(evt, _options);

        var deserialized = JsonSerializer.Deserialize(json, AGUIJsonSerializerContext.Default.BaseEvent);
        Assert.NotNull(deserialized);
        Assert.IsType<TextMessageStartEvent>(deserialized);
        var textStart = (TextMessageStartEvent)deserialized;
        Assert.Equal("msg-123", textStart.MessageId);
        Assert.Equal("assistant", textStart.Role);
        Assert.Equal("TEXT_MESSAGE_START", textStart.Type);
    }

    [Fact]
    public void SerializeTextMessageContentEvent_ProducesCorrectJson()
    {
        var evt = new TextMessageContentEvent { MessageId = "msg-1", Delta = "Hello, world!" };

        var json = JsonSerializer.Serialize<BaseEvent>(evt, _options);

        var deserialized = JsonSerializer.Deserialize(json, AGUIJsonSerializerContext.Default.BaseEvent);
        Assert.NotNull(deserialized);
        Assert.IsType<TextMessageContentEvent>(deserialized);
        var textContent = (TextMessageContentEvent)deserialized;
        Assert.Equal("msg-1", textContent.MessageId);
        Assert.Equal("Hello, world!", textContent.Delta);
        Assert.Equal("TEXT_MESSAGE_CONTENT", textContent.Type);
    }

    [Fact]
    public void SerializeTextMessageEndEvent_ProducesCorrectJson()
    {
        var evt = new TextMessageEndEvent { MessageId = "msg-1" };

        var json = JsonSerializer.Serialize<BaseEvent>(evt, _options);

        var deserialized = JsonSerializer.Deserialize(json, AGUIJsonSerializerContext.Default.BaseEvent);
        Assert.NotNull(deserialized);
        Assert.IsType<TextMessageEndEvent>(deserialized);
        var textEnd = (TextMessageEndEvent)deserialized;
        Assert.Equal("msg-1", textEnd.MessageId);
        Assert.Equal("TEXT_MESSAGE_END", textEnd.Type);
    }

    #endregion

    #region RunAgentInput Serialization Tests

    [Fact]
    public void SerializeRunAgentInput_WithEmptyMessages_ProducesCorrectJson()
    {
        var input = new RunAgentInput
        {
            ThreadId = "thread-1",
            RunId = "run-1",
            Messages = []
        };

        var json = JsonSerializer.Serialize(input, _options);

        Assert.Contains("\"threadId\":\"thread-1\"", json);
        Assert.Contains("\"runId\":\"run-1\"", json);
        Assert.Contains("\"messages\":[]", json);
    }

    [Fact]
    public void SerializeRunAgentInput_WithMessages_ProducesCorrectJson()
    {
        var input = new RunAgentInput
        {
            ThreadId = "thread-1",
            RunId = "run-1",
            Messages =
            [
                new UserMessage { Id = "m1", Content = "Hello" },
                new AssistantMessage { Id = "m2", Content = "Hi there!" }
            ]
        };

        var json = JsonSerializer.Serialize(input, _options);

        Assert.Contains("\"threadId\":\"thread-1\"", json);
        Assert.Contains("\"runId\":\"run-1\"", json);
        Assert.Contains("\"messages\":", json);
        Assert.Contains("\"Hello\"", json);
        Assert.Contains("\"Hi there!\"", json);
    }

    [Fact]
    public void SerializeRunAgentInput_WithState_ProducesCorrectJson()
    {
        var stateObj = new { counter = 42, name = "test" };
        var input = new RunAgentInput
        {
            ThreadId = "thread-1",
            RunId = "run-1",
            Messages = [],
            State = JsonSerializer.SerializeToElement(stateObj)
        };

        var json = JsonSerializer.Serialize(input, _options);

        Assert.Contains("\"state\":", json);
        Assert.Contains("\"counter\":42", json);
        Assert.Contains("\"name\":\"test\"", json);
    }

    [Fact]
    public void SerializeRunAgentInput_WithNullState_OmitsStateProperty()
    {
        var input = new RunAgentInput
        {
            ThreadId = "thread-1",
            RunId = "run-1",
            Messages = [],
            State = null
        };

        var json = JsonSerializer.Serialize(input, _options);

        Assert.DoesNotContain("\"state\"", json);
    }

    [Fact]
    public void SerializeRunAgentInput_WithTools_ProducesCorrectJson()
    {
        var input = new RunAgentInput
        {
            ThreadId = "thread-1",
            RunId = "run-1",
            Messages = [],
            Tools = [new AGUITool { Name = "search", Description = "Search the web" }]
        };

        var json = JsonSerializer.Serialize(input, _options);

        Assert.Contains("\"tools\":", json);
        Assert.Contains("\"search\"", json);
    }

    [Fact]
    public void SerializeRunAgentInput_WithContext_ProducesCorrectJson()
    {
        var input = new RunAgentInput
        {
            ThreadId = "thread-1",
            RunId = "run-1",
            Messages = [],
            ContextItems = [new Context { Description = "API Key", Value = "secret" }]
        };

        var json = JsonSerializer.Serialize(input, _options);

        Assert.Contains("\"context\":", json);
        Assert.Contains("\"API Key\"", json);
    }

    #endregion

    #region RunAgentInput Deserialization Tests

    [Fact]
    public void DeserializeRunAgentInput_WithEmptyMessages_Succeeds()
    {
        var json = "{\"threadId\":\"thread-1\",\"runId\":\"run-1\",\"messages\":[]}";

        var input = JsonSerializer.Deserialize<RunAgentInput>(json, _options);

        Assert.NotNull(input);
        Assert.Equal("thread-1", input.ThreadId);
        Assert.Equal("run-1", input.RunId);
        Assert.Empty(input.Messages);
    }

    [Fact]
    public void DeserializeRunAgentInput_WithState_Succeeds()
    {
        var json = "{\"threadId\":\"t1\",\"runId\":\"r1\",\"messages\":[],\"state\":{\"count\":5}}";

        var input = JsonSerializer.Deserialize<RunAgentInput>(json, _options);

        Assert.NotNull(input);
        Assert.NotNull(input.State);
    }

    [Fact]
    public void DeserializeRunAgentInput_WithoutState_HasNullState()
    {
        var json = "{\"threadId\":\"t1\",\"runId\":\"r1\",\"messages\":[]}";

        var input = JsonSerializer.Deserialize<RunAgentInput>(json, _options);

        Assert.NotNull(input);
        Assert.Null(input.State);
    }

    [Fact]
    public void RoundTripSerialization_RunAgentInput_PreservesData()
    {
        var stateObj = new { counter = 42 };
        var original = new RunAgentInput
        {
            ThreadId = "thread-abc",
            RunId = "run-xyz",
            Messages = [],
            State = JsonSerializer.SerializeToElement(stateObj)
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<RunAgentInput>(json, _options);

        Assert.NotNull(deserialized);
        Assert.Equal(original.ThreadId, deserialized.ThreadId);
        Assert.Equal(original.RunId, deserialized.RunId);
        Assert.NotNull(deserialized.State);
        Assert.Equal(42, deserialized.State.Value.GetProperty("counter").GetInt32());
    }

    #endregion

    #region Message Type Tests

    [Fact]
    public void CreateUserMessage_WithRequiredProperties_Succeeds()
    {
        var msg = new UserMessage 
        { 
            Id = "u1", 
            Content = "Hello" 
        };

        Assert.Equal("u1", msg.Id);
        Assert.Equal(Role.User, msg.Role);
        Assert.Equal("Hello", msg.Content);
    }

    [Fact]
    public void CreateAssistantMessage_WithRequiredProperties_Succeeds()
    {
        var msg = new AssistantMessage 
        { 
            Id = "a1", 
            Content = "Hi there!" 
        };

        Assert.Equal(Role.Assistant, msg.Role);
    }

    [Fact]
    public void CreateSystemMessage_WithRequiredProperties_Succeeds()
    {
        var msg = new SystemMessage 
        { 
            Id = "s1", 
            Content = "You are helpful" 
        };

        Assert.Equal(Role.System, msg.Role);
    }

    [Fact]
    public void CreateDeveloperMessage_WithRequiredProperties_Succeeds()
    {
        var msg = new DeveloperMessage 
        { 
            Id = "d1", 
            Content = "Debug info" 
        };

        Assert.Equal(Role.Developer, msg.Role);
    }

    [Fact]
    public void CreateToolMessage_WithRequiredProperties_Succeeds()
    {
        var msg = new ToolMessage 
        { 
            Id = "t1",
            Content = "Result", 
            ToolCallId = "call-123" 
        };

        Assert.Equal(Role.Tool, msg.Role);
        Assert.Equal("call-123", msg.ToolCallId);
    }

    #endregion

    #region Role Enum Serialization Tests

    [Theory]
    [InlineData(Role.User, "User")]
    [InlineData(Role.Assistant, "Assistant")]
    [InlineData(Role.System, "System")]
    [InlineData(Role.Developer, "Developer")]
    [InlineData(Role.Tool, "Tool")]
    public void SerializeRole_ProducesPascalCaseString(Role role, string expectedString)
    {
        var json = JsonSerializer.Serialize(role, _options);

        Assert.Equal($"\"{expectedString}\"", json);
    }

    #endregion

    #region Message Polymorphic Serialization Tests

    [Fact]
    public void SerializeUserMessage_HasRoleFirst()
    {
        var msg = new UserMessage { Id = "msg-1", Content = "Hello" };
        
        var json = JsonSerializer.Serialize<Message>(msg, _options);
        
        // Debug: Print actual JSON to understand the structure
        System.Console.WriteLine($"Serialized JSON: {json}");
        
        // Role must be first property for polymorphic deserialization
        Assert.StartsWith("{\"role\":", json);
        Assert.Contains("\"id\":\"msg-1\"", json);
        Assert.Contains("\"content\":\"Hello\"", json);
    }

    [Fact]
    public void SerializeAssistantMessage_HasRoleFirst()
    {
        var msg = new AssistantMessage { Id = "msg-2", Content = "Hi there" };
        
        var json = JsonSerializer.Serialize<Message>(msg, _options);
        
        Assert.StartsWith("{\"role\":", json);
        Assert.Contains("\"id\":\"msg-2\"", json);
    }

    [Fact]
    public void SerializeSystemMessage_HasRoleFirst()
    {
        var msg = new SystemMessage { Id = "msg-3", Content = "You are helpful" };
        
        var json = JsonSerializer.Serialize<Message>(msg, _options);
        
        Assert.StartsWith("{\"role\":", json);
    }

    [Fact]
    public void DeserializeUserMessage_WithRoleFirst_Succeeds()
    {
        var json = "{\"role\":\"user\",\"id\":\"m1\",\"content\":\"Hello\"}";
        
        var msg = JsonSerializer.Deserialize<Message>(json, _options);
        
        Assert.NotNull(msg);
        Assert.IsType<UserMessage>(msg);
        var userMsg = (UserMessage)msg;
        Assert.Equal("m1", userMsg.Id);
        Assert.Equal(Role.User, userMsg.Role);
        Assert.Equal("Hello", userMsg.Content);
    }

    [Fact]
    public void DeserializeAssistantMessage_WithRoleFirst_Succeeds()
    {
        var json = "{\"role\":\"assistant\",\"id\":\"m2\",\"content\":\"Hi\"}";
        
        var msg = JsonSerializer.Deserialize<Message>(json, _options);
        
        Assert.NotNull(msg);
        Assert.IsType<AssistantMessage>(msg);
        Assert.Equal("m2", msg.Id);
        Assert.Equal(Role.Assistant, msg.Role);
    }

    [Fact]
    public void SerializeRunAgentInput_WithMessages_HasRoleFirstInMessages()
    {
        var input = new RunAgentInput
        {
            ThreadId = "thread-1",
            RunId = "run-1",
            Messages =
            [
                new UserMessage { Id = "m1", Content = "Hello" }
            ]
        };

        var json = JsonSerializer.Serialize(input, _options);
        
        // Verify the message array contains role-first serialized messages
        Assert.Contains("\"messages\":[{\"role\":", json);
    }

    [Fact]
    public void RoundTripMessage_PreservesAllData()
    {
        var original = new UserMessage 
        { 
            Id = "test-123", 
            Content = "Test content",
            Name = "TestUser"
        };

        var json = JsonSerializer.Serialize<Message>(original, _options);
        var deserialized = JsonSerializer.Deserialize<Message>(json, _options);

        Assert.NotNull(deserialized);
        Assert.IsType<UserMessage>(deserialized);
        var userMsg = (UserMessage)deserialized;
        Assert.Equal(original.Id, userMsg.Id);
        Assert.Equal(original.Role, userMsg.Role);
        Assert.Equal(original.Content, userMsg.Content);
        Assert.Equal(original.Name, userMsg.Name);
    }

    [Fact]
    public void DeserializeRunAgentInput_WithUserMessage_Succeeds()
    {
        var json = @"{
            ""threadId"": ""t1"",
            ""runId"": ""r1"",
            ""messages"": [
                {
                    ""role"": ""user"",
                    ""id"": ""msg-1"",
                    ""content"": ""Hello""
                }
            ]
        }";

        var input = JsonSerializer.Deserialize<RunAgentInput>(json, _options);

        Assert.NotNull(input);
        Assert.Single(input.Messages);
        Assert.IsType<UserMessage>(input.Messages[0]);
        var userMsg = (UserMessage)input.Messages[0];
        Assert.Equal("msg-1", userMsg.Id);
        Assert.Equal("Hello", userMsg.Content);
    }

    [Fact]
    public void DeserializeRunAgentInput_WithMultipleMessageTypes_Succeeds()
    {
        var json = @"{
            ""threadId"": ""t1"",
            ""runId"": ""r1"",
            ""messages"": [
                {""role"": ""user"", ""id"": ""m1"", ""content"": ""Hi""},
                {""role"": ""assistant"", ""id"": ""m2"", ""content"": ""Hello""},
                {""role"": ""system"", ""id"": ""m3"", ""content"": ""System msg""}
            ]
        }";

        var input = JsonSerializer.Deserialize<RunAgentInput>(json, _options);

        Assert.NotNull(input);
        Assert.Equal(3, input.Messages.Length);
        Assert.IsType<UserMessage>(input.Messages[0]);
        Assert.IsType<AssistantMessage>(input.Messages[1]);
        Assert.IsType<SystemMessage>(input.Messages[2]);
    }

    #endregion
}
