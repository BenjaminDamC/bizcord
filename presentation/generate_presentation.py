#!/usr/bin/env python3
"""Generate a self-contained HTML presentation (then PDF) for the Bizcord
System Integration CA#1 messaging microservice. Reads the actual source files
so the code shown is byte-accurate."""
import html as _html
import os
import re

REPO = "/Users/benjamin/bizcord/apps/messaging-microservice"
OUT_HTML = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Bizcord-Presentation.html")

# ---------------------------------------------------------------- highlighter
KEYWORDS = set(
    """abstract as base bool break byte case catch char checked class const
    continue decimal default delegate do double else enum event explicit extern
    false finally fixed float for foreach goto if implicit in int interface
    internal is lock long namespace new null object operator out override params
    private protected public readonly ref return sbyte sealed short sizeof
    stackalloc static string struct switch this throw true try typeof uint ulong
    unchecked unsafe ushort using virtual void volatile while async await var
    record init with get set value required nameof where when global file scoped
    partial""".split()
)

_TOKEN = re.compile(
    r"(?P<comment>//[^\n]*|/\*[\s\S]*?\*/)"
    r"|(?P<string>@?\"(?:[^\"\\\n]|\\.)*\"|@?'(?:[^'\\\n]|\\.)*')"
    r"|(?P<number>\b\d[\d_.]*\b)"
    r"|(?P<kw>\b(?:" + "|".join(sorted(KEYWORDS, key=len, reverse=True)) + r")\b)"
)


def highlight(code: str) -> str:
    out, last = [], 0
    for m in _TOKEN.finditer(code):
        out.append(_html.escape(code[last:m.start()]))
        tok = m.group()
        cls = {"comment": "c", "string": "s", "number": "n", "kw": "k"}[m.lastgroup]
        out.append(f'<span class="{cls}">{_html.escape(tok)}</span>')
        last = m.end()
    out.append(_html.escape(code[last:]))
    return "".join(out)


def read_lines(relpath, start=None, end=None):
    with open(os.path.join(REPO, relpath), encoding="utf-8") as f:
        lines = [l.rstrip("\n").rstrip("\r") for l in f.read().split("\n")]
    s = (start - 1) if start else 0
    e = end if end else len(lines)
    return "\n".join(lines[s:e]).rstrip()


def code_block(label, code_text):
    return (
        f'<div class="code"><div class="code-head">{_html.escape(label)}</div>'
        f"<pre>{highlight(code_text)}</pre></div>"
    )


# -------------------------------------------------------------------- content
P = []  # html parts

# ---------- cover ----------
P.append(
    """
<div class="cover">
  <div class="cover-badge">EASV · System Integration (E2026)</div>
  <h1>Bizcord — Messaging Microservice</h1>
  <div class="cover-sub">Compulsory Assignment #1 · Weeks 36–40</div>
  <div class="cover-meta">
    A Slack/Discord-style business-communication platform, split into
    independently deployable microservices that talk over REST and RabbitMQ.
    This deck walks through all five tasks and the C#/.NET code behind them.
  </div>
  <div class="cover-foot">Benjamin Dam Christensen</div>
</div>
<div class="pagebreak"></div>
"""
)

# ---------- what this is ----------
P.append(
    """
<h2>What this assignment asked for</h2>
<p>
Bizcord is decomposed into <strong>eight microservices</strong>, each owning one
bounded context. The assignment is to finalize the weekly labs from W36–W40 for
<strong>one</strong> service — the Messaging service — and document the analysis
&amp; design in a <code>docs/</code> folder. Submission is a single repository link.
</p>
<table>
  <tr><th>Task</th><th>Week</th><th>Deliverable in this repo</th></tr>
  <tr><td>1. Analysis &amp; Design</td><td>W36</td><td><code>docs/architecture.md</code>, <code>docs/interactions.md</code></td></tr>
  <tr><td>2. Setup &amp; Messaging</td><td>W37</td><td>Microservice in the monorepo + EasyNetQ message client</td></tr>
  <tr><td>3. Inter-service communication</td><td>W38</td><td>Domain model, shared model, REST API</td></tr>
  <tr><td>4. Deployment</td><td>W39</td><td><code>Dockerfile</code> + <code>docker-compose.yml</code></td></tr>
  <tr><td>5. First feature &amp; testing</td><td>W40</td><td>Channel membership feature + xUnit tests</td></tr>
</table>
<div class="callout"><strong>Stack:</strong> .NET 9 (ASP.NET Core minimal API) · RabbitMQ via EasyNetQ ·
in-memory repositories · Docker · xUnit</div>
"""
)

