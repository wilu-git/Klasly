# How to use the `dev` branch workflow

## Prerequisites

- Git, and access to https://github.com/wilu-git/Klasly.
- Your git identity set for this repo, so GitHub credits your commits:

```bash
git config user.name "Your Name"
git config user.email "your-github-email@example.com"
```

## Branches

- `master` — stable. Only gets `dev` merged in when a set of milestones is tested.
- `dev` — integration. Every milestone is merged here first.
- `feature/m<N>-<short-name>` — one branch per milestone, cut from `dev`.

## Step by step: working on the next milestone

1. Get the latest `dev`: `git switch dev` then `git pull`.
2. Make your milestone branch: `git switch -c feature/m3-auth`.
3. Code, `dotnet build`, commit as you go.
4. Push it: `git push -u origin feature/m3-auth`.
5. Merge into `dev` (on GitHub with a pull request into `dev`, or locally):
   `git switch dev`, `git merge --no-ff feature/m3-auth`, `git push`.
6. When `dev` is tested, merge `dev` into `master` the same way.

## Example

After this task, `git log --oneline --graph dev` shows:

```
*   0814e1b Merge Milestone 2 into dev
|\
| * 81fe685 feat: Milestone 2 — Core entities, EF Core config, seed, InitialCreate migration
|/
* 79762c2 docs: match docs to the Core/Infrastructure layout; restore guide and schema
```

## Troubleshooting

- **My commits on GitHub show no avatar / wrong name** — the commit email isn't on your GitHub account.
  Check `git config user.email` and add that email under GitHub → Settings → Emails.
- **`git pull` says "no tracking information"** — run `git branch --set-upstream-to=origin/dev dev`.
- **Merge conflict in `Migrations/KlaslyDbContextModelSnapshot.cs`** — two branches added migrations.
  Keep `dev`'s version, delete your migration, then re-run `dotnet ef migrations add` on top of `dev`.
