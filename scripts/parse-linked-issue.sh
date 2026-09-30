#!/usr/bin/env bash
# Extracts the linked issue number from a PR's body and/or branch name.
# Looks for "Closes/Fixes/Resolves #N" (case-insensitive) in PR_BODY first,
# then falls back to a "feature/N-..." HEAD_REF branch name.
#
# SECURITY: PR_BODY and HEAD_REF are untrusted, attacker-controlled text.
# They must only ever be read via env vars (never interpolated into a
# `run:` template string) and are only ever matched against, never executed.
#
# Usage: PR_BODY="..." HEAD_REF="..." ./scripts/parse-linked-issue.sh
# Prints the issue number to stdout and exits 0 if found, otherwise prints
# nothing and exits 1.

set -euo pipefail

issue_number=""

if [[ -n "${PR_BODY:-}" ]]; then
  match=$(printf '%s' "$PR_BODY" | grep -iPo '(close[sd]?|fix(e[sd])?|resolve[sd]?)\s*:?\s*#\K[0-9]+' | head -n1 || true)
  if [[ -n "$match" ]]; then
    issue_number="$match"
  fi
fi

if [[ -z "$issue_number" && -n "${HEAD_REF:-}" ]]; then
  match=$(printf '%s' "$HEAD_REF" | grep -Po '^feature/\K[0-9]+' || true)
  if [[ -n "$match" ]]; then
    issue_number="$match"
  fi
fi

if [[ -z "$issue_number" ]]; then
  exit 1
fi

printf '%s\n' "$issue_number"
