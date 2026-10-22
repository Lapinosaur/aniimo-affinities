import json, os, re, sys
from urllib.request import Request, urlopen
from urllib.parse import quote, urlsplit
from urllib.error import HTTPError, URLError

def clean(value, limit):
    text = str(value or '').replace('@', '@\u200b')
    return text if len(text) <= limit else text[:limit - 1] + '\u2026'

ROLE_IDS = {'major': '1562796495986626620', 'minor': '1562796606556864692', 'patch': '1562796669307846716'}
MARKER = re.compile(r'<!-- discord-release-message: ([0-9]+) -->')
PENDING = re.compile(r'<!-- discord-release-pending: [0-9]+ -->')

def stable_version(tag):
    match = re.fullmatch(r'v?(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)', tag)
    return tuple(map(int, match.groups())) if match else None

def notification_role(release, editing=False, previous=None):
    version = stable_version(release['tag_name'])
    if editing or release.get('prerelease') or not version:
        return None
    baseline = previous or (0, 0, 0)
    if version <= baseline:
        return None
    level = 'major' if version[0] != baseline[0] else ('minor' if version[1] != baseline[1] else 'patch')
    return ROLE_IDS[level]

def previous_stable(releases, current):
    versions = [stable_version(item.get('tag_name', '')) for item in releases
                if not item.get('draft') and not item.get('prerelease')]
    return max((v for v in versions if v and v < current), default=(0, 0, 0))

def existing_message(release, requested=''):
    saved = MARKER.search(release.get('body') or '')
    if requested and not re.fullmatch(r'[0-9]+', requested):
        raise ValueError('Invalid message ID')
    if saved and requested and requested != saved.group(1):
        raise ValueError('Message ID conflicts with the release announcement record')
    if PENDING.search(release.get('body') or '') and not saved and not requested:
        raise ValueError('Previous delivery unconfirmed: inspect Discord and supply its message ID; do not repost')
    return requested or (saved.group(1) if saved else '')

def payload(release, repo, editing=False, previous=None):
    base = 'https://github.com/' + repo
    page = release['html_url']
    if release.get('draft') or not page.startswith(base + '/releases/tag/'):
        raise ValueError('Only published releases from this repository may be announced')
    links = []
    for asset in release.get('assets', []):
        url = asset.get('browser_download_url', '')
        if asset.get('state') != 'uploaded' or not url.startswith(base + '/releases/download/'):
            continue
        name = re.sub(r'[\[\]()`*_<>]', '', clean(asset.get('name'), 90))
        size = asset.get('size', 0)
        label = f'{size / 1048576:.1f} MB' if size >= 1048576 else (f'{size / 1024:.0f} KB' if size >= 1024 else f'{size} B')
        line = f'[{name}]({url}) \u00b7 {label}'
        if len('\n'.join(links + [line])) > 900:
            break
        links.append(line)
    downloads = '\n'.join(links) if links else 'No downloadable files attached. Check the release page.'
    role = notification_role(release, editing, previous)
    return {
        'content': '<@&' + role + '>' if role else '',
        'username': 'Lapinosaur Releases',
        'allowed_mentions': {'parse': [], 'roles': [role] if role else []},
        'embeds': [{
            'author': {'name': 'Lapinosaur \u00b7 Tool updates', 'url': base},
            'title': clean(release.get('name') or release['tag_name'], 240),
            'url': page,
            'color': 0x19C5C7,
            'description': clean(PENDING.sub('', MARKER.sub('', release.get('body') or '')).strip() or 'A new update is available.', 1800),
            'fields': [
                {'name': 'Version', 'value': clean(release['tag_name'], 100), 'inline': True},
                {'name': 'Channel', 'value': 'Preview build' if release.get('prerelease') else 'Stable release', 'inline': True},
                {'name': 'Downloads', 'value': downloads + f'\n[All files & release notes]({page})'},
                {'name': 'Community', 'value': 'Ideas & bugs: <#1562109423516917765>\nNeed help: <#1562649870320533554>'}
            ],
            'footer': {'text': clean(repo + ' \u00b7 Official GitHub release', 150)}
        }]
    }

def save_notes(repo, release, notes):
    record = Request('https://api.github.com/repos/' + repo + '/releases/' + str(release['id']),
                     data=json.dumps({'body': notes}).encode('utf-8'), headers={
                         'Authorization': 'Bearer ' + os.environ['GH_TOKEN'],
                         'Accept': 'application/vnd.github+json', 'User-Agent': 'Lapinosaur-Releases',
                         'Content-Type': 'application/json'}, method='PATCH')
    with urlopen(record, timeout=30) as response:
        json.load(response)

