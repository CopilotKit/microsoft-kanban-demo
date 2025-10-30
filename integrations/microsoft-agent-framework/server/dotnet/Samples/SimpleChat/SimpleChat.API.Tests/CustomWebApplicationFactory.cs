using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Text.Json;
using AGUI;

namespace SimpleChat.API.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Configure JSON options to use AGUI serializer context
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, AGUIJsonSerializerContext.Default);
            });

            // Remove the existing AGUIAgent registration
            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(AGUIAgent));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Register a test AGUIAgent using ChatClientAGUIAgent
            services.AddSingleton<AGUIAgent>(sp =>
            {
                var chatClient = new TestChatClient(["Hello! I'm a test response."]);
                var chatClientAgent = new Microsoft.Agents.AI.ChatClientAgent(
                    chatClient,
                    instructions: "You are a test assistant.",
                    name: "TestAgent",
                    description: "A test agent for integration testing");
                return new Microsoft.Agents.AI.AGUI.ChatClientAGUIAgent(chatClientAgent);
            });
        });
    }

    private class TestChatClient : IChatClient
    {
        private readonly string[] _responses;

        public TestChatClient(params string[] responses)
        {
            _responses = responses ?? ["Test response"];
        }

        public ChatClientMetadata Metadata => new ChatClientMetadata("test-client");

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, 
            ChatOptions? options = null, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var response in _responses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(10, cancellationToken); // Simulate streaming delay
                yield return new ChatResponseUpdate(ChatRole.Assistant, response);
            }
        }

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, 
            ChatOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            var message = new ChatMessage(ChatRole.Assistant, string.Join(" ", _responses));
            return new ChatResponse(message);
        }

        public object? GetService(Type serviceType, object? key = null) => null;

        public void Dispose() { }
    }
}
