"""Audit the exact Git index without printing any potential secret values. Stdlib only."""
from pathlib import Path
import hashlib,json,re,subprocess,sys
from urllib.parse import unquote

ROOT=Path(__file__).resolve().parents[1]
AUDIO_GUID='6fb3514024f896645bd52a2bbbaa5956'
NOREPLY='156163069+manthan3502@users.noreply.github.com'
REMOTE='https://github.com/manthan3502/BobaBrawlers.git'
LEGAL_EMAIL='juan@remolacha.biz'

def git(*args):
    return subprocess.run(['git','-C',str(ROOT),*args],check=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE).stdout

def main():
    names=[n.decode('utf-8') for n in git('ls-files','-z').split(b'\0') if n]
    if not names: raise SystemExit('No staged files; stage the intended source/docs first.')
    blobs={name:git('show',':'+name) for name in names}
    failures=[]; notices=[]
    def fail(category,path,line=None):
        row={'category':category,'file':path}
        if line: row['line']=line
        failures.append(row)
    # Do not include values in findings: a report must not become another leak.
    email=re.compile(r'[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}')
    patterns={
        'machine-specific path':re.compile(r'(?<![A-Za-z])[A-Za-z]:[\\/]|/(?:Users|home)/[^/\s]+'),
        'credential token':re.compile(r'(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,}|AKIA[0-9A-Z]{16}|sk-(?:proj-)?[A-Za-z0-9_-]{24,}|xox[baprs]-[A-Za-z0-9-]{20,}|-----BEGIN (?:RSA |OPENSSH |EC )?PRIVATE KEY-----)'),
        'assigned secret':re.compile(r'(?i)(?:password|api[_-]?key|access[_-]?token|client[_-]?secret)\s*[:=]\s*[\x22\x27][A-Za-z0-9_+/=-]{12,}[\x22\x27]'),
        'student identifier':re.compile(r'(?i)\b(?:PRN|student[_ ]?(?:id|number))\s*[:=]\s*[\x22\x27]?[A-Za-z0-9]{7,}')
    }
    largest=[]; total=0; legal_contacts=[]
    for name,data in blobs.items():
        p=Path(name); total+=len(data); largest.append((len(data),name))
        if name.split('/')[0].lower() in {'library','temp','logs','obj','usersettings','backups','builds','qa','.vscode','.design'}: fail('excluded local tree tracked',name)
        if p.suffix.lower() in {'.exe','.dll','.zip','.mp4','.mov','.pdb','.pdf','.docx','.pptx','.pfx','.key','.pem','.pyc'}: fail('unwanted binary/private document',name)
        if name.startswith('Assets/Audio/'): fail('uncleared audio tree tracked',name)
        if len(data)>10*1024*1024: fail('file exceeds 10 MiB repository budget',name)
        binary = '\0' in data[:4096].decode('latin1')
        if not binary:
            try: texts=[data.decode('utf-8-sig')]
            except UnicodeError: texts=[data.decode('latin1')]
        else:
            texts=[data.decode('latin1'),data.decode('utf-16-le',errors='ignore')]
        for text in texts:
            for number,line in enumerate(text.splitlines(),1):
                for category,pattern in patterns.items():
                    if binary and category=='machine-specific path':
                        pattern=re.compile(r'(?i)[A-Z]:[\\/]+(?:Users|Documents|GD_Assignments)[\\/]|/(?:Users|home)/[A-Za-z0-9_.-]+/')
                    if pattern.search(line): fail(category,name,number)
                for match in (email.finditer(line) if '@' in line else []):
                    value=match.group().strip("'")
                    if value==NOREPLY: continue
                    if value==LEGAL_EMAIL and name in {'Assets/BobaBrawlers/Resources/Fonts/LilitaOne-OFL.txt','Assets/BobaBrawlers/Resources/Fonts/LilitaOne.ttf','Tools/repository_audit.py'}:
                        legal_contacts.append(name); continue
                    fail('unapproved email',name,number)
        if p.suffix.lower()=='.webp' and (b'EXIF' in data or b'XMP ' in data): fail('image metadata requires review',name)
    # Every committed Unity asset/folder has its sidecar; no orphan sidecars.
    available=set(names)
    for name in names:
        if not name.startswith('Assets/'): continue
        if name.endswith('.meta'):
            target=name[:-5]
            if target not in available and not any(n.startswith(target+'/') for n in names): fail('orphan Unity metadata',name)
        elif name+'.meta' not in available: fail('missing Unity metadata',name)
        parts=Path(name).parts
        for i in range(2,len(parts)):
            folder='/'.join(parts[:i])
            if folder+'.meta' not in available: fail('missing Unity folder metadata',folder)
    # Resolve project GUIDs and restored-package GUIDs, with one explicit rights omission.
    guid_re=re.compile(rb'guid:\s*([0-9a-f]{32})')
    own={}
    for name,data in blobs.items():
        if name.endswith('.meta'):
            found=guid_re.search(data)
            if found:
                guid=found[1].decode()
                if guid in own: fail('duplicate Unity GUID',name)
                own[guid]=name
    package_guids=set(); cache=ROOT/'Library/PackageCache'
    if cache.exists():
        for meta in cache.rglob('*.meta'):
            found=guid_re.search(meta.read_bytes()[:300])
            if found: package_guids.add(found[1].decode())
    # Existing Unity 6 template assets contain obsolete debug/test references also
    # present in the pinned URP package. Report these separately; never edit game assets.
    reviewed_legacy={
        'Assets/Settings/PC_Renderer.asset':set('e5c6678ed2aaa91408dd3df699057aae 03cfc4915c15d504a9ed85ecc404e607 53a11f4ebaebf4049b3638ef78dc9664 8f96cd657dc40064aa21efcc7e50a2e7 57d7c4c16e2765b47a4d2069b311bffe 24ec0e140fb444a44ab96ee80844e18e b9a23f869c4fd45f19c5ada54dd82176'.split()),
        'Assets/Settings/DefaultVolumeProfile.asset':set('60f3b30c03e6ba64d9a27dc9dba8f28d 0fd9ee276a1023e439cf7a9c393195fa 5a00a63fdd6bd2a45ab1f2d869305ffd 74955a4b0b4243bc87231e8b59ed9140'.split())
    }
    upstream_legacy=set()
    for package in cache.glob('com.unity.render-pipelines.universal@*'):
        for relative in ['Editor/Volume/DefaultVolumeProfile.asset','Samples~/URPPackageSamples/SharedAssets/Settings/SamplesForwardRenderer.asset']:
            template=package/relative
            if template.exists(): upstream_legacy.update(m[1].decode() for m in guid_re.finditer(template.read_bytes()))
        deprecated=package/'Runtime/Deprecated.cs'
        if deprecated.exists() and 'public ComputeShader probeVolumeBlendStatesCS;' in deprecated.read_text():
            # This serialized field was moved to ProbeVolumeRuntimeResources using
            # ResourcePath. Its old GUID has no current asset; keep that explicit.
            upstream_legacy.add('b9a23f869c4fd45f19c5ada54dd82176')
    unresolved={}; rights=[]; legacy=[]
    for name,data in blobs.items():
        if not name.startswith(('Assets/','ProjectSettings/')) or name.endswith('.meta'): continue
        for found in guid_re.finditer(data):
            guid=found[1].decode()
            if guid==AUDIO_GUID: rights.append(name); continue
            if guid in own or guid in package_guids or guid.startswith('0000000000000000'): continue
            if guid in reviewed_legacy.get(name,set()) and guid in upstream_legacy:
                classification = 'Deprecated probe-volume blend field; old GUID unresolved, current package uses ResourcePath; retained unchanged' if guid=='b9a23f869c4fd45f19c5ada54dd82176' else 'Existing legacy URP template debug/test reference, also in pinned upstream template; retained unchanged'
                legacy.append({'file':name,'guid':guid,'classification':classification}); continue
            unresolved.setdefault(guid,[]).append(name)
    for guid,paths in unresolved.items():
        for name in set(paths): fail('unresolved Unity/package GUID',name)
    # Local Markdown destinations must be tracked and stay inside the project.
    links=0
    for name,data in blobs.items():
        if not name.endswith('.md'): continue
        text=data.decode('utf-8-sig')
        destinations=re.findall(r'\]\(([^)]+)\)',text)+re.findall(r'<(?:img|a)\b[^>]+(?:src|href)=[\x22\x27]([^\x22\x27]+)',text)
        for target in destinations:
            target=target.strip().strip('<>').split(' "')[0]
            if target.startswith(('https://','http://','mailto:','#')): continue
            resolved=(ROOT/name).parent.joinpath(unquote(target.split('#')[0]))
            try: relative=resolved.resolve().relative_to(ROOT.resolve()).as_posix()
            except ValueError: fail('Markdown escapes repository',name); continue
            links+=1
            if relative not in available: fail('Markdown target missing from index',name)
    manifest=json.loads(blobs['Packages/manifest.json']); lock=json.loads(blobs['Packages/packages-lock.json'])['dependencies']
    for name,version in manifest['dependencies'].items():
        if name not in lock or lock[name]['version']!=version: fail('dependency direct-version mismatch','Packages/manifest.json')
    for name,entry in lock.items():
        if entry.get('source') not in {'registry','builtin'}: fail('nonstandard dependency source','Packages/packages-lock.json')
        if entry.get('url','https://packages.unity.com')!='https://packages.unity.com': fail('unexpected package registry','Packages/packages-lock.json')
        for child in entry.get('dependencies',{}):
            if child not in lock: fail('dependency absent from lockfile','Packages/packages-lock.json')
    for name in ['.gitignore','.gitattributes','README.md','THIRD_PARTY_NOTICES.md','Assets/BobaBrawlers/Scenes/BobaArena.unity','Assets/BobaBrawlers/Prefabs/PlayerPearl.prefab','ProjectSettings/ProjectVersion.txt']:
        if name not in blobs: fail('required source file absent',name)
    if git('remote','get-url','origin').decode().strip()!=REMOTE: fail('wrong origin','.git/config')
    if git('config','--local','--get','user.email').decode().strip()!=NOREPLY: fail('wrong repository-local identity','.git/config')
    history=[]
    try:
        rows=git('log','--all','--format=%H%x09%ae%x09%ce').decode().splitlines()
        for row in rows:
            commit,author,committer=row.split('\t')
            history.append(commit)
            if author!=NOREPLY or committer!=NOREPLY: fail('private/unapproved commit identity','git history')
        for commit in history:
            historical=set(git('ls-tree','-r','--name-only',commit).decode().splitlines())
            if historical != available: notices.append('Historical file set differs from current index; inspect separately before publication.')
    except subprocess.CalledProcessError: pass
    baseline=ROOT/'QA/RepositoryPreparation/game-file-baseline.json'
    preserved=0
    if baseline.exists():
        for name,digest in json.loads(baseline.read_text()).items():
            p=ROOT/name
            if not p.is_file() or hashlib.sha256(p.read_bytes()).hexdigest()!=digest: fail('original game/build/media file changed',name)
            else: preserved+=1
    result={'result':'PASS' if not failures else 'FAIL','indexed_files':len(names),'indexed_bytes':total,'largest_files':[{'bytes':size,'path':name} for size,name in sorted(largest,reverse=True)[:10]],'local_markdown_links_checked':links,'unity_asset_guids':len(own),'restored_package_guids':len(package_guids),'rights_exclusion_reference_files':sorted(set(rights)),'reviewed_upstream_legacy_guid_references':legacy,'dependency_direct_count':len(manifest['dependencies']),'dependency_locked_count':len(lock),'history_commits_checked':len(history),'original_game_build_media_files_unchanged':preserved,'intentional_public_legal_contact_files':sorted(set(legal_contacts)),'failures':failures,'notices':notices,'limits':['Pattern-based audit is not a proof that no conceivable secret exists.','The optional supplied audio, current Windows candidate and voiced video remain blocked for public distribution.','Required upstream copyright contact is retained unchanged.','Unchanged upstream legacy GUID fields are reported separately, not represented as resolved game assets.']}
    output=ROOT/'QA/RepositoryPreparation/repository-audit.json'; output.parent.mkdir(parents=True,exist_ok=True); output.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result,indent=2)); return bool(failures)

if __name__=='__main__': sys.exit(main())