# ---------- Task 1 ----------
P.append(
    """
<div class="pagebreak"></div>
<h2>Task 1 — Analysis &amp; Design</h2>
<p>
The platform splits into bounded contexts along <em>“one reason to change”</em>:
auth changes for security reasons, messaging for product features, billing for
pricing — so they become separate services. Two of them are <strong>derived</strong>
services (Presence, Search): they mirror data owned elsewhere and consume events
instead of owning the source of truth.
</p>
<table>
  <tr><th>Service</th><th>Responsibility</th><th>Owns (data)</th></tr>
  <tr><td>Identity &amp; Access</td><td>accounts, SSO, profiles, sessions</td><td>users, credentials, sessions</td></tr>
  <tr><td>Organisation &amp; Membership</td><td>workspaces, members, roles, invites</td><td>organisations, memberships, roles</td></tr>
  <tr><td class="hl">Messaging</td><td class="hl">channels, threads, DMs, reactions, read-state</td><td class="hl">channels, messages, reactions</td></tr>
  <tr><td>Presence</td><td>online/away status, typing</td><td>presence state</td></tr>
  <tr><td>Notifications</td><td>in-app, email, push delivery</td><td>preferences, delivery state</td></tr>
  <tr><td>Files &amp; Media</td><td>store/serve uploads</td><td>file metadata, blobs</td></tr>
  <tr><td>Search</td><td>full-text index</td><td>search index <em>(derived)</em></td></tr>
  <tr><td>Billing &amp; Subscriptions</td><td>plans, payments, invoices</td><td>subscriptions, payments, invoices</td></tr>
</table>
<div class="callout"><strong>Boundary rule:</strong> services never write to another
service’s store directly — they call its API or consume its events. Search is the
clearest case: it keeps a read-only copy of messages, rebuilt from events, while
Messaging stays the single source of truth.</div>
"""
)

# ---------- Task 2 ----------
P.append(
    """
<div class="pagebreak"></div>
<h2>Task 2 — Setup &amp; the message client</h2>
<p>
The service lives in a monorepo (<code>apps/messaging-microservice</code>) and
talks to RabbitMQ through a thin abstraction. The rest of the service depends on
<code>IMessageClient</code> — not EasyNetQ directly — so the broker could be swapped
without touching domain code.
</p>
"""
)
P.append(code_block("src/MessagingService/Messaging/IMessageClient.cs",
                    read_lines("src/MessagingService/Messaging/IMessageClient.cs")))
P.append(code_block("src/MessagingService/Messaging/MessageClient.cs",
                    read_lines("src/MessagingService/Messaging/MessageClient.cs")))
P.append(
    """
<p>Wiring in <code>Program.cs</code> — EasyNetQ reads the connection string, and the
repositories/message client are registered for dependency injection:</p>
"""
)
P.append(code_block("src/MessagingService/Program.cs (lines 19–27)",
                    read_lines("src/MessagingService/Program.cs", 19, 27)))

# ---------- Task 3: domain model ----------
P.append(
    """
<div class="pagebreak"></div>
<h2>Task 3 — Domain model (internal entities)</h2>
<p>
The domain entities are the <strong>internal</strong> truth — richer than what other
services need. <code>ChannelMember</code>, for example, keeps the role, per-channel
permission overrides <em>and</em> an audit log of every role change.
</p>
"""
)
P.append(code_block("src/MessagingService/Domain/Channel.cs",
                    read_lines("src/MessagingService/Domain/Channel.cs")))
P.append(code_block("src/MessagingService/Domain/Message.cs",
                    read_lines("src/MessagingService/Domain/Message.cs")))
