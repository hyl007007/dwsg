#!/bin/sh
# Keep one previous copy of these dedicated, non-sensitive service error logs.
set -eu
for name in php-fpm.log php-worker.log; do
    file=/userdata/dwsg/run/$name
    [ -f "$file" ] && [ ! -L "$file" ] && [ ! -L "$file.1" ] || continue
    if [ "$(wc -c < "$file")" -gt 5242880 ]; then
        cp -p "$file" "$file.1"
        : > "$file"
    fi
done
