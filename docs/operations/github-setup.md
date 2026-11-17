# GitHub Remote Setup

## Current status — blocked by GitHub

Authentication succeeded as `mond7444-sketch`, but the push was rejected:

```
remote: Your account is suspended. Please visit https://support.github.com for more information.
fatal: unable to access ... The requested URL returned error: 403
```

`gh repo create` reported `https://github.com/mond7444-sketch/EgyptLab-LIS`, but no commits were uploaded. This is an account restriction enforced by GitHub; it cannot be bypassed locally.

The complete history is safe in two places:

- Working repository: `Z:\lab` on branch `main`.
- Backup remote: `D:\EgyptLab-LIS-backup.git`.

## Recovery when an active GitHub account is available

1. Log out of the suspended account:

   ```powershell
   gh auth logout --hostname github.com
   ```

2. Log in with an active account:

   ```powershell
   gh auth login --web --hostname github.com
   ```

3. Create a private repository and push the existing history:

   ```powershell
   gh repo create EgyptLab-LIS --private --source=. --remote=origin --push
   ```

4. If a repository already exists under the active account:

   ```powershell
   git remote remove origin
   git remote add origin https://github.com/<owner>/EgyptLab-LIS.git
   git push -u origin main
   ```

5. Verify:

   ```powershell
   git remote -v
   git status --short --branch
   ```

Never store tokens or credentials in the repository.
