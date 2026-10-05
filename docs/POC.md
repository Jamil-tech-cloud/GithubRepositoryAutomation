# AI-Assisted Software Delivery on GitHub: Proof of Concept

This repository is a proof of concept for an AI-assisted delivery workflow. Issues, pull
requests, workflows, the Kanban board, and the production deployment all live in one
GitHub repository. Claude (via Claude Code) proposes work. Humans approve every step that
changes what users see or what runs in production.

## Principle

**Claude proposes, humans approve.** Claude can read the repository, comment on issues and
pull requests, write code on a feature branch, and open pull requests into `dev`. Claude
cannot approve, merge, close, or deploy anything. Those actions require a human.

## Flow

```mermaid
flowchart LR
  A[Issue created] -->|board: Backlog| B[@claude plan]
  B -->|plan comment| C[@claude implement]
  C -->|branch + tests + PR into dev| D[PR opened]
  D -->|board: Development Done| E[Claude PR review]
  E --> F{Human reviews and merges to dev}
  F --> G[QA handoff note on the issue]
  G --> H{Human promotes dev to main}
  H -->|CD: tests| I{Human approves production}
  I -->|build, ship, health check| J[Live on VPS]
  J --> K[Human publishes draft release]
  K -->|board: Completed, issue closed| L[Done]
```

Automated runs in the middle of the flow (review, plan, implement, CI failure analysis, QA
handoff, release-notes draft) happen on GitHub's servers. Nobody needs to keep a laptop on.

## Board

The Project board has five Status values: **Backlog → In Progress → Development Done → QA
→ Completed**. The `QA` column is available but skipped in this demo, so issues can move
from Development Done straight to Completed.

| Trigger | Status change |
|---|---|
| Issue created | Backlog |
| Issue assigned, or `feature/N-…` branch created | Backlog → In Progress |
| PR into `dev` opened or marked ready | → Development Done |
| Draft PR (or PR converted back to draft) | → In Progress |
| PR into `dev` closed without merging | → In Progress |
| `qa-failed` label added while in QA | QA → In Progress, with an explanatory comment |
| Release published | → Completed, and the issue is closed |

## Who does what

| Step | Automated | Human |
|---|---|---|
| Create issue | | ✅ |
| Plan (`@claude plan`) | ✅ Claude comments the plan | |
| Implement (`@claude implement`) | ✅ Claude creates branch, code, tests, PR | |
| Board moves | ✅ scripts | |
| PR checks (build, issue link) | ✅ required checks | |
| PR review comment | ✅ Claude, advisory only | |
| CI failure analysis | ✅ Claude, on failed PR runs | |
| Merge PR into `dev` | | ✅ |
| QA handoff note | ✅ Claude, on merge into dev | |
| Promote `dev` to `main` | | ✅ |
| Tests before deploy | ✅ | |
| Approve production deploy | | ✅ required reviewer |
| Build, ship, health check | ✅ | |
| Release notes draft | ✅ Claude, on manual run | |
| Publish release | | ✅ |
| Move issue to Completed, close | ✅ on publish | |

## Repository layout

- `CLAUDE.md`: conventions and guardrails read by Claude
- `src/SampleApi`, `tests/SampleApi.Tests`: the sample ASP.NET Core (.NET 9) API and its xUnit tests
- `Dockerfile`, `.dockerignore`: production image
- `scripts/`: board and link helpers (`set-issue-status.sh`, `parse-linked-issue.sh`, `complete-released-issues.sh`, `promote-dev-issues-to-qa.sh`)
- `.github/workflows/`: all automation (see below)
- `.github/actions/set-issue-status/`: reusable composite action for board moves
- `docs/`: this document and the demo script

### Workflows

