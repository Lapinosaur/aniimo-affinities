"""Generate offline UI catalogs with Argos/OPUS models; never used by the app.

Run in an isolated tooling environment with argostranslate installed. Models are
not shipped. Preserve credits in docs/localization.md and review generated copy.
"""
import gc
import json
import os
import re
import time
import urllib.request
import zipfile
from pathlib import Path
import ctranslate2
from argostranslate.package import Package, AvailablePackage
from catalog import ROOT, inventory

CACHE = Path(os.environ['TEMP']) / 'affinisaur-language-models'
CACHE.mkdir(exist_ok=True)
PROTECTED = re.compile(r'\[key:[^\]]*\]|\[types:[^\]]*\]|https?://\S+|\b(?:Affinisaur|Aniimo|Lapinosaur|GitHub|Discord|Twitch|YouTube|Ctrl|Alt|Shift|Esc|Escape|Delete|Del|Space|Tab|Enter|Home|End|F\d{1,2})\b|#[a-z]+|\{\d+\}|\d+(?:[.,]\d+)*(?:%|x)?')

def load_model(info):
    code = info['to_code']
    folder = CACHE / code
    if not folder.exists():
        archive = CACHE / (code + '.zip')
        if not archive.exists():
            import shutil
            shutil.copyfile(AvailablePackage(info).download(), archive)
        folder.mkdir()
        with zipfile.ZipFile(archive) as bundle:
            for entry in bundle.infolist():
                destination = (folder / entry.filename).resolve()
                if not destination.is_relative_to(folder.resolve()):
                    raise ValueError('Unsafe model archive path')
            bundle.extractall(folder)
    metadata = next(folder.rglob('metadata.json'))
    package = Package(metadata.parent)
    translator = ctranslate2.Translator(str(metadata.parent / 'model'), device='cpu', compute_type='int8', intra_threads=4, inter_threads=1)
    return package, translator

def translate_strings(strings, package, engine):
    # Keep every protected token and line break outside the neural translator.
    # Translation fragments retain the source's surrounding whitespace.
    pieces = {}
    for source in strings:
        for fragment in PROTECTED.split(source):
            for line in re.split(r'(\r?\n|(?<=[.!?])(?=\s+[A-Z]))', fragment):
                text = line.strip()
                if re.search('[A-Za-z]', text): pieces[text] = None
    phrases = list(pieces)
    for offset in range(0, len(phrases), 64):
        batch = phrases[offset:offset+64]
        tokens = [package.tokenizer.encode(text) for text in batch]
        options = {'target_prefix': [[package.target_prefix]] * len(tokens)} if package.target_prefix else {}
        translated = engine.translate_batch(tokens, beam_size=2, max_batch_size=1024, batch_type='tokens', replace_unknowns=True, max_decoding_length=512, **options)
        for source, result in zip(batch, translated):
            value = package.tokenizer.decode(result.hypotheses[0]).strip()
            if package.target_prefix: value = value.removeprefix(package.target_prefix).strip()
            pieces[source] = value or source
    def replace_fragment(fragment):
        return ''.join(re.sub(r'\S(?:.*\S)?', lambda m: pieces.get(m.group(), m.group()), line) for line in re.split(r'(\r?\n|(?<=[.!?])(?=\s+[A-Z]))', fragment))
    output = {}
    for source in strings:
        start = 0
        translated = []
        for match in PROTECTED.finditer(source):
            translated.append(replace_fragment(source[start:match.start()]))
            translated.append(match.group())
            start = match.end()
        translated.append(replace_fragment(source[start:]))
        output[source] = ''.join(translated)
    return output

if __name__ == '__main__':
    sources = inventory()
    for override in (ROOT / 'tools/localization').glob('overrides.*.json'):
        for key in json.loads(override.read_text(encoding='utf-8')): sources[key] = []
    for element in 'Dark Lightning Fire Grass Light Ice Earth Water Wind Top Bottom Left Right'.split(): sources[element] = []
    for name in re.findall(r'new Choice\("[a-z]+", "[^"]+", "([^"]+)"\)', (ROOT / 'src/Data/Language.cs').read_text(encoding='utf-8')): sources[name] = []
    source_path = ROOT / 'data/locale-en.txt'
    source_path.write_text(json.dumps({s:s for s in sorted(sources)}, ensure_ascii=False, indent=2), encoding='utf-8')
    index = json.load(urllib.request.urlopen('https://raw.githubusercontent.com/argosopentech/argospm-index/main/index.json'))
    codes = re.findall(r'new Choice\("([a-z]+)"', (ROOT / 'src/Data/Language.cs').read_text(encoding='utf-8'))
    requested = os.environ.get('AFFINISAUR_TRANSLATE_LANGUAGES', '').split(',')
    for code in codes:
        if code == 'en' or requested != [''] and code not in requested: continue
        destination = ROOT / ('data/locale-' + code + '.txt')
        catalog = json.loads(destination.read_text(encoding='utf-8')) if destination.exists() else {}
        missing = [key for key in sources if key not in catalog or os.environ.get('AFFINISAUR_RETRANSLATE') == '1']
        if not missing:
            override = ROOT / ('tools/localization/overrides.' + code + '.json')
            if override.exists():
                catalog.update(json.loads(override.read_text(encoding='utf-8')))
                destination.write_text(json.dumps(catalog, ensure_ascii=False, indent=2, sort_keys=True), encoding='utf-8')
            continue
        started = time.time()
        print(f'{code}: preparing {len(missing)} strings', flush=True)
        info = next(p for p in index if p.get('from_code') == 'en' and p.get('to_code') == code)
        package, engine = load_model(info)
        catalog.update(translate_strings(missing, package, engine))
        override = ROOT / ('tools/localization/overrides.' + code + '.json')
        if override.exists(): catalog.update(json.loads(override.read_text(encoding='utf-8')))
        destination.write_text(json.dumps(catalog, ensure_ascii=False, indent=2, sort_keys=True), encoding='utf-8')
        print(f'{code}: saved in {time.time()-started:.1f}s', flush=True)
        del package, engine
        gc.collect()
