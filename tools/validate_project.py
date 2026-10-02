#!/usr/bin/env python3
"""
Static validation of the Unity project (no Unity Editor required, Python 3.8+, standard library only).

It catches the class of mistakes that otherwise only show up when somebody opens the project:

  META001  asset without a .meta file                 META002  .meta file without an asset
  META003  duplicate GUID                              META004  malformed .meta
  YAML001  malformed Unity YAML header                 YAML002  duplicate fileID inside one file
  REF001   reference to a GUID that does not exist     REF002   reference to a fileID that does not exist in the file
  SCRIPT001 MonoBehaviour points to a missing script / a script whose class does not match the file name
  BUILD001 EditorBuildSettings problems                ASMDEF001 broken assembly definition
  GIT001   junk / user specific files in the repo      CODE001  UnityEditor used by runtime code without a guard
  PLAYER001 Player prefab body part setup (unique body states, cascades contain the part itself)

Usage:   python3 tools/validate_project.py [--root PATH] [--strict]
Exit code 1 when an error was found (with --strict warnings count as errors).
"""
import argparse
import glob
import json
import os
import re
import subprocess
import sys

GUID_RE = re.compile(r'guid: ([0-9a-f]{32})')
REF_RE = re.compile(r'\{fileID: (-?\d+)(?:,\s*guid: ([0-9a-f]{32}))?')
DOC_RE = re.compile(r'^--- !u!(\d+) &(-?\d+)', re.M)
BUILTIN_GUID_RE = re.compile(r'^0{16}[def]0{15}$')

TEXT_ASSET_EXT = ('.unity', '.prefab', '.asset', '.mat', '.controller', '.anim', '.guiskin', '.overrideController',
                  '.mask', '.physicMaterial', '.flare', '.renderTexture', '.lighting', '.preset')

JUNK_NAMES = ('.DS_Store', 'Thumbs.db', 'Desktop.ini')
JUNK_PREFIXES = ('UserSettings/', 'Library/', 'Temp/', 'Logs/', 'obj/')


class Report:
    def __init__(self):
        self.errors = []
        self.warnings = []

    def error(self, code, path, message):
        self.errors.append((code, path, message))

    def warn(self, code, path, message):
        self.warnings.append((code, path, message))


def read(path):
    with open(path, 'r', encoding='utf-8-sig', errors='replace', newline='') as f:
        return f.read()


def list_assets(root):
    """Every file and folder below Assets/ that Unity imports (hidden and ~ entries are ignored by Unity)."""
    entries = []
    base = os.path.join(root, 'Assets')
    for dirpath, dirnames, filenames in os.walk(base):
        dirnames[:] = [d for d in dirnames if not d.startswith('.') and not d.endswith('~')]
        rel_dir = os.path.relpath(dirpath, root).replace(os.sep, '/')
        for d in dirnames:
            entries.append(rel_dir + '/' + d)
        for f in filenames:
            if f.startswith('.') or f.endswith('~') or f.endswith('.meta'):
                continue
            entries.append(rel_dir + '/' + f)
    return entries


def load_known_guids(path):
    known = set()
    if os.path.exists(path):
        for line in read(path).splitlines():
            line = line.split('#', 1)[0].strip()
            if re.fullmatch(r'[0-9a-f]{32}', line):
                known.add(line)
    return known


def check_meta(root, report):
    guid_to_path = {}
    entries = list_assets(root)
    entry_set = set(entries)

    for rel in entries:
        if not os.path.exists(os.path.join(root, rel + '.meta')):
            report.error('META001', rel, 'asset has no .meta file (Unity would generate a new GUID on every machine)')

    for dirpath, dirnames, filenames in os.walk(os.path.join(root, 'Assets')):
        dirnames[:] = [d for d in dirnames if not d.startswith('.') and not d.endswith('~')]
        for f in filenames:
            if not f.endswith('.meta'):
                continue
            meta_rel = os.path.relpath(os.path.join(dirpath, f), root).replace(os.sep, '/')
            asset_rel = meta_rel[:-5]
            if asset_rel not in entry_set:
                report.error('META002', meta_rel, 'orphaned .meta (its asset does not exist); Unity deletes it with a warning')
                continue
            text = read(os.path.join(root, meta_rel))
            m = re.search(r'^guid: ([0-9a-f]{32})\s*$', text, re.M)
            if not m:
                report.error('META004', meta_rel, 'no valid "guid:" line')
                continue
            guid = m.group(1)
            if guid in guid_to_path:
                report.error('META003', meta_rel, 'duplicate GUID %s (also used by %s)' % (guid, guid_to_path[guid]))
            guid_to_path[guid] = asset_rel
    return guid_to_path


