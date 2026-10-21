import importlib.util
from pathlib import Path
import subprocess
import tempfile
import os
import unittest

spec = importlib.util.spec_from_file_location('impact', Path(__file__).with_name('impact.py'))
impact = importlib.util.module_from_spec(spec)
spec.loader.exec_module(impact)


class ImpactTest(unittest.TestCase):
    def test_dependency_boundaries(self):
        for path, expected in [
            ('TORSEPAN.Panel/wwwroot/images/workspace/account-office.webp', {'panel'}),
            ('TORSEPAN.API/Controllers/BowlsController.cs', {'api'}),
            ('TORSEPAN.Infrastructure/Persistence/AppDbContext.cs', {'api'}),
            ('TORSEPAN.Application/DTOs/OrderDto.cs', {'panel', 'api'}),
            ('TORSEPAN.Domain/Entities/Bowl.cs', {'panel', 'api'}),
            ('Dockerfile', {'panel', 'api'}), ('Dockerfile.api', {'api'}),
            ('Dockerfile.panel.dockerignore', {'panel'}),
            ('Directory.Build.props', {'panel', 'api'}),
            ('tests/profile-layout.cjs', set()), ('ops/release-notes/readme.md', set()),
        ]:
            with self.subTest(path=path):
                self.assertEqual(expected, impact.affected_products(path))

    def test_separate_baselines_and_deleted_inputs(self):
        with tempfile.TemporaryDirectory() as directory:
            def git(*args):
                return subprocess.check_output(['git', '-C', directory, *args], stderr=subprocess.DEVNULL).decode().strip()
            git('init'); git('config', 'user.email', 'fixture@example.invalid'); git('config', 'user.name', 'Fixture')
            root = Path(directory)
            shared = root / 'TORSEPAN.Application' / 'Shared.cs'
            shared.parent.mkdir(); shared.write_text('baseline')
            git('add', '.'); git('commit', '-m', 'baseline'); base = git('rev-parse', 'HEAD')
            shared.write_text('api change not deployed yet')
            git('commit', '-am', 'shared'); panel_base = git('rev-parse', 'HEAD')
            panel = root / 'TORSEPAN.Panel' / 'page.razor'; panel.parent.mkdir(); panel.write_text('header')
            git('add', '.'); git('commit', '-m', 'panel'); head = git('rev-parse', 'HEAD')
            previous_cwd = os.getcwd()
            try:
                os.chdir(directory)
                planned = impact.plan(panel_base, base, head)
                self.assertTrue(planned['products']['panel']['deploy'])
                self.assertTrue(planned['products']['api']['deploy'])
                self.assertFalse(impact.plan(panel_base, panel_base, head)['products']['api']['deploy'])
            finally:
                os.chdir(previous_cwd)
            # API must catch up the earlier shared commit even though the newest commit is UI-only.
            self.assertTrue(any('api' in impact.affected_products(p) for p in git('diff', '--name-only', base, head).splitlines()))
            self.assertFalse(any('api' in impact.affected_products(p) for p in git('diff', '--name-only', panel_base, head).splitlines()))
            shared.unlink(); git('commit', '-am', 'delete shared')
            self.assertIn('TORSEPAN.Application/Shared.cs', git('diff', '--name-only', '--no-renames', head, 'HEAD'))


if __name__ == '__main__':
    unittest.main()
