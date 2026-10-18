using EasyNetQ;
using MessagingService.Contracts;
using MessagingService.Data;
using MessagingService.Domain;
using MessagingService.Messages;
using MessagingService.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// RabbitMQ (EasyNetQ) + the message client wrapper.
var rabbitMq = builder.Configuration.GetConnectionString("RabbitMQ") ?? "host=localhost";
builder.Services.AddEasyNetQ(rabbitMq);
builder.Services.AddSingleton<IMessageClient, MessageClient>();

// In-memory persistence (swap for a real store later).
builder.Services.AddSingleton<IChannelRepository, InMemoryChannelRepository>();
builder.Services.AddSingleton<IMessageRepository, InMemoryMessageRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "messaging-service" }));

// Subscribe to MessageCreated so we can see messages published by anyone
// (including our own REST API) round-trip through the broker.
var messageClient = app.Services.GetRequiredService<IMessageClient>();
await messageClient.SubscribeAsync<MessageCreated>("messaging-service", msg =>
{
    app.Logger.LogInformation("Received MessageCreated {MessageId} in channel {ChannelId}", msg.MessageId, msg.ChannelId);
    return Task.CompletedTask;
});

// --- REST API (exposes the shared model, not the domain) ---

app.MapGet("/channels", async (IChannelRepository channels) =>
{
    var all = await channels.GetAllAsync();
    return Results.Ok(all.Select(c => new ChannelDto(c.Id, c.Name, c.Description, c.CreatedAt)));
});

app.MapGet("/channels/{id:guid}", async (Guid id, IChannelRepository channels) =>
{
    var channel = await channels.GetByIdAsync(id);
    return channel is null
        ? Results.NotFound()
        : Results.Ok(new ChannelDto(channel.Id, channel.Name, channel.Description, channel.CreatedAt));
});

app.MapPost("/channels", async (CreateChannelRequest request, IChannelRepository channels) =>
{
    var channel = new Channel { Name = request.Name, Description = request.Description };
    await channels.AddAsync(channel);
    return Results.Created($"/channels/{channel.Id}", new ChannelDto(channel.Id, channel.Name, channel.Description, channel.CreatedAt));
});

app.MapGet("/channels/{channelId:guid}/messages", async (Guid channelId, IMessageRepository messages) =>
{
    var result = await messages.GetByChannelAsync(channelId);
    return Results.Ok(result.Select(m => new MessageDto(m.Id, m.ChannelId, m.AuthorId, m.Content, m.CreatedAt)));
});

app.MapPost("/channels/{channelId:guid}/messages", async (
    Guid channelId,
    CreateMessageRequest request,
    IMessageRepository messages,
    IMessageClient client) =>
{
    var message = new Message { ChannelId = channelId, AuthorId = request.AuthorId, Content = request.Content };
    await messages.AddAsync(message);

    // REST complements messaging: persist + reply synchronously, but also publish
    // an event so Notifications/Search can react without the caller waiting.
    await client.PublishAsync(new MessageCreated(message.Id, message.ChannelId, message.AuthorId, message.Content, message.CreatedAt));

    return Results.Created(
        $"/channels/{channelId}/messages/{message.Id}",
        new MessageDto(message.Id, message.ChannelId, message.AuthorId, message.Content, message.CreatedAt));
});

app.Run();