def main():
    repo = os.environ['GITHUB_REPOSITORY']
    with open(os.environ['GITHUB_EVENT_PATH'], encoding='utf-8') as stream:
        event = json.load(stream)
    tag = (event.get('release') or {}).get('tag_name') or event.get('inputs', {}).get('tag')
    if not tag:
        raise ValueError('An existing release tag is required')
    # Re-fetch at publication time so attached downloads are current.
    request = Request('https://api.github.com/repos/' + repo + '/releases/tags/' + quote(tag, safe=''), headers={
        'Authorization': 'Bearer ' + os.environ['GH_TOKEN'],
        'Accept': 'application/vnd.github+json', 'User-Agent': 'Lapinosaur-Releases'
    })
    with urlopen(request, timeout=30) as response:
        release = json.load(response)
    if release.get('prerelease'):
        print('Prerelease: no announcement or role notification.')
        return
    current = stable_version(release['tag_name'])
    if not current:
        raise ValueError('A stable semantic version is required')
    releases = []
    page = 1
    while True:
        listing = Request('https://api.github.com/repos/' + repo + '/releases?per_page=100&page=' + str(page),
                          headers={'Authorization': 'Bearer ' + os.environ['GH_TOKEN'],
                                   'Accept': 'application/vnd.github+json', 'User-Agent': 'Lapinosaur-Releases'})
        with urlopen(listing, timeout=30) as response:
            batch = json.load(response)
        releases.extend(batch)
        if len(batch) < 100:
            break
        page += 1
    previous = previous_stable(releases, current)
    message_id = existing_message(release, event.get('inputs', {}).get('message_id', '').strip())
    if int(os.environ.get('GITHUB_RUN_ATTEMPT', '1')) > 1 and not message_id:
        raise ValueError('Unrecorded previous attempt: verify Discord before posting again')
    body = json.dumps(payload(release, repo, bool(message_id), previous)).encode('utf-8')
    hook = os.environ.get('DISCORD_RELEASES_WEBHOOK', '')
    parsed = urlsplit(hook)
    if parsed.scheme != 'https' or parsed.netloc != 'discord.com' or not re.fullmatch(r'/api/webhooks/[0-9]+/[A-Za-z0-9_-]+', parsed.path):
        raise ValueError('Configure the DISCORD_RELEASES_WEBHOOK repository secret')
    if not message_id:
        # Persist intent before sending: ambiguous failures must never create a second post.
        pending = (release.get('body') or '').rstrip() + '\n\n<!-- discord-release-pending: ' + os.environ['GITHUB_RUN_ID'] + ' -->'
        save_notes(repo, release, pending)
    endpoint = hook + ('/messages/' + message_id if message_id else '?wait=true')
    request = Request(endpoint, data=body, headers={
        'Content-Type': 'application/json', 'User-Agent': 'Lapinosaur-Releases'
    }, method='PATCH' if message_id else 'POST')
    # No blind retries: an ambiguous timeout may already have posted the message.
    with urlopen(request, timeout=30) as response:
        message = json.load(response)
    print('Release announcement delivered. Message ID: ' + message['id'])
    print('Mentioned roles: ' + json.dumps(message.get('mention_roles', [])))
    # Persist the message ID on the release; subsequent runs edit instead of posting.
    if not MARKER.search(release.get('body') or ''):
        notes = PENDING.sub('', release.get('body') or '').rstrip() + '\n\n<!-- discord-release-message: ' + message['id'] + ' -->'
        save_notes(repo, release, notes)
    expected = notification_role(release, bool(message_id), previous)
    if expected and message.get('mention_roles', []) != [expected]:
        raise ValueError('Announcement posted, but the expected role mention was not confirmed; do not repost')

if __name__ == '__main__':
    try:
        main()
    except HTTPError as error:
        print('Announcement failed: HTTP ' + str(error.code), file=sys.stderr)
        sys.exit(1)
    except (URLError, TimeoutError, ValueError, KeyError) as error:
        # Never print request objects or URLs containing the webhook credential.
        print('Announcement failed (' + type(error).__name__ + '). Check the secret, release tag and service availability.', file=sys.stderr)
        sys.exit(1)
