using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using MicrosoftAgentFrameworkServer.Models;
using MicrosoftAgentFrameworkServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(logging =>
{
	logging.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.RequestBody 
		| HttpLoggingFields.ResponsePropertiesAndHeaders | HttpLoggingFields.ResponseBody;
	logging.RequestBodyLogLimit = int.MaxValue;
	logging.ResponseBodyLogLimit = int.MaxValue;
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.PropertyNamingPolicy = EventStreamWriter.JsonOptions.PropertyNamingPolicy;
	options.SerializerOptions.DefaultIgnoreCondition = EventStreamWriter.JsonOptions.DefaultIgnoreCondition;
});

var app = builder.Build();

app.UseHttpLogging();

app.MapPost("/", async (RunAgentInput input, HttpContext context) =>
{
	var cancellationToken = context.RequestAborted;

	// if (string.IsNullOrWhiteSpace(input.ThreadId) || string.IsNullOrWhiteSpace(input.RunId))
	// {
	// 	context.Response.StatusCode = StatusCodes.Status400BadRequest;
	// 	await context.Response.WriteAsJsonAsync(
	// 		new { error = "thread_id and run_id are required." },
	// 		EventStreamWriter.JsonOptions,
	// 		cancellationToken);
	// 	return;
	// }

	input.ThreadId ??= Guid.NewGuid().ToString();
	input.RunId ??= Guid.NewGuid().ToString();

	var events = BuildEvents(input.ThreadId, input.RunId);

	// The sample currently streams events as SSE. When protobuf support becomes available,
	// the Accept header can be inspected via EventStreamWriter.AcceptsProtobuf.
	await EventStreamWriter.WriteSseAsync(context, events, cancellationToken);
});

app.Run();

static IEnumerable<BaseEvent> BuildEvents(string threadId, string runId)
{
	var messageId = Guid.NewGuid().ToString();

	yield return new RunStartedEvent
	{
		ThreadId = threadId,
		RunId = runId
	};

	yield return new TextMessageStartEvent
	{
		MessageId = messageId,
		Role = "assistant"
	};

	yield return new TextMessageContentEvent
	{
		MessageId = messageId,
		Delta = "Hello world!"
	};

	yield return new TextMessageEndEvent
	{
		MessageId = messageId
	};

	yield return new RunFinishedEvent
	{
		ThreadId = threadId,
		RunId = runId
	};
}
