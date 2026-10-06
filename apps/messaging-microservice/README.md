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

## Message client

`Messaging/IMessageClient` is a thin wrapper over EasyNetQ's `IBus` so the rest
of the service depends on a plain publish/subscribe interface instead of the
broker directly. `Program.cs` wires it up, subscribes to `MessageCreated`, and
exposes a demo `POST /messages/test-publish` endpoint for a quick round-trip test.
