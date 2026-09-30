#!/bin/bash
# Online backups of both account and game databases. Run with sudo on the board.
set -euo pipefail
base=/userdata/dwsg
[[ $(id -u) == 0 && $(readlink -f "$base") == "$base" ]] || exit 1
umask 077
target=$(mktemp -d "$base/data/backups/$(date -u +%Y%m%dT%H%M%SZ)-XXXXXX")
chown dwsg:dwsg "$target"
systemd-run --quiet --wait --pipe --collect \
    -p User=dwsg -p Group=dwsg -p RootDirectory="$base/runtime/rootfs" -p MountAPIVFS=yes \
    -p BindReadOnlyPaths="$base/current:/opt/dwsg" \
    -p BindPaths="$base/data/world:/var/lib/dwsg" -p BindPaths="$target:/backup" \
    -p EnvironmentFile="$base/config/game.env" \
    /bin/sh -c 'exec /opt/dwsg/host/Dwsg.Host --backup /backup/world.sqlite'
# MariaDB administrative access is restricted to the root-owned Unix socket identity.
systemd-run --quiet --wait --pipe --collect \
    -p RootDirectory="$base/runtime/rootfs" -p MountAPIVFS=yes -p BindPaths="$base/run:/run/dwsg" \
    /bin/sh -c 'exec /usr/bin/mariadb-dump --socket=/run/dwsg/mysql.sock --user=root --single-transaction --routines --events --databases dwsg_auth' > "$target/accounts.sql"
[[ -s "$target/world.sqlite" && -s "$target/accounts.sql" ]]
sha256sum "$target/world.sqlite" "$target/accounts.sql" > "$target/SHA256SUMS"
chmod 600 "$target"/*
chown root:root "$target" "$target"/*
printf 'Backup completed: %s\n' "$target"