| Workflow | Trigger | Purpose |
|---|---|---|
| CI | PR and push to dev, qa, main | Build and test |
| PR Issue Link Check | PR into dev | Required: PR must reference an issue |
| Board - New Issue | Issue opened | Set Backlog |
| Board - In Progress | Issue assigned; branch created | Set In Progress |
| Board - PR Opened | PR opened, ready, reopened, converted to draft | Set Development Done or In Progress |
| Board - PR Closed Without Merging | PR closed unmerged | Set In Progress |
| Board - QA Failed | `qa-failed` label | Back to In Progress, with comment |
| Claude - Issue Plan | `@claude plan` comment or `claude-plan` label | Plan comment |
| Claude - Implement Issue | `@claude implement` comment | Branch, code, tests, PR |
| Claude - PR Review | PR into dev | Advisory review and description suggestion |
| Claude - CI Failure Analysis | Failed CI run on a PR | Root-cause comment |
| QA Handoff Note | PR merged into dev | Handoff comment on the issue |
| Release Notes (draft) | Manual run with a version | Draft release with changelog and checklists |
| Release Published | Release published | Issues moved to Completed and closed |
| CD - Production | Push to main | Tests, then approval-gated deploy to the VPS |

## Setup

These steps assume a fresh repository and a user-owned GitHub Project.

1. **Repository.** Create the repository. Make it **public**. Branch protection and
   environment protection on a private repository require a paid plan, and a public
   repository gets them on the free plan.
2. **Branches.** Create `main` (default), `dev`, and `qa`.
3. **Branch protection** on `main`, `dev`, and `qa`: require pull requests and the CI
   status check, block force-push and deletion, and enable "include administrators". On
   `dev`, also require the `PR Issue Link Check` status. Required approvals are **0**
   because GitHub does not let an author approve their own pull request. See limitations.
4. **Project board.** Create a Project (v2), link the repository, and set the Status
   field options to exactly `Backlog, In Progress, Development Done, QA, Completed`.
5. **Labels.** Create `claude-plan` and `qa-failed`.
6. **Secrets and variables** (Settings → Secrets and variables → Actions):

   | Name | Type | Value |
   |---|---|---|
   | `CLAUDE_CODE_OAUTH_TOKEN` | secret | Created with `claude setup-token` on a machine logged into a Claude Pro/Max account |
   | `PROJECT_TOKEN` | secret | A classic personal access token with `repo` and `project` scopes (see below) |
   | `VPS_SSH_KEY` | secret | A dedicated ed25519 private key whose public half is in the deploy user's `authorized_keys` |
   | `PROJECT_OWNER` | variable | The account that owns the Project |
   | `PROJECT_NUMBER` | variable | The Project number |
   | `VPS_HOST` | variable | The server's address |
   | `VPS_USER` | variable | The deploy user, for example `deploy` |

7. **Production environment.** Settings → Environments → New environment `production`.
   Add yourself as a required reviewer and restrict deployments to the `main` branch.
8. **VPS.** Ubuntu with Docker installed. Create a non-root deploy user, add the public
   key to its `authorized_keys`, and allow only ports 22, 80, and 443 in `ufw`.
9. **Workflow permissions.** Each workflow sets its own `permissions:` block. Keep the
   repository default at read-only.

### Why `PROJECT_TOKEN` is a personal access token

Projects v2 cannot be updated with the default `GITHUB_TOKEN`. A classic personal access
token works. A fine-grained token did not work for Projects v2 mutations in our testing.
Also, pull requests opened with the default `GITHUB_TOKEN` do not trigger other workflows,
so `claude-implement.yml` uses `PROJECT_TOKEN` for its git and PR operations. This is
documented in that file.

## Security and guardrails

**Untrusted input.** Issue bodies, comments, pull request text, diffs, and CI logs are
written by anyone with access to the repository, so they are treated as data. Every Claude
prompt in this repository says so, and tells Claude not to follow instructions found in
them. CI logs and pull request content are passed as files for analysis, not interpolated
into commands.

**What Claude cannot do.** Claude's permission lists explicitly deny merging, approving,
closing pull requests, raw API calls (`gh api`), `curl`, and `wget`. Claude's own
`GITHUB_TOKEN` is limited by each job's `permissions:` block. Claude has no credential that
can approve or deploy.

**Human gates.** A human must merge into `dev`. A human must promote `dev` into `main`. A
human must approve every production deployment. A human must publish every release.

**Least privilege.** Every workflow declares the minimum `permissions:`. Claude runs
with `--max-turns` limits (15 to 40 depending on the task).

**Who can trigger Claude.** Comment and label triggers come from users with write access,
because `claude-code-action` only acts on write-access users by default. This matters because
the repository is public.

**Secrets.** Secrets are only referenced in workflow files and are masked in logs. The
deploy job removes its SSH key at the end of the run. Nobody pastes a secret into an issue,
pull request, or chat.

