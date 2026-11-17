# Phase 0 Environment Audit

Date: 17 November 2026
Status: Completed on the current machine. Not yet reviewed or approved by the project owner.
Scope: The machine available to this development session only. It is not the target laboratory Windows server unless confirmed.

## Important environment mismatch

The implementation guide specifies a **Linux** development environment and a **Windows** deployment target. The machine actually available here is **Windows Server 2025 Datacenter**. No Linux distribution or kernel is present in this session. This assumption is therefore unresolved and must be decided before Phase 1.

## Host facts

| Item | Observed value |
| --- | --- |
| Hostname | `runnervmm7ud1` |
| Operating system | Microsoft Windows Server 2025 Datacenter |
| OS version / build | 10.0.26100 / 26100 |
| Architecture | 64-bit |
| CPU | AMD EPYC 7763 64-Core Processor |
| Cores / logical processors | 2 / 4 |
| Total RAM | 15.99 GB |
| Free RAM at audit time | 11.05 GB |
| Last boot | 17 November 2026, 07:51:59 |
| Time zone | UTC |

## Storage

| Drive | Used | Free | Root / note |
| --- | ---: | ---: | --- |
| C: | 122.73 GB | 27.16 GB | Local system drive |
| D: | 3.01 GB | 146.98 GB | Local fixed storage; holds Git metadata |
| Z: | 82.31 GB | 110.54 GB | SSHFS mount to `\\sshfs.r\mrdp_24193@191.215.42.79\home\mrdp_24193` |

The `Z:` mapping was initially in an Unavailable state and had to be reconnected before use. It is a network mount, not local storage. Backup storage, retention, and off-device storage have not been defined.

## Tool versions

| Tool | Status |
| --- | --- |
| Python (`python`, `python3`) | 3.12.10 at `C:\hostedtoolcache\windows\Python\3.12.10\x64\` |
| Python launcher (`py`) | 3.15.2 |
| pip | 26.3, for Python 3.12 |
| Node.js | v22.23.4 |
| npm / npx | 10.9.9 |
| Git | 2.56.0.windows.4 |
| Docker | 29.7.2 |
| .NET SDK | 10.0.403 |
| PostgreSQL client (`psql`, `pg_ctl`) | Not found on PATH |
| VS Code CLI | Not found on PATH |
| Claude Code CLI | Not found on PATH |

No package versions or deployment tooling have been selected; the guide requires a small prototype first.

## Network

| Address | Prefix | Interface |
| --- | --- | --- |
| 192.168.32.1 | /20 | vEthernet (nat) |
| 10.1.0.39 | /20 | Ethernet 3 |

The target laboratory network, firewall rules, and LAN client requirements have not been inspected.

## Git repository workaround

`git init` fails directly on the `Z:` SSHFS mount with `Permission denied` and `Function not implemented` errors, although ordinary file creation works. The repository was initialized as follows:

1. A bare Git directory was created at `D:\EgyptLab-LIS-gitdir` with `main` as the initial branch.
2. `core.bare` was set to `false` and `core.worktree` to `Z:/lab`.
3. A plain text file `Z:\lab\.git` was created with the content `gitdir: D:/EgyptLab-LIS-gitdir`.

Verified: `git -C Z:\lab status --short --branch` reports `## No commits yet on main`. This workaround must be accepted or replaced before Phase 1.

## Unresolved items requiring owner decision

1. Whether development will remain on this Windows host or move to a Linux host as the guide assumes.
2. Whether the `Z:` SSHFS mount and the Git metadata workaround are acceptable for the project.
3. How the Windows target laboratory machine will be accessed, maintained, and tested.
4. Branch and server topology, including whether branches can operate during an inter-site outage.
5. Which workflows must remain available offline.
6. Whether one database or one server per site is intended, and how globally unique identifiers will be assigned.
7. Whether multiple independent laboratory businesses (tenants) must be supported in one installation.
8. Backup frequency, retention, encryption, RPO, RTO, and off-device storage.
9. Names and roles of the qualified laboratory professionals who will approve test catalogues and clinical configuration.
10. Installation of PostgreSQL for development, required by Phase 1.

## Phase 0 acceptance status

The environment is documented and the unresolved assumptions are explicitly recorded. The acceptance gate is **not yet approved**, because the owner decisions above are outstanding and the Linux/Windows mismatch is unresolved.
