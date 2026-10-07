#!/bin/sh
set -eu

if [ "$(id -u)" -ne 0 ]; then
    exec "$@"
fi

workspace=/workspaces/aspnet-boilerplate
uid=$(stat -c '%u' "$workspace")
gid=$(stat -c '%g' "$workspace")

if [ "$uid" -eq 0 ]; then
    printf '%s\n' 'The workspace must be owned by a non-root host user.' >&2
    exit 1
fi

current_uid=$(id -u app)
current_gid=$(id -g app)
groupmod --non-unique --gid "$gid" app
if [ "$current_uid" -ne "$uid" ] || [ "$current_gid" -ne "$gid" ]; then
    usermod --non-unique --uid "$uid" --gid "$gid" app
fi

mkdir -p /home/app/.nuget/packages "$workspace/.artifacts"
chown "$uid:$gid" /home/app /home/app/.nuget
find /home/app/.nuget/packages "$workspace/.artifacts" -xdev \
    \( ! -uid "$uid" -o ! -gid "$gid" \) \
    -exec chown --no-dereference "$uid:$gid" {} +

if [ "${REPAIR_DEV_STATE_OWNERSHIP:-0}" = 1 ]; then
    mkdir -p /home/app/.vscode-server/data /home/app/.copilot
    chown "$uid:$gid" /home/app/.vscode-server
    find /home/app/.vscode-server/data /home/app/.copilot -xdev \
        \( ! -uid "$uid" -o ! -gid "$gid" \) \
        -exec chown --no-dereference "$uid:$gid" {} +
fi

if [ "${REPAIR_GIT_OWNERSHIP:-0}" = 1 ] && [ -d "$workspace/.git" ]; then
    find "$workspace/.git" -xdev \
        \( ! -uid "$uid" -o ! -gid "$gid" \) \
        -exec chown --no-dereference "$uid:$gid" {} +
fi

exec setpriv --reuid=app --regid=app --clear-groups "$@"
