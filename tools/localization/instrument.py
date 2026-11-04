"""One-time reviewed migration of display literals; reruns are idempotent."""
import re
from catalog import ROOT, TOKENS, decode, chosen

ELEMENTS = set('Dark Light Lightning Fire Grass Ice Earth Water Wind'.split())
THEME_FILES = {'OnboardingWindow.cs', 'SettingsDialog.cs', 'DisplayDialog.cs'}

for folder in ['UI', 'App', 'Domain', 'Infrastructure']:
    for path in (ROOT / 'src' / folder).rglob('*.cs'):
        source = path.read_text(encoding='utf-8-sig')
        edits = []
        for match in TOKENS.finditer(source):
            token = match.group()
            if not token.startswith(('"', '@"')): continue
            value = decode(token)
            if not chosen(value): continue
            prefix = source[max(0, match.start()-80):match.start()]
            line = source[source.rfind('\n', 0, match.start())+1:source.find('\n', match.end())]
            if re.search(r'Language\.(T|Original)\(\s*$', prefix): continue
            if 'SuppressMessage' in line or re.search(r'case\s*$', prefix): continue
            if path.name == 'KeyBindings.cs' and 'Actions =' in line: continue
            if value in ELEMENTS and path.name not in THEME_FILES: continue
            if path.name == 'OnboardingWindow.cs' and match.start() < source.index('private readonly Label heading'): continue
            edits.append((match.start(), match.end(), 'Language.T(' + token + ')'))
        for start, end, replacement in reversed(edits):
            source = source[:start] + replacement + source[end:]
        if edits:
            source = source.replace('const string guidance = Language.T(', 'string guidance = Language.T(')
            path.write_text(source, encoding='utf-8')
            print(path.relative_to(ROOT), len(edits))
