#!/bin/sh
# 游戏在专用 WSL 虚拟屏幕运行；浏览器仅连接本机的画面和输入转发。
set -eu
export PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
base=/opt/dwsg
run="$base/state/run"
logs="$base/state/game-logs"
action=${1:-status}
alive() { [ -s "$1" ] && kill -0 "$(cat "$1")" 2>/dev/null; }
active_game() {
    pgrep -u dwsg -f '^/opt/dwsg/client[^/]*/DWSG[.]exe([[:space:]]|$)' | head -n 1 || true
}
active_preview_url() {
    current=$(active_game)
    port=18082
    if [ -n "$current" ] && tr '\0' '\n' < "/proc/$current/environ" | grep -qx 'DISPLAY=:120'; then
        port=18086
    fi
    printf 'http://127.0.0.1:%s/vnc.html?autoconnect=1&resize=scale\n' "$port"
}
stop_service() {
    if alive "$1" && tr '\0' ' ' < "/proc/$(cat "$1")/cmdline" | grep -Fq -- "$2"; then kill "$(cat "$1")"; fi
}
display_pid=$(pgrep -u dwsg -f '^Xvfb :119 ' | head -n 1 || true)
if [ -n "$display_pid" ]; then printf '%s\n' "$display_pid" > "$run/xvfb.pid"; fi

case "$action" in
play)
    current=$(active_game)
    if [ -n "$current" ] && ! tr '\0' '\n' < "/proc/$current/cmdline" | head -n 1 | grep -qx '/opt/dwsg/client/DWSG.exe'; then
        echo "Game: already running (PID $current); keeping the current player."
        echo "Game: $(active_preview_url)"
        exit 0
    fi
    test -f "$base/client/DWSG.exe" || { echo 'Client build is missing.'; exit 1; }
    test -f "$base/state/vnc.pass" || { echo 'Preview password is missing.'; exit 1; }
    mkdir -p "$run" "$logs" "$base/state/wine" "$base/state/tmp"
    chown dwsg:dwsg "$logs" "$base/state/wine" "$base/state/tmp"
    uid=$(id -u dwsg)
    iptables -C OUTPUT -m owner --uid-owner "$uid" ! -d 127.0.0.0/8 -j REJECT 2>/dev/null || iptables -A OUTPUT -m owner --uid-owner "$uid" ! -d 127.0.0.0/8 -j REJECT
    ip6tables -C OUTPUT -m owner --uid-owner "$uid" -j REJECT 2>/dev/null || ip6tables -A OUTPUT -m owner --uid-owner "$uid" -j REJECT
    if ! pgrep -u dwsg -f '^Xvfb :119 ' >/dev/null; then
        su -s /bin/sh dwsg -c 'nohup setsid Xvfb :119 -screen 0 1600x900x24 -nolisten tcp -noreset >/opt/dwsg/state/game-logs/xvfb.log 2>&1 </dev/null & echo $! >/opt/dwsg/state/game-logs/xvfb.pid'
        sleep 1
    fi
    if [ -s "$logs/xvfb.pid" ]; then cp "$logs/xvfb.pid" "$run/xvfb.pid"; fi
    if ! alive "$run/vnc.pid"; then
        su -s /bin/sh dwsg -c 'nohup setsid env -u WAYLAND_DISPLAY DISPLAY=:119 XDG_SESSION_TYPE=x11 x11vnc -display :119 -rfbport 5905 -localhost -rfbauth /opt/dwsg/state/vnc.pass -forever -shared -noxdamage >/opt/dwsg/state/game-logs/vnc.log 2>&1 </dev/null & echo $! >/opt/dwsg/state/game-logs/vnc.pid'
        cp "$logs/vnc.pid" "$run/vnc.pid"
    fi
    if ! alive "$run/web-preview.pid"; then
        su -s /bin/sh dwsg -c 'nohup setsid websockify --web /usr/share/novnc 127.0.0.1:18082 127.0.0.1:5905 >/opt/dwsg/state/game-logs/web-preview.log 2>&1 </dev/null & echo $! >/opt/dwsg/state/game-logs/web-preview.pid'
        cp "$logs/web-preview.pid" "$run/web-preview.pid"
    fi
    if ! pgrep -u dwsg -f '/opt/dwsg/client/DWSG.exe' >/dev/null; then
        su -s /bin/sh dwsg -c ': >/opt/dwsg/state/game-logs/player-live.log'
        su -s /bin/sh dwsg -c 'nohup setsid env -u WAYLAND_DISPLAY DISPLAY=:119 XDG_SESSION_TYPE=x11 LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe WINEPREFIX=/opt/dwsg/state/wine WINEDEBUG=-all TMPDIR=/opt/dwsg/state/tmp wine /opt/dwsg/client/DWSG.exe -screen-fullscreen 0 -screen-width 1600 -screen-height 900 -logFile Z:\\opt\\dwsg\\state\\game-logs\\player-live.log >/opt/dwsg/state/game-logs/wine-live.log 2>&1 </dev/null & echo $! >/opt/dwsg/state/game-logs/game.pid'
        cp "$logs/game.pid" "$run/game.pid"
    fi
    for n in $(seq 1 15); do
        curl --fail --silent --output /dev/null --max-time 1 http://127.0.0.1:18082/vnc.html && break
        sleep 1
    done
    curl --fail --silent --output /dev/null --max-time 1 http://127.0.0.1:18082/vnc.html || { echo 'Browser preview did not become ready.'; exit 1; }
    for n in $(seq 1 30); do
        grep -Fq '初始化结束' "$logs/player-live.log" && break
        grep -q 'Failed to initialize graphics' "$logs/player-live.log" && break
        sleep 1
    done
    pgrep -u dwsg -f '/opt/dwsg/client/DWSG.exe' >/dev/null && grep -Fq '初始化结束' "$logs/player-live.log" || { echo 'Game did not initialize; check state/game-logs/player-live.log.'; exit 1; }
    python3 -c 'import socket; s=socket.create_connection(("127.0.0.1",5905),timeout=2); assert s.recv(12).startswith(b"RFB"); s.close()' || { echo 'Virtual screen connection is unavailable.'; exit 1; }
    echo 'Game: http://127.0.0.1:18082/vnc.html?autoconnect=1&resize=scale'
    ;;
stop-play)
    runuser -u dwsg -- env WINEPREFIX="$base/state/wine" wineserver -k || true
    runuser -u dwsg -- env WINEPREFIX="$base/state/wine" wineserver -w || true
    stop_service "$run/web-preview.pid" '127.0.0.1:18082'
    stop_service "$run/vnc.pid" '-rfbport 5905'
    stop_service "$run/xvfb.pid" 'Xvfb :119 '
    echo 'Isolated game stopped.'
    ;;
status)
    if [ -n "$(active_game)" ]; then echo 'Game: running'; else echo 'Game: stopped'; fi
    url=$(active_preview_url)
    if curl --fail --silent --output /dev/null --max-time 1 "$url"; then echo 'Browser preview: running'; else echo 'Browser preview: stopped'; fi
    echo "Game: $url"
    ;;
*) echo 'Usage: game-preview.sh play|stop-play|status'; exit 2 ;;
esac
