using AGUI;
using AGUI.Events;
using AGUI.Messages;
using Azure.AI.Agents.Persistent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using System; // Added for Console.WriteLine

namespace Microsoft.Agents.AI.AGUI;

/// <summary>
/// AG-UI agent implementation that wraps an AIAgent from Microsoft.Agents.AI.AzureAI
/// </summary>
public class AzureAIAGUIAgent : AGUIAgent
{
    private readonly PersistentAgent _aiAgent;
    private readonly PersistentAgentsClient _client;

    /// <summary>
    /// Initializes a new instance of AzureAIAGUIAgent
    /// </summary>
    /// <param name="aiAgent">The underlying AIAgent</param>
    public AzureAIAGUIAgent(PersistentAgent aiAgent, PersistentAgentsClient agentsClient)
    {
        _aiAgent = aiAgent ?? throw new ArgumentNullException(nameof(aiAgent));
        _client = agentsClient ?? throw new ArgumentNullException(nameof(agentsClient));
        Name = aiAgent.Name;
        Description = aiAgent.Description;
    }

    /// <summary>
    /// Runs the agent with the specified request and streams AG-UI events
    /// Pseudocode:
    /// 1. Ensure thread exists or create.
    /// 2. Map incoming AGUI messages to ChatMessages.
    /// 3. Verify existing thread messages match provided messages by ID.
    /// 4. Append any new messages to thread.
    /// 5. Start streaming run updates.
    /// 6. For each update:
    ///    - Log update kind and details to console.
    ///    - Yield appropriate AGUI events (RunStarted, RunFinished, TextMessageStart, TextMessageContent, TextMessageEnd).
    /// </summary>
    public override async IAsyncEnumerable<BaseEvent> RunAsync(
        RunAgentInput input,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var thread = GetOrCreateThread(input, cancellationToken);
        input.ThreadId = thread.Id;
        // Build chat messages from input
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

            if (msg is UserMessage userMsg)
            {
                messages.Add(new ChatMessage(role, userMsg.Content) { MessageId = msg.Id });
            }
            else if (msg is SystemMessage sysMsg)
            {
                messages.Add(new ChatMessage(role, sysMsg.Content) { MessageId = msg.Id });
            }
            else if (msg is AssistantMessage astMsg)
            {
                messages.Add(new ChatMessage(role, astMsg.Content) { MessageId = msg.Id });
            }
            else if (msg is ToolMessage toolMsg)
            {
                messages.Add(new ChatMessage(role, toolMsg.Content) { MessageId = msg.Id });
            }
        }

        var instructions = messages.Count > 0 && messages[0].Role == ChatRole.System ? messages[0].Text : null;
        if (instructions != null)
        {
            messages.RemoveAt(0);
        }

        var i = 0;
        var existingMessages = _client.Messages.GetMessages(thread.Id, order: ListSortOrder.Ascending).ToList();
        foreach (var existingMessage in existingMessages)
        {
            if (i >= messages.Count)
            {
                throw new InvalidOperationException("Received fewer messages than existing messages in the thread.");
            }
            var receivedMessage = messages[i];
            if (!string.Equals(existingMessage.Id, receivedMessage.MessageId, StringComparison.Ordinal))
            {
                if (!existingMessage.Metadata.Contains(new KeyValuePair<string, string>("client_id", receivedMessage.MessageId ?? string.Empty)))
                {
                    throw new InvalidOperationException("Message ID mismatch between existing and received messages.");
                }
            }
            i++;
        }
        
        var run = GetExistingRun(input, thread, messages);
        var isToolRun = messages.Count > 0 && messages[^1].Role == ChatRole.Tool;
        if (!isToolRun)
        {
            for (; i < messages.Count; i++)
            {
                var msg = messages[i];
                var createdMessage = _client.Messages.CreateMessage(
                    input.ThreadId,
                    msg.Role == ChatRole.User ? MessageRole.User : MessageRole.Agent,
                    msg.Text,
                    metadata: new Dictionary<string, string> { ["client_id"] = msg.MessageId ?? string.Empty });
            }
        }

