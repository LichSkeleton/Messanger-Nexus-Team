#!/bin/bash

set -euo pipefail

badge_file="$1"
badge_name="$2"
badge_worktree="$(mktemp -d)"

cleanup() {
  git worktree remove --force "$badge_worktree" >/dev/null 2>&1 || true
}
trap cleanup EXIT

if git fetch origin test-badges:test-badges; then
  git worktree add "$badge_worktree" test-badges
else
  git worktree add --detach "$badge_worktree" HEAD
  git -C "$badge_worktree" switch --orphan test-badges
  git -C "$badge_worktree" rm -rf . || true
fi

cp "$badge_file" "$badge_worktree/$badge_name"
git -C "$badge_worktree" add "$badge_name"

if git -C "$badge_worktree" diff --cached --quiet; then
  exit 0
fi

git -C "$badge_worktree" config user.name "github-actions[bot]"
git -C "$badge_worktree" config user.email "41898282+github-actions[bot]@users.noreply.github.com"
git -C "$badge_worktree" commit -m "update $badge_name"
git -C "$badge_worktree" push origin HEAD:test-badges
