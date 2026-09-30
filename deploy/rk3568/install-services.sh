#!/bin/bash
# Run as root after preparing a verified ARM64 rootfs and a release in BASE/current.
# Private inputs: config/game.env, config/php-config.php. No credentials belong here.
set -euo pipefail
base=${1:-/userdata/dwsg}
address=${2:?Usage: install-services.sh /userdata/dwsg LAN_IPV4}
[[ $(id -u) == 0 ]] || { printf 'Run as root.\n' >&2; exit 1; }
[[ "$base" == /userdata/dwsg && $(readlink -f "$base") == "$base" ]] || { printf 'Unexpected deployment root.\n' >&2; exit 1; }
[[ "$address" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]] || exit 1
IFS=. read -r -a octets <<< "$address"
for octet in "${octets[@]}"; do ((10#$octet <= 255)) || exit 1; done
network="${octets[0]}.${octets[1]}.${octets[2]}.0/24"
if ! awk '$1 == "memory" && $4 == 1 { found=1 } END { exit !found }' /proc/cgroups; then
    printf 'Memory cgroup unavailable: MemoryMax is not enforced on this kernel.\n' >&2
fi
root=$base/runtime/rootfs
if [[ ! -s "$root/etc/hosts" ]]; then
    printf '127.0.0.1 localhost %s\n::1 localhost ip6-localhost\n' "$(hostname)" > "$root/etc/hosts"
fi
for file in "$root/usr/sbin/mariadbd" "$root/usr/sbin/php-fpm8.4" "$root/usr/sbin/nginx" "$base/current/host/Dwsg.Host" "$base/config/game.env" "$base/config/php-config.php"; do
    [[ -f "$file" ]] || { printf 'Missing prerequisite: %s\n' "$file" >&2; exit 1; }
done
chmod 755 "$base/current/host/Dwsg.Host"
if ! id dwsg >/dev/null 2>&1; then
    useradd --system --no-create-home --home-dir /nonexistent --shell /usr/sbin/nologin dwsg
fi
uid=$(id -u dwsg)
gid=$(id -g dwsg)
if ! grep -q '^dwsg:' "$root/etc/passwd"; then
    printf 'dwsg:x:%s:%s:DWSG services:/nonexistent:/usr/sbin/nologin\n' "$uid" "$gid" >> "$root/etc/passwd"
    printf 'dwsg:x:%s:\n' "$gid" >> "$root/etc/group"
fi
[[ $(awk -F: '$1=="dwsg" {print $3}' "$root/etc/passwd") == "$uid" ]] || exit 1
install -d -o dwsg -g dwsg -m 750 "$base/run" "$base/data/mysql" "$base/data/world" "$base/data/backups" "$base/qa"
install -d -m 755 "$root/opt/dwsg" "$root/etc/dwsg" "$root/run/dwsg" "$root/var/lib/dwsg" "$root/var/lib/mysql"
chown root:dwsg "$base/config"
chmod 750 "$base/config"
chown root:dwsg "$base/config/php-config.php"
chmod 640 "$base/config/php-config.php"
chmod 600 "$base/config/game.env"

cat > "$base/config/mariadb.cnf" <<'EOF'
[mysqld]
datadir=/var/lib/mysql
socket=/run/dwsg/mysql.sock
pid-file=/run/dwsg/mysql.pid
skip-networking
character-set-server=utf8mb4
collation-server=utf8mb4_unicode_ci
innodb-buffer-pool-size=128M
max-connections=32
EOF
cat > "$base/config/php-fpm.conf" <<'EOF'
[global]
daemonize=no
error_log=/run/dwsg/php-fpm.log
[dwsg]
listen=/run/dwsg/php.sock
listen.mode=0600
pm=ondemand
pm.max_children=4
pm.process_idle_timeout=20s
pm.max_requests=500
catch_workers_output=yes
clear_env=yes
php_admin_value[memory_limit]=64M
php_admin_value[mysqli.default_socket]=/run/dwsg/mysql.sock
php_admin_flag[display_errors]=off
php_admin_flag[log_errors]=on
php_admin_value[error_log]=/run/dwsg/php-worker.log
EOF
cat > "$base/config/nginx.conf" <<EOF
worker_processes 1;
pid /run/dwsg/nginx.pid;
error_log stderr warn;
events { worker_connections 128; }
http {
    access_log off;
    client_body_temp_path /tmp/dwsg-nginx;
    fastcgi_temp_path /tmp/dwsg-fastcgi;
    proxy_temp_path /tmp/dwsg-proxy;
    uwsgi_temp_path /tmp/dwsg-uwsgi;
    scgi_temp_path /tmp/dwsg-scgi;
    client_max_body_size 64k;
    server {
        listen 127.0.0.1:18080;
        listen $address:18080;
        server_name _;
        allow 127.0.0.1;
        allow $network;
        deny all;
        location = /api.php {
            include /etc/nginx/fastcgi_params;
            fastcgi_param SCRIPT_FILENAME /opt/dwsg/admin/api.php;
            fastcgi_pass unix:/run/dwsg/php.sock;
        }
        location / { return 404; }
    }
}
EOF

# Each unit gets the same read-only runtime, with only its required state mounted writable.
common() {
    cat <<EOF
[Service]
Type=simple
User=dwsg
Group=dwsg
RootDirectory=$root
MountAPIVFS=yes
BindReadOnlyPaths=$base/current:/opt/dwsg
BindReadOnlyPaths=$base/config:/etc/dwsg
BindPaths=$base/run:/run/dwsg
ReadWritePaths=/run/dwsg
PrivateTmp=yes
PrivateDevices=yes
ProtectSystem=strict
ProtectHome=yes
NoNewPrivileges=yes
UMask=0077
Restart=on-failure
RestartSec=3
TimeoutStopSec=30
StandardOutput=journal
StandardError=journal
EOF
}
for name in db php auth game log-rotation; do
    unit=/etc/systemd/system/dwsg-$name.service
    if [[ -e "$unit" ]] && ! grep -q '^# Managed by dwsg install-services.sh$' "$unit"; then
        printf 'Refusing to overwrite an unrelated unit: %s\n' "$unit" >&2; exit 1
    fi
done
if [[ -e /etc/systemd/system/dwsg-log-rotation.timer ]] && ! grep -q '^# Managed by dwsg install-services.sh$' /etc/systemd/system/dwsg-log-rotation.timer; then
    printf 'Refusing to overwrite an unrelated log timer.\n' >&2; exit 1
fi
{
    printf '# Managed by dwsg install-services.sh\n[Unit]\nDescription=DWSG private account database\nAfter=local-fs.target\nRequiresMountsFor=%s\n' "$base"
    common
    printf 'BindPaths=%s/data/mysql:/var/lib/mysql\nReadWritePaths=/var/lib/mysql\nExecStart=/usr/sbin/mariadbd --defaults-file=/etc/dwsg/mariadb.cnf\nMemoryMax=512M\n[Install]\nWantedBy=multi-user.target\n' "$base"
} > /etc/systemd/system/dwsg-db.service
{
    printf '# Managed by dwsg install-services.sh\n[Unit]\nDescription=DWSG PHP account API\nAfter=dwsg-db.service\nRequires=dwsg-db.service\n'
    common
    printf 'BindReadOnlyPaths=%s/config/php-config.php:/opt/dwsg/admin/include/config.php\nExecStart=/usr/sbin/php-fpm8.4 --nodaemonize --fpm-config /etc/dwsg/php-fpm.conf\nMemoryMax=384M\n[Install]\nWantedBy=multi-user.target\n' "$base"
} > /etc/systemd/system/dwsg-php.service
{
    printf '# Managed by dwsg install-services.sh\n[Unit]\nDescription=DWSG LAN account endpoint\nAfter=dwsg-php.service network-online.target\nWants=network-online.target\nRequires=dwsg-php.service\n'
    common
    printf 'ExecStart=/usr/sbin/nginx -c /etc/dwsg/nginx.conf -g "daemon off;"\nMemoryMax=64M\n[Install]\nWantedBy=multi-user.target\n'
} > /etc/systemd/system/dwsg-auth.service
{
    printf '# Managed by dwsg install-services.sh\n[Unit]\nDescription=DWSG authoritative game server\nAfter=dwsg-auth.service network-online.target\nRequires=dwsg-auth.service\n'
    common
    printf 'WorkingDirectory=/opt/dwsg/host\nBindPaths=%s/data/world:/var/lib/dwsg\nReadWritePaths=/var/lib/dwsg\nEnvironmentFile=%s/config/game.env\nExecStart=/opt/dwsg/host/Dwsg.Host\nMemoryMax=1536M\n[Install]\nWantedBy=multi-user.target\n' "$base" "$base"
} > /etc/systemd/system/dwsg-game.service
install -o root -g dwsg -m 750 "$(dirname "$0")/rotate-logs.sh" "$base/config/rotate-logs.sh"
cat > /etc/systemd/system/dwsg-log-rotation.service <<EOF
# Managed by dwsg install-services.sh
[Unit]
Description=Bound DWSG PHP service logs
RequiresMountsFor=$base
[Service]
Type=oneshot
User=dwsg
Group=dwsg
ExecStart=/bin/sh $base/config/rotate-logs.sh
ProtectSystem=strict
ReadWritePaths=$base/run
ProtectHome=yes
PrivateTmp=yes
NoNewPrivileges=yes
EOF
cat > /etc/systemd/system/dwsg-log-rotation.timer <<'EOF'
# Managed by dwsg install-services.sh
[Unit]
Description=Rotate DWSG service logs hourly when needed
[Timer]
OnCalendar=hourly
Persistent=true
[Install]
WantedBy=timers.target
EOF
systemctl daemon-reload
printf 'Units installed. Initialize the private database before enabling dwsg-game.service.\n'
