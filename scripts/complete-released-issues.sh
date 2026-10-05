#!/usr/bin/env bash
# Called by the Phase 5 production-release workflow after a release is published.
# Given the list of PR numbers included in the release, moves their linked issues
# to "Completed" and closes them with a comment pointing at the release.
#
# Required env vars:
#   REPO, PROJECT_OWNER, PROJECT_NUMBER, GH_TOKEN (PROJECT_TOKEN), RELEASE_TAG
#   PR_NUMBERS - space-separated list of merged PR numbers included in the release

set -euo pipefail
: "${REPO:?}" "${PROJECT_OWNER:?}" "${PROJECT_NUMBER:?}" "${GH_TOKEN:?}" "${RELEASE_TAG:?}" "${PR_NUMBERS:?}"

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

declare -A seen_issues=()

for pr_number in $PR_NUMBERS; do
  pr_json=$(gh pr view "$pr_number" --repo "$REPO" --json body,headRefName)
  body=$(jq -r '.body' <<<"$pr_json")
  head_ref=$(jq -r '.headRefName' <<<"$pr_json")

  if issue_number=$(PR_BODY="$body" HEAD_REF="$head_ref" bash "$script_dir/parse-linked-issue.sh"); then
    seen_issues["$issue_number"]=1
  else
    echo "[complete-released-issues] PR #$pr_number has no linked issue; skipping."
  fi
done

if [[ ${#seen_issues[@]} -eq 0 ]]; then
  echo "[complete-released-issues] No linked issues found in this release."
  exit 0
fi

for issue_number in "${!seen_issues[@]}"; do
  PROJECT_OWNER="$PROJECT_OWNER" PROJECT_NUMBER="$PROJECT_NUMBER" REPO="$REPO" \
    ISSUE_NUMBER="$issue_number" TARGET_STATUS="Completed" FROM_STATUSES="QA,Development Done" \
    bash "$script_dir/set-issue-status.sh"

  current_state=$(gh issue view "$issue_number" --repo "$REPO" --json state --jq '.state')
  if [[ "$current_state" == "CLOSED" ]]; then
    echo "[complete-released-issues] issue #$issue_number already closed (no-op)"
  else
    gh issue close "$issue_number" --repo "$REPO" \
      --comment "Released to production in $RELEASE_TAG."
    echo "[complete-released-issues] issue #$issue_number closed (released in $RELEASE_TAG)"
  fi
done