P.append(code_block("src/MessagingService/Domain/ChannelMember.cs (excerpt)",
                    read_lines("src/MessagingService/Domain/ChannelMember.cs", 18, 47)))

# ---------- Task 3: shared model ----------
P.append(
    """
<div class="pagebreak"></div>
<h2>Task 3 — Shared model (the contract others integrate with)</h2>
<p>
The <code>Contracts/</code> records are the <strong>narrow</strong> surface other
services see. A <code>ChannelMember</code> has overrides and a role history
internally, but the API only exposes <code>userId</code>, <code>roleType</code> and the
<em>resolved</em> permissions.
</p>
"""
)
P.append(code_block("src/MessagingService/Contracts/ChannelMemberDto.cs",
                    read_lines("src/MessagingService/Contracts/ChannelMemberDto.cs")))
P.append(code_block("src/MessagingService/Contracts/ChannelDto.cs",
                    read_lines("src/MessagingService/Contracts/ChannelDto.cs")))
P.append(code_block("src/MessagingService/Contracts/MessageDto.cs",
                    read_lines("src/MessagingService/Contracts/MessageDto.cs")))
P.append(
    """
<p>The mapping from domain → shared model lives on the entity itself, so the
resolution logic (role defaults + overrides) stays in one place:</p>
"""
)
P.append(code_block("src/MessagingService/Domain/ChannelMember.cs — EffectivePermissions + ToDto",
                    read_lines("src/MessagingService/Domain/ChannelMember.cs", 53, 86)))

# ---------- Task 3: REST + events ----------
P.append(
    """
<div class="pagebreak"></div>
<h2>Task 3 — REST API (sync) + events (async)</h2>
<p>
The minimal API exposes channels, messages and members. The interesting part is
the <code>POST</code> on messages: it <strong>persists + replies synchronously</strong>, then
<strong>publishes a <code>MessageCreated</code> event</strong> so Notifications/Search can
react without the caller waiting.
</p>
"""
)
P.append(code_block("src/MessagingService/Program.cs — channels & messages endpoints",
                    read_lines("src/MessagingService/Program.cs", 48, 95)))
P.append(code_block("src/MessagingService/Messages/MessageCreated.cs",
                    read_lines("src/MessagingService/Messages/MessageCreated.cs")))
P.append(
    """
<div class="callout"><strong>Why both?</strong> REST (request/response) when the
caller needs an answer now; events (fire-and-forget) when the work can happen later.
This keeps the core Messaging path fast — it publishes and moves on instead of
waiting for every downstream service.</div>
"""
)

# ---------- Task 4 ----------
P.append(
    """
<div class="pagebreak"></div>
<h2>Task 4 — Deployment</h2>
<p>
A multi-stage <code>Dockerfile</code> builds in the SDK image and runs in the tiny
<code>aspnet</code> runtime image. <code>docker-compose</code> then wires RabbitMQ and the
service together with a healthcheck so the service only starts once the broker is up.
</p>
"""
)
P.append(code_block("Dockerfile", read_lines("Dockerfile")))
P.append(code_block("docker-compose.yml", read_lines("docker-compose.yml")))

# ---------- Task 5 ----------
P.append(
    """
<div class="pagebreak"></div>
<h2>Task 5 — First feature &amp; testing</h2>
<p>
The “first feature” is channel membership. The tests cover the domain logic the
REST API and shared model build on: role changes, the audit log, effective
permissions, and the narrow DTO. <strong>8 tests pass</strong>
(<code>dotnet test</code>).
</p>
"""
)
P.append(code_block("tests/MessagingService.Tests/ChannelMemberTests.cs (excerpt)",
                    read_lines("tests/MessagingService.Tests/ChannelMemberTests.cs", 17, 39)))
P.append(
    """
<div class="callout"><strong>Verification:</strong>
<code>dotnet build</code> → 0 errors, 0 warnings ·
<code>dotnet test</code> → 8 passed · service runs via
<code>docker compose up</code> (RabbitMQ + messaging-service).</div>
"""
)

