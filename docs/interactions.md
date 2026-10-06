# Interactions

How the services communicate. Two patterns: synchronous REST for
request/response, and asynchronous events (over RabbitMQ) for fire-and-forget.

```mermaid
flowchart TB
    Web["Web client"] --> GW["API Gateway"]
    Mobile["Mobile client"] --> GW

    GW --> IAM["Identity & Access"]
    GW --> ORG["Organisation & Membership"]
    GW --> MSG["Messaging"]
    GW --> PRS["Presence"]
    GW --> FLS["Files & Media"]
    GW --> SRH["Search"]
    GW --> BLL["Billing & Subscriptions"]

    MSG -- "event: message.created" --> NTF["Notifications"]
    MSG -- "event: user.active" --> PRS
    MSG -- "REST: upload attachment" --> FLS
    MSG -- "event: message.created" --> SRH
    ORG -- "REST: get user roles" --> IAM
    ORG -- "REST: get plan" --> BLL
```

## Sync vs async

- **REST (sync)** when the caller needs a result before it can continue — e.g.
  Messaging asks Files to store an attachment, or Organisation checks a user's
  role in Identity.
- **Events (async)** when the work can happen later and must not slow down the
  core path — e.g. a new message fans out to Notifications and Search.

The point is to keep the core Messaging path fast: it publishes an event and
moves on, instead of waiting for every downstream service.