def parse_docs(text):
    """Returns {fileID: (classID, start_offset)}; duplicates are returned separately."""
    docs = {}
    dups = []
    for m in DOC_RE.finditer(text):
        fid = m.group(2)
        if fid in docs:
            dups.append(fid)
        docs[fid] = m.group(1)
    return docs, dups


def check_yaml_assets(root, guid_to_path, external, report):
    paths = []
    for rel in guid_to_path.values():
        if rel.endswith(TEXT_ASSET_EXT):
            paths.append(rel)
    for p in glob.glob(os.path.join(root, 'ProjectSettings', '*.asset')):
        paths.append(os.path.relpath(p, root).replace(os.sep, '/'))

    for rel in sorted(set(paths)):
        full = os.path.join(root, rel)
        if not os.path.isfile(full):
            continue
        text = read(full)
        if not text.startswith('%YAML 1.1'):
            # binary / non YAML serialisation (e.g. LightingData.asset): nothing to validate
            if '--- !u!' in text[:200]:
                report.error('YAML001', rel, 'does not start with the "%YAML 1.1" header')
            continue

        docs, dups = parse_docs(text)
        for fid in dups:
            report.error('YAML002', rel, 'fileID %s is defined twice' % fid)

        for m in REF_RE.finditer(text):
            fid, guid = m.group(1), m.group(2)
            if guid:
                if BUILTIN_GUID_RE.match(guid) or guid in guid_to_path or guid in external:
                    continue
                report.error('REF001', rel, 'reference to unknown GUID %s (asset deleted? add package GUIDs to tools/known_external_guids.txt)' % guid)
            elif fid != '0' and fid not in docs:
                report.error('REF002', rel, 'reference to fileID %s which does not exist in this file' % fid)

        # MonoBehaviour -> script sanity
        for doc in re.split(r'(?m)^(?=--- !u!)', text):
            if not doc.startswith('--- !u!114'):
                continue
            m = re.search(r'm_Script: \{fileID: (-?\d+),\s*guid: ([0-9a-f]{32})', doc)
            if not m:
                continue
            guid = m.group(2)
            if guid not in guid_to_path:
                continue  # package script or already reported as REF001
            script = guid_to_path[guid]
            if script.endswith('.dll'):
                continue  # compiled plugin (e.g. DOTween.dll)
            if not script.endswith('.cs'):
                report.error('SCRIPT001', rel, 'MonoBehaviour points to %s which is not a script' % script)
                continue
            source = read(os.path.join(root, script))
            stem = os.path.splitext(os.path.basename(script))[0]
            if not re.search(r'\b(class|struct)\s+' + re.escape(stem) + r'\b', source):
                report.error('SCRIPT001', rel, 'script %s does not declare a class named %s (Unity needs file name == class name)' % (script, stem))


def check_build_settings(root, guid_to_path, report):
    path = os.path.join(root, 'ProjectSettings', 'EditorBuildSettings.asset')
    if not os.path.exists(path):
        report.error('BUILD001', 'ProjectSettings/EditorBuildSettings.asset', 'missing')
        return
    text = read(path)
    scenes = re.findall(r'- enabled: (\d)\s+path: (.+)\s+guid: ([0-9a-f]{32})', text)
    if not any(enabled == '1' for enabled, _, _ in scenes):
        report.error('BUILD001', 'ProjectSettings/EditorBuildSettings.asset', 'no enabled scene in the build')
    for enabled, scene_path, guid in scenes:
        scene_path = scene_path.strip()
        if not os.path.exists(os.path.join(root, scene_path)):
            report.error('BUILD001', 'ProjectSettings/EditorBuildSettings.asset', 'scene %s does not exist' % scene_path)
        elif guid_to_path.get(guid) != scene_path:
            report.error('BUILD001', 'ProjectSettings/EditorBuildSettings.asset', 'GUID of %s does not match its .meta' % scene_path)


def check_git_junk(root, report):
    try:
        out = subprocess.run(['git', 'ls-files'], cwd=root, capture_output=True, text=True, check=True).stdout
    except Exception:
        return
    for rel in out.splitlines():
        base = os.path.basename(rel)
        if base in JUNK_NAMES or rel.startswith(JUNK_PREFIXES) or base.endswith(('.orig', '.swp')):
            if os.path.exists(os.path.join(root, rel)):
                report.error('GIT001', rel, 'junk / user specific file is tracked by git')