# ---------- architecture diagram ----------
P.append(
    """
<div class="pagebreak"></div>
<h2>The architecture at a glance</h2>
<div class="diagram">
"""
    + """
<svg viewBox="0 0 720 430" width="100%" xmlns="http://www.w3.org/2000/svg">
  <style>
    .bx { fill:#eef2ff; stroke:#4f46e5; stroke-width:1.3; }
    .bx-msg { fill:#e0f2fe; stroke:#0369a1; stroke-width:1.6; }
    .lbl { font-family:Helvetica,Arial,sans-serif; font-size:12px; fill:#1e293b; text-anchor:middle; }
    .lbl-b { font-weight:bold; }
    .arr { stroke:#64748b; stroke-width:1.4; fill:none; marker-end:url(#ah); }
    .arr-e { stroke:#0369a1; stroke-width:1.4; fill:none; marker-end:url(#ah); stroke-dasharray:4 3; }
    .tag { font-family:Helvetica,Arial,sans-serif; font-size:9px; fill:#475569; text-anchor:middle; }
  </style>
  <defs>
    <marker id="ah" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
      <path d="M0,0 L10,5 L0,10 z" fill="#64748b"/>
    </marker>
  </defs>

  <rect x="288" y="18" width="144" height="30" rx="6" class="bx"/><text x="360" y="37" class="lbl lbl-b">API Gateway</text>
  <rect x="10" y="76" width="80" height="30" rx="6" class="bx"/><text x="50" y="95" class="lbl">Web</text>
  <rect x="120" y="76" width="80" height="30" rx="6" class="bx"/><text x="160" y="95" class="lbl">Mobile</text>
  <path d="M50,106 C50,126 300,108 318,54" class="arr"/>
  <path d="M160,106 C160,126 320,108 336,54" class="arr"/>

  <rect x="40" y="160" width="120" height="40" rx="6" class="bx"/><text x="100" y="184" class="lbl">Identity &amp; Access</text>
  <rect x="190" y="160" width="120" height="40" rx="6" class="bx"/><text x="250" y="184" class="lbl">Organisation</text>
  <rect x="300" y="160" width="120" height="40" rx="6" class="bx-msg"/><text x="360" y="184" class="lbl lbl-b">Messaging</text>
  <rect x="430" y="160" width="110" height="40" rx="6" class="bx"/><text x="485" y="184" class="lbl">Presence</text>
  <rect x="560" y="160" width="120" height="40" rx="6" class="bx"/><text x="620" y="184" class="lbl">Files &amp; Media</text>

  <rect x="140" y="290" width="120" height="40" rx="6" class="bx"/><text x="200" y="314" class="lbl">Notifications</text>
  <rect x="300" y="290" width="120" height="40" rx="6" class="bx"/><text x="360" y="314" class="lbl">Search</text>
  <rect x="460" y="290" width="120" height="40" rx="6" class="bx"/><text x="520" y="314" class="lbl">Billing</text>

  <path d="M360,18 C360,44 100,44 100,158" class="arr"/>
  <path d="M360,18 C360,44 250,44 250,158" class="arr"/>
  <path d="M360,18 C360,44 360,44 360,158" class="arr"/>
  <path d="M360,18 C360,44 485,44 485,158" class="arr"/>
  <path d="M360,18 C360,44 620,44 620,158" class="arr"/>

  <path d="M360,200 C360,250 200,250 200,288" class="arr-e"/>
  <text x="270" y="243" class="tag">event: message.created</text>
  <path d="M360,200 C360,250 360,250 360,288" class="arr-e"/>
  <text x="395" y="243" class="tag">event: message.created</text>

  <text x="360" y="410" class="lbl" style="font-size:10px;fill:#64748b;">solid = REST (sync) · dashed = event (async, over RabbitMQ)</text>
</svg>
"""
    + """
</div>
<div class="callout"><strong>Sync vs async, once more:</strong> the gateway calls
services over REST for request/response; Messaging fans a <code>message.created</code>
event out to Notifications and Search so the sender is never blocked.</div>
"""
)

