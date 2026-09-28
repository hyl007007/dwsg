#!/bin/sh
# 项目专用 WSL 环境的离屏服务控制；源码部署和凭据初始化完成后使用。
set -eu
export PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
action=${1:-status}
base=/opt/dwsg
run="$base/state/run"
alive() { [ -s "$1" ] && kill -0 "$(cat "$1")" 2>/dev/null; }

case "$action" in
start)
    test -f "$base/admin/include/config.php" || { echo 'Preview has not been initialized.'; exit 1; }
    mkdir -p "$run" "$base/state/sessions" "$base/state/logs"
    chown mysql:mysql "$run"
    chown dwsg:dwsg "$base/state/sessions" "$base/state/logs"
    uid=$(id -u dwsg)
    iptables -C OUTPUT -m owner --uid-owner "$uid" ! -d 127.0.0.0/8 -j REJECT 2>/dev/null || iptables -A OUTPUT -m owner --uid-owner "$uid" ! -d 127.0.0.0/8 -j REJECT
    ip6tables -C OUTPUT -m owner --uid-owner "$uid" -j REJECT 2>/dev/null || ip6tables -A OUTPUT -m owner --uid-owner "$uid" -j REJECT
    if ! alive "$run/db.pid"; then
        nohup mariadbd --no-defaults --user=mysql --datadir="$base/state/db" --socket="$run/db.sock" --pid-file="$run/db.pid" --bind-address=127.0.0.1 --port=18306 --skip-name-resolve --log-error="$base/state/db.log" >/dev/null 2>&1 </dev/null &
    fi
    for n in $(seq 1 30); do mariadb-admin --socket="$run/db.sock" ping >/dev/null 2>&1 && break; sleep 1; done
    mariadb-admin --socket="$run/db.sock" ping >/dev/null 2>&1 || { echo 'Database did not become ready.'; exit 1; }
    if ! alive "$run/php.pid"; then
        su -s /bin/sh dwsg -c 'cd /opt/dwsg/admin; nohup php84 -d display_errors=0 -d log_errors=1 -d error_reporting=32767 -d session.save_path=/opt/dwsg/state/sessions -d session.use_strict_mode=1 -d session.cookie_httponly=1 -d open_basedir=/opt/dwsg:/tmp -d disable_functions=exec,shell_exec,system,passthru,proc_open,popen -S 127.0.0.1:18080 >/opt/dwsg/state/logs/php.log 2>&1 </dev/null & echo $! >/opt/dwsg/state/logs/php.pid'
        cp "$base/state/logs/php.pid" "$run/php.pid"
    fi
    for n in $(seq 1 10); do
        curl --fail --silent --output /dev/null --max-time 1 http://127.0.0.1:18080/admin/ && break
        sleep 1
    done
    curl --fail --silent --output /dev/null --max-time 1 http://127.0.0.1:18080/admin/ || { echo 'PHP did not become ready; check state/logs/php.log.'; exit 1; }
    echo 'Backend: http://127.0.0.1:18080/admin/'
    ;;
stop)
    if alive "$run/php.pid" && tr '\0' ' ' < "/proc/$(cat "$run/php.pid")/cmdline" | grep -q '127.0.0.1:18080'; then kill "$(cat "$run/php.pid")"; fi
    if alive "$run/db.pid" && tr '\0' ' ' < "/proc/$(cat "$run/db.pid")/cmdline" | grep -q '/opt/dwsg/state/db'; then
        mariadb-admin --socket="$run/db.sock" shutdown
    fi
    echo 'Preview services stopped.'
    ;;
status)
    if alive "$run/php.pid"; then echo 'PHP: running'; else echo 'PHP: stopped'; fi
    if alive "$run/db.pid"; then echo 'Database: running'; else echo 'Database: stopped'; fi
    echo 'Backend: http://127.0.0.1:18080/admin/'
    ;;
*) echo 'Usage: local-preview.sh start|stop|status'; exit 2 ;;
esac
