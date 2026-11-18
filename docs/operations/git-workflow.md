# Git Workflow and Backup Policy

Status: Active for this repository
Date: 18 November 2026

## Owner decision (18 November 2026)

The owner decided that this repository does **not use pull requests**. All accepted work, including documentation and code, is committed directly to `main` and pushed immediately by the automatic backup hook. This supersedes the earlier requirement to open pull requests for review. The decision applies to every later task.

## Source of truth

GitHub is the source of truth. The private repository is:

`https://github.com/azzam-mo/EgyptLab-LIS.git`

No work should remain only on a local machine. Every accepted change must be pushed to GitHub.

## Required routine

1. **Before starting work:** `git fetch origin && git pull --rebase origin main`.
2. **Work on one bounded task at a time** directly on `main`. Do not create feature branches.
3. **After every commit:** the local `post-commit` hook pushes `main` to `origin` automatically. There is no pull request and no separate merge step.
4. **Verify the backup** using the checklist below.

## Automatic backup hook

The repository uses a tracked hook at `.githooks/post-commit`. Enable it once per clone:

```sh
git config core.hooksPath .githooks
```

The hook:

- pushes `main` to `origin` after every commit;
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
git log origin/main -1 --oneline
```

The local commit and `origin/main` must have the same hash.

## Known limitation

The hook requires a working network connection and valid credentials at commit time. If the machine is offline, the commit remains local and must be pushed manually as soon as connectivity returns. This is why the log file exists: a failed automatic push is visible and actionable.
