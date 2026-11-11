# How to use the new branch workflow

## Prerequisites
- Git installed, access to `https://github.com/wilu-git/Klasly`.
- Nothing needs to be running (no SQL Server, API or emulator) for this.

## Step by step
1. Get the new history:
   ```bash
   git fetch origin
   git switch dev
   git reset --hard origin/dev
   ```
   Only use `reset --hard` if you have no uncommitted work. Otherwise commit or stash it first.
2. Look at it: `git log --oneline --first-parent dev`. You see one merge per milestone and platform.
3. Start the next milestone from `dev`:
   ```bash
   git switch -c dotnet/m14-role-access-tests
   ```
   Use `android/m14-...` for Android work. If the milestone needs both, use the same slug for both branches.
4. Push the branch, open a PR into `dev`, and merge it with **"Create a merge commit"**.
   End the merge message with `(.NET)`, `(Android)`, `(.NET half)` or `(Android half)`.
5. Full rules: `docs/GIT-WORKFLOW-MANUAL.md`.

## Example
On GitHub → **Insights → Network** (or `git log --oneline --first-parent dev`), you see 11 merges. The branches themselves were deleted after merging, but each merge message keeps its name, for example:
- `dotnet/m10-grades-push`: the API, Web and test part of Milestone 10
- `android/m10-grades-push`: the Android FCM part of the same milestone

## Troubleshooting
| Problem | Fix |
|---------|-----|
| `git pull` on `dev` says "divergent branches" | You still have the old commits. Run `git reset --hard origin/dev` (after saving your own work). |
| Need something from before the re-sort | `git log backup/dev-before-split` (only on Venalouie's PC). |
| GitHub PR merged with "Squash" by mistake | That still works, but the branch line is lost from the graph. Use "Create a merge commit" next time. |
