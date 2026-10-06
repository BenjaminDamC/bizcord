# Analysis & Design

This document records the analysis and design for Bizcord: the domains, the
microservices and what each is responsible for, where the boundaries are, and
who owns which data.

## Domains

Bizcord breaks down into these domains (bounded contexts):

- **Identity & access** — who someone is, and that they are who they say they are.
- **Organisation & membership** — the workspaces and who is a member with which role.
- **Messaging** — the actual conversation: channels, threads, direct messages.
- **Presence** — who is online right now, and who is typing. Short-lived and changes constantly.
- **Notifications** — getting a ping (in-app, email, push) to the right people.
- **Files & media** — uploads and attachments.
- **Search** — finding messages, files and people across a workspace.
- **Billing** — plans and payments.

## Microservices and responsibilities

Each domain is one service:

| Service | Responsibility |
|---|---|
| Identity & Access | accounts, authentication (SSO), profiles, sessions |
| Organisation & Membership | workspaces, members, roles, invites |
| Messaging | channels, threads, direct messages, reactions, mentions, read-state |
| Presence | online/away status and typing indicators |
| Notifications | in-app, email and push delivery, per user preferences |
| Files & Media | store and serve uploads (attachments, images, avatars) |
| Search | full-text index over messages, files and people |
| Billing & Subscriptions | plans, payment records, invoices |

Voice & video and bots/integrations are deliberately left out for now — they can
start as features inside Messaging/Organisation and split out later.

## Boundaries

The boundaries follow "one reason to change":

- Auth changes for security reasons, messaging for product features, billing for
  pricing — so they are separate services.
- Presence and Search are *derived* services: they mirror data owned elsewhere,
  so they consume events instead of owning the source of truth.
- Request/response (REST) where the caller needs an answer now; events where the
  work is fire-and-forget.

## Data ownership

Every piece of data has exactly one owner:

| Data | Owned by |
|---|---|
| users, credentials, profiles, sessions | Identity & Access |
| organisations, memberships, roles, invites | Organisation & Membership |
| channels, messages, reactions, threads, read-state | Messaging |
| presence state | Presence |
| notification preferences, delivery state | Notifications |
| file metadata, blobs | Files & Media |
| search index (derived) | Search |
| subscriptions, payments, invoices | Billing |

Services never write to another service's store directly — they go through the
owner's API or consume its events. Search is the clearest case: it keeps a
read-only copy of messages, rebuilt from events, while Messaging stays the
single source of truth.

See [`interactions.md`](interactions.md) for how the services talk to each other.
