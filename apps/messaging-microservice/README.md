# Messaging Microservice

The core messaging domain for Bizcord: channels, threads, direct messages,
reactions, mentions and read-state. One service in the Bizcord monorepo — see
the repo root for the full architecture and `docs/` for the analysis & design.

## Stack

- .NET 9 (ASP.NET Core minimal API)
- RabbitMQ via EasyNetQ

## Run locally

```bash
# start RabbitMQ
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# run the service
cd src/MessagingService
dotnet run
```

## Layout

- `Domain/` — internal entities (Channel, Message, Reaction, ReadState, ChannelMember)
- `Contracts/` — the shared model: DTOs other services integrate with, hiding internal fields
- `Data/` — in-memory repositories
- `Messaging/` — the EasyNetQ message client (`IMessageClient`)

## REST API

- `GET /channels`
- `GET /channels/{id}`
- `POST /channels`
- `GET /channels/{channelId}/messages`
- `POST /channels/{channelId}/messages` — persists the message and publishes a `MessageCreated` event
- `GET /channels/{channelId}/members`
- `POST /channels/{channelId}/members`
- `PATCH /channels/{channelId}/members/{userId}/role`

Members are a good example of the shared-model idea: internally a `ChannelMember`
keeps the role, per-channel permission overrides, and an audit log of role changes,
but the API only exposes `userId`, `roleType` and the resolved `permissions`.

## Message client

`Messaging/IMessageClient` is a thin wrapper over EasyNetQ's `IBus` so the rest
of the service depends on a plain publish/subscribe interface instead of the
broker directly. Posting a message via the REST API publishes a `MessageCreated`
event; the service also subscribes to it and logs it for a quick round-trip test.
