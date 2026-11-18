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

## Git repository setup

`git init` fails directly on the `Z:` SSHFS mount with `Permission denied` and `Function not implemented` errors, although ordinary file creation works. Copying a locally initialized `.git` directory onto the mount does work. The final setup is:

1. Repository root: `Z:\lab`, including a real `.git` directory.
2. Initial branch: `main`.
3. The temporary `D:\EgyptLab-LIS-gitdir` metadata location and the `.git` pointer file were removed.

Verified: `git -C Z:\lab status --short --branch` works. A repository-local commit identity was configured as `EgyptLab LIS Developer <dev@egyptlab.local>` because no global identity exists.

## Owner decisions recorded during Phase 0

1. Development environment: a Linux host was originally planned. On 18 November 2026 the owner explicitly allowed application work on this Windows Server 2025 machine, so Linux is no longer a blocker.
2. Repository location: `Z:\lab`. The earlier choice to move the repository to `D:` was reversed by the owner.
3. Git commit identity: repository-local generic identity, recorded in the Git setup section above.
4. GitHub remote: private repository `azzam-mo/EgyptLab-LIS`, authenticated as `azzam-mo`; local backup at `D:\EgyptLab-LIS-backup.git`.

## Unresolved items requiring owner decision

1. Branch and server topology, including whether branches can operate during an inter-site outage. The earlier answer “z return” referred to the repository location, not topology.
2. Which workflows must remain available offline.
3. Whether one database or one server per site is intended, and how globally unique identifiers will be assigned.
4. Whether multiple independent laboratory businesses (tenants) must be supported in one installation; currently unknown.
5. Backup frequency, retention, encryption, RPO, RTO, and off-device storage.
6. Names and roles of the qualified laboratory professionals who will approve test catalogues and clinical configuration.
7. Installation of PostgreSQL for development, required by Phase 1.

## Phase 0 acceptance status

The environment is documented and the owner decisions are recorded. The Linux-host blocker was removed by explicit owner approval on 18 November 2026. The acceptance gate is **not yet approved**: the topology, offline, tenancy, backup, and clinical-approver questions above remain open. GitHub version history is connected.