def check_asmdefs(root, external, report):
    names = {}
    guid_to_name = {}
    defs = []
    for p in glob.glob(os.path.join(root, 'Assets', '**', '*.asmdef'), recursive=True):
        rel = os.path.relpath(p, root).replace(os.sep, '/')
        try:
            data = json.loads(read(p))
        except ValueError as e:
            report.error('ASMDEF001', rel, 'invalid JSON: %s' % e)
            continue
        name = data.get('name')
        if not name:
            report.error('ASMDEF001', rel, 'missing "name"')
            continue
        if name in names:
            report.error('ASMDEF001', rel, 'assembly name %s is also used by %s' % (name, names[name]))
        names[name] = rel
        meta = p + '.meta'
        if os.path.exists(meta):
            m = re.search(r'^guid: ([0-9a-f]{32})', read(meta), re.M)
            if m:
                guid_to_name[m.group(1)] = name
        defs.append((rel, data))

    package_prefixes = ('UnityEngine.', 'UnityEditor.', 'Unity.', 'Cinemachine', 'DOTween', 'Tayx.')
    for rel, data in defs:
        for ref in data.get('references', []):
            if ref.startswith('GUID:'):
                if ref[5:] not in guid_to_name and ref[5:] not in external:
                    report.warn('ASMDEF001', rel, 'reference %s does not resolve to a project assembly (package assemblies go into tools/known_external_guids.txt)' % ref)
            elif ref not in names and not ref.startswith(package_prefixes):
                report.warn('ASMDEF001', rel, 'reference "%s" is neither a project assembly nor a known package assembly' % ref)


def check_runtime_code(root, report):
    """Runtime scripts must not touch UnityEditor unless guarded: it breaks player builds."""
    for p in glob.glob(os.path.join(root, 'Assets', '**', '*.cs'), recursive=True):
        rel = os.path.relpath(p, root).replace(os.sep, '/')
        if '/Editor/' in rel or '/Tests/' in rel or rel.startswith('Assets/Plugins/'):
            continue
        text = read(p)
        if re.search(r'^\s*using\s+UnityEditor\b', text, re.M) and '#if UNITY_EDITOR' not in text:
            report.error('CODE001', rel, 'uses UnityEditor outside an Editor folder without #if UNITY_EDITOR')


def check_player_prefab(root, report):
    rel = 'Assets/_Main/Prefabs/Characters/Player.prefab'
    path = os.path.join(root, rel)
    if not os.path.exists(path):
        return
    text = read(path)
    docs = {}
    for doc in re.split(r'(?m)^(?=--- !u!)', text):
        m = re.match(r'--- !u!(\d+) &(-?\d+)', doc)
        if m:
            docs[m.group(2)] = doc

    parts = {}  # fileID -> (name, state, related list)
    for fid, doc in docs.items():
        if not doc.startswith('--- !u!114') or 'shaderColorParam' not in doc:
            continue
        go = re.search(r'm_GameObject: \{fileID: (-?\d+)\}', doc)
        name = '?'
        if go and go.group(1) in docs:
            nm = re.search(r'm_Name: (.*)', docs[go.group(1)])
            name = nm.group(1).strip() if nm else '?'
        state = int(re.search(r'bodyState: (\d+)', doc).group(1))
        rel_block = re.search(r'relatedBodyParts:\n((?:  - \{fileID: -?\d+\}\n)+)', doc)
        related = re.findall(r'fileID: (-?\d+)', rel_block.group(1)) if rel_block else []
        parts[fid] = (name, state, related)

    seen = {}
    for fid, (name, state, related) in parts.items():
        if state == 0:
            report.error('PLAYER001', rel, 'body part %s has no body state' % name)
        elif state in seen:
            report.error('PLAYER001', rel, 'body state %d is used by both %s and %s (limp / crawl animation needs unique states)' % (state, seen[state], name))
        seen[state] = name
        if fid not in related:
            report.warn('PLAYER001', rel, 'body part %s is not part of its own related list' % name)
        for r in related:
            if r not in parts:
                report.error('PLAYER001', rel, 'body part %s references an unknown related part (%s)' % (name, r))

    missing = [s for s in range(1, 12) if s not in seen]
    if parts and missing:
        report.error('PLAYER001', rel, 'missing body states: %s' % missing)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--root', default=os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
    parser.add_argument('--strict', action='store_true', help='treat warnings as errors')
    args = parser.parse_args()
    root = os.path.abspath(args.root)

    report = Report()
    external = load_known_guids(os.path.join(root, 'tools', 'known_external_guids.txt'))

    guid_to_path = check_meta(root, report)
    check_yaml_assets(root, guid_to_path, external, report)
    check_build_settings(root, guid_to_path, report)
    check_git_junk(root, report)
    check_asmdefs(root, external, report)
    check_runtime_code(root, report)
    check_player_prefab(root, report)

    for code, path, message in report.errors:
        print('ERROR   %-9s %s: %s' % (code, path, message))
    for code, path, message in report.warnings:
        print('WARNING %-9s %s: %s' % (code, path, message))

    print('\n%d assets checked, %d error(s), %d warning(s)' % (len(guid_to_path), len(report.errors), len(report.warnings)))
    failed = bool(report.errors) or (args.strict and bool(report.warnings))
    return 1 if failed else 0


if __name__ == '__main__':
    sys.exit(main())
