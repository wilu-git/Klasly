# Developer docs — `dev` workflow branch + Milestone 2 merge

## Summary

Asked: set up a `master → dev` branch workflow and merge the current milestone (Milestone 2) into `dev`,
with commits authored under the student's own GitHub identity (no AI co-author).
Done: set the repo-local git author to `Venalouie Cruz <venalouiec@gmail.com>`, committed the Milestone 2
work that was sitting uncommitted in its worktree, created `dev` from `master`, merged Milestone 2 with
`--no-ff`, built the solution, and pushed `master` and `dev` to `origin`.

## Projects touched

No code changed in this task. The merge brings in the Milestone 2 work (DOMAIN, EF CORE, WEB API, MSSQL
migration) — see [Milestone 2 report](../2026-11-09-1545-milestone-2-entities-migration/DEVELOPER-DOCS.md).

## Git changes

| Item | Change | Why |
| --- | --- | --- |
| `.git/config` (repo-local) | `user.name` / `user.email` set | Commits show up on GitHub as the student's, not `User@DESKTOP-UAN0RK3`. Global config untouched. |
| `claude/klasly-milestone-2-9baad5` | commit `81fe685` | The Milestone 2 work (50 files) was uncommitted. |
| `dev` | new branch from `master` (`79762c2`) | Integration branch; milestones land here first. |
| `dev` | merge commit `0814e1b` (`--no-ff`) | Keeps one visible "Merge Milestone 2" commit per milestone. |
| `origin` | pushed `master` (was 1 ahead) and `dev` (tracking) | So the teammate can pull `dev`. |

Not merged (left as they are): `claude/database-connection-issue-a557aa` and
`claude/erd-functions-completeness-9d38e9` — older Milestone 2 attempts on an old `master`; the first one
also adds EF Core to `Klasly.Web`, which CLAUDE.md §3.4 forbids.

## Database changes

None in this task. The `InitialCreate` migration is now on `dev` but not applied to the `Klasly` database.

## How it was verified

- `dotnet build` in the Milestone 2 worktree: succeeded, 0 warnings, 0 errors.
- `dotnet list package`: every Microsoft package is 10.0.14.
- `dotnet build` on `dev` after the merge: succeeded, 0 warnings, 0 errors.
- `git log --format='%an <%ae>'` on the new commits: `Venalouie Cruz <venalouiec@gmail.com>`, no co-author trailer.

## Known limits / TODO

- Older commits stay authored as `User <User@DESKTOP-UAN0RK3>`; rewriting pushed history was out of scope.
- No GitHub branch protection set — do it in the repo's GitHub settings if wanted.
- Matt should set his own repo-local `user.name` / `user.email` too.
