"""Build a locally signed ARM64 APK in an ignored copy; never upgrade web/ in place.

Example (Python 3.8+): build.py --unity <Unity.exe> --auth-url <HTTP URL>
    --game-url <HTTP URL> [--world main] [--development]
The installed editor needs Android Build Support, SDK, NDK and OpenJDK.
Both normal and development builds use the local debug key for device testing.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parents[2]
AUDIT = (ROOT / 'audit').resolve()


def local_output(value):
    path = Path(value).resolve()
    if AUDIT not in path.parents:
        raise ValueError('Build copies and artifacts must be inside the repository audit directory.')
    return path


def mirror(source, destination):
    destination.mkdir(parents=True, exist_ok=True)
    live = {p.relative_to(source) for p in source.rglob('*') if p.is_file()}
    for path in destination.rglob('*'):
        if path.is_file() and path.relative_to(destination) not in live:
            resolved = path.resolve()
            if destination.resolve() not in resolved.parents:
                raise ValueError('Copy contains a file outside its directory.')
            path.unlink()
    shutil.copytree(source, destination, dirs_exist_ok=True, copy_function=shutil.copy2)


def digest_source():
    digest = hashlib.sha256()
    for folder in ('Assets', 'Packages', 'ProjectSettings'):
        for path in sorted((ROOT / 'web' / folder).rglob('*')):
            if path.is_file():
                digest.update(path.relative_to(ROOT).as_posix().encode())
                with path.open('rb') as stream:
                    for data in iter(lambda: stream.read(1024 * 1024), b''):
                        digest.update(data)
    return digest.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity', required=True, type=Path)
    parser.add_argument('--auth-url', required=True)
    parser.add_argument('--game-url', required=True)
    parser.add_argument('--world', default='main')
    parser.add_argument('--development', action='store_true')
    parser.add_argument('--project-copy', default=str(AUDIT / 'android/project'))
    parser.add_argument('--output', default=str(AUDIT / 'android/DWSG.apk'))
    args = parser.parse_args()
    unity = args.unity.resolve(strict=True)
    project = local_output(args.project_copy)
    output = local_output(args.output)
    if project == output or project in output.parents or output.suffix.lower() != '.apk':
        parser.error('APK must be outside the project copy and use the .apk extension.')
    for value in (args.auth_url, args.game_url):
        uri = urlsplit(value)
        if uri.scheme not in ('http', 'https') or not uri.hostname or uri.username or uri.password:
            parser.error('Provide HTTP(S) endpoints without embedded credentials.')
    if not args.world.strip():
        parser.error('World ID cannot be blank.')
    if (project / 'Temp/UnityLockfile').exists():
        parser.error('The disposable project is already open; close its build process first.')
    version = unity.parent.parent.name
    original = (ROOT / 'web/ProjectSettings/ProjectVersion.txt').read_text(encoding='utf-8')
    if not (version.startswith('6000.3.') or ('m_EditorVersion: ' + version) in original):
        parser.error('Use the original project editor or the validated Unity 6000.3 series.')
    before = digest_source()
    for folder in ('Assets', 'ProjectSettings'):
        mirror(ROOT / 'web' / folder, project / folder)
    if version.startswith('6000.3.'):
        # Compatibility changes belong only to the disposable build copy.
        (project / 'Packages').mkdir(exist_ok=True)
        shutil.copy2(Path(__file__).with_name('manifest-unity6.json'), project / 'Packages/manifest.json')
        (project / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: ' + version + '\n', encoding='utf-8')
    else:
        mirror(ROOT / 'web/Packages', project / 'Packages')
    output.parent.mkdir(parents=True, exist_ok=True)
    log = output.with_suffix('.build.log')
    if log.exists():
        log.unlink()
    env = dict(os.environ, DWSG_ANDROID_OUTPUT=str(output), DWSG_AUTH_URL=args.auth_url,
               DWSG_GAME_URL=args.game_url, DWSG_WORLD_ID=args.world,
               DWSG_ANDROID_DEVELOPMENT='1' if args.development else '0')
    command = [str(unity), '-batchmode', '-nographics', '-quit', '-projectPath', str(project),
               '-buildTarget', 'Android', '-executeMethod', 'AndroidClientBuild.Build', '-logFile', str(log)]
    print('Building Android ARM64; log: ' + str(log), flush=True)
    started = time.time_ns()
    code = subprocess.run(command, env=env, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0)).returncode
    unchanged = before == digest_source()
    result = dict(exit_code=code, source_unchanged=unchanged, source_sha256=before, editor=version,
                  development=args.development, output=str(output), log=str(log))
    confirmed = log.is_file() and ('DWSG_ANDROID_BUILD_OK ' + str(output)) in log.read_text(encoding='utf-8', errors='replace')
    fresh = output.is_file() and output.stat().st_mtime_ns >= started - 2_000_000_000
    result['build_confirmed'] = confirmed and fresh
    if code == 0 and confirmed and fresh:
        result['apk_sha256'] = hashlib.sha256(output.read_bytes()).hexdigest()
    output.with_suffix('.build.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result), flush=True)
    return code or (0 if unchanged and 'apk_sha256' in result else 2)


if __name__ == '__main__':
    sys.exit(main())
