using AGUI;
using AGUI.Events;
using AGUI.Messages;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Agents.AI.AGUI;

/// <summary>
/// AG-UI agent implementation that wraps a ChatClientAgent from Microsoft Agent Framework
/// </summary>
public class ChatClientAGUIAgent : AGUIAgent
{
    private readonly ChatClientAgent _chatClientAgent;

    /// <summary>
    /// Initializes a new instance of ChatClientAGUIAgent
    /// </summary>
    /// <param name="chatClientAgent">The underlying ChatClientAgent</param>
    public ChatClientAGUIAgent(ChatClientAgent chatClientAgent)
    {
        _chatClientAgent = chatClientAgent ?? throw new ArgumentNullException(nameof(chatClientAgent));
        Name = chatClientAgent.Name;
        Description = chatClientAgent.Description;
    }

    /// <summary>
    /// Creates a ChatClientAGUIAgent from an IChatClient
    /// </summary>
    /// <param name="chatClient">The chat client</param>
    /// <param name="instructions">Optional system instructions</param>
    /// <param name="name">Optional agent name</param>
    /// <param name="description">Optional agent description</param>
    /// <returns>A new ChatClientAGUIAgent instance</returns>
    public static ChatClientAGUIAgent FromChatClient(
        IChatClient chatClient,
        string? instructions = null,
        string? name = null,
        string? description = null)
    {
        var agent = new ChatClientAgent(chatClient, instructions, name, description);
        return new ChatClientAGUIAgent(agent);
    }

    /// <summary>
    /// Runs the agent with the specified request and streams AG-UI events
    /// </summary>
    public override async IAsyncEnumerable<BaseEvent> RunAsync(
        RunAgentInput input,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messageId = Guid.NewGuid().ToString();

        // Build chat messages from RunAgentInput.Messages
        var messages = new List<ChatMessage>();

        foreach (var msg in input.Messages)
        {
            var role = msg.Role switch
            {
                Role.Developer => ChatRole.System,
                Role.System => ChatRole.System,
                Role.User => ChatRole.User,
                Role.Assistant => ChatRole.Assistant,
                Role.Tool => ChatRole.Tool,
                _ => ChatRole.User
            };

            var content = msg switch
            {
                DeveloperMessage dev => dev.Content,
                SystemMessage sys => sys.Content,
                UserMessage user => user.Content,
                AssistantMessage asst => asst.Content,
                ToolMessage tool => tool.Content,
                _ => null
            };

            switch (msg)
            {
                case AssistantMessage { ToolCalls: [_, ..] } assistant:
                    var assistantMessage = new ChatMessage(ChatRole.Assistant, assistant.Content);
                    foreach (var toolCall in assistant.ToolCalls)
                    {
                        var args = JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.Function.Arguments);
                        var aiContentFunctionCall = new FunctionCallContent(toolCall.Id, toolCall.Function.Name, args);
                        assistantMessage.Contents.Add(aiContentFunctionCall);
                    }
                    messages.Add(assistantMessage);
                    break;
                case ToolMessage tool:
                    var toolMessage = new ChatMessage(ChatRole.Tool, tool.Content);
                    var result = new FunctionResultContent(tool.ToolCallId, tool.Content)
                    {
                        Exception = tool.Error is not null ? new Exception(tool.Error) : null
                    };
                    toolMessage.Contents.Add(result);
                    messages.Add(toolMessage);
                    break;
                default:
                    var message = new ChatMessage(role, content);
                    messages.Add(message);
                    break;
            }
        }

        var hasError = false;

        // Yield RUN_STARTED
        yield return new RunStartedEvent
        {
            ThreadId = input.ThreadId,
            RunId = input.RunId
        };

        var agentRunOptions = new ChatClientAgentRunOptions
        {            
            ChatClientFactory = chatClient =>
            {
                var newClient = chatClient
                    .AsBuilder()
                    .ConfigureOptions(o =>
                    {
                        o.Tools ??= [];
                        foreach (var tool in input.Tools)
                        {
                            o.Tools.Add(tool.AsAITool());
                        }
                        if (input.State != null)
                        {
                            o.Instructions = "Always call get_state to read the current state the prompt refers to, and update_state to update it before producing a response";
                            var emptySchema = JsonDocument.Parse("{}").RootElement;
                            var stateSchema = JsonDocument.Parse("""                                
                                {
                                  "type": "object",
                                  "properties": {
                                    "state": {}
                                  },
                                  "required": ["state"]
                                }                                
                                """).RootElement;
                            o.Tools.Add(AIFunctionFactory.Create(() => input.State, "get_state", "Gets the state you need to work with. The state is the object/entity/subject the user is referring to in the conversation. You must read the state before answering"));
                            o.Tools.Add(AIFunctionFactory.Create((JsonElement state) => input.State = state, "update_state", "Updates the state you are working with. The state is the object/entity/subject the user is referring to in the conversation. You must update the state before answering"));
                        }
                    })
                    .Build();
                return newClient;
            }
        };

