# ADR-0001 — Development Environment and Repository Location

Status: Accepted by owner, with conditions
Date: 17 November 2026
Owner: project owner

## Context

The implementation guide specifies a Linux development environment and a Windows local server deployment target. The machine used for Phase 0 is Windows Server 2025 Datacenter with Python 3.12.10, Node.js 22.23.4, Git 2.56, and Docker 29.7.2, but no PostgreSQL client and no Linux environment.

The requested repository location is `Z:\lab`, an SSHFS network mount. `git init` fails directly on that mount, but a `.git` directory copied from a locally initialized repository works there.

## Options

1. Keep the repository on `Z:\lab` with a real `.git` directory created by the copy workaround.
2. Move the repository to local `D:` storage.
3. Provision a Linux development host and keep `Z:` only for sharing artifacts.

## Selected approach

- Repository location: `Z:\lab`, including a real `.git` directory on the mount. Option 1 is accepted; the earlier choice to move to `D:` was reversed by the owner.
- Development environment: option 3 is accepted as the target state. A Linux host will be provisioned, and application work is paused until it is available. The current Windows machine was used only for Phase 0 documentation.
- Git identity: a repository-local generic identity is used because no global identity is configured.

## Reasons

- Preserves the owner's chosen repository location.
- A real `.git` directory on `Z:` was verified working with `git status`, so the local `D:` metadata and pointer file were removed.
- The guide requires Linux development; the owner confirmed that a Linux host should be provisioned rather than treating this Windows machine as the development environment.

## Risks

- An SSHFS outage makes the whole repository unavailable, as observed at the start of the audit.
- The copied `.git` directory must not be replaced with a pointer file or deleted by tools that do not understand the mount.
- The generic commit identity should be replaced when the real developer identity is available.
- No application work may begin until the Linux host is provisioned and the remaining Phase 0 questions are answered.

## Approval

Approved by the project owner on 17 November 2026 for repository location, Linux-host direction, and generic commit identity. Remaining Phase 0 questions are still open.
