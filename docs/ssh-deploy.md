# 🚀 Marmotte.Devops — SSH deployment

The SSH deployment proof of concept in Marmotte.Devops uses the OpenSSH client (`ssh`, `scp`) that
already ships with Windows, Linux and macOS. Nothing to install on the targets
besides an SSH server, no extra NuGet package, and one script for every OS:
[`deploy-ssh.cs`](../deploy-ssh.cs).

This script belongs to the Deploy side of [Marmotte.Devops](../README.md#build-and-deploy).
It currently reuses `Marmotte.Build` for paths and process execution; it is independent
of the build and package workflow and does not use `Marmotte.Deploy.Windows`.

> 🧪 **Experimental.** This is a proof of concept: the script and the steps below may change.

**Legend:** 🪟 Windows · 🐧 Linux · 🍎 macOS · 💻 the machine that deploys (client) · 🖥️ the target (server)

## 🗺️ Overview

| Step | Where | How often |
|------|-------|-----------|
| 1️⃣ [Install the SSH server](#1️⃣-install-the-ssh-server) | 🖥️ target | once per target |
| 2️⃣ [Create a dedicated key](#2️⃣-create-a-dedicated-key) | 💻 client | once |
| 3️⃣ [Authorize the key](#3️⃣-authorize-the-key-on-the-target) | 🖥️ target | once per target |
| 4️⃣ [Load the key into ssh-agent](#4️⃣-load-the-key-into-ssh-agent-required) | 💻 client | ❗ required with a passphrase |
| 5️⃣ [Declare the target](#5️⃣-declare-the-target-optional) | 💻 client | optional |
| 6️⃣ [Accept the host key](#6️⃣-accept-the-host-key) | 💻 client | once per target |
| 7️⃣ [Run the smoke test](#7️⃣-run-the-smoke-test) | 💻 client | any time |

## ✅ Client prerequisites

- 🟣 The **.NET 11 SDK**.
- 🔌 The **OpenSSH client**. Check with `ssh -V`.
  - 🪟 Built in since Windows 10 1809 and Windows Server 2019. If it is missing, in an
    elevated console: `Add-WindowsCapability -Online -Name OpenSSH.Client~~~~0.0.1.0`
  - 🐧 Package `openssh-client` (Debian/Ubuntu) or `openssh-clients` (RHEL/Fedora),
    usually already installed.
  - 🍎 Built in.

## 1️⃣ Install the SSH server

### 🪟 Windows target

In an **elevated** Windows PowerShell console on the target:

```powershell
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
Start-Service sshd
Set-Service sshd -StartupType Automatic
Get-NetFirewallRule -Name OpenSSH-Server-In-TCP | Select-Object Name, Enabled
```

Installing the capability creates the `OpenSSH-Server-In-TCP` firewall rule (port 22).

### 🐧 Linux target

```bash
# Debian / Ubuntu
sudo apt install openssh-server
sudo systemctl enable --now ssh

# RHEL / Fedora
sudo dnf install openssh-server
sudo systemctl enable --now sshd

# Only if the ufw firewall is enabled
sudo ufw allow OpenSSH
```

### 🍎 macOS target

Turn on **System Settings › General › Sharing › Remote Login**, or:

```bash
sudo systemsetup -setremotelogin on
```

> ⚠️ Never expose SSH to the internet. On managed machines, follow your
> administrator's policy.

## 2️⃣ Create a dedicated key

🔑 Give the key a name that says what it is for, instead of the default
`id_ed25519`, and a comment. The comment is copied into the target's authorized
keys file, so later you can still tell which line grants what.

🪟 PowerShell:

```powershell
ssh-keygen -t ed25519 -f "$HOME\.ssh\marmotte-deploy" -C "marmotte-deploy@$env:COMPUTERNAME"
```

🐧 🍎 shell:

```bash
ssh-keygen -t ed25519 -f ~/.ssh/marmotte-deploy -C "marmotte-deploy@$(hostname)"
```

This creates two files:

| File | What it is | Share it? |
|------|------------|-----------|
| `~/.ssh/marmotte-deploy` | 🔒 private key | ❌ never |
| `~/.ssh/marmotte-deploy.pub` | 📤 public key | ✅ goes on the targets |

💡 A passphrase is recommended. If you set one, [step 4](#4️⃣-load-the-key-into-ssh-agent-required) is required.

## 3️⃣ Authorize the key on the target

Add the content of `marmotte-deploy.pub` (a single line starting with
`ssh-ed25519`) to the target account.

### 🪟 Windows target

Where the key goes depends on whether the target account is an administrator:

| Target account | File read by sshd |
|----------------|-------------------|
| 👤 standard user | `C:\Users\<user>\.ssh\authorized_keys` |
| 🛡️ member of Administrators | `C:\ProgramData\ssh\administrators_authorized_keys` (the user's own file is **ignored**) |

👤 **Standard user**, in a console on the target (paste your public key line):

```powershell
$publicKey = "ssh-ed25519 AAAA... marmotte-deploy@MY-PC"
$dir = "C:\Users\deploy-user\.ssh"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
Add-Content -Path "$dir\authorized_keys" -Value $publicKey
```

🛡️ **Administrator account**, in an **elevated** console on the target. The file
must be writable only by Administrators and SYSTEM, otherwise sshd silently
ignores it:

```powershell
$publicKey = "ssh-ed25519 AAAA... marmotte-deploy@MY-PC"
$file = "$env:ProgramData\ssh\administrators_authorized_keys"
Add-Content -Path $file -Value $publicKey
icacls.exe $file /inheritance:r /grant "*S-1-5-32-544:F" /grant "*S-1-5-18:F"
```

The SIDs stand for Administrators and SYSTEM, so the command works on any
Windows display language.

> 💡 A deployment account rarely needs to be an administrator: prefer a standard
> account with only the rights the deployment needs.

### 🐧 Linux / 🍎 macOS target

From a 🐧 🍎 client, with a password login allowed once:

```bash
ssh-copy-id -i ~/.ssh/marmotte-deploy.pub deploy-user@my-server
```

From a 🪟 client (there is no `ssh-copy-id`):

```powershell
Get-Content "$HOME\.ssh\marmotte-deploy.pub" | ssh deploy-user@my-server 'umask 077; mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys'
```

## 4️⃣ Load the key into ssh-agent (required)

> ❗ **Required when the key has a passphrase.**
>
> `deploy-ssh.cs` runs ssh with `BatchMode=yes`: ssh is never allowed to ask
> anything, so an unattended deployment cannot hang on a prompt. The passphrase
> must therefore come from `ssh-agent`. Without it, the target answers
> `Permission denied (publickey,...)` **even though the key is authorized**,
> while a manual `ssh` still works because it can ask you for the passphrase.

### 🪟 Windows client

Once, in an **elevated** console:

```powershell
Set-Service ssh-agent -StartupType Automatic
Start-Service ssh-agent
```

Then as the deploying user (asks for the passphrase once):

```powershell
ssh-add "$HOME\.ssh\marmotte-deploy"
```

♻️ The Windows agent stores loaded keys encrypted for your user and keeps them
**across reboots**: this is a one-time step.

### 🐧 Linux client

Desktop sessions usually already run an agent (GNOME Keyring, KDE Wallet...).
Otherwise, start one in your shell:

```bash
eval "$(ssh-agent -s)"
ssh-add ~/.ssh/marmotte-deploy
```

♻️ This agent lives as long as your session: repeat after each login, or add
`AddKeysToAgent yes` to the host block in `~/.ssh/config` so the first manual
`ssh` loads the key for you.

### 🍎 macOS client

```bash
ssh-add --apple-use-keychain ~/.ssh/marmotte-deploy
```

♻️ To reload it automatically after a reboot, add these lines to the host block
in `~/.ssh/config`:

```text
    UseKeychain yes
    AddKeysToAgent yes
```

### 🔍 Check

```bash
ssh-add -l
```

✅ One line must end with `marmotte-deploy@...`. Other keys may be listed too.

> 🤖 **CI agents:** there is nobody to type a passphrase. Load the key into the
> agent from a CI secret, or use a key without passphrase stored as a secret
> file readable only by the agent's account, and pass it with `--identity`.

## 5️⃣ Declare the target (optional)

🏷️ Because the key does not have a default name, ssh does not try it on its own.
Either pass it every time with `--identity`, or declare the target once in your
ssh config file (🪟 `C:\Users\<you>\.ssh\config` · 🐧 🍎 `~/.ssh/config`):

```text
Host my-server
    HostName my-server.example.test
    User deploy-user
    IdentityFile ~/.ssh/marmotte-deploy
    IdentitiesOnly yes
```

Then `my-server` is enough everywhere: `ssh my-server`, `scp`, `deploy-ssh.cs`.

## 6️⃣ Accept the host key

🤝 `deploy-ssh.cs` refuses unknown targets instead of trusting them blindly.
Connect once manually and answer `yes`:

```bash
ssh my-server hostname
```

🔎 To check that the fingerprint shown is really the target's, run on the target:

- 🪟 `ssh-keygen -lf "$env:ProgramData\ssh\ssh_host_ed25519_key.pub"`
- 🐧 🍎 `ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub`

## 7️⃣ Run the smoke test

From the repository root:

```bash
dotnet run --file deploy-ssh.cs -- my-server
```

or without a config alias:

```bash
dotnet run --file deploy-ssh.cs -- deploy-user@my-server --identity ~/.ssh/marmotte-deploy
```

| Argument | Meaning |
|----------|---------|
| `<[user@]host>` | 🎯 the target, or an alias from `~/.ssh/config` |
| `--port <port>`, `-p` | 🔌 SSH port, when it is not 22 |
| `--identity <path>`, `-i` | 🔑 private key to use; ssh then offers only that key (`IdentitiesOnly=yes`) |

✅ Expected output (values depend on the machine):

```text
running ssh -o BatchMode=yes -o ConnectTimeout=15 my-server hostname
MY-SERVER
running scp -o BatchMode=yes -o ConnectTimeout=15 upload.txt my-server:marmotte-deploy-<unique-id>.txt
running scp -o BatchMode=yes -o ConnectTimeout=15 my-server:marmotte-deploy-<unique-id>.txt download.txt
Remote file: ~/marmotte-deploy-<unique-id>.txt
Remote write/read verification succeeded.
```

The script:

1. 🏷️ prints the target's host name with `ssh`;
2. 📤 uploads a uniquely named text file to the target user's home directory with `scp`;
3. 📥 downloads it again and checks that the content is identical;
4. 📌 leaves the file on the target for manual inspection, and exits nonzero on failure.

## 🩺 Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| `Permission denied (publickey,...)`, but a manual `ssh` works after asking for the **passphrase** | 🔐 key not loaded in the agent | [step 4](#4️⃣-load-the-key-into-ssh-agent-required) |
| `Permission denied (publickey,...)`, and a manual `ssh` asks for the account **password** | 🔑 key not authorized, or in the wrong file | [step 3](#3️⃣-authorize-the-key-on-the-target) (🪟 admin file and its permissions) |
| `Host key verification failed.` | 🤝 target not in `known_hosts` yet, or its key changed | [step 6](#6️⃣-accept-the-host-key) |
| `Connection refused` or a timeout | 🔌 SSH server stopped, or a firewall in the way | [step 1](#1️⃣-install-the-ssh-server) |
| `Error connecting to agent` from `ssh-add` | 🔐 no agent running | [step 4](#4️⃣-load-the-key-into-ssh-agent-required) |
| `Private key not found: ...` | 📁 wrong `--identity` path | check the path |

🔍 To see what ssh does, add `-v`: `ssh -v -o BatchMode=yes my-server hostname`.
`Server accepts key` followed by `Permission denied` means the key is authorized
but its passphrase could not be supplied: see [step 4](#4️⃣-load-the-key-into-ssh-agent-required).

## 🔒 Security and scope

- 🚫 **No prompts** (`BatchMode=yes`): a missing key, a password-only account or
  an unknown host key fails immediately instead of blocking an unattended run.
- 🤝 **Host keys are always verified** against `known_hosts`; the script never
  disables that check.
- 🪟 **No network credentials on Windows targets**: with key authentication,
  remote commands cannot reach file shares with the user's identity. Push files
  with `scp` instead.
- 🎲 **Unique file names**: remote names rely on a random GUID; the script does
  not check for an existing file first.
- 🧩 **Not covered yet**: service management, cancellation and rollback.
