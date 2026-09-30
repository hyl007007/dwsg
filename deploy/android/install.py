"""Install an Android build without opening desktop windows (Python 3.8+).

install.py --adb <adb.exe> --serial <device-serial> --apk <DWSG.apk> [--launch]
The phone must already have authorized USB debugging and package installation.
"""
import argparse
import hashlib
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--adb', required=True, type=Path)
    parser.add_argument('--serial', required=True)
    parser.add_argument('--apk', required=True, type=Path)
    parser.add_argument('--launch', action='store_true')
    args = parser.parse_args()
    adb = args.adb.resolve(strict=True)
    apk = args.apk.resolve(strict=True)
    if apk.suffix.lower() != '.apk':
        parser.error('An APK file is required.')
    flags = getattr(subprocess, 'CREATE_NO_WINDOW', 0)

    def run(*command, timeout=90):
        result = subprocess.run([str(adb), '-s', args.serial, *command],
                                creationflags=flags, stdout=subprocess.PIPE,
                                stderr=subprocess.STDOUT, encoding='utf-8', errors='replace', timeout=timeout)
        if result.returncode:
            raise RuntimeError(result.stdout.strip())
        return result.stdout.strip()

    if run('get-state', timeout=10) != 'device':
        raise RuntimeError('The selected device is not authorized and online.')
    result = run('install', '-r', str(apk), timeout=180)
    if 'Success' not in result.splitlines():
        raise RuntimeError('Android did not confirm installation: ' + result)
    print('Installed on ' + args.serial + ': ' + str(apk))
    print('SHA256: ' + hashlib.sha256(apk.read_bytes()).hexdigest())
    if args.launch:
        launch = run('shell', 'am', 'start', '-W', '-n', 'dd.sg/com.unity3d.player.UnityPlayerActivity')
        # Some Android versions return exit code 0 even when Activity Manager
        # reports a missing activity or a launch timeout in its output.
        lines = [line.strip() for line in launch.splitlines()]
        if 'Status: ok' not in lines or any(line.startswith('Error') for line in lines):
            raise RuntimeError('Android did not confirm launch: ' + launch)
        print(launch)


if __name__ == '__main__':
    main()
