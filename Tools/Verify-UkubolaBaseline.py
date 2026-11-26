"""Check the immutable conservation snapshot and original saves without rewriting them."""
from pathlib import Path
import hashlib
import json

root = Path(__file__).resolve().parents[1]
backup = root / 'Backups/Evolution/baseline'
manifest = json.loads((backup / 'manifest.json').read_text(encoding='utf-8'))
errors = []
for item in manifest['files']:
    path = backup / item['path']
    if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != item['sha256']:
        errors.append(item['path'])
production = Path.home() / 'AppData/LocalLow/DefaultCompany/DoomClone/UKUBOLA/Saves'
for item in manifest['productionSaves']:
    for path in [backup / 'ProductionSaves' / item['path'], production / item['path']]:
        if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != item['sha256']:
            errors.append(str(path))
print(json.dumps({'success': not errors, 'snapshotFiles': len(manifest['files']),
                  'productionSaves': len(manifest['productionSaves']), 'errors': errors}, indent=2))
raise SystemExit(bool(errors))
