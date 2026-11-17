# ADR-0001 — Development Environment and Git Metadata Location

Status: Proposed — pending owner approval
Date: 17 November 2026
Owner: project owner to confirm

## Context

The implementation guide specifies a Linux development environment and a Windows local server deployment target. The machine actually available in this session is Windows Server 2025 Datacenter with Python 3.12.10, Node.js 22.23.4, Git 2.56, and Docker 29.7.2, but no PostgreSQL client and no Linux environment.

The requested working tree is `Z:\lab`, an SSHFS network mount. Git for Windows cannot initialize a repository directly on that mount; it fails with `Permission denied` and `Function not implemented` errors, while ordinary file creation succeeds.

## Options

1. Keep source files on `Z:\lab` and store Git metadata at `D:\EgyptLab-LIS-gitdir`, linked by a plain `.git` pointer file. Verified working for `git status`.
2. Move the entire working tree to local `D:` storage and treat `Z:` only as a remote copy.
3. Provision a Linux development host and keep `Z:` only for sharing artifacts.

## Selected approach

Temporarily use option 1 so that Phase 0 documentation can be versioned without changing the requested working-tree location. This is a tooling workaround, not a product architecture decision. It is recorded here for review and may be replaced before Phase 1.

## Reasons

- Preserves the owner's chosen location, `Z:\lab`.
- Uses local storage for the Git object database, where Git operations are reliable.
- Requires no new infrastructure or credentials at this stage.

## Risks

- The `.git` pointer on `Z:` could be deleted or overwritten by a tool that does not understand it.
- An SSHFS outage makes the working tree unavailable, as observed at the start of the audit.
- Git hooks or tools that assume a real `.git` directory may need adjustment.
- The underlying Linux-versus-Windows environment mismatch remains unresolved.

## Approval

Pending. The project owner must confirm the development environment and whether this Git workaround is acceptable before Phase 1 begins.