# ---------- takeaways ----------
P.append(
    """
<div class="pagebreak"></div>
<h2>Key takeaways</h2>
<ul>
  <li><strong>Bound by “one reason to change”</strong> — each service owns one bounded context and its data.</li>
  <li><strong>Domain model ≠ shared model</strong> — entities stay rich internally; other services only see narrow DTOs.</li>
  <li><strong>REST for answers, events for side-effects</strong> — persist + reply, then publish; never block the hot path.</li>
  <li><strong>Abstractions over infrastructure</strong> — <code>IMessageClient</code> hides the broker; repositories hide storage.</li>
  <li><strong>Ship it reproducibly</strong> — multi-stage Docker build + compose with healthchecks.</li>
  <li><strong>Test the domain</strong> — 8 xUnit tests prove role changes, audit logging and permission resolution.</li>
</ul>
<div class="callout">Repo: <code>github.com/BenjaminDamC/bizcord</code> · service:
<code>apps/messaging-microservice</code> · docs: <code>docs/architecture.md</code> +
<code>docs/interactions.md</code></div>
"""
)

# ------------------------------------------------------------------ assemble
CSS = """
@page { size: A4; margin: 12mm 10mm; }
* { box-sizing: border-box; }
body {
  font-family: -apple-system, "Segoe UI", Helvetica, Arial, sans-serif;
  color: #1e293b; font-size: 11px; line-height: 1.55; margin: 0;
  -webkit-print-color-adjust: exact; print-color-adjust: exact;
}
h1 { font-size: 30px; margin: 0 0 6px; color: #1e293b; }
h2 {
  font-size: 19px; color: #4f46e5; border-bottom: 2px solid #4f46e5;
  padding-bottom: 6px; margin: 0 0 10px;
}
p { margin: 6px 0; }
code {
  font-family: "SF Mono", Menlo, Consolas, monospace; font-size: 10px;
  background: #f1f5f9; padding: 1px 4px; border-radius: 3px; color: #0f172a;
}
table { border-collapse: collapse; width: 100%; margin: 8px 0 12px; }
th, td { border: 1px solid #cbd5e1; padding: 5px 8px; text-align: left; vertical-align: top; }
th { background: #eef2ff; color: #312e81; }
td.hl { background: #f0f9ff; }
.code {
  margin: 8px 0 10px; border: 1px solid #e2e8f0; border-radius: 6px; overflow: hidden;
}
.code-head {
  background: #0f172a; color: #e2e8f0; font-family: "SF Mono", Menlo, Consolas, monospace;
  font-size: 9px; padding: 3px 10px;
}
.code pre {
  margin: 0; padding: 8px 10px; background: #f8fafc;
  font-family: "SF Mono", Menlo, Consolas, monospace; font-size: 8.5px; line-height: 1.5;
  white-space: pre-wrap; word-wrap: break-word;
}
.k { color: #7c3aed; font-weight: 600; }
.s { color: #b45309; }
.c { color: #64748b; font-style: italic; }
.n { color: #0f766e; }
.callout {
  background: #eef2ff; border-left: 4px solid #4f46e5; padding: 8px 12px;
  margin: 10px 0; border-radius: 0 6px 6px 0;
}
ul { margin: 6px 0; padding-left: 20px; }
li { margin: 4px 0; }
.cover { text-align: left; padding: 90px 10px 0; }
.cover-badge { color: #4f46e5; font-weight: 700; letter-spacing: 1px; text-transform: uppercase; font-size: 12px; }
.cover h1 { font-size: 42px; margin-top: 18px; }
.cover-sub { font-size: 16px; color: #475569; margin: 10px 0 22px; }
.cover-meta { font-size: 13px; color: #334155; max-width: 520px; line-height: 1.7; }
.cover-foot { margin-top: 70px; font-size: 13px; color: #64748b; border-top: 1px solid #e2e8f0; padding-top: 12px; }
.diagram { margin: 12px 0; }
.pagebreak { page-break-after: always; }
"""

html_doc = f"""<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8">
<title>Bizcord — Messaging Microservice</title>
<style>{CSS}</style></head>
<body>
{''.join(P)}
</body></html>"""

with open(OUT_HTML, "w", encoding="utf-8") as f:
    f.write(html_doc)

print(f"Wrote {OUT_HTML} ({len(html_doc)} chars)")