**Branch protection.** Pushing directly to `dev`, `qa`, or `main` is rejected, including for
administrators.

**Loops.** Claude's own comments post under the triggering user's identity. A comment that
mentions a trigger phrase could re-trigger an automation. This happened in testing and is
prevented by two guards: a check for the action's own footer, and an exact phrase check in
each workflow's `if:`. Claude is also told never to write trigger phrases in its comments.

## Cost

Costs below are based on runs observed during this POC. They are estimates, not a bill.

| Item | Per run | Notes |
|---|---|---|
| Claude plan | about $0.04–0.09 | Observed |
| Claude implement | about $0.04–0.11 | Observed. Longer runs cost more |
| Claude PR review | about $0.05–0.13 | Observed |
| CI failure analysis, QA handoff, release notes | about $0.05–0.10 each | Estimated; not yet measured |
| GitHub Actions | free | Public repository |
| VPS | your provider's monthly price | Not included here |

Claude usage is drawn from the Claude Pro subscription used for `CLAUDE_CODE_OAUTH_TOKEN`,
not billed per token on a separate invoice. Heavy use can hit the plan's usage limits. For
a company rollout, check which plan or API billing it should use.

## Known limitations

- **Self-approval.** The owner of a solo repository cannot be blocked from approving their
  own pull requests, and required approvals are set to zero for the same reason. Production
  needs at least one other reviewer.
- **Single production target.** There is no QA or staging environment in this demo. The
  QA column is kept in the board but skipped.
- **Deploys reset data.** The sample API keeps items in memory. Each production deploy
  resets it to the seed items.
- **Plain HTTP.** The VPS is served over HTTP only. HTTPS needs a domain and a reverse
  proxy such as Caddy.
- **Docker group is effectively root.** The deploy user is in the `docker` group, so it can
  control the host. Use rootless Docker or a narrowly scoped `sudo` rule in production.
- **Personal project.** The Project board is owned by a personal account. Automation that
  reacts to Project status changes via GitHub's `projects_v2_item` event is unverified for
  personal projects, so the release flow uses publish events instead.
- **Release timing.** Production deploys run when a change reaches `main` and is approved.
  Publishing the release afterwards records what shipped and closes the issues. It does not
  start the deploy.
- **Claude is advisory.** Review and CI analysis can be wrong or miss things. A human still
  decides.
- **Permission allowlists are not a sandbox.** Commands such as `dotnet` run arbitrary code in
  the repository by design. The real boundaries are the token scopes, branch protection, and
  the approval gates.
- **Token expiry.** `PROJECT_TOKEN` is a personal access token and must be renewed when it
  expires. Renewal and token ownership are a manual process.
- **Trigger phrase matching.** The action's own trigger matching is not exact. Each
  workflow has an explicit exact-phrase check to compensate.
- **Comment deduplication.** Sticky comments did not reliably update a single comment across
  separate runs in testing. Repeated runs may add comments.

## Production rollout roadmap

1. **Reviewers.** Require at least one human reviewer on `dev` and `main`. Add a `CODEOWNERS`
   file.
2. **Identity.** Replace the personal access token with a GitHub App installation token, so
   automation is not tied to one person's account.
3. **Board ownership.** Move the Project and repository to an organization. This enables
   organization-level project events, if they are confirmed to work for this use case.
4. **Deploy hardening.** Use a dedicated deploy user with rootless Docker or a narrow sudo
   rule. Pin the host's SSH key in `known_hosts` instead of scanning it on each run.
5. **HTTPS and domain.** Add a domain and Caddy with automatic TLS.
6. **Environments.** Add `qa` (and optionally `staging`) with the same approval pattern, and
   turn on the QA column.
7. **Data.** Replace the in-memory store with a real database, with migrations reviewed as
   part of the risk checklist.
8. **Observability.** Add structured logs, uptime monitoring, and alerts on failed deploys.
9. **Security scanning.** Add dependency and secret scanning, and a container image scan
   before deploy.
10. **Cost controls.** Set a monthly budget for Claude usage, cap concurrent automation runs,
    and track cost per pull request.
11. **Model and tool review.** Re-evaluate the model choice and each automation's permission
    allowlist on a schedule.
