# Git Workflow and Backup Policy

Status: Active for this repository
Date: 18 November 2026

## Source of truth

GitHub is the source of truth. The private repository is:

`https://github.com/azzam-mo/EgyptLab-LIS.git`

No work should remain only on a local machine. Every accepted change must be pushed to GitHub.

## Required routine

1. **Before starting work:** `git fetch origin && git pull --rebase origin main` (or the current feature branch).
2. **Work on one bounded task at a time** on a feature branch, for example `docs/stack-adr`.
3. **After every commit:** the local `post-commit` hook pushes the current branch to `origin` automatically.
4. **Open a pull request** into `main` for review. Do not merge without owner approval.
5. **After merge:** switch to `main`, pull, and delete the merged local and remote feature branch.

## Automatic backup hook

The repository uses a tracked hook at `.githooks/post-commit`. Enable it once per clone:

```sh
git config core.hooksPath .githooks
```

The hook:

- pushes the current branch to `origin` after every commit;
- never fails the commit if the network or authentication is unavailable;
- writes success and failure records to `.git/auto-push.log` inside the local repository.

If a push fails, the log contains the exact command to retry. The failure is never silent.

To use a different remote, set the environment variable before committing:

```sh
EGYPTLAB_BACKUP_REMOTE=backup git commit -m "..."
```

## Verification checklist

After a commit, confirm that the backup happened:

```sh
git status --short --branch
git log origin/<branch> -1 --oneline
```

The local commit and `origin/<branch>` must have the same hash.

## Known limitation

The hook requires a working network connection and valid credentials at commit time. If the machine is offline, the commit remains local and must be pushed manually as soon as connectivity returns. This is why the log file exists: a failed automatic push is visible and actionable.
