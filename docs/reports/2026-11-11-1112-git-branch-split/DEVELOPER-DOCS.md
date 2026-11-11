# Git branch split: dev re-sorted into .NET and Android milestone branches

## Summary
The team asked for per-platform milestone branches off `dev`, with the existing history sorted to match.
The 63 unpushed commits on `dev` (Milestones 5–13) were rebuilt as 11 short-lived branches
(`dotnet/m<N>-<slug>`, `android/m<N>-<slug>`), each merged into `dev` with `--no-ff`.
The mixed Milestone 10 commit was split by folder into a .NET half and an Android half.
The old `backendDev` and `claude/*` branches and worktrees were deleted, and the workflow
is written up for the team in `docs/GIT-WORKFLOW-MANUAL.md`.

## Projects touched
None of the code projects changed (no file under `src/`, `tests/` or `android/` differs). Only git history and docs.

## Files changed
| Path | Change | Why |
|------|--------|-----|
| `docs/GIT-WORKFLOW-MANUAL.md` | added | Team manual: branch naming, the `(.NET half)` / `(Android half)` labels, milestone steps, re-sort record, recovery. |
| `docs/reports/2026-11-11-1112-git-branch-split/*` | added | This report. |
| `docs/reports/INDEX.md` | modified | Index line for this report. |

## Key changes (git)
New first-parent history of `dev` after `f87ee09` (Milestone 4, = `origin/dev` before the push):

```
Merge dotnet/m5-web-admin: Milestone 5 Web MVC login and Admin pages (.NET)
Merge dotnet/m6-api-sessions: Milestone 6 sessions, QR token and scan endpoint (.NET)
Merge dotnet/m7-web-teacher-session: Milestone 7 teacher session page with rotating QR (.NET)
Merge android/m8-login-scan: Milestone 8 Android login and Scan QR (Android)
Merge dotnet/m9-worker-corrections: Milestone 9 Hangfire Worker, auto-Absent and corrections (.NET)
Merge dotnet/m10-grades-push: Milestone 10 assessments, grades and FCM sender (.NET half)
Merge android/m10-grades-push: Milestone 10 FCM grade push in the app (Android half)
Merge dotnet/m11-worker-risk: Milestone 11 Worker risk flags (.NET)
Merge dotnet/m12-alerts-brevo: Milestone 12 parent alerts and Brevo emails (.NET)
Merge dotnet/m13-dashboards: Milestone 13 overview endpoints and Parent portal (.NET half)
Merge android/m13-dashboards: Milestone 13 student and parent overview screens (Android half)
```

How it was done (in a temporary worktree, so the main working copy was not touched):

```bash
git branch backup/dev-before-split e70c556
git worktree add --detach ../Klasly-split f87ee09
# per group: git switch -c <branch> split-dev; git cherry-pick <first>^..<last>
#            git switch split-dev; git merge --no-ff <branch> -m "Merge <branch>: ..."
# M10: cherry-pick -n 8f31453, reset/clean android/ -> .NET half; cherry-pick -n again -> Android half
git reset --soft split-dev     # in the main checkout: dev moved, files untouched (trees identical)
```

Deleted (hash kept here for recovery with `git branch <name> <hash>`):

| Branch | Hash | Note |
|--------|------|------|
| `backendDev` | `2dfd15e` | "Start over" docs commit, not in dev |
| `claude/database-connection-issue-a557aa` | `9d8dc42` | PR #1, reverted on master; also deleted on GitHub |
| `claude/erd-functions-completeness-9d38e9` | `9b70479` | old EF Core attempt, not in dev |
| `claude/file-structure-milestone-check-b7bc1b` | `2dfd15e` | same commit as backendDev |
| `claude/klasly-milestone-2-9baad5` | `81fe685` | already in dev; its uncommitted M3 draft saved as `stash@{0}` |
| `claude/solution-folder-git-integration-5ae107` | `79762c2` | already in dev |

Worktrees under `D:/Claude/Klasly/` for those five `claude/*` branches were removed.

## Database changes
None.

## How it was verified
- `git diff e70c556 split-dev` → empty (final files identical to old `dev`).
- `git merge-base --is-ancestor f87ee09 dev` → true (no force-push).
- 64 non-merge commits = 63 original + 1 extra from the M10 split; author dates unchanged.
- Android half of M10: `git diff 8f31453 <android half>` → empty. .NET half differs from `8f31453` only under `android/`.
- At the M10 .NET-half merge (the only new in-between state): `dotnet build Klasly.slnx` → 0 warnings, 0 errors; `dotnet test` → Passed.
- Every other merge point equals an old commit's tree, so it builds as before. `assembleDebug` was not re-run (Android files at the tip are byte-identical to the last verified commit `e70c556`).

## Known limits / TODO
- `backup/dev-before-split` is local only; delete it once the team is happy (`git branch -D backup/dev-before-split`).
- Commit hashes after Milestone 4 changed. Anyone holding a local copy of the old unpushed commits should reset to `origin/dev`.
- The uncommitted report edits in the main working copy (Milestone 2 / docs-layout reports) were left as they were.
