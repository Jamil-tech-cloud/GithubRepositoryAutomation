#!/usr/bin/env bash
# Called by the Phase 5 "deploy to qa succeeded" workflow. Finds every issue whose
# PR was merged into dev and is still sitting in "Development Done", and promotes
# it to "QA". Idempotent: issues already moved past Development Done are left alone.
#
# Required env vars:
#   REPO, PROJECT_OWNER, PROJECT_NUMBER, GH_TOKEN (PROJECT_TOKEN)

set -euo pipefail
: "${REPO:?}" "${PROJECT_OWNER:?}" "${PROJECT_NUMBER:?}" "${GH_TOKEN:?}"

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "[promote-dev-issues-to-qa] Scanning PRs merged into dev on $REPO..."

mapfile -t issue_numbers < <(
  gh pr list --repo "$REPO" --base dev --state merged --limit 200 \
    --json body,headRefName --jq '.[] | [.body, .headRefName] | @tsv' |
  while IFS=$'\t' read -r body head_ref; do
    PR_BODY="$body" HEAD_REF="$head_ref" bash "$script_dir/parse-linked-issue.sh" || true
  done | sort -un
)

if [[ ${#issue_numbers[@]} -eq 0 ]]; then
  echo "[promote-dev-issues-to-qa] No linked issues found on merged PRs."
  exit 0
fi

for issue_number in "${issue_numbers[@]}"; do
  PROJECT_OWNER="$PROJECT_OWNER" PROJECT_NUMBER="$PROJECT_NUMBER" REPO="$REPO" \
    ISSUE_NUMBER="$issue_number" TARGET_STATUS="QA" FROM_STATUSES="Development Done" \
    bash "$script_dir/set-issue-status.sh"
done
