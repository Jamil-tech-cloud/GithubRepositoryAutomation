# 10-Minute Demo Script

Goal: show a change going from idea to production with Claude doing the drafting and humans
making every decision that matters.

## Before you start (5 minutes beforehand)

- [ ] Open the repository's **Issues**, **Pull requests**, **Actions**, and **Project** tabs
      in separate browser tabs.
- [ ] Open the live API: http://173.212.207.172/swagger
- [ ] Confirm `main` is deployed: http://173.212.207.172/health returns `healthy`.
- [ ] Have a sample issue ready to create (below). Do not reuse issues from earlier testing.
- [ ] Be logged in as the reviewer, so you can approve the production deployment.

**Sample issue to create live:**

Title: `Add GET /api/items/count endpoint`

Body: `Add an endpoint that returns the total number of items as a plain integer, e.g. {"count": 2}. Add tests.`

## Script

### 0:00–1:00 · Framing
Say: "Claude proposes. Humans approve. Claude can comment, write code on a branch, and open
a pull request. It cannot merge, approve, or deploy."

Show the Project board with its five columns.

### 1:00–2:00 · Create the issue (Backlog)
Create the sample issue. Point out that it lands in **Backlog** automatically.

### 2:00–3:00 · Plan
Comment `@claude plan`. While it runs, explain that it reads the issue and writes a plan
without touching code. When the comment appears, read two lines of it aloud.

### 3:00–4:30 · Implement (In Progress → Development Done)
Comment `@claude implement`. Explain that the workflow creates a `feature/N-…` branch from
`dev`, writes the code and tests, runs `dotnet build` and `dotnet test`, and opens a pull
request.

While it runs, show the workflow in the **Actions** tab. Expect 1 to 3 minutes. When the
PR appears, point out:
- `Closes #N` links it to the issue.
- The issue has moved to **Development Done** on the board.
- The required checks passed: CI, the issue link check, and the board move.

### 4:30–5:30 · Review
Open the PR. Show Claude's **first-pass review** comment. Point out that it reports only
concrete problems, and that it never approves.

Say: "Claude found this. A human decides whether it matters." Then merge the PR into `dev`
yourself.

### 5:30–6:15 · QA handoff
After the merge, the **QA handoff** note appears on the issue within about a minute. Read the
checklist: what changed, what to test, and regression areas.

### 6:15–7:30 · Promote and approve production
Open a pull request from `dev` into `main`. Explain that promoting is a human decision.
Merge it.

The **CD - Production** run starts. The tests pass, and the deploy job waits. Show the
**Waiting for approval** banner. Click **Review deployments**, tick **production**, and
approve.

When the deploy finishes, open the live API and call `GET /api/items/count`, using Swagger's
**Try it out**.

### 7:30–8:45 · Release notes and completion
Run the **Release Notes (draft)** workflow with a version, for example `v1.0.0`. Explain that
Claude drafts the changelog, the risk checklist, and the rollback checklist, and that the
result is a *draft*.

Open the draft release. Point out the risk checklist, then **Publish release**.

Publishing triggers the **Release Published** workflow. Refresh the board: the issue is
**Completed** and closed.

### 8:45–9:30 · Failure analysis (optional, if time allows)
Break a test on a branch, or show a previous red CI run. Point out the **CI failure analysis**
comment, which gives a likely root cause and suggested fix.

### 9:30–10:00 · Recap
Summarize:
- Humans decided: what to build, what merged, what reached production, and what was released.
- Claude did: planning, implementation, review, CI analysis, the handoff note, and the
  release draft.
- It runs on GitHub's servers, not on anyone's laptop.

Point to `docs/POC.md` for the security model, cost, and limitations.

## If something is slow or fails

| Problem | What to do |
|---|---|
| `@claude implement` takes more than 3 minutes | Narrate the steps, and show the running workflow in Actions |
| Claude asks clarifying questions instead of implementing | Say that's the intended behavior for unclear issues. Answer in the issue, then re-trigger |
| A check is red | Show the CI failure analysis comment, and explain that the gate did its job |
| The production deploy is slow | Skip the wait. Show the approved run and the live API |
| Out of time | Skip step 8, and end at the approved production deploy |
