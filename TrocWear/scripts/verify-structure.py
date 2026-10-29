#!/usr/bin/env python3
"""Contrôles statiques des fichiers du dépôt ; ce n'est PAS une compilation."""
from pathlib import Path
from xml.etree import ElementTree as ET
import json,re,sys
root=Path(__file__).resolve().parents[1]
projects=list((root/'backend').rglob('*.csproj'))
errors=[]
for project in projects:
    try:
        element=ET.parse(project).getroot()
    except Exception as exc:
        errors.append(f'XML invalid: {project}: {exc}');continue
    for ref in element.findall('.//ProjectReference'):
        target=(project.parent/ref.attrib['Include']).resolve()
        if not target.is_file(): errors.append(f'Missing reference: {project} => {target}')
for file in [root/'frontend/trocwear-web/package.json',root/'frontend/trocwear-web/angular.json',
             root/'frontend/trocwear-web/tsconfig.json',root/'frontend/trocwear-web/tsconfig.app.json',
             root/'frontend/trocwear-web/proxy.conf.json',root/'frontend/trocwear-web/proxy.docker.conf.json']:
    try: json.loads(file.read_text())
    except Exception as exc:errors.append(f'Invalid JSON {file}: {exc}')
solution=(root/'backend/TrocWear.sln').read_text()
project_paths=re.findall(r'^Project\(.*?\) = ".*?", "(.*?)",',solution,re.M)
for p in project_paths:
    path=root/'backend'/p.replace(chr(92),'/')
    if not path.is_file():errors.append(f'Missing SLN project {path}')
try:
    import yaml
    yaml.safe_load((root/'docker-compose.yml').read_text())
    yaml.safe_load((root/'docker-compose.dev.yml').read_text())
except ImportError:
    print('PyYAML missing, Compose YAML syntax not checked')
except Exception as exc: errors.append(f'YAML: {exc}')
# Namespace resolution inside this solution (third-party namespaces are ignored).
cs_files = list((root / 'backend').rglob('*.cs'))
known = set()
for f in cs_files:
    known.update(re.findall(r'^namespace ([\w.]+);', f.read_text(), re.M))
for f in cs_files:
    for ns in re.findall(r'^using ([\w.]+);', f.read_text(), re.M):
        if ns.startswith(('AuthService.', 'AnnouncementService.', 'NegotiationService.',
                          'DeliveryService.', 'RecommendationService.', 'TrocWear.')) and ns not in known:
            errors.append(f'Unknown internal namespace: {f}: {ns}')
components = list((root / 'frontend/trocwear-web/src').rglob('*.component.ts'))
for f in components:
    code = f.read_text()
    if re.search(r'\btemplate\s*:', code): errors.append(f'Inline template: {f}')
    for key in ('templateUrl', 'styleUrl'):
        match = re.search(key + r"\s*:\s*['\"]([^'\"]+)['\"]", code)
        if not match or not (f.parent / match.group(1)).is_file():
            errors.append(f'Missing {key}: {f}')
    if not f.with_suffix('.scss').exists(): errors.append(f'Missing SCSS: {f}')
angular = json.loads((root / 'frontend/trocwear-web/angular.json').read_text())
for entry in angular['projects']['trocwear-web']['architect']['build']['options']['styles']:
    if not (root / 'frontend/trocwear-web' / entry).is_file(): errors.append(f'Missing global style: {entry}')
print(f'{len(components)} components with external HTML/SCSS checked')

print(f'{len(projects)} .csproj, {len(project_paths)} SLN project paths, {len(errors)} static errors')
if errors:
    print('\n'.join(errors));sys.exit(1)
