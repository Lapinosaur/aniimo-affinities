"""Inventory C# UI literals for a reviewed, offline translation catalog."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TOKENS = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"')
SINGLE = set('Language Welcome Back Continue Optional English Dark System Light Settings Display Types Attack Defense Main Sub Damage Resistance Weakness Strong Weak Neutral All None Opponent Ally Allies Opponents Team Teams Matchups Coverage Counter Suggested Suggestions Apply Cancel Close Reset Download Later Skip Help Support Tutorials Replay Play Completed Paused Resume Restart Rebind Copied Minimize Pin Lock Unlock About Version Installed Latest Ready Small Default Large Shown Hidden Keyboard Shortcuts Loading Saving Saved Search Theme Overview Controls Compare Combined Separate Export Import Clear Next Previous Status Report Advantages Disadvantages'.split())
SINGLE.update('ACTION CREDITS SHORTCUT Appearance Before Checking Cleaning Cleared Copy Defaults Demo Disadvantage Disadvantaged Done Downloading Element Finish Loading Matchup Measuring Member Members Opening Overlay Press Pressured Received Dealt Resist Restarting Revert Row Save Secondary Shortcut Slot Suggest Threat Troubleshooting Tutorial Unassigned Unofficial Updates against and applied attack defense disadvantaged element elements groups name of percent pressured seconds threats vulnerable Incoming Release adds'.split())
EXCLUDE = {'Segoe UI', 'Segoe UI Symbol', 'a a', 'GET ', 'POST ', 'User-Agent', 'AniimoAffinities', 'Affinisaur', 'Lapinosaur'}

def decode(token):
    if token.startswith('@'):
        return token[2:-1].replace('""', '"')
    try:
        return json.loads(token)
    except ValueError:
        return None

def chosen(value):
    if value is None or value in EXCLUDE or not re.search('[A-Za-z]', value):
        return False
    if value.startswith(('http', 'https', '^', 'System.', 'AniimoAffinities.')) or re.search(r'\\[bdsw]|\.(dll|exe|txt|png|svg|ico|md|json)\b', value):
        return False
    plain = re.sub(r'^[^A-Za-z]+|[^A-Za-z]+$', '', value)
    return plain in SINGLE or bool(re.search(r'[A-Za-z][ ,:;.!?·—–&/%0-9]+[A-Za-z]', value))

def inventory():
    result = {}
    for folder in ['UI', 'App', 'Domain', 'Infrastructure']:
        for path in (ROOT / 'src' / folder).rglob('*.cs'):
            source = path.read_text(encoding='utf-8-sig')
            for match in TOKENS.finditer(source):
                token = match.group()
                if not token.startswith(('"', '@"')):
                    continue
                value = decode(token)
                if chosen(value):
                    result.setdefault(value, []).append({'file': path.relative_to(ROOT).as_posix(), 'line': source.count('\n', 0, match.start()) + 1})
    return result

if __name__ == '__main__':
    result = inventory()
    out = ROOT / 'dist' / 'localization-inventory.json'
    out.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f'{len(result)} candidate strings / {sum(len(k) for k in result)} characters: {out}')
    remaining = set()
    for path in (ROOT / 'src/UI').rglob('*.cs'):
        for match in TOKENS.finditer(path.read_text(encoding='utf-8-sig')):
            if match.group().startswith(('"', '@"')):
                value = decode(match.group())
                if value and not chosen(value):
                    remaining.add(value)
    (ROOT / 'dist/localization-remaining.json').write_text(json.dumps(sorted(remaining), ensure_ascii=False, indent=2), encoding='utf-8')
