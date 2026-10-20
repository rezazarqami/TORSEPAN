import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('impact', Path(__file__).resolve().parents[1] / 'ops/deployment/impact.py')
impact = importlib.util.module_from_spec(spec)
spec.loader.exec_module(impact)


class DeploymentImpactTests(unittest.TestCase):
    def targets(self, paths):
        return {key for key, value in impact.classify(paths).items() if value}

    def test_qc_ui_does_not_redeploy_api(self):
        self.assertEqual(self.targets(['TORSEPAN.Panel/Components/Pages/Dimpling.razor.css']), {'panel', 'android'})

    def test_shared_code_affects_all_consumers(self):
        self.assertEqual(self.targets(['TORSEPAN.Domain/Entities/Bowl.cs']), {'panel', 'api', 'android'})

    def test_api_only(self):
        self.assertEqual(self.targets(['TORSEPAN.Infrastructure/Persistence/Migrations/example.cs']), {'api'})

    def test_android_only(self):
        self.assertEqual(self.targets(['TORSEPAN.Panel.Offline/Program.cs']), {'android'})

    def test_unknown_inputs_require_review(self):
        self.assertEqual(self.targets(['Directory.Build.props', 'new-build-input.json']), {'panel', 'api', 'android'})

    def test_docs_do_not_release_products(self):
        self.assertEqual(self.targets(['docs/release.md', 'AGENTS.md']), set())


if __name__ == '__main__':
    unittest.main()
