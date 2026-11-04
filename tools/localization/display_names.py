"""Keep model IDs stable; localize only their presentation in UI expressions."""
import re
from catalog import ROOT

paths = list((ROOT / 'src/UI').rglob('*.cs')) + [ROOT / 'src/Domain/TeamThreats.cs']
for path in paths:
    if path.name == 'VectorIcon.cs': continue
    source = path.read_text(encoding='utf-8-sig')
    edits = []
    for match in re.finditer(r'Model\.Names\[', source):
        start = match.start()
        if source[max(0,start-11):start] == 'Language.T(': continue
        line = source[source.rfind('\n',0,start)+1:source.find('\n',start)]
        if 'KeyBindings.Actions' in line: continue
        end, depth = match.end(), 1
        while depth:
            if source[end] == '[': depth += 1
            if source[end] == ']': depth -= 1
            end += 1
        edits.append((start,end,'Language.T('+source[start:end]+')'))
    for start,end,value in reversed(edits): source = source[:start]+value+source[end:]
    if path.name == 'SettingsDialog.cs':
        source = source.replace('AccessibleName = KeyBindings.Actions[i]', 'AccessibleName = Language.T(KeyBindings.Actions[i])')
        source = source.replace(': KeyBindings.Actions[i], Location', ': Language.T(KeyBindings.Actions[i]), Location')
        source = source.replace('+ KeyBindings.Actions[i], Location', '+ Language.T(KeyBindings.Actions[i]), Location')
        source = source.replace('button.Text == name)', 'button.Text == Language.T(name))')
    if edits or path.name == 'SettingsDialog.cs': path.write_text(source,encoding='utf-8')
