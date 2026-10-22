import unittest
from discord_release import payload, notification_role, existing_message, previous_stable, ROLE_IDS

class Announcements(unittest.TestCase):
    def release(self, tag='v0.5.0', **extra):
        return dict(tag_name=tag, html_url='https://github.com/Lapinosaur/aniimo-affinities/releases/tag/'+tag,
                    body='Rename @everyone <@&123> to Affinisaur', **extra)

    def test_only_matching_role_can_notify(self):
        for tag, level in [('v1.0.0','major'), ('v0.5.0','minor'), ('v0.5.1','patch')]:
            result=payload(self.release(tag), 'Lapinosaur/aniimo-affinities', previous=(0,4,3) if level != 'patch' else (0,5,0))
            self.assertEqual(result['content'], '<@&'+ROLE_IDS[level]+'>')
            self.assertEqual(result['allowed_mentions'], {'parse':[], 'roles':[ROLE_IDS[level]]})
            self.assertNotIn('@everyone', result['embeds'][0]['description'])

    def test_skipped_versions_compare_with_previous_stable(self):
        self.assertEqual(notification_role(self.release('v0.6.1'),previous=(0,5,0)),ROLE_IDS['minor'])
        self.assertEqual(notification_role(self.release('v2.3.4'),previous=(1,9,8)),ROLE_IDS['major'])
        releases=[self.release('v0.4.3'),self.release('v0.5.0'),self.release('v0.6.0',prerelease=True),self.release('v0.6.1',draft=True)]
        self.assertEqual(previous_stable(releases,(0,6,2)),(0,5,0))

    def test_edits_and_prereleases_never_ping(self):
        for release, editing in [(self.release(),True), (self.release(prerelease=True),False),
                                 (self.release('v0.5.0-rc.1'),False), (self.release('arbitrary'),False)]:
            self.assertIsNone(notification_role(release, editing))
            self.assertEqual(payload(release,'Lapinosaur/aniimo-affinities',editing)['allowed_mentions']['roles'],[])

    def test_saved_message_reused_and_marker_hidden(self):
        release=self.release(); release['body']+='\n<!-- discord-release-message: 987 -->'
        self.assertEqual(existing_message(release),'987')
        self.assertEqual(existing_message(release,'987'),'987')
        with self.assertRaises(ValueError): existing_message(release,'123')
        with self.assertRaises(ValueError): existing_message(self.release(),'not-an-id')
        self.assertNotIn('discord-release-message',payload(release,'Lapinosaur/aniimo-affinities',True)['embeds'][0]['description'])

    def test_ambiguous_delivery_requires_recovery_without_repost(self):
        release=self.release(); release['body']+='\n<!-- discord-release-pending: 123 -->'
        with self.assertRaises(ValueError): existing_message(release)
        self.assertEqual(existing_message(release,'987'),'987')
        self.assertNotIn('discord-release-pending',payload(release,'Lapinosaur/aniimo-affinities',True)['embeds'][0]['description'])

    def test_foreign_and_draft_releases_rejected(self):
        with self.assertRaises(ValueError): payload(self.release(draft=True),'Lapinosaur/aniimo-affinities')
        with self.assertRaises(ValueError): payload(self.release(),'Another/repo')

if __name__ == '__main__': unittest.main()
