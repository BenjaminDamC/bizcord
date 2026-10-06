# Bizcord

A business communication platform (Slack/Discord-style), built as a set of
microservices. This is the System Integration (E2026) project.

- `docs/` — analysis & design: domains, service boundaries, data ownership, interactions
- `src/` — the microservices themselves (added one service at a time)

## What Bizcord is

Users belong to a workspace, chat in channels and direct messages, share files,
and get notified about activity. The platform is split into independently
deployable services that talk to each other over REST and RabbitMQ.

## Docs

- [`docs/architecture.md`](docs/architecture.md) — domains, responsibilities, boundaries, data ownership
- [`docs/interactions.md`](docs/interactions.md) — how the services communicate (sync vs async)
