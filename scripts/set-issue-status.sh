#!/usr/bin/env bash
# Moves an issue to a target Status column on the Projects v2 board. Idempotent:
# safe to run twice, no-ops (with a log line) if the issue is already at the
# target status. Adds the issue to the project first if it isn't on it yet.
#
# Requires GH_TOKEN to be a token with `project` + `repo` scope (the PROJECT_TOKEN
# secret) — the default GITHUB_TOKEN cannot write to Projects v2.
#
# Required env vars:
#   PROJECT_OWNER   - login of the project's owner (user-owned project)
#   PROJECT_NUMBER  - project number, e.g. 1
#   REPO            - "owner/name" of the repository the issue lives in
#   ISSUE_NUMBER    - issue number to move
#   TARGET_STATUS   - one of: Backlog, In Progress, Development Done, QA, Completed
# Optional env vars:
#   FROM_STATUSES   - comma-separated list of statuses this move is allowed from.
#                      If the issue's current status isn't in this list (and isn't
#                      already TARGET_STATUS), the move is skipped with a log line.
#                      Omit to allow the move from any current status.

set -euo pipefail

: "${PROJECT_OWNER:?}" "${PROJECT_NUMBER:?}" "${REPO:?}" "${ISSUE_NUMBER:?}" "${TARGET_STATUS:?}"

repo_owner="${REPO%%/*}"
repo_name="${REPO##*/}"

log() { echo "[set-issue-status] $*"; }

project_json=$(gh api graphql -f query='
  query($login: String!, $number: Int!) {
    user(login: $login) {
      projectV2(number: $number) {
        id
        field(name: "Status") {
          ... on ProjectV2SingleSelectField { id options { id name } }
        }
      }
    }
  }' -f login="$PROJECT_OWNER" -F number="$PROJECT_NUMBER")

project_id=$(jq -r '.data.user.projectV2.id' <<<"$project_json")
status_field_id=$(jq -r '.data.user.projectV2.field.id' <<<"$project_json")
target_option_id=$(jq -r --arg name "$TARGET_STATUS" '.data.user.projectV2.field.options[] | select(.name == $name) | .id' <<<"$project_json")

if [[ -z "$project_id" || -z "$status_field_id" ]]; then
  echo "::error::Could not resolve project or Status field for $PROJECT_OWNER/$PROJECT_NUMBER" >&2
  exit 1
fi
if [[ -z "$target_option_id" ]]; then
  echo "::error::'$TARGET_STATUS' is not a valid Status option on the project" >&2
  exit 1
fi

issue_json=$(gh api graphql -f query='
  query($owner: String!, $repo: String!, $number: Int!) {
    repository(owner: $owner, name: $repo) {
      issue(number: $number) {
        id
        projectItems(first: 20) {
          nodes {
            id
            project { number }
            fieldValueByName(name: "Status") {
              ... on ProjectV2ItemFieldSingleSelectValue { name }
            }
          }
        }
      }
    }
  }' -f owner="$repo_owner" -f repo="$repo_name" -F number="$ISSUE_NUMBER")

issue_node_id=$(jq -r '.data.repository.issue.id' <<<"$issue_json")
if [[ -z "$issue_node_id" || "$issue_node_id" == "null" ]]; then
  echo "::error::Issue #$ISSUE_NUMBER not found in $REPO" >&2
  exit 1
fi

item_id=$(jq -r --argjson n "$PROJECT_NUMBER" '.data.repository.issue.projectItems.nodes[] | select(.project.number == $n) | .id' <<<"$issue_json")
current_status=$(jq -r --argjson n "$PROJECT_NUMBER" '.data.repository.issue.projectItems.nodes[] | select(.project.number == $n) | .fieldValueByName.name // empty' <<<"$issue_json")

if [[ "$current_status" == "$TARGET_STATUS" ]]; then
  log "issue #$ISSUE_NUMBER already '$TARGET_STATUS' (no-op)"
  exit 0
fi

if [[ -n "${FROM_STATUSES:-}" ]]; then
  allowed=false
  IFS=',' read -ra allowed_list <<<"$FROM_STATUSES"
  for s in "${allowed_list[@]}"; do
    if [[ "$s" == "$current_status" ]]; then
      allowed=true
      break
    fi
  done
  if [[ "$allowed" != "true" ]]; then
    log "issue #$ISSUE_NUMBER current status '${current_status:-<none>}' not in allowed source statuses ($FROM_STATUSES); skipping move to '$TARGET_STATUS'"
    exit 0
  fi
fi

if [[ -z "$item_id" ]]; then
  add_json=$(gh api graphql -f query='
    mutation($projectId: ID!, $contentId: ID!) {
      addProjectV2ItemById(input: { projectId: $projectId, contentId: $contentId }) {
        item { id }
      }
    }' -f projectId="$project_id" -f contentId="$issue_node_id")
  item_id=$(jq -r '.data.addProjectV2ItemById.item.id' <<<"$add_json")
fi

gh api graphql -f query='
  mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $optionId: String!) {
    updateProjectV2ItemFieldValue(input: {
      projectId: $projectId, itemId: $itemId, fieldId: $fieldId,
      value: { singleSelectOptionId: $optionId }
    }) { projectV2Item { id } }
  }' -f projectId="$project_id" -f itemId="$item_id" -f fieldId="$status_field_id" -f optionId="$target_option_id" >/dev/null

log "issue #$ISSUE_NUMBER: ${current_status:-<none>} -> $TARGET_STATUS"