        var streamingContent = false;
        var activeToolCalls = new Dictionary<string, (string toolCallId, string toolCallName)>();
        var toolCallArguments = new Dictionary<string, string>();

        // Stream responses from the ChatClientAgent
        await foreach (var update in _chatClientAgent.RunStreamingAsync(
            messages,
            thread: null,
            agentRunOptions,
            cancellationToken))
        {
            foreach (var content in update.Contents)
            {
                if (content is TextContent { Text.Length: > 0 } textContent)
                {
                    if (!streamingContent)
                    {
                        yield return new TextMessageStartEvent
                        {
                            MessageId = messageId,
                            Role = "assistant"
                        };
                        streamingContent = true;
                    }

                    yield return new TextMessageContentEvent
                    {
                        MessageId = messageId,
                        Delta = textContent.Text
                    };
                    streamingContent = true;
                }

                if (streamingContent && content is not TextContent)
                {
                    yield return new TextMessageEndEvent
                    {
                        MessageId = messageId
                    };
                    streamingContent = false;
                }

                if (content is FunctionCallContent function)
                {
                    if (function.Name == "get_state")
                    {
                        activeToolCalls[function.CallId] = (function.CallId, function.Name);
                        continue;
                    }
                    else if (function.Name == "update_state")
                    {
                        activeToolCalls[function.CallId] = (function.CallId, function.Name);
                        var state = function.Arguments!["state"];
                        yield return new StateSnapshotEvent
                        {
                            Snapshot = state
                        };
                        continue;
                    }
                    else
                    {
                        var toolCallId = function.CallId;

                        // Check if this is a new tool call or continuation
                        if (!activeToolCalls.ContainsKey(toolCallId))
                        {
                            // New tool call - emit START event
                            activeToolCalls[toolCallId] = (toolCallId, function.Name);
                            toolCallArguments[toolCallId] = string.Empty;

                            yield return new ToolCallStartEvent
                            {
                                ToolCallId = toolCallId,
                                ToolCallName = function.Name,
                                ParentMessageId = messageId
                            };
                        }

                        // Stream arguments if available
                        if (function.Arguments != null)
                        {
                            var argsJson = JsonSerializer.Serialize(function.Arguments);
                            var previousArgs = toolCallArguments[toolCallId];

                            yield return new ToolCallArgsEvent
                            {
                                ToolCallId = toolCallId,
                                Delta = argsJson
                            };
                        }
                    }
                }

                if (content is FunctionResultContent resultContent)
                {
                    var functionName = activeToolCalls.ContainsKey(resultContent.CallId)
                        ? activeToolCalls[resultContent.CallId].toolCallName
                        : string.Empty;

                    if (functionName == "get_state" || functionName == "update_state")
                    {
                        activeToolCalls.Remove(resultContent.CallId);
                        continue;
                    }

                    var toolCallId = resultContent.CallId;

                    // Check for error first
                    if (resultContent.Exception != null)
                    {
                        hasError = true;
                        yield return new RunErrorEvent
                        {
                            Message = resultContent.Exception.Message,
                            Code = ""
                        };
                    }

                    // Only process result and close tool call if it was active
                    if (!string.IsNullOrEmpty(toolCallId) && activeToolCalls.ContainsKey(toolCallId))
                    {
                        // Emit tool result if available
                        if (resultContent.Result != null)
                        {
                            var resultString = resultContent.Result switch
                            {
                                string s => s,
                                _ => JsonSerializer.Serialize(resultContent.Result)
                            };

                            yield return new ToolCallResultEvent
                            {
                                MessageId = messageId,
                                ToolCallId = toolCallId,
                                Content = resultString,
                                Role = "tool"
                            };
                        }

                        // Emit END event to close the tool call
                        yield return new ToolCallEndEvent
                        {
                            ToolCallId = toolCallId
                        };
                        activeToolCalls.Remove(toolCallId);
                        toolCallArguments.Remove(toolCallId);
                    }
                }
            }
        }

        // Close any remaining open text message
        if (streamingContent)
        {
            yield return new TextMessageEndEvent
            {
                MessageId = messageId
            };
        }

        // Close any remaining open tool calls
        foreach (var (toolCallId, _) in activeToolCalls.Values)
        {
            yield return new ToolCallEndEvent
            {
                ToolCallId = toolCallId
            };
        }

        // Yield RUN_FINISHED if no error occurred
        if (!hasError)
        {
            yield return new RunFinishedEvent
            {
                ThreadId = input.ThreadId,
                RunId = input.RunId
            };
        }
    }
}
