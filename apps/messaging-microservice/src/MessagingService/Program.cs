using EasyNetQ;
using MessagingService.Messages;
using MessagingService.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// RabbitMQ (EasyNetQ) + the message client wrapper.
var rabbitMq = builder.Configuration.GetConnectionString("RabbitMQ") ?? "host=localhost";
builder.Services.AddEasyNetQ(rabbitMq);
builder.Services.AddSingleton<IMessageClient, MessageClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "messaging-service" }));

// Demonstrate the message client end to end: log every MessageCreated the broker
// delivers to this service.
var messageClient = app.Services.GetRequiredService<IMessageClient>();
await messageClient.SubscribeAsync<MessageCreated>("messaging-service", msg =>
{
    app.Logger.LogInformation("Received MessageCreated {MessageId} in channel {ChannelId}", msg.MessageId, msg.ChannelId);
    return Task.CompletedTask;
});

// Demo endpoint that publishes a MessageCreated, so the round trip is easy to test.
app.MapPost("/messages/test-publish", async (IMessageClient client) =>
{
    var message = new MessageCreated(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "hello from the message client",
        DateTimeOffset.UtcNow);

    await client.PublishAsync(message);
    return Results.Ok(new { published = message.MessageId });
});

app.Run();
