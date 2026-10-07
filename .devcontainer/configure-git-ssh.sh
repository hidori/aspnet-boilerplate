#!/bin/sh
set -eu

workspace=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
state="$HOME/.copilot/git-ssh"
identity="$state/identity.pub"

if [ "$#" -gt 1 ]; then
    printf '%s\n' 'Usage: configure-git-ssh.sh [public-key-file]' >&2
    exit 1
fi

if [ "$#" -eq 1 ]; then
    if ! awk 'NR == 1 && $1 ~ /^(ssh-|ecdsa-|sk-)/ && NF >= 2 { valid = 1 } END { exit !(valid && NR == 1) }' "$1"; then
        printf '%s\n' 'Only a single OpenSSH public key is accepted; never supply a private key.' >&2
        exit 1
    fi
    ssh-keygen -l -f "$1" >/dev/null
    mkdir -p "$state"
    chmod 700 "$state"
    cp -- "$1" "$identity"
    chmod 600 "$identity"
fi

if [ ! -f "$identity" ]; then
    printf '%s\n' 'No Git SSH public identity configured. SSH agent forwarding remains unchanged.' \
        'To select an agent key, run: sh .devcontainer/configure-git-ssh.sh /path/to/key.pub' >&2
    exit 0
fi

ssh-keygen -l -f "$identity" >/dev/null
git config --file "$state/config" core.sshCommand "ssh -o IdentitiesOnly=yes -i '$identity'"
git config --global --replace-all "includeIf.gitdir:$workspace/.path" "$state/config"
printf '%s\n' 'Git SSH identity selection configured for this container workspace using a public key and SSH agent.'