        //var agentRunOptions = new ChatClientAgentRunOptions
        //{
        //    ChatClientFactory = chatClient =>
        //    {
        //        var newClient = chatClient
        //            .AsBuilder()
        //            .ConfigureOptions(o =>
        //            {
        //                o.Tools ??= [];
        //                foreach (var tool in input.Tools)
        //                {
        //                    o.Tools.Add(tool.AsAITool());
        //                }
        //            })
        //            .Build();
        //        return newClient;
        //    }
        //};

        //List<AITool> tools = [];
        //foreach (var tool in input.Tools)
        //{
        //    tools.Add(tool.AsAITool());
        //}
        //var agent = _client.GetAIAgent(
        //    _aiAgent,
        //    new ChatOptions()
        //    {
        //        Instructions = instructions,
        //        Tools = tools,
        //        RawRepresentationFactory = (raw) => new CreateRunStreamingOptions
        //        {
        //            OverrideInstructions = instructions,
        //            OverrideTools = tools.Select(t => new FunctionToolDefinition(t.Name, t.Description, BinaryData.FromString(((AIFunction)t).JsonSchema.ToString())))
        //        }
        //    });

        //var aiagentthread = agent.GetNewThread(input.ThreadId);

        //var hasError = false;

        //// Yield RUN_STARTED
        //yield return new RunStartedEvent
        //{
        //    ThreadId = input.ThreadId,
        //    RunId = input.RunId
        //};

        //var streamingContent = false;
        //var activeToolCalls = new Dictionary<string, (string toolCallId, string toolCallName)>();
        //var toolCallArguments = new Dictionary<string, string>();
        //string lastMessageId = null;
        //// Stream responses from the ChatClientAgent
        //await foreach (var update in agent.RunStreamingAsync(
        //    aiagentthread,
        //    agentRunOptions,
        //    cancellationToken))
        //{
        //    var chatUpdate = update.AsChatResponseUpdate();
        //    var rawRepresentation = chatUpdate.RawRepresentation;
        //    foreach (var content in update.Contents)
        //    {
        //        if (content is TextContent { Text.Length: > 0 } textContent)
        //        {
        //            if (!streamingContent)
        //            {
        //                var chatResponse = (ChatResponseUpdate)update.RawRepresentation;
        //                var messageContentUpdate = (MessageContentUpdate)chatResponse.RawRepresentation!;
        //                lastMessageId = messageContentUpdate.MessageId;
        //                yield return new TextMessageStartEvent
        //                {
        //                    MessageId = lastMessageId,
        //                    Role = "assistant"
        //                };
        //                streamingContent = true;
        //            }

        //            yield return new TextMessageContentEvent
        //            {
        //                MessageId = lastMessageId!,
        //                Delta = textContent.Text
        //            };
        //            streamingContent = true;
        //        }

        //        if (streamingContent && content is not TextContent)
        //        {
        //            yield return new TextMessageEndEvent
        //            {
        //                MessageId = lastMessageId!
        //            };
        //            streamingContent = false;
        //        }

        //        if (content is FunctionCallContent function)
        //        {
        //            var toolCallId = function.CallId;

        //            // Check if this is a new tool call or continuation
        //            if (!activeToolCalls.ContainsKey(toolCallId))
        //            {
        //                // New tool call - emit START event
        //                activeToolCalls[toolCallId] = (toolCallId, function.Name);
        //                toolCallArguments[toolCallId] = string.Empty;

        //                yield return new ToolCallStartEvent
        //                {
        //                    ToolCallId = toolCallId,
        //                    ToolCallName = function.Name,
        //                    ParentMessageId = lastMessageId!
        //                };
        //            }

        //            // Stream arguments if available
        //            if (function.Arguments != null)
        //            {
        //                var argsJson = JsonSerializer.Serialize(function.Arguments);
        //                var previousArgs = toolCallArguments[toolCallId];

