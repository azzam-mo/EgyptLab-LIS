# GitHub Remote Setup

The local repository is at `Z:\lab` on branch `main` and currently has a local backup remote named `backup` at `D:\EgyptLab-LIS-backup.git`.

To connect GitHub:

1. Authenticate with `gh auth login` and complete the browser or device-code approval.
2. Create a new private repository and push the existing history:

   ```powershell
   gh repo create EgyptLab-LIS --private --source=. --remote=origin --push
   ```

3. To use an existing repository instead:

   ```powershell
   git remote add origin <repository-url>
   git push -u origin main
   ```

4. Verify with `git remote -v` and `git status --short --branch`.

Never store tokens or credentials in the repository.
