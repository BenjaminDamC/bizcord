using EasyNetQ;
using MessagingService.Contracts;
using MessagingService.Data;
using MessagingService.Domain;
using MessagingService.Messages;
using MessagingService.Messaging;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Serialize enums as strings (e.g. "Moderator" instead of 1) so the API contract
// is stable and readable.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// RabbitMQ (EasyNetQ) + the message client wrapper.
var rabbitMq = builder.Configuration.GetConnectionString("RabbitMQ") ?? "host=localhost";
builder.Services.AddEasyNetQ(rabbitMq);
builder.Services.AddSingleton<IMessageClient, MessageClient>();

// In-memory persistence (swap for a real store later).
builder.Services.AddSingleton<IChannelRepository, InMemoryChannelRepository>();
builder.Services.AddSingleton<IMessageRepository, InMemoryMessageRepository>();
builder.Services.AddSingleton<IChannelMemberRepository, InMemoryChannelMemberRepository>();

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

// --- Channels ---

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

// --- Messages ---

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

// --- Members (channel-level roles & permissions) ---

app.MapGet("/channels/{channelId:guid}/members", async (Guid channelId, IChannelMemberRepository members) =>
{
    var result = await members.GetByChannelAsync(channelId);
    return Results.Ok(result.Select(m => m.ToDto()));
});

app.MapPost("/channels/{channelId:guid}/members", async (
    Guid channelId,
    CreateChannelMemberRequest request,
    IChannelMemberRepository members) =>
{
    var member = new ChannelMember(channelId, request.UserId, ToDomainRole(request.RoleType));
    await members.AddAsync(member);
    return Results.Created($"/channels/{channelId}/members/{member.UserId}", member.ToDto());
});

app.MapPatch("/channels/{channelId:guid}/members/{userId:guid}/role", async (
    Guid channelId,
    Guid userId,
    UpdateMemberRoleRequest request,
    IChannelMemberRepository members) =>
{
    var member = await members.GetAsync(channelId, userId);
    if (member is null)
    {
        return Results.NotFound();
    }

    // ChangeRole appends to the internal audit log, but the response (shared
    // model) only shows the resulting role + permissions.
    member.ChangeRole(ToDomainRole(request.RoleType), request.ChangedBy);
    return Results.Ok(member.ToDto());
});

app.Run();

static ChannelRole ToDomainRole(ChannelRoleType roleType) => roleType switch
{
    ChannelRoleType.Admin => ChannelRole.Admin,
    ChannelRoleType.Moderator => ChannelRole.Moderator,
    _ => ChannelRole.Member,
};