        //                yield return new ToolCallArgsEvent
        //                {
        //                    ToolCallId = toolCallId,
        //                    Delta = argsJson
        //                };
        //            }
        //        }

        //        if (content is FunctionResultContent resultContent)
        //        {
        //            var toolCallId = resultContent.CallId;

        //            // Check for error first
        //            if (resultContent.Exception != null)
        //            {
        //                hasError = true;
        //                yield return new RunErrorEvent
        //                {
        //                    Message = resultContent.Exception.Message,
        //                    Code = ""
        //                };
        //            }

        //            // Only process result and close tool call if it was active
        //            if (!string.IsNullOrEmpty(toolCallId) && activeToolCalls.ContainsKey(toolCallId))
        //            {
        //                // Emit tool result if available
        //                if (resultContent.Result != null)
        //                {
        //                    var resultString = resultContent.Result switch
        //                    {
        //                        string s => s,
        //                        _ => JsonSerializer.Serialize(resultContent.Result)
        //                    };

        //                    yield return new ToolCallResultEvent
        //                    {
        //                        MessageId = Guid.NewGuid().ToString(),
        //                        ToolCallId = toolCallId,
        //                        Content = resultString,
        //                        Role = "tool"
        //                    };
        //                }

        //                // Emit END event to close the tool call
        //                yield return new ToolCallEndEvent
        //                {
        //                    ToolCallId = toolCallId
        //                };
        //                activeToolCalls.Remove(toolCallId);
        //                toolCallArguments.Remove(toolCallId);
        //            }
        //        }
        //    }
        //}

        //// Close any remaining open tool calls
        //foreach (var (toolCallId, _) in activeToolCalls.Values)
        //{
        //    yield return new ToolCallEndEvent
        //    {
        //        ToolCallId = toolCallId
        //    };
        //}

        //// Yield RUN_FINISHED if no error occurred
        //if (!hasError)
        //{
        //    yield return new RunFinishedEvent
        //    {
        //        ThreadId = input.ThreadId,
        //        RunId = input.RunId
        //    };
        //}

        // Run the agent and stream responses
        var options = new CreateRunStreamingOptions
        {
            Metadata = input.RunId != null ? new Dictionary<string, string> { ["client_id"] = input.RunId } : null,
            OverrideInstructions = instructions,
            OverrideTools = input.Tools.Select(t => new FunctionToolDefinition(t.Name, t.Description, BinaryData.FromString(t.Parameters.ToString())))
        };

        var stream = isToolRun ? _client.Runs.SubmitToolOutputsToStreamAsync(
            run,
            [new ToolOutput(messages[^2].MessageId!, messages[^1].Text)]) :
            _client.Runs.CreateRunStreamingAsync(input.ThreadId, _aiAgent.Id, options: options);

