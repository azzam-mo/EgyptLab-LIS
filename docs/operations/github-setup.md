# GitHub Remote Setup

## Current status — connected

- GitHub account: `azzam-mo`
- Repository: `https://github.com/azzam-mo/EgyptLab-LIS` (private)
- Local branch: `main`, tracking `origin/main`
- Local backup remote: `D:\EgyptLab-LIS-backup.git`

The full history was pushed successfully. The earlier account `mond7444-sketch` was suspended by GitHub and its login was removed from this machine.

## Remotes

```text
backup  D:\EgyptLab-LIS-backup.git (fetch/push)
origin  https://github.com/azzam-mo/EgyptLab-LIS.git (fetch/push)
```

## Routine version saving

```powershell
git -C Z:\lab add -A
git -C Z:\lab commit -m "describe the change"
git -C Z:\lab push origin main
git -C Z:\lab push backup main
```

## Verification

```powershell
git -C Z:\lab remote -v
git -C Z:\lab status --short --branch
gh repo view azzam-mo/EgyptLab-LIS
```

Never store tokens or credentials in the repository.
