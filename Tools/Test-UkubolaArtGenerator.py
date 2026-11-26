"""Black-box repeatability and manual-edit protection tests, outside Unity Assets."""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import sys
import uuid

project = Path(__file__).resolve().parents[1]
root = project / 'Evidence' / 'Evolution' / ('art-source-' + uuid.uuid4().hex)
(root / 'Tools').mkdir(parents=True)
source = project / 'Tools' / 'Generate-UkubolaArt.py'
script = root / 'Tools' / source.name
shutil.copyfile(source, script)
output = root / 'Assets' / '_UKUBOLA' / 'Art' / 'Evolution'

def run():
    return subprocess.run([sys.executable, str(script)], capture_output=True, text=True)

def hashes():
    return {p.relative_to(output).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in sorted(output.rglob('*')) if p.is_file()}

first = run()
assert first.returncode == 0, first.stderr
baseline = hashes()
second = run()
assert second.returncode == 0, second.stderr
assert hashes() == baseline, 'Two identical generations changed output bytes.'
pngs = sorted(output.rglob('*.png'))
assert len(pngs) == 99, f'Unexpected generated texture coverage: {len(pngs)}'
changed = pngs[0]
changed.write_bytes(changed.read_bytes() + b'\n')
manual_state = hashes()
rejected = run()
assert rejected.returncode != 0 and 'preserving outputs' in (rejected.stdout + rejected.stderr)
assert hashes() == manual_state, 'Rejected generation changed existing output.'
report = {'success': True, 'python': sys.version.split()[0], 'textures': len(pngs),
          'generatorSha256': hashlib.sha256(source.read_bytes()).hexdigest(),
          'checks': ['Two generations are byte-identical', '99 expected textures',
                     'Manual edit rejects regeneration without altering any output'],
          'evidence': str(root)}
(root / 'result.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