        await foreach (var update in stream)
        {
            var updateKind = update.UpdateKind;
            Console.WriteLine($"[Update] Kind={updateKind} RawType={update.GetType().Name}");

            switch (update)
            {
                case RequiredActionUpdate requiredActionUpdate:
                    Console.WriteLine($"[RequiredActionUpdate] Kind={requiredActionUpdate.UpdateKind}");
                    yield return new ToolCallStartEvent
                    {
                        ToolCallId = requiredActionUpdate.ToolCallId,
                        ToolCallName = requiredActionUpdate.FunctionName
                    };
                    yield return new ToolCallArgsEvent
                    {
                        ToolCallId = requiredActionUpdate.ToolCallId,
                        Delta = requiredActionUpdate.FunctionArguments
                    };
                    break;

                case RunUpdate runUpdate:
                    Console.WriteLine($"[RunUpdate] Kind={runUpdate.UpdateKind} RunId={runUpdate.Value?.Id} Status={runUpdate.Value?.Status} Error={runUpdate.Value?.LastError?.Message}");
                    switch (runUpdate.UpdateKind)
                    {
                        case StreamingUpdateReason.RunInProgress:
                            Console.WriteLine($"[RunStarted] ThreadId={input.ThreadId} RunId={input.RunId ?? string.Empty}");
                            yield return new RunStartedEvent
                            {
                                ThreadId = thread.Id,
                                RunId = runUpdate.Value.Id!
                            };
                            break;
                        case StreamingUpdateReason.RunCompleted:
                            Console.WriteLine($"[RunCompleted] ThreadId={input.ThreadId} RunId={input.RunId ?? string.Empty}");
                            yield return new RunFinishedEvent
                            {
                                ThreadId = input.ThreadId,
                                RunId = input.RunId ?? string.Empty
                            };
                            break;
                        case StreamingUpdateReason.RunFailed:
                            Console.WriteLine($"[RunFailed] ThreadId={input.ThreadId} RunId={input.RunId ?? string.Empty} Error={runUpdate.Value?.LastError?.Message}");
                            yield return new RunFinishedEvent
                            {
                                ThreadId = input.ThreadId,
                                RunId = input.RunId ?? string.Empty,
                                Result = new { error = runUpdate.Value?.LastError?.Message }
                            };
                            break;
                    }
                    break;

                case MessageStatusUpdate messageStatusUpdate:
                    Console.WriteLine($"[MessageStatusUpdate] Kind={messageStatusUpdate.UpdateKind} MessageId={messageStatusUpdate.Value?.Id} Role={messageStatusUpdate.Value?.Role}");
                    if (messageStatusUpdate.UpdateKind == StreamingUpdateReason.MessageCompleted)
                    {
                        Console.WriteLine($"[MessageCompleted] MessageId={messageStatusUpdate.Value.Id}");
                        yield return new TextMessageEndEvent
                        {
                            MessageId = messageStatusUpdate.Value.Id
                        };
                    }
                    if (messageStatusUpdate.UpdateKind == StreamingUpdateReason.MessageCreated)
                    {
                        Console.WriteLine($"[MessageCreated] MessageId={messageStatusUpdate.Value.Id} Role={(messageStatusUpdate.Value.Role == MessageRole.Agent ? "assistant" : "user")}");
                        yield return new TextMessageStartEvent
                        {
                            MessageId = messageStatusUpdate.Value.Id,
                            Role = messageStatusUpdate.Value.Role == MessageRole.Agent ? "assistant" : "user"
                        };
                    }
                    break;

                case MessageContentUpdate messageContentUpdate:
                    Console.WriteLine($"[MessageContentUpdate] MessageId={messageContentUpdate.MessageId} Delta=\"{messageContentUpdate.Text}\"");
                    yield return new TextMessageContentEvent
                    {
                        MessageId = messageContentUpdate.MessageId,
                        Delta = messageContentUpdate.Text
                    };
                    break;

                case ThreadUpdate threadUpdate:
                    Console.WriteLine($"[ThreadUpdate] Kind={threadUpdate.UpdateKind} ThreadId={threadUpdate.Value?.Id}");
                    break;
            }
        }
    }

    private ThreadRun? GetExistingRun(RunAgentInput input, PersistentAgentThread thread, List<ChatMessage> messages)
    {
        if (messages.Count == 0 || messages[^1].Role != ChatRole.Tool)
        {
            return null;
        }
        var existingRuns = _client.Runs.GetRuns(thread.Id).ToList();
        foreach (var run in existingRuns)
        {
            return run;
            //if (run.Metadata.Contains(new KeyValuePair<string, string>("client_id", input.RunId)))
            //{
            //    return run;
            //}
        }

        return null;
    }

    private PersistentAgentThread GetOrCreateThread(RunAgentInput input, CancellationToken cancellationToken)
    {
        var threads = _client.Threads.GetThreads();
        foreach (var thread in threads)
        {
            if (thread.Metadata.Contains(new KeyValuePair<string, string>("client_id", input.ThreadId)))
            {
                return thread;
            }
        }

        return _client.Threads.CreateThread(metadata: new Dictionary<string, string> { ["client_id"] = input.ThreadId! });
    }
}
